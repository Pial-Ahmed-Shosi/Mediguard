using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuardApp.Models.ViewModels;

namespace MediGuardApp.Services
{
    public interface IDeliveryService
    {
        Task<IEnumerable<DeliveryDetailsDto>> GetDeliveriesForUserAsync(string deliverymanId, string statusFilter);
        Task<(bool Success, string Message)> UpdateDeliveryStatusAsync(string deliverymanId, UpdateDeliveryStatusDto dto);
    }
}