using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuardApp.Models.ViewModels;
using MediGuardApp.Services;

namespace MediGuardApp.Controllers
{
    [Authorize(Roles = "Deliveryman")]
    public class DeliverymanController : Controller
    {
        private readonly IDeliveryService _deliveryService;

        public DeliverymanController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        // GET: /Deliveryman/Index (Main Delivery Dashboard View)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Deliveryman/GetMyDeliveries?statusFilter=PENDING
        [HttpGet]
        public async Task<IActionResult> GetMyDeliveries(string statusFilter = "PENDING")
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { message = "User identification error." });
            }

            var deliveries = await _deliveryService.GetDeliveriesForUserAsync(currentUserId, statusFilter);
            return Json(new { success = true, data = deliveries });
        }

        // POST: /Deliveryman/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDeliveryStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data submitted." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { success = false, message = "User identification error." });
            }

            var (success, message) = await _deliveryService.UpdateDeliveryStatusAsync(currentUserId, dto);
            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Json(new { success = true, message });
        }
    }
}