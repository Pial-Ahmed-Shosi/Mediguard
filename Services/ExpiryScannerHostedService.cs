using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediGuard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediGuard.Services
{
    public class ExpiryScannerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ExpiryScannerHostedService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

        public ExpiryScannerHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ExpiryScannerHostedService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Expiry Scanner Hosted Service start hocche...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanAndProcessExpiriesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch expiry scanner service execution-e error hoyeche.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ScanAndProcessExpiriesAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automated inventory batch expiry scan shuru hocche...");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var today = DateTime.UtcNow.Date;
                var sixtyDaysLater = today.AddDays(60);

                // 1. Process Expired Batches from InventoryBatches (Ticket 20 Schema)
                var expiredBatches = await dbContext.InventoryBatches
                    .Include(b => b.Medicine)
                    .Where(b => b.Status == "ACTIVE" && b.ExpiryDate.Date <= today)
                    .ToListAsync(stoppingToken);

                foreach (var batch in expiredBatches)
                {
                    // Automatic status change to "EXPIRED"
                    batch.Status = "EXPIRED";

                    string medicineName = batch.Medicine?.BrandName ?? "Batch Medicine";

                    // CRITICAL level notification create/update
                    await notificationService.CreateOrUpdateExpiryNotificationAsync(
                        batch.PharmacyId,
                        batch.Id,
                        batch.BatchNumber,
                        medicineName,
                        batch.ExpiryDate,
                        "CRITICAL",
                        stoppingToken
                    );
                }

                // 2. Process Near-Expiry Batches (Next 60 days)
                var nearExpiryBatches = await dbContext.InventoryBatches
                    .Include(b => b.Medicine)
                    .Where(b => b.Status == "ACTIVE"
                        && b.ExpiryDate.Date > today
                        && b.ExpiryDate.Date <= sixtyDaysLater)
                    .ToListAsync(stoppingToken);

                foreach (var batch in nearExpiryBatches)
                {
                    string medicineName = batch.Medicine?.BrandName ?? "Batch Medicine";

                    // WARNING level notification create/update
                    await notificationService.CreateOrUpdateExpiryNotificationAsync(
                        batch.PharmacyId,
                        batch.Id,
                        batch.BatchNumber,
                        medicineName,
                        batch.ExpiryDate,
                        "WARNING",
                        stoppingToken
                    );
                }

                // Commit database changes
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Expiry scan complete. Automatically Expired: {ExpiredCount}, Near Expiry Warnings: {WarningCount}",
                    expiredBatches.Count, nearExpiryBatches.Count);
            }
        }
    }
}