using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Filters;
using MediGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediGuard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    [HasPermission("prescription.verify")]
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        [HttpPost("Approve")]
        public async Task<IActionResult> Approve([FromBody] ApprovePrescriptionRequest request)
        {
            if (request == null || request.PrescriptionId == Guid.Empty)
            {
                return BadRequest(new { message = "A valid PrescriptionId is required." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User identity could not be resolved." });
            }

            try
            {
                await _prescriptionService.ApprovePrescriptionAsync(request.PrescriptionId, userId);
                return Ok(new { message = "Prescription approved successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("Reject")]
        public async Task<IActionResult> Reject([FromBody] RejectPrescriptionRequest request)
        {
            if (request == null || request.PrescriptionId == Guid.Empty)
            {
                return BadRequest(new { message = "A valid PrescriptionId is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "A non-empty rejection reason is required." });
            }

            try
            {
                await _prescriptionService.RejectPrescriptionAsync(request.PrescriptionId, request.Reason);
                return Ok(new { message = "Prescription rejected successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class ApprovePrescriptionRequest
    {
        public Guid PrescriptionId { get; set; }
    }

    public class RejectPrescriptionRequest
    {
        public Guid PrescriptionId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}