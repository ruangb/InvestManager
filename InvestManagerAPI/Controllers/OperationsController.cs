using InvestManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace InvestManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperationsController : Controller
    {

        [HttpGet]
        [Route("wallet-process")]
        public IList<Operation> WalletProcess([FromBody]List<Operation> operations)
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

                    operation.Price = averagePrice;
                    operation.Asset = item.Key;
                    operation.InvestValue = operation.Price * operation.Quantity;

                    listOperation.Add(operation);
                }
            }

            return listOperation.OrderByDescending(x => x.InvestValue).ThenBy(x => x.Asset).ToList();
        }

        [HttpGet]
        [Route("teste")]
        // GET: OperationsController
        public ActionResult Index()
        {
            return Ok();
        }

        // GET: OperationsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: OperationsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: OperationsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: OperationsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: OperationsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: OperationsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: OperationsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
