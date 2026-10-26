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
                return BadRequest(new { success = false, message = "A valid PrescriptionId is required." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User identity could not be resolved." });
            }

            var (found, status) = await GetOwnedPrescriptionStatusAsync(request.PrescriptionId);
            if (!found)
            {
                return NotFound(new { success = false, message = "Prescription not found." });
            }
            if (IsDecided(status))
            {
                return Conflict(new { success = false, message = $"This prescription has already been {status!.ToLowerInvariant()}." });
            }

            try
            {
                await _prescriptionService.ApprovePrescriptionAsync(request.PrescriptionId, userId);
                return Ok(new { success = true, message = "Prescription approved successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve prescription {PrescriptionId}", request.PrescriptionId);
                return StatusCode(500, new { success = false, message = "Unable to approve the prescription right now. Please try again." });
            }
        }

        [HttpPost("Prescription/Reject")]
        [ValidateAntiForgeryToken]
        [HasPermission("prescription.verify")]
        public async Task<IActionResult> Reject([FromBody] RejectPrescriptionRequest request)
        {
            if (request == null || request.PrescriptionId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "A valid PrescriptionId is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { success = false, message = "A non-empty rejection reason is required." });
            }

            var (found, status) = await GetOwnedPrescriptionStatusAsync(request.PrescriptionId);
            if (!found)
            {
                return NotFound(new { success = false, message = "Prescription not found." });
            }
            if (IsDecided(status))
            {
                return Conflict(new { success = false, message = $"This prescription has already been {status!.ToLowerInvariant()}." });
            }

            try
            {
                await _prescriptionService.RejectPrescriptionAsync(request.PrescriptionId, request.Reason.Trim());
                return Ok(new { success = true, message = "Prescription rejected successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject prescription {PrescriptionId}", request.PrescriptionId);
                return StatusCode(500, new { success = false, message = "Unable to reject the prescription right now. Please try again." });
            }
        }

        // ==========================================
        // Helpers
        // ==========================================

        // Tenant check: only prescriptions of the current pharmacy are visible
        private async Task<(bool Found, string? Status)> GetOwnedPrescriptionStatusAsync(Guid prescriptionId)
        {
            var pharmacyId = GetCurrentPharmacyId();

            var row = await _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.Id == prescriptionId && p.PharmacyId == pharmacyId)
                .Select(p => new { p.Status })
                .FirstOrDefaultAsync();

            return (row != null, row?.Status);
        }

        private static bool IsDecided(string? status) =>
            string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase);

        // Turns the stored file path into a URL the browser can load.
        //  1. Already an http(s) URL            -> used as is
        //  2. "Supabase:Url" is configured      -> Supabase public-bucket URL
        //  3. otherwise                         -> site-relative path (file under wwwroot)
        private string? ResolveDocumentUrl(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            var path = storedPath.Trim();

            if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                return path;
            }

            var normalized = path.Replace('\\', '/').TrimStart('/');
            var encoded = string.Join("/", normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(Uri.EscapeDataString));

            var supabaseUrl = _configuration["Supabase:Url"];
            if (!string.IsNullOrWhiteSpace(supabaseUrl))
            {
                var bucket = _configuration["Supabase:PrescriptionBucket"]
                             ?? _configuration["Supabase:Bucket"]
                             ?? "prescriptions";

                return $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucket}/{encoded}";
            }

            return "/" + encoded;
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