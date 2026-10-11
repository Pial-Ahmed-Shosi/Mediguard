using System.Threading.Tasks;

namespace MediGuard.Services
{
    public interface IPaymentWebhookService
    {
        Task<bool> ProcessWebhookEventAsync(string rawJson, string signatureHeader);
    }
}