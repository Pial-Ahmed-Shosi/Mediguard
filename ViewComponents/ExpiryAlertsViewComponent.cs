// File: ViewComponents/ExpiryAlertsViewComponent.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Services;

namespace MediGuard.ViewComponents
{
    public class ExpiryAlertsViewComponent : ViewComponent
    {
        private readonly IDashboardService _dashboardService;

        public ExpiryAlertsViewComponent(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Logged-in user-er Claims theke PharmacyId extract kora
            var pharmacyIdClaim = UserClaimsPrincipal.FindFirst("PharmacyId")?.Value;

            Guid pharmacyId = Guid.Empty;
            if (!string.IsNullOrEmpty(pharmacyIdClaim))
            {
                Guid.TryParse(pharmacyIdClaim, out pharmacyId);
            }

            // Database theke alerts fetch kora
            var model = await _dashboardService.GetExpiryAndShortageAlertsAsync(pharmacyId);

            return View(model);
        }
    }
}