using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediGuard.Services
{
    public interface INotificationService
    {
        Task CreateOrUpdateExpiryNotificationAsync(
            Guid pharmacyId,
            Guid batchId,
            string batchNumber,
            string medicineName,
            DateTime expiryDate,
            string urgencyLevel,
            CancellationToken cancellationToken);
    }
}