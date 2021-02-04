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

            listOperation = listOperation.OrderBy(x => x.Date).ThenBy(x => x.Asset).ThenByDescending(x => x.Price).ToList();

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
                decimal priceSold;

                int count = 0;
                decimal averagePrice = 0;
                decimal quantity = 0;

                quantityPurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Sum(x => x.Quantity);
                quantitySold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                operation.Quantity = quantityPurchased - quantitySold;

                if (operation.Quantity > 0)
                {
                    pricePurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Select(x => x.Price * x.Quantity).Sum();
                    priceSold      = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    IList<Operation> listByAsset = list.Where(x => x.Asset == item.Key).OrderBy(x => x.Date).ToList();

                    foreach (var op in listByAsset)
                    {
                        if (count == 0 && op.Type == Enums.OperationType.Purchase.GetDescription())
                        {
                            averagePrice = op.Price;
                            quantity = op.Quantity;

                            count++;
                        }
                        else if (op.Type == Enums.OperationType.Sale.GetDescription())
                            quantity -= op.Quantity;
                        else
                        {
                            averagePrice = ((averagePrice * quantity) + (op.Price * op.Quantity)) / (quantity + op.Quantity);
                            quantity += op.Quantity;
                        }
                    }

                    operation.Price       = averagePrice;
                    operation.Asset       = item.Key;
                    operation.InvestValue = operation.Price * operation.Quantity;

                    listOperation.Add(operation);
                }
            }

            listOperation = listOperation.OrderByDescending(x => x.InvestValue).ThenBy(x => x.Asset).ToList();

            return View(listOperation); 
        }

        public async Task<IActionResult> Liquidation()
        {
            var list = await _operationService.FindAllAsync();
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

                int soldQuantity = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                if (soldQuantity > 0)
                {
                    int count = 0;
                    decimal purchaseTotalPrice = 0;
                    decimal soldTotalPrice = 0;
                    decimal rentabilityValue = 0;
                    int purchaseQuantity = 0;
                    soldQuantity = 0;

                    IList<Operation> listByAsset = list.Where(x => x.Asset == item.Key).OrderBy(x => x.Date).ToList();

                    foreach (var op in listByAsset)
                    {
                        if (count == 0 && op.Type == Enums.OperationType.Purchase.GetDescription())
                        {
                            purchaseTotalPrice = op.Price * op.Quantity;
                            purchaseQuantity = op.Quantity;

                            count++;
                        }
                        else if (op.Type == Enums.OperationType.Sale.GetDescription())
                        {
                            soldTotalPrice += (op.Price * op.Quantity);
                            soldQuantity += op.Quantity;
                        }
                        else
                        {
                            purchaseTotalPrice += (op.Price * op.Quantity);
                            purchaseQuantity += op.Quantity;
                        }
                    }

                    if (item.Key == "STBP3")
                    {

                    }

                    if (purchaseQuantity > soldQuantity)
                    {
                        int differenceQuantity = purchaseQuantity - soldQuantity;

                        purchaseTotalPrice -= (purchaseTotalPrice / purchaseQuantity) * soldQuantity;
                    }
                    rentabilityValue = soldTotalPrice - purchaseTotalPrice;

                    purchaseTotalPrice -= ((purchaseTotalPrice) * (listParameter[0].TradingFee / 100));

                    operation.Price = (purchaseTotalPrice / purchaseQuantity);
                    operation.Quantity = soldQuantity;

                    //decimal totalPriceSold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    //totalPriceSold -= (totalPriceSold * (listParameter[0].LiquidityFee / 100));

                    //operation.RentabilityValue = ((soldTotalPrice / soldQuantity) - operation.Price) * soldQuantity;

                    operation.RentabilityValue = /*soldTotalPrice - purchaseTotalPrice*/ rentabilityValue;

                    operation.RentabilityPercentage = (((soldTotalPrice / soldQuantity) - operation.Price) * soldQuantity) / (operation.Price * soldQuantity);
                    operation.RentabilityPercentage -= operation.RentabilityPercentage * (listParameter[0].LiquidityFee / 100);

                    rentabilityTotalValue += operation.RentabilityValue;
                    rentabilityTotalPercentage += operation.RentabilityPercentage;

                    operation.Asset = item.Key;

                    listOperation.Add(operation);

                    registerSoldQuantity++;
                }
            }

            ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:F2} / {1:P2}", rentabilityTotalValue, rentabilityTotalPercentage / registerSoldQuantity);

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

            return View(listOperation);
        }

        public async Task<IActionResult> Liquidation2()
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

                int quantitySold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                if (quantitySold > 0)
                {
                    //int quantityPurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Sum(x => x.Quantity);
                    //decimal totalPricePurchased = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    //totalPricePurchased -= ((totalPricePurchased) * (listParameter[0].TradingFee / 100));

                    //operation.Price = (totalPricePurchased / quantityPurchased);
                    //operation.Quantity = quantitySold;

                    //decimal totalPriceSold = list.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    //totalPriceSold -= (totalPriceSold * (listParameter[0].LiquidityFee / 100));

                    //operation.RentabilityValue = ((totalPriceSold / quantitySold) - operation.Price) * quantitySold;

                    //operation.RentabilityPercentage = (((totalPriceSold / quantitySold) - operation.Price) * quantitySold) / (operation.Price * quantitySold);
                    //operation.RentabilityPercentage -= operation.RentabilityPercentage * (listParameter[0].LiquidityFee / 100);

                    //rentabilityTotalValue += operation.RentabilityValue;
                    //rentabilityTotalPercentage += operation.RentabilityPercentage;

                    //operation.Asset = item.Key;

                    //listOperation.Add(operation);

                    //registerSoldQuantity++;
                }
            }

            ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:F2} / {1:P2}", rentabilityTotalValue, rentabilityTotalPercentage / registerSoldQuantity);

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

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
