using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
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

            var pharmacyId = GetCurrentPharmacyId();
            if (pharmacyId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid pharmacy context." });
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