using System;
using System.Threading.Tasks;

namespace MediGuard.Services
{
    public class PaymentIntentResult
    {
        public bool Success { get; set; }
        public string? ClientSecret { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsGatewayError { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentIntentResult> CreatePaymentIntentAsync(Guid orderId, Guid pharmacyId);
    }
}