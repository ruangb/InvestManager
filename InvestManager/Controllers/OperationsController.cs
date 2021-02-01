using Microsoft.AspNetCore.Mvc;
using InvestManager.Services;
using InvestManager.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace InvestManager.Controllers
{
    public class OperationsController : Controller
    {
        private readonly OperationService _operationService;
        private readonly ParameterService _parameterService;

        public OperationsController(OperationService operationService, ParameterService parameterService)
        {
            _operationService = operationService;
            _parameterService = parameterService;
        }

        public async Task<IActionResult> Index()
        {
            var listOperation = await _operationService.FindAllAsync();
            var listParameter = await _parameterService.FindAllAsync();

            if (listParameter.Any())
            {
                foreach (var item in listOperation)
                {
                    item.InvestValue = item.Quantity * item.Price - ((item.Quantity * item.Price) * (listParameter[0].TradingFee / 100));
                }
            }
            else
            {
                foreach (var item in listOperation)
                {
                    item.InvestValue = item.Quantity * item.Price;
                }
            }

            listOperation.OrderBy(x => x.Date).ThenBy(x => x.Asset).ThenByDescending(x => x.Price);

            return View(listOperation);
        }

        public async Task<IActionResult> Wallet()
        {
            var list          = await _operationService.FindAllAsync();
            var listParameter = await _parameterService.FindAllAsync();

            IList<Operation> listOperation = new List<Operation>();

            var listGroup = list.GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                Operation operation = new Operation();

                int quantityPurchased;
                int quantitySold;
                decimal pricePurchased;

                quantityPurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Sum(x => x.Quantity);
                quantitySold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                operation.Quantity = quantityPurchased - quantitySold;

                if (operation.Quantity > 0)
                {
                    pricePurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    operation.Price       = (pricePurchased / quantityPurchased) - ((pricePurchased / quantityPurchased) * (listParameter[0].TradingFee / 100));
                    operation.Asset       = item.Key;
                    operation.InvestValue = operation.Price * operation.Quantity;

                    listOperation.Add(operation);
                }
            }

            return View(listOperation); 
        }

        public async Task<IActionResult> Liquidation()
        {
            var list          = await _operationService.FindAllAsync();
            var listParameter = await _parameterService.FindAllAsync();

            IList<Operation> listOperation = new List<Operation>();

            decimal rentabilityTotalValue = 0;
            decimal rentabilityTotalPercentage = 0;
            int registerSoldQuantity = 0;

            var listGroup = list.Where(x => x.Type == Enums.OperationType.Purchase.GetDescription()).GroupBy(x => x.Asset);

            IEnumerable<Operation> smths = listGroup.SelectMany(group => group);

            foreach (var item in listGroup)
            {
                Operation operation = new Operation();

                int quantityPurchased;
                int quantitySold;
                decimal pricePurchased;

                quantitySold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                if (quantitySold > 0)
                {
                    quantityPurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Sum(x => x.Quantity);
                    pricePurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    operation.Price = pricePurchased / quantityPurchased;
                    operation.Quantity = quantitySold;

                    decimal priceSold;

                    priceSold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    operation.RentabilityValue = ((priceSold / quantitySold) - operation.Price) * quantitySold;
                    operation.RentabilityValue -= operation.RentabilityValue * (listParameter[0].LiquidityFee / 100);

                    operation.RentabilityPercentage = (((priceSold / quantitySold) - operation.Price) * quantitySold) / (operation.Price * quantitySold);
                    operation.RentabilityPercentage -= operation.RentabilityPercentage * (listParameter[0].LiquidityFee / 100);

                    rentabilityTotalValue += operation.RentabilityValue;
                    rentabilityTotalPercentage += operation.RentabilityPercentage;

                    operation.Asset = item.Key;

                    listOperation.Add(operation);

                    registerSoldQuantity++;
                }
            }

            ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:F2} / {1:P2}", rentabilityTotalValue, rentabilityTotalPercentage / registerSoldQuantity);

            return View(listOperation);
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Operation operation)
        {
            if (!ModelState.IsValid)
                return View(operation);

            await _operationService.InsertAsync(operation);

            TempData["$AlertMessage$"] = "registro salvo com sucesso";

            return RedirectToAction("Create");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id != null)
            {
                var obj = await _operationService.FindByIdAsync(id.Value);

                if (obj != null)
                    return View(obj);
            }

            return RedirectToAction(nameof(Error), new { message = "Id not provided" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _operationService.RemoveAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (IntegrityException e)
            {
                return RedirectToAction(nameof(Error), new { message = e.Message });
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id != null)
            {
                var obj = await _operationService.FindByIdAsync(id.Value);

                if (obj != null)
                    return View(obj);
            }

            return RedirectToAction(nameof(Error), new { message = "Id not found" });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return RedirectToAction(nameof(Error), new { message = "Id not found" });

            var obj = await _operationService.FindByIdAsync(id.Value);

            if (obj == null)
                return RedirectToAction(nameof(Error), new { message = "Id not found" });

            obj.ListType = Enums.GetDescriptions<Enums.OperationType>().ToList();

            obj.ListType.Insert(0, "Selecione uma opção");

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Operation operation)
        {
            if (!ModelState.IsValid)
                return View(operation);

            if (id != operation.Id)
                return RedirectToAction(nameof(Error), new { message = "Id mismatch" });

            try
            {
                await _operationService.UpdateAsync(operation);
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException e)
            {
                return RedirectToAction(nameof(Error), new { message = e.Message });
            }
            catch (DBConcurrencyException e)
            {
                return RedirectToAction(nameof(Error), new { message = e.Message });
            }
        }

        public IActionResult Error(string message)
        {
            var viewModel = new ErrorViewModel
            {
                Message = message,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View(viewModel);
        }
    }
}
