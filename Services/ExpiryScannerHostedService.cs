using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediGuard.Data;

namespace MediGuard.Services
{
    public class ExpiryScannerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ExpiryScannerHostedService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Daily background task run hobe

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

            // Application startup-e run hobe ebong daily repeat hobe
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

                // 24 hours pause korbe next daily execution porjonto
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

                // 1. dbContext.Batches bebohar kora hocche (Active ebong Expired batches filter)
                var expiredBatches = await dbContext.Batches
                    .Where(b => b.Status == "ACTIVE" && b.ExpiryDate.Date <= today)
                    .ToListAsync(stoppingToken);

                foreach (var batch in expiredBatches)
                {
                    // Automatic status change to "EXPIRED"
                    batch.Status = "EXPIRED";

                    string medicineName = "Batch Medicine";

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

                // 2. Near Expiry Batches (Next 60 days-er majhe expiry hone wala batches)
                var nearExpiryBatches = await dbContext.Batches
                    .Where(b => b.Status == "ACTIVE"
                        && b.ExpiryDate.Date > today
                        && b.ExpiryDate.Date <= sixtyDaysLater)
                    .ToListAsync(stoppingToken);

                foreach (var batch in nearExpiryBatches)
                {
                    string medicineName = "Batch Medicine";

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

                // Database update commit
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Expiry scan complete. Automatically Expired: {ExpiredCount}, Near Expiry Warnings: {WarningCount}",
                    expiredBatches.Count, nearExpiryBatches.Count);
            }
        }
    }
}