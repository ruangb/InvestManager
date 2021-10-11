﻿using InvestManager.Models;
using InvestManager.Manager.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace InvestManager.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly IOperationRepository _operationRepository;

        public FileUploadController(IOperationRepository operationRepository)
        {
            _operationRepository = operationRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("FileUpload")]
        public async Task<IActionResult> Index(List<IFormFile> archives)
        {
            bool fileUploaded = await UploadFileAsync(archives);

            if (fileUploaded)
                ViewBag.Range = "Foi tudo salvo";
            else
                ViewBag.Range = "Não foi tudo salvo";

            return Index();
        }

        #region Auxiliar

        private async Task<bool> UploadFileAsync(List<IFormFile> archives)
        {
            bool fileUploaded = false;

            foreach (var archive in archives)
            {
                DataTable dt = new DataTable();

                using (XLWorkbook workbook = new XLWorkbook(archive.OpenReadStream(), XLEventTracking.Disabled))
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
                        }
                        else
                        {
                            dt.Rows.Add();

                            int i = 0;

                            foreach (IXLCell cell in row.Cells())
                            {
                                if (i < 9)
                                    dt.Rows[dt.Rows.Count - 1][i++] = cell.Value.ToString();
                            }
                        }
                    }

                    IList<DataRow> dr = dt.Rows.Cast<DataRow>().ToList();

                    IList<Operation> listOperation = new List<Operation>();

                    foreach (var item in dr)
                    {
                        Operation operation = new Operation();

                        operation.Date = Convert.ToDateTime(item.ItemArray[0].ToString().Trim());
                        operation.Type = item.ItemArray[2].ToString().Trim() == "C" ? Enums.GetDescription(Enums.OperationType.Purchase) : Enums.GetDescription(Enums.OperationType.Sale);
                        operation.Asset = item.ItemArray[5].ToString().Trim().EndsWith("F") ? item.ItemArray[5].ToString().Trim().Remove(item.ItemArray[5].ToString().Trim().Length - 1) : item.ItemArray[5].ToString().Trim();
                        operation.Quantity = Convert.ToInt32(item.ItemArray[7].ToString().Trim());
                        operation.Price = Convert.ToDecimal(item.ItemArray[8].ToString().Trim());
                        operation.Status = 0;

                        listOperation.Add(operation);
                    }

                    bool allSaved = await _operationRepository.InsertAsync(listOperation);

                    if (!allSaved)
                    {
                        ViewBag.Range = "Não foi tudo salvo";
                        return fileUploaded;
                    }
                }
            }

            fileUploaded = true;

            return fileUploaded;
        }

        #endregion
    }
}
