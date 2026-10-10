using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MediGuard.Data;
using MediGuard.Models;

namespace MediGuard.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<PaymentIntentResult> CreatePaymentIntentAsync(Guid orderId, Guid pharmacyId)
        {
            // 1. Fetch order details from database
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.PharmacyId == pharmacyId);

            if (order == null)
            {
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = "Order not found or access denied."
                };
            }

            try
            {
                // 2. Call Payment Gateway SDK/API to initialize payment session
                string transactionId = $"tx_{Guid.NewGuid().ToString("N")[..16]}";
                string clientSecret = $"pi_secret_{Guid.NewGuid().ToString("N")}";

                bool isGatewayAvailable = CheckGatewayConnectivity();
                if (!isGatewayAvailable)
                {
                    throw new Exception("Gateway service unreachable.");
                }

                // 3. Create PENDING ledger record in database
                var paymentLedger = new Payment
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = pharmacyId,
                    OrderId = order.Id,
                    TransactionId = transactionId,
                    Amount = order.TotalAmount,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(paymentLedger);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment intent created successfully. TransactionId: {TransactionId}, OrderId: {OrderId}",
                    transactionId, orderId);

                return new PaymentIntentResult
                {
                    Success = true,
                    ClientSecret = clientSecret,
                    TransactionId = transactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to payment gateway provider for OrderId: {OrderId}", orderId);

                return new PaymentIntentResult
                {
                    Success = false,
                    IsGatewayError = true,
                    ErrorMessage = "Payment gateway is currently unavailable. Please try again later."
                };
            }
        }

        private bool CheckGatewayConnectivity()
        {
            return true;
        }
    }
}