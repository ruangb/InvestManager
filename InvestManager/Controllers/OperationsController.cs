using Microsoft.AspNetCore.Mvc;
using InvestManager.Services;
using InvestManager.Models;
using InvestManager.Models.ViewModels;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System;

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
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            return View(WalletProcess(operations, parameters)); 
        }

        public IList<Operation> WalletProcess(List<Operation> operations, List<Parameter> parameters)
        {
            IList<Operation> listOperation = new List<Operation>();

            var listGroup = operations.GroupBy(x => x.Asset);

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

                quantityPurchased = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Sum(x => x.Quantity);
                quantitySold = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                operation.Quantity = quantityPurchased - quantitySold;

                if (operation.Quantity > 0)
                {
                    pricePurchased = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Select(x => x.Price * x.Quantity).Sum();
                    priceSold = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    IList<Operation> listByAsset = operations.Where(x => x.Asset == item.Key).OrderBy(x => x.Date).ToList();

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

            return listOperation.OrderByDescending(x => x.InvestValue).ThenBy(x => x.Asset).ToList();
        }

        public async Task<IActionResult> Liquidation()
        {
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            IList<Operation> listWallet = WalletProcess(operations, parameters);

            IList<Operation> listOperation = new List<Operation>();

            decimal rentabilityTotalValue = 0;
            decimal rentabilityTotalPercentage = 0;
            int registerSoldQuantity = 0;

            var listGroup = operations.Where(x => x.Type == Enums.OperationType.Purchase.GetDescription()).GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                Operation operation = new Operation();

                int soldQuantity = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                if (soldQuantity > 0)
                {
                    int count = 0;
                    decimal purchaseTotalPrice = 0;
                    decimal soldTotalPrice = 0;
                    int purchaseQuantity = 0;
                    soldQuantity = 0;

                    IList<Operation> listByAsset = operations.Where(x => x.Asset == item.Key).OrderBy(x => x.Date).ToList();

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

                    if (purchaseQuantity > soldQuantity)
                        purchaseTotalPrice -= listWallet.Where(x => x.Asset == item.Key).Select(x => x.Price * x.Quantity).Sum();

                    purchaseTotalPrice -= ((purchaseTotalPrice) * (parameters[0].TradingFee / 100));
                    soldTotalPrice     -= ((soldTotalPrice) * (parameters[0].LiquidityFee / 100));

                    operation.Quantity = soldQuantity;

                    operation.RentabilityValue      = soldTotalPrice - purchaseTotalPrice;
                    operation.RentabilityPercentage = operation.RentabilityValue / purchaseTotalPrice;

                    rentabilityTotalValue      += operation.RentabilityValue;
                    rentabilityTotalPercentage += operation.RentabilityPercentage;

                    operation.Asset = item.Key;

                    listOperation.Add(operation);

                    registerSoldQuantity++;
                }
            }

            ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", rentabilityTotalValue, rentabilityTotalPercentage / registerSoldQuantity);

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

            return View(listOperation);
        }

        [HttpPost]
        public async Task<IActionResult> RentabilityPerMonth(Operation operation)
        {
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            Operation operationView = new Operation();

            IList<Operation> listWallet = WalletProcess(operations, parameters);
            IList<Operation> listOperation = new List<Operation>();

            string monthIndex = Enums.GetIndexByDescription(Enums.Month.None, operation.ReferenceMonth).ToString();

            if (monthIndex.Length == 1)
                monthIndex = "0" + monthIndex;

            DateTime startDate = Convert.ToDateTime($"{"01/"}{monthIndex}{"/"}{operation.ReferenceYear}");
            DateTime endDate = startDate.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

            decimal rentabilityTotalValue = 0;
            decimal rentabilityTotalPercentage = 0;
            int registerSoldQuantity = 0;

            var listGroup = operations.GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                int soldQuantity = operations.Where(x => x.Asset == item.Key
                                                && x.Type == Enums.OperationType.Sale.GetDescription()
                                                && x.Date >= startDate
                                                && x.Date <= endDate).Sum(x => x.Quantity);

                if (soldQuantity > 0)
                {
                    int count = 0;
                    decimal purchaseTotalPrice = 0;
                    decimal soldTotalPrice = 0;
                    int purchaseQuantity = 0;
                    soldQuantity = 0;

                    IList<Operation> listByAsset = operations.Where(x => x.Asset == item.Key
                                                                && ((x.Type == Enums.OperationType.Purchase.GetDescription()
                                                                && x.Date <= endDate)
                                                                    || (x.Type == Enums.OperationType.Sale.GetDescription()
                                                                    && x.Date >= startDate
                                                                    && x.Date <= endDate))).OrderBy(x => x.Date).ToList();

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

                    if (purchaseQuantity > soldQuantity)
                    {
                        int preDateSoldQuantity = 0;
                        int posDateSoldQuantity = 0;
                        decimal inWalletValue = listWallet.Where(x => x.Asset == item.Key).Select(x => x.Price * x.Quantity).Sum();
                        int inWalletQuantity = listWallet.Where(x => x.Asset == item.Key).Select(x => x.Quantity).Sum();

                        if (inWalletQuantity != (purchaseQuantity - soldQuantity))
                        {
                            preDateSoldQuantity = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription() && x.Date < startDate).Sum(x => x.Quantity);
                            posDateSoldQuantity = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription() && x.Date > endDate).Sum(x => x.Quantity);
                        }

                        decimal preDateSoldValue = (purchaseTotalPrice / purchaseQuantity) * preDateSoldQuantity;
                        decimal posDateSoldValue = (purchaseTotalPrice / purchaseQuantity) * posDateSoldQuantity;

                        purchaseTotalPrice -= inWalletValue;
                        purchaseTotalPrice -= preDateSoldValue;
                        purchaseTotalPrice -= posDateSoldValue;
                    }

                    purchaseTotalPrice -= ((purchaseTotalPrice) * (parameters[0].TradingFee / 100));
                    soldTotalPrice -= ((soldTotalPrice) * (parameters[0].LiquidityFee / 100));

                    operationView.Quantity = soldQuantity;

                    operationView.RentabilityValue = soldTotalPrice - purchaseTotalPrice;
                    operationView.RentabilityPercentage = operationView.RentabilityValue / purchaseTotalPrice;

                    rentabilityTotalValue += operationView.RentabilityValue;
                    rentabilityTotalPercentage += operationView.RentabilityPercentage;

                    operationView.Asset = item.Key;

                    listOperation.Add(operationView);

                    operationView = new Operation();

                    registerSoldQuantity++;
                }
            }

            if (registerSoldQuantity > 0)
                ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", rentabilityTotalValue, rentabilityTotalPercentage / registerSoldQuantity);

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

            operationView.Months     = Enums.GetDescriptions<Enums.Month>();
            operationView.Years      = Utilities.GetPastYears();
            operationView.Operations = listOperation;

            return View(operationView);
        }

        public async Task<IActionResult> RentabilityPerMonth()
        {
            Operation operation = new Operation();

            operation.Operations = new List<Operation>();
            operation.Months     = Enums.GetDescriptions<Enums.Month>();
            operation.Years      = Utilities.GetPastYears();

            return View(operation);
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
