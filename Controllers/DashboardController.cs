using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // GET: /Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Extract UserId from Claims
            string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Extract PharmacyId from Claims
            string? pharmacyIdClaim = User.FindFirst("PharmacyId")?.Value;
            if (!Guid.TryParse(pharmacyIdClaim, out Guid pharmacyId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Extract Role from Claims
            string role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";

            var summaryModel = await _dashboardService.GetDashboardDataAsync(userId, pharmacyId, role);

            return View(summaryModel);
        }
    }
}