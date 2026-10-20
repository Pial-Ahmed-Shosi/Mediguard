using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(
            [FromQuery] Guid orderId,
            [FromQuery] string? status = null,
            [FromQuery] string? txId = null,
            [FromQuery] string? paymentMethod = null)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest("A valid order ID is required.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Fetch logged-in Customer details for name & initials
            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Verify order ownership
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Read latest payment attempt
            var payment = await _context.Payments
                .AsNoTracking()
                .Where(p => p.OrderId == order.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            // Derive status from route parameters first; fallback to database
            var effectiveStatus = !string.IsNullOrWhiteSpace(status)
                ? status.Trim().ToUpperInvariant()
                : payment?.Status?.Trim().ToUpperInvariant() ?? "PENDING";

            var isSuccessful =
                effectiveStatus == "SUCCEEDED" ||
                effectiveStatus == "SUCCESS" ||
                effectiveStatus == "PAID" ||
                effectiveStatus == "COMPLETED";

            var isFailed =
                effectiveStatus == "FAILED" ||
                effectiveStatus == "CANCELED" ||
                effectiveStatus == "CANCELLED" ||
                effectiveStatus == "DECLINED";

            // Derive Gateway Reference ID
            var effectiveTxId = !string.IsNullOrWhiteSpace(txId)
                ? txId
                : (!string.IsNullOrWhiteSpace(payment?.GatewayTransactionId)
                    ? payment.GatewayTransactionId
                    : "N/A");

            // Derive Payment Method
            var effectivePaymentMethod = !string.IsNullOrWhiteSpace(paymentMethod)
                ? paymentMethod
                : (!string.IsNullOrWhiteSpace(payment?.PaymentMethod)
                    ? payment.PaymentMethod
                    : "Cash / Online Payment");

            // Derive Customer Name
            var displayName = !string.IsNullOrWhiteSpace(customer?.FullName)
                ? customer.FullName
                : (!string.IsNullOrWhiteSpace(User.Identity?.Name) ? User.Identity.Name : "Valued Customer");

            // Calculate Dynamic Initials from First Name, Middle Name, Last Name
            var nameParts = displayName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var computedInitials = string.Concat(nameParts.Select(part => char.ToUpper(part[0])));

            if (string.IsNullOrWhiteSpace(computedInitials))
            {
                computedInitials = "CU";
            }

            var model = new OrderConfirmationViewModel
            {
                CustomerName = displayName,
                CustomerInitials = computedInitials,

                OrderNumber = order.Id.ToString().Substring(0, 8).ToUpper(),
                PaymentStatus = effectiveStatus,
                IsPaymentSuccessful = isSuccessful,
                IsPaymentFailed = isFailed,
                TransactionReferenceId = effectiveTxId,
                TotalPaidAmount = isSuccessful && payment != null ? payment.Amount : order.TotalAmount,
                OrderTotalAmount = order.TotalAmount,
                PaymentMethod = effectivePaymentMethod,
                EstimatedDeliveryTimeframe = "Within 1-2 hours",
                PaymentStatusMessage = isSuccessful
                    ? "Your order has been placed successfully and is being prepared by the pharmacy."
                    : isFailed
                        ? "Your transaction could not be completed. Please check your details or try another payment method."
                        : "Your payment is currently awaiting confirmation from the payment gateway."
            };

            return View("~/Views/Checkout/Confirmation.cshtml", model);
        }
    }
}