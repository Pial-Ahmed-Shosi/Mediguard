using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MediGuard.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/webhooks")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IPaymentWebhookService _webhookService;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            IPaymentWebhookService webhookService,
            ILogger<PaymentWebhookController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost("payment")]
        public async Task<IActionResult> ProcessPaymentWebhook()
        {
            // 1. Read Raw Body Stream for HMAC Signature Verification
            string rawJson;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawJson = await reader.ReadToEndAsync();
            }

            // 2. Extract Gateway Signature Header
            string signatureHeader = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                signatureHeader = Request.Headers["X-Webhook-Signature"].ToString();
            }
            if (string.IsNullOrEmpty(signatureHeader))
            {
                signatureHeader = Request.Headers["X-Signature"].ToString();
            }

            if (string.IsNullOrWhiteSpace(rawJson) || string.IsNullOrWhiteSpace(signatureHeader))
            {
                _logger.LogWarning("Webhook request rejected. Missing body payload or signature header.");
                return BadRequest(new { message = "Missing request payload or signature header." });
            }

            // 3. Delegate to Service Layer for Signature Verification and State Updates
            var result = await _webhookService.ProcessWebhookAsync(rawJson, signatureHeader);

            if (!result.IsValidSignature)
            {
                _logger.LogWarning("Webhook request rejected due to invalid signature.");
                return BadRequest(new { message = result.ErrorMessage ?? "Invalid or tampered signature." });
            }

            if (!result.Success)
            {
                _logger.LogWarning("Webhook payload failed processing: {ErrorMessage}", result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage ?? "Webhook processing failed." });
            }

            // 4. Acknowledge Successful Receipt (HTTP 200 OK)
            return Ok(new { received = true, message = "Webhook received and processed successfully." });
        }
    }
}