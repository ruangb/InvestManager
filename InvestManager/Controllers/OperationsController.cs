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
using System.Threading;
using InvestManager.Utilities;

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

            return View(_operationService.WalletProcess(operations, parameters)); 
        }

        public async Task<IActionResult> Liquidation()
        {
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            IList<Operation> listWallet = _operationService.WalletProcess(operations, parameters);

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

            IList<Operation> listOperation = _operationService.GetRentabilityPerPeriod(operation, operations, parameters);

            if (listOperation.Count() > 0)
                ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", listOperation.Sum(x => x.RentabilityValue), listOperation.Sum(x => x.RentabilityPercentage) / listOperation.Count());

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

            operationView.Months     = Enums.GetDescriptions<Enums.Month>();
            operationView.Years      = ToolKit.GetPastYears();
            operationView.Operations = listOperation;

            StaticClass.sOperation = operation;

            return View(operationView);
        }

        [HttpPost]
        public async Task<IActionResult> RentabilityPerYear(Operation operation)
        {
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            Operation operationView = new Operation();

            IList<Operation> listOperation = _operationService.GetRentabilityPerPeriod(operation, operations, parameters);

            if (listOperation.Count() > 0)
                ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", listOperation.Sum(x => x.RentabilityValue), listOperation.Sum(x => x.RentabilityPercentage) / listOperation.Count());

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

            operationView.Years = ToolKit.GetPastYears();
            operationView.Operations = listOperation;

            StaticClass.sOperation = operation;

            return View(operationView);
        }

        public async Task<IActionResult> RentabilityPerMonth()
        {
            Operation operation = new Operation();

            operation.Operations = new List<Operation>();
            operation.Months = Enums.GetDescriptions<Enums.Month>();
            operation.Years = ToolKit.GetPastYears();

            return View(operation);
        }

        public async Task<IActionResult> RentabilityPerYear()
        {
            Operation operation = new Operation();

            operation.Operations = new List<Operation>();
            operation.Years = ToolKit.GetPastYears();

            return View(operation);
        }

        public async Task<JsonResult> BuildRentabilityPerPeriodChartAsync()
        {
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            List<Operation> listOperation = new List<Operation>();

            Operation operation = StaticClass.sOperation;

            if (operation != null)
            {
                listOperation = (List<Operation>)_operationService.GetRentabilityPerPeriod(StaticClass.sOperation, operations, parameters);

                foreach (var item in listOperation)
                {
                    item.RentabilityValue = Math.Round(item.RentabilityValue, 2);
                    item.RentabilityPercentage = Math.Round(item.RentabilityPercentage * 100, 2);
                }
            }

            StaticClass.sOperation = null;

            return Json(listOperation);
        }

        public async Task<JsonResult> BuildWalletChartAsync()
        {
            var operations = await _operationService.FindAllAsync();
            var parameters = await _parameterService.FindAllAsync();

            return Json(_operationService.WalletProcess(operations, parameters));
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Operation operation)
        {
            ModelState["ReferenceMonth"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            ModelState["ReferenceYear"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;

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
