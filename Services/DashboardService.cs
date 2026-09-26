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

            var metrics = new DashboardSummaryViewModel
            {
                Role = role
            };

            var today = DateTime.UtcNow.Date;
            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
            string userIdStr = userId.ToString();

            // Aggregate metrics based on Role (Enforces tenant isolation with pharmacyId)
            switch (role.Trim().ToLower())
            {
                case "manager":
                case "pharmacy manager":
                    // 1. Total sales today
                    metrics.TotalSalesToday = await _context.Sales
                        .Where(s => s.PharmacyId == pharmacyId && s.CreatedAt.Date == today)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

                    // 2. Count of batches where quantity <= low_stock_threshold
                    metrics.LowStockBatchesCount = await _context.Batches
                        .Where(b => b.PharmacyId == pharmacyId && b.Quantity <= b.LowStockThreshold)
                        .CountAsync();

                    // 3. Count of pending orders
                    metrics.PendingOrdersCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.Status == "PENDING")
                        .CountAsync();

                    // 4. Count of active deliverymen
                    metrics.ActiveDeliverymenCount = await _context.Users
                        .Where(u => u.PharmacyId == pharmacyId && u.Role == "Deliveryman" && u.IsActive)
                        .CountAsync();
                    break;

                case "deliveryman":
                    // 1. Orders assigned to userId where status == "PENDING"
                    metrics.AssignedPendingOrdersCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.DeliverymanId == userIdStr && o.Status == "PENDING")
                        .CountAsync();

                    // 2. Count completed today
                    metrics.CompletedOrdersTodayCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.DeliverymanId == userIdStr && o.Status == "COMPLETED" && o.UpdatedAt != null && o.UpdatedAt.Value.Date == today)
                        .CountAsync();

                    // 3. Count cancelled
                    metrics.CancelledOrdersCount = await _context.Orders
                        .Where(o => o.PharmacyId == pharmacyId && o.DeliverymanId == userIdStr && o.Status == "CANCELLED")
                        .CountAsync();
                    break;

                case "pharmacist":
                    // 1. Pending prescription uploads where status == "PENDING_VERIFICATION"
                    metrics.PendingPrescriptionsCount = await _context.Prescriptions
                        .Where(p => p.PharmacyId == pharmacyId && p.Status == "PENDING_VERIFICATION")
                        .CountAsync();

                    // 2. Batches expiring in less than 30 days
                    metrics.ExpiringBatchesCount = await _context.Batches
                        .Where(b => b.PharmacyId == pharmacyId && b.ExpiryDate <= thirtyDaysFromNow && b.ExpiryDate >= DateTime.UtcNow)
                        .CountAsync();
                    break;

                default:
                    _logger.LogWarning("Unrecognized user role '{Role}' provided for dashboard data.", role);
                    break;
            }

            // Cache for 60 seconds
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));

            _cache.Set(cacheKey, metrics, cacheEntryOptions);

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