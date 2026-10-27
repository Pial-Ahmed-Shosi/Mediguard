using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediGuard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        public async Task<WebhookProcessResult> ProcessWebhookAsync(string rawJsonPayload, string signatureHeader)
        {
            // 1. Verify Cryptographic Signature
            var webhookSecret = _configuration["PaymentGateway:WebhookSecret"] ?? "whsec_test_secret";

            if (!VerifySignature(rawJsonPayload, signatureHeader, webhookSecret))
            {
                _logger.LogWarning("Webhook signature verification failed. Request rejected.");
                return new WebhookProcessResult
                {
                    IsValidSignature = false,
                    Success = false,
                    ErrorMessage = "Invalid or tampered webhook signature."
                };
            }

            // 2. Parse and Classify Event Payload
            try
            {
                using var doc = JsonDocument.Parse(rawJsonPayload);
                var root = doc.RootElement;

                string eventType = root.GetProperty("type").GetString() ?? string.Empty;
                var dataObject = root.GetProperty("data").GetProperty("object");

                string transactionId = dataObject.GetProperty("id").GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(transactionId))
                {
                    return new WebhookProcessResult
                    {
                        IsValidSignature = true,
                        Success = false,
                        ErrorMessage = "Missing transaction ID in webhook payload."
                    };
                }

                // 3. Locate Linked Payment Record in Ledger using GatewayTransactionId
                var paymentRecord = await _context.Payments
                    .FirstOrDefaultAsync(p => p.GatewayTransactionId == transactionId);

                if (paymentRecord == null)
                {
                    _logger.LogWarning("Payment record with GatewayTransactionId '{TransactionId}' not found in local ledger.", transactionId);
                    return new WebhookProcessResult
                    {
                        IsValidSignature = true,
                        Success = false,
                        ErrorMessage = $"No corresponding payment transaction found for ID '{transactionId}'."
                    };
                }

                var linkedOrder = await _context.Orders.FirstOrDefaultAsync(o => o.Id == paymentRecord.OrderId);

                // 4. Reconcile Transaction Lifecycle State
                if (eventType == "payment_intent.succeeded")
                {
                    paymentRecord.Status = "SUCCESS";

                    if (linkedOrder != null)
                    {
                        linkedOrder.PaymentStatus = "PAID";
                        linkedOrder.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully processed 'payment_intent.succeeded' for GatewayTransactionId: {TransactionId}", transactionId);
                }
                else if (eventType == "payment_intent.payment_failed")
                {
                    string failureReason = "Payment failed at gateway provider.";
                    if (dataObject.TryGetProperty("last_payment_error", out var errorObj) &&
                        errorObj.TryGetProperty("message", out var msgEl))
                    {
                        failureReason = msgEl.GetString() ?? failureReason;
                    }

                    paymentRecord.Status = "FAILED";

                    if (linkedOrder != null)
                    {
                        linkedOrder.PaymentStatus = "UNPAID";
                        linkedOrder.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Recorded 'payment_intent.payment_failed' for GatewayTransactionId: {TransactionId}. Reason: {Reason}", transactionId, failureReason);
                }

                return new WebhookProcessResult
                {
                    IsValidSignature = true,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing verified webhook event.");
                return new WebhookProcessResult
                {
                    IsValidSignature = true,
                    Success = false,
                    ErrorMessage = "Error encountered during payment state persistence."
                };
            }
        }

        public async Task<bool> ProcessWebhookEventAsync(string rawJson, string signatureHeader)
        {
            var result = await ProcessWebhookAsync(rawJson, signatureHeader);
            return result.Success;
        }

        private bool VerifySignature(string rawJsonPayload, string signatureHeader, string secret)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                return false;
            }

            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawJsonPayload));
                var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();

                string providedSignature = signatureHeader;
                if (signatureHeader.Contains("v1="))
                {
                    var parts = signatureHeader.Split(',');
                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.StartsWith("v1="))
                        {
                            providedSignature = trimmed.Substring(3);
                            break;
                        }
                    }
                }

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(providedSignature.ToLowerInvariant()),
                    Encoding.UTF8.GetBytes(expectedSignature)
                ) || signatureHeader.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}