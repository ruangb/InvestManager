using Microsoft.AspNetCore.Mvc;
using InvestManager.Services;
using InvestManager.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;

namespace InvestManager.Controllers
{
    public class ParametersController : Controller
    {
        private readonly ParameterService _parameterService;

        public ParametersController(ParameterService parameterService)
        {
            _parameterService = parameterService;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _parameterService.FindAllAsync();

            return View(list);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Parameter Parameter)
        {
            if (!ModelState.IsValid)
                return View(Parameter);

            await _parameterService.InsertAsync(Parameter);
            return Redirect(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id != null)
            {
                var obj = await _parameterService.FindByIdAsync(id.Value);

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
                await _parameterService.RemoveAsync(id);
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
                var obj = await _parameterService.FindByIdAsync(id.Value);

                if (obj != null)
                    return View(obj);
            }

            return RedirectToAction(nameof(Error), new { message = "Id not found" });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return RedirectToAction(nameof(Error), new { message = "Id not found" });

            var obj = await _parameterService.FindByIdAsync(id.Value);

            if (obj == null)
                return RedirectToAction(nameof(Error), new { message = "Id not found" });

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Parameter Parameter)
        {
            if (!ModelState.IsValid)
                return View(Parameter);

            if (id != Parameter.Id)
                return RedirectToAction(nameof(Error), new { message = "Id mismatch" });

            try
            {
                await _parameterService.UpdateAsync(Parameter);
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
