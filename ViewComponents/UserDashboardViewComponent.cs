using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.ViewComponents
{
    public class UserDashboardViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public UserDashboardViewComponent(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _context = context;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (UserClaimsPrincipal?.Identity == null || !UserClaimsPrincipal.Identity.IsAuthenticated)
            {
                return Content(string.Empty);
            }

            var userId = _userManager.GetUserId(UserClaimsPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return Content(string.Empty);
            }

            string cacheKey = $"UserDashboardSummary_{userId}";

            // Asynchronous 1-minute caching
            var model = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

                var user = await _context.Users
                    .Include(u => u.Pharmacy)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new DashboardSummaryViewModel();
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                string primaryRole = userRoles.FirstOrDefault() ?? user.Role ?? "Customer";

                var viewModel = new DashboardSummaryViewModel
                {
                    FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "User" : user.FullName,
                    RoleName = primaryRole,
                    PharmacyName = user.Pharmacy?.Name ?? "MediGuard Central"
                };

                // Detect role and populate respective KPIs
                if (UserClaimsPrincipal.IsInRole("Manager") || primaryRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    viewModel.TodaySalesTotal = 1450.50m;
                    viewModel.LowStockCount = 8;
                    viewModel.PendingOrders = 12;
                    viewModel.ActiveDeliverymen = 4;
                }
                else if (UserClaimsPrincipal.IsInRole("Pharmacist") || primaryRole.Equals("Pharmacist", StringComparison.OrdinalIgnoreCase))
                {
                    viewModel.PendingPrescriptions = 5;
                    viewModel.ExpiringBatchesCount = 14;
                    viewModel.OutOfStockCount = 3;
                }
                else if (UserClaimsPrincipal.IsInRole("Deliveryman") || primaryRole.Equals("Deliveryman", StringComparison.OrdinalIgnoreCase))
                {
                    viewModel.PendingDeliveries = 6;
                    viewModel.CompletedToday = 18;
                    viewModel.CancelledDeliveries = 1;
                }
                else if (UserClaimsPrincipal.IsInRole("Cashier") || primaryRole.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
                {
                    viewModel.DailyDrawerTotal = 820.00m;
                    viewModel.ActiveCartItems = 3;
                    viewModel.PendingPickups = 4;
                }
                else // Customer
                {
                    viewModel.ActiveOrdersInTransit = 2;
                    viewModel.UploadedPrescriptionStatus = "Verified & Approved";
                }

                return viewModel;
            });

            return View(model ?? new DashboardSummaryViewModel());
        }
    }
}