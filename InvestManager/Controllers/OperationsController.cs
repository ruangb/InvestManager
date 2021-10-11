using InvestManager.Services;
using InvestManager.Models;
using InvestManager.Models.ViewModels;
using InvestManager.Utilities;
using InvestManager.Manager.Repositories;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IOperationRepository _operationRepository;
        private readonly IParameterRepository _parameterRepository;

        public OperationsController(IOperationRepository operationRepository, IParameterRepository parameterRepository)
        {
            _operationRepository = operationRepository;
            _parameterRepository = parameterRepository;
        }

        public async Task<IActionResult> Index()
        {
            var listOperation = await _operationRepository.FindAllAsync();
            var listParameter = await _parameterRepository.FindAllAsync();

            if (listParameter.Any())
            {
                foreach (var item in listOperation)
                {
                    item.InvestValue = item.Quantity * item.Price - ((item.Quantity * item.Price) * (listParameter[0].TradingFee / 100));
                }
            }

            return View(listOperation);
        }

        public async Task<IActionResult> Wallet()
        {
            var listOperation = await _operationRepository.FindAllAsync();

            return View(_operationRepository.WalletProcess(listOperation)); 
        }

        public async Task<IActionResult> Liquidation()
        {
            var operations = await _operationRepository.FindAllAsync();
            var parameters = await _parameterRepository.FindAllAsync();
            var listWallet = _operationRepository.WalletProcess(operations);

            var listLiquidation = await GetLiquidation(operations, listWallet, parameters);

            return View(listLiquidation);
        }

        [HttpPost]
        public async Task<IActionResult> RentabilityPerMonth(Operation operation)
        {
            var operations = await _operationRepository.FindAllAsync();
            var parameters = await _parameterRepository.FindAllAsync();

            Operation operationView = new Operation();

            IList<Operation> listOperation = _operationRepository.GetRentabilityPerPeriod(operation, operations, parameters);

            if (listOperation.Count() > 0)
                ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", listOperation.Sum(x => x.RentabilityValue), listOperation.Sum(x => x.RentabilityPercentage) / listOperation.Count());

            operationView.Months     = Enums.GetDescriptions<Enums.Month>();
            operationView.Years      = ToolKit.GetPastYears();
            operationView.Operations = listOperation.OrderBy(x => x.Asset).ToList();

            StaticClass.sOperation = operation;

            return View(operationView);
        }

        [HttpPost]
        public async Task<IActionResult> RentabilityPerYear(Operation operation)
        {
            var operations = await _operationRepository.FindAllAsync();
            var parameters = await _parameterRepository.FindAllAsync();

            Operation operationView = new Operation();

            IList<Operation> listOperation = _operationRepository.GetRentabilityPerPeriod(operation, operations, parameters);

            if (listOperation.Count() > 0)
                ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", listOperation.Sum(x => x.RentabilityValue), listOperation.Sum(x => x.RentabilityPercentage) / listOperation.Count());

            operationView.Years      = ToolKit.GetPastYears();
            operationView.Operations = listOperation.OrderBy(x => x.Asset).ToList();

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
            var operations = await _operationRepository.FindAllAsync();
            var parameters = await _parameterRepository.FindAllAsync();

            List<Operation> listOperation = new List<Operation>();

            Operation operation = StaticClass.sOperation;

            if (operation != null)
            {
                listOperation = (List<Operation>)_operationRepository.GetRentabilityPerPeriod(StaticClass.sOperation, operations, parameters);

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
            var operations = await _operationRepository.FindAllAsync();
            var parameters = await _parameterRepository.FindAllAsync();

            return Json(_operationRepository.WalletProcess(operations));
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Operation operation)
        {
            SetExtraFieldsValidation();

            if (!ModelState.IsValid)
                return View(operation);

            await _operationRepository.InsertAsync(operation);

            TempData["$AlertMessage$"] = "Registro salvo com sucesso!";

            return RedirectToAction("Create");
            //return View();
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id != null)
            {
                var obj = await _operationRepository.FindByIdAsync(id.Value);

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
                await _operationRepository.RemoveAsync(id);
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
                var obj = await _operationRepository.FindByIdAsync(id.Value);

                if (obj != null)
                    return View(obj);
            }

            return RedirectToAction(nameof(Error), new { message = "Id not found" });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return RedirectToAction(nameof(Error), new { message = "Id not found" });

            var obj = await _operationRepository.FindByIdAsync(id.Value);

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
            SetExtraFieldsValidation();

            if (!ModelState.IsValid)
                return View(operation);

            if (id != operation.Id)
                return RedirectToAction(nameof(Error), new { message = "Id mismatch" });

            try
            {
                await _operationRepository.UpdateAsync(operation);
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

        #region Auxiliar

        private void SetExtraFieldsValidation()
        {
            ModelState["ReferenceMonth"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            ModelState["ReferenceYear"].ValidationState  = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
        }

        private async Task<IList<Operation>> GetLiquidation(IList<Operation> listOperation, IList<Operation> listWallet, IList<Parameter> listParameter)
        {
            decimal rentabilityTotalValue = 0;
            decimal rentabilityTotalPercentage = 0;
            int assetsSoldQuantity = 0;
            
            var listLiquidation = await _operationRepository.GetLiquidation(listOperation, listWallet, listParameter);

            var listGroup = listOperation.Where(x => x.Type == Enums.OperationType.Purchase.GetDescription()).GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                if (listOperation.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity) > 0)
                    assetsSoldQuantity++;
            }

            rentabilityTotalValue = listLiquidation.Sum(x => x.RentabilityValue);
            rentabilityTotalPercentage = listLiquidation.Sum(x => x.RentabilityPercentage);

            if (assetsSoldQuantity > 0)
                ViewBag.RentabilityTotal = string.Format("Rentabilidade Total R$ {0:N2} / {1:P2}", rentabilityTotalValue, rentabilityTotalPercentage / assetsSoldQuantity);
            else
                ViewBag.RentabilityTotal = "Rentabilidade Total R$ 0,00 / 0,00 %";

            return listLiquidation.OrderBy(x => x.Asset).ToList();
        }

        #endregion
    }
}
