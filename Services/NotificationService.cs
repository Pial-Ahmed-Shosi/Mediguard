using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;

namespace MediGuard.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _dbContext;

        public NotificationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateOrUpdateExpiryNotificationAsync(
            Guid pharmacyId,
            Guid batchId,
            string batchNumber,
            string medicineName,
            DateTime expiryDate,
            string urgencyLevel,
            CancellationToken cancellationToken)
        {
            var existingNotification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.PharmacyId == pharmacyId
                    && n.BatchId == batchId
                    && n.UrgencyLevel == urgencyLevel
                    && !n.IsRead, cancellationToken);

            string title = urgencyLevel == "CRITICAL"
                ? $"CRITICAL: Medicine Batch Expired ({medicineName})"
                : $"WARNING: Near Expiry Batch Alert ({medicineName})";

            string message = urgencyLevel == "CRITICAL"
                ? $"Batch '{batchNumber}' of '{medicineName}' expired on {expiryDate:yyyy-MM-dd}. Status automatically set to EXPIRED."
                : $"Batch '{batchNumber}' of '{medicineName}' will expire on {expiryDate:yyyy-MM-dd} (within 60 days).";

            if (existingNotification != null)
            {
                existingNotification.Message = message;
                existingNotification.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var notification = new Notification
                {
                    PharmacyId = pharmacyId,
                    BatchId = batchId,
                    Title = title,
                    Message = message,
                    UrgencyLevel = urgencyLevel,
                    Type = "EXPIRY_ALERT",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.Notifications.AddAsync(notification, cancellationToken);
            }
        }
    }
}