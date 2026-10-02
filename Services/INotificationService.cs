using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediGuard.Services
{
    public interface INotificationService
    {
        Task CreateOrUpdateExpiryNotificationAsync(
            int pharmacyId,
            int batchId,
            string batchNumber,
            string medicineName,
            DateTime expiryDate,
            string urgencyLevel,
            CancellationToken cancellationToken);
    }
}