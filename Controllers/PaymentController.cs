using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _context;

        public PaymentController(IPaymentService paymentService, ApplicationDbContext context)
        {
            _paymentService = paymentService;
            _context = context;
        }

        private Guid GetCurrentPharmacyId()
        {
            var claim = User.FindFirst("PharmacyId") ?? User.FindFirst(ClaimTypes.GroupSid);
            if (claim != null && Guid.TryParse(claim.Value, out var pharmacyId))
            {
                return pharmacyId;
            }
            return Guid.Empty;
        }

        [HttpPost("create-intent")]
        public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentRequest request)
        {
            if (request == null || request.OrderId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid order ID provided." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pharmacyId = GetCurrentPharmacyId();

            // If Customer user without PharmacyId claim, look up the order to resolve its pharmacy context
            if (pharmacyId == Guid.Empty)
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && (o.CustomerId == userId || o.UserId == userId));

                if (order == null)
                {
                    return NotFound(new { message = "Order not found or access denied." });
                }

                pharmacyId = order.PharmacyId;
            }

            var result = await _paymentService.CreatePaymentIntentAsync(request.OrderId, pharmacyId);

            if (!result.Success)
            {
                if (result.IsGatewayError)
                {
                    return StatusCode(500, new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            // Expected JSON Response format: { clientSecret = "...", transactionId = "..." }
            return Ok(new
            {
                clientSecret = result.ClientSecret,
                transactionId = result.TransactionId
            });
        }
    }

    public class CreatePaymentIntentRequest
    {
        public Guid OrderId { get; set; }
    }
}