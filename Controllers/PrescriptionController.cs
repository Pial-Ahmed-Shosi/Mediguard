using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Filters;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class PrescriptionController : Controller
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ApplicationDbContext _context;

        public PrescriptionController(
            IPrescriptionService prescriptionService,
            IFileStorageService fileStorageService,
            ApplicationDbContext context)
        {
            _prescriptionService = prescriptionService;
            _fileStorageService = fileStorageService;
            _context = context;
        }

        [HttpGet("Upload")]
        public async Task<IActionResult> Upload([FromQuery] Guid? orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid targetOrderId = orderId ?? Guid.Empty;

            var model = new PrescriptionUploadViewModel
            {
                OrderId = targetOrderId
            };

            return View("~/Views/Prescription/Upload.cshtml", model);
        }

        [HttpPost("Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(PrescriptionUploadViewModel model, IFormFile? prescriptionFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var file = prescriptionFile ?? Request.Form.Files["PrescriptionFile"] ?? Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("PrescriptionFile", "Please select a valid image (.jpg, .png) or PDF prescription file.");
                return View("~/Views/Prescription/Upload.cshtml", model);
            }

            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId);
                Guid pharmacyId = order?.PharmacyId ?? await _context.Pharmacies.Select(p => p.Id).FirstOrDefaultAsync();

                if (pharmacyId == Guid.Empty)
                {
                    pharmacyId = Guid.NewGuid();
                }

                Guid targetOrderId = model.OrderId != Guid.Empty ? model.OrderId : Guid.NewGuid();
                string relativePath = await _fileStorageService.UploadPrescriptionAsync(file, pharmacyId, targetOrderId);

                // Associate CustomerId to prescription record
                var rx = await _context.Prescriptions.FirstOrDefaultAsync(p => p.FilePath == relativePath);
                if (rx != null)
                {
                    rx.CustomerId = userId;
                    if (order != null)
                    {
                        rx.OrderId = order.Id;
                        rx.PharmacyId = order.PharmacyId;
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Prescription uploaded successfully. A pharmacist will review it shortly.";
                return RedirectToAction("UserProfile", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Upload failed: " + ex.Message);
                return View("~/Views/Prescription/Upload.cshtml", model);
            }
        }

        [HttpPost("Approve")]
        [HasPermission("prescription.verify")]
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