using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MediGuard.Data;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardSummaryViewModel> GetDashboardDataAsync(Guid userId, Guid pharmacyId, string role)
        {
            string cacheKey = $"dashboard_{pharmacyId}_{userId}";

            if (_cache.TryGetValue(cacheKey, out DashboardSummaryViewModel? cachedMetrics) && cachedMetrics != null)
            {
                _logger.LogInformation("Dashboard metrics retrieved from cache for key: {CacheKey}", cacheKey);
                return cachedMetrics;
            }

            string userIdStr = userId.ToString();
            var today = DateTime.UtcNow.Date;
            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);

            var userProfile = await _context.Users
                .AsNoTracking()
                .Include(u => u.Pharmacy)
                .FirstOrDefaultAsync(u => u.Id == userIdStr);

            var metrics = new DashboardSummaryViewModel
            {
                FullName = userProfile?.FullName ?? "User",
                RoleName = role,
                PharmacyName = userProfile?.Pharmacy?.Name ?? "Pharmacy Tenant"
            };

            string normalizedRole = (role ?? string.Empty).Trim().ToLowerInvariant();

            switch (normalizedRole)
            {
                case "manager":
                case "pharmacy manager":
                    metrics.TotalSalesToday = await _context.Sales
                        .Where(s => s.PharmacyId == pharmacyId && s.CreatedAt.Date == today)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

                    metrics.LowStockBatchesCount = await _context.Batches
                        .Where(b => b.PharmacyId == pharmacyId && b.Quantity <= b.LowStockThreshold)
                        .CountAsync();

                    metrics.PendingOrdersCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.Status == "PENDING")
                        .CountAsync();

                    metrics.ActiveDeliverymenCount = await _context.Users
                        .Where(u => u.PharmacyId == pharmacyId && u.Role == "Deliveryman" && u.IsActive)
                        .CountAsync();
                    break;

                case "deliveryman":
                    metrics.AssignedPendingOrdersCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId
                                 && o.DeliverymanId == userIdStr
                                 && o.Status == "PENDING")
                        .CountAsync();

                    metrics.CompletedOrdersTodayCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId
                                 && o.DeliverymanId == userIdStr
                                 && o.Status == "COMPLETED"
                                 && o.UpdatedAt != null
                                 && o.UpdatedAt.Value.Date == today)
                        .CountAsync();

                    metrics.CancelledOrdersCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId
                                 && o.DeliverymanId == userIdStr
                                 && o.Status == "CANCELLED")
                        .CountAsync();
                    break;

                case "pharmacist":
                    metrics.PendingPrescriptionsCount = await _context.Prescriptions
                        .Where(p => p.PharmacyId == pharmacyId && p.Status == "PENDING_VERIFICATION")
                        .CountAsync();

                    metrics.ExpiringBatchesCount = await _context.Batches
                        .Where(b => b.PharmacyId == pharmacyId
                                 && b.ExpiryDate <= thirtyDaysFromNow
                                 && b.ExpiryDate >= DateTime.UtcNow)
                        .CountAsync();

                    metrics.OutOfStockCount = await _context.Batches
                        .Where(b => b.PharmacyId == pharmacyId && b.Quantity == 0)
                        .CountAsync();
                    break;

                case "cashier":
                    metrics.DailyDrawerTotal = await _context.Sales
                        .Where(s => s.PharmacyId == pharmacyId && s.CreatedAt.Date == today)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

                    metrics.PendingPickups = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.Status == "READY_FOR_PICKUP")
                        .CountAsync();
                    break;

                case "customer":
                    metrics.ActiveOrdersInTransit = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.Status == "IN_TRANSIT")
                        .CountAsync();
                    break;
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));

            _cache.Set(cacheKey, metrics, cacheOptions);
            _logger.LogInformation("Dashboard metrics calculated and cached for key: {CacheKey}", cacheKey);

            return metrics;
        }

        public void InvalidateDashboardCache(Guid userId, Guid pharmacyId)
        {
            string cacheKey = $"dashboard_{pharmacyId}_{userId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Dashboard cache invalidated for key: {CacheKey}", cacheKey);
        }
    }
}