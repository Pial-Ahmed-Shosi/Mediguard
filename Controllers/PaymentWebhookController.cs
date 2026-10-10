using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/webhooks")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IPaymentWebhookService _webhookService;

        public PaymentWebhookController(IPaymentWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost("payment")]
        public async Task<IActionResult> ProcessPaymentWebhook()
        {
            // 1. Read Raw Body Stream
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string rawJson = await reader.ReadToEndAsync();

            // 2. Extract Signature Header
            string signatureHeader = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                signatureHeader = Request.Headers["X-Webhook-Signature"].ToString();
            }

            if (string.IsNullOrWhiteSpace(rawJson) || string.IsNullOrWhiteSpace(signatureHeader))
            {
                return BadRequest(new { message = "Missing request payload or signature header." });
            }

            // 3. Process Webhook Payload
            bool success = await _webhookService.ProcessWebhookEventAsync(rawJson, signatureHeader);

            if (!success)
            {
                return BadRequest(new { message = "Invalid or tampered signature." });
            }

            // 4. Acknowledge Receipt
            return Ok(new { received = true });
        }
    }
}