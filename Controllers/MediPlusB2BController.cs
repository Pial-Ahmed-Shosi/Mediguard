using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Filters;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize(Roles = "Pharmacy Manager,Admin")]
    [RequiresMediPlus]
    public class MediPlusB2BController : Controller
    {
        private readonly IMediPlusB2BService _b2bService;

        public MediPlusB2BController(IMediPlusB2BService b2bService)
        {
            _b2bService = b2bService;
        }

        // GET: /MediPlusB2B/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new B2BOrderViewModel());
        }

        // POST: /MediPlusB2B/CreateB2BOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateB2BOrder([FromBody] B2BOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid order data provided.", errors = ModelState });
            }

            var (success, message, orderId) = await _b2bService.CreateB2BOrderAsync(model);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Json(new { success = true, message, orderId });
        }
    }
}