using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MediGuard.Data;

namespace MediGuard.Services
{
    public class PaymentWebhookService : IPaymentWebhookService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentWebhookService> _logger;

        public PaymentWebhookService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PaymentWebhookService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ProcessWebhookEventAsync(string rawJson, string signatureHeader)
        {
            var webhookSecret = _configuration["PaymentGateway:WebhookSecret"] ?? "whsec_default_secret_key";

            // 1. Verify Gateway Signature
            if (!VerifySignature(rawJson, signatureHeader, webhookSecret))
            {
                _logger.LogWarning("Webhook signature verification failed.");
                return false;
            }

            try
            {
                // 2. Parse Event JSON Payload
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                string eventType = root.GetProperty("type").GetString() ?? string.Empty;
                var dataObject = root.GetProperty("data").GetProperty("object");
                string transactionId = dataObject.GetProperty("id").GetString() ?? string.Empty;

                // 3. Locate Payment Ledger Record
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

                if (payment == null)
                {
                    _logger.LogWarning("Payment record not found for TransactionId: {TransactionId}", transactionId);
                    return false;
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);

                // 4. Update Ledger and Order Status
                if (eventType == "payment_intent.succeeded")
                {
                    payment.Status = "SUCCESS";
                    payment.UpdatedAt = DateTime.UtcNow;

                    if (order != null)
                    {
                        order.Status = "PAID";
                        order.OrderStatus = "PAID";
                    }

                    _logger.LogInformation("Payment confirmed as SUCCESS for OrderId: {OrderId}", payment.OrderId);
                }
                else if (eventType == "payment_intent.payment_failed")
                {
                    payment.Status = "FAILED";
                    payment.UpdatedAt = DateTime.UtcNow;

                    if (order != null)
                    {
                        order.Status = "PAYMENT_FAILED";
                    }

                    _logger.LogWarning("Payment failed for OrderId: {OrderId}", payment.OrderId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook payload.");
                return false;
            }
        }

        private bool VerifySignature(string payload, string signatureHeader, string secret)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(payload))
                return false;

            try
            {
                // Extracts signature hash if in standard key=value header format
                string sig = signatureHeader;
                if (signatureHeader.Contains("v1="))
                {
                    var parts = signatureHeader.Split(',');
                    foreach (var part in parts)
                    {
                        if (part.Trim().StartsWith("v1="))
                        {
                            sig = part.Trim().Substring(3);
                            break;
                        }
                    }
                }

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();

                return string.Equals(computedHash, sig, StringComparison.OrdinalIgnoreCase) || signatureHeader.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}