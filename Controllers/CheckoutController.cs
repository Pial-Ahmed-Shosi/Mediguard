```csharp
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
        public async Task<IActionResult> Confirmation(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest("A valid order ID is required.");
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Only allow the customer who owns this order.
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Read the latest payment attempt for this order.
            var payment = await _context.Payments
                .AsNoTracking()
                .Where(p => p.OrderId == order.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            var status = payment?.Status?.Trim().ToUpperInvariant()
                         ?? "PENDING";

            var isSuccessful =
                status == "SUCCEEDED" ||
                status == "SUCCESS" ||
                status == "PAID" ||
                status == "COMPLETED";

            var isFailed =
                status == "FAILED" ||
                status == "CANCELED" ||
                status == "CANCELLED";

            var model = new OrderConfirmationViewModel
            {
                OrderNumber = order.Id.ToString(),

                PaymentStatus = status,
                IsPaymentSuccessful = isSuccessful,
                IsPaymentFailed = isFailed,

                TransactionReferenceId =
                    string.IsNullOrWhiteSpace(payment?.TransactionId)
                        ? "Not available"
                        : payment.TransactionId,

                TotalPaidAmount =
                    isSuccessful && payment != null
                        ? payment.Amount
                        : 0m,

                OrderTotalAmount = order.TotalAmount,

                PaymentMethod = "Not available",

                EstimatedDeliveryTimeframe = "To be confirmed",

                PaymentStatusMessage = isSuccessful
                    ? "Your payment has been confirmed."
                    : isFailed
                        ? "Your payment was not successful."
                        : "Your payment is awaiting confirmation."
            };

            return View(
                "~/Views/Checkout/Confirmation.cshtml",
                model);
        }
    }
}
```