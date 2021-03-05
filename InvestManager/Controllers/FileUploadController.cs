using InvestManager.Models;
using InvestManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ClosedXML.Excel;

namespace InvestManager.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly OperationService _operationService;

        public FileUploadController(OperationService operationService)
        {
            _operationService = operationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("FileUpload")]
        public async Task<IActionResult> Index(List<IFormFile> archives)
        {
            foreach (var archive in archives)
            {
                DataTable dt = new DataTable();

                using (XLWorkbook workbook = new XLWorkbook(archive.OpenReadStream()))
                {
                    bool isFirstRow = true;
                    var rows = workbook.Worksheet(1).RowsUsed();

                    foreach (var row in rows)
                    {
                        if (isFirstRow)
                        {
                            foreach (IXLCell cell in row.Cells())
                                dt.Columns.Add(cell.Value.ToString());

                            isFirstRow = false;

                            IXLCells teste = row.Cells();
                        }
                        else
                        {
                            dt.Rows.Add();

                            int i = 0;

                            foreach (IXLCell cell in row.Cells())
                                dt.Rows[dt.Rows.Count - 1][i++] = cell.Value.ToString();
                        }
                    }
                    IList<DataRow> dss = dt.Rows.Cast<DataRow>().ToList();

                    IList<Operation> listOperation = new List<Operation>();

                    foreach (var item in dss)
                    {
                        Operation operation = new Operation();

                        operation.Date     = Convert.ToDateTime(item.ItemArray[0].ToString().Trim());
                        operation.Type     = item.ItemArray[2].ToString().Trim() == "C" ? Enums.GetDescription(Enums.OperationType.Purchase) : Enums.GetDescription(Enums.OperationType.Sale);
                        operation.Asset    = item.ItemArray[5].ToString().Trim().EndsWith("F") ? item.ItemArray[5].ToString().Trim().Remove(item.ItemArray[5].ToString().Trim().Length -1) : item.ItemArray[5].ToString().Trim();
                        operation.Quantity = Convert.ToInt32(item.ItemArray[7].ToString().Trim());
                        operation.Price    = Convert.ToDecimal(item.ItemArray[8].ToString().Trim());
                        operation.Status   = 0;

                        listOperation.Add(operation);
                    }

                    bool allSaved = await _operationService.InsertAsync(listOperation);

                    if (!allSaved)
                    {
                        ViewBag.Range = "Não foi tudo salvo";
                        return Index();
                    }
                }
            }

            ViewBag.Range = "Foi tudo salvo";

            return Index();
        }
    }
}
