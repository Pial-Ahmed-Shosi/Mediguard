using System.Threading.Tasks;

namespace MediGuard.Services
{
    public class WebhookProcessResult
    {
        public bool IsValidSignature { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IPaymentWebhookService
    {
        Task<WebhookProcessResult> ProcessWebhookAsync(string rawJsonPayload, string signatureHeader);
    }
}