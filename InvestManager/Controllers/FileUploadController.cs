using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using ClosedXML.Excel;

namespace InvestManager.Controllers
{
    public class FileUploadController : Controller
    {
        //Define uma instância de IHostingEnvironment
        IHostingEnvironment _appEnvironment;
        //Injeta a instância no construtor para poder usar os recursos
        public FileUploadController(IHostingEnvironment env)
        {
            _appEnvironment = env;
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
                }
            }

            return Index();
        }

    }
}
