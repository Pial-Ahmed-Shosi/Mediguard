using System;
using System.Threading.Tasks;

namespace MediGuard.Services
{
    public interface IPrescriptionService
    {
        Task ApprovePrescriptionAsync(Guid prescriptionId, string currentUserId);
        Task RejectPrescriptionAsync(Guid prescriptionId, string reason);
    }
}