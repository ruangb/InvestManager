using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InvestManager.Models;

namespace InvestManager.Controllers
{
    public class FileUploadController : Controller
    {
        [HttpPost]
        public IActionResult ReadFile()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
