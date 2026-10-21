using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Filters;
using MediGuard.Models;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediGuard.Controllers
{
    [Authorize]
    public class PrescriptionController : Controller
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrescriptionController> _logger;

        public PrescriptionController(
            IPrescriptionService prescriptionService,
            IFileStorageService fileStorageService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PrescriptionController> logger)
        {
            _prescriptionService = prescriptionService;
            _fileStorageService = fileStorageService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
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

        // ==========================================
        // Customer Prescription Upload Views (Ticket 33)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Upload(Guid orderId)
        {
            var pharmacyId = GetCurrentPharmacyId();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.PharmacyId == pharmacyId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            var viewModel = new PrescriptionUploadViewModel
            {
                OrderId = orderId,
                OrderNumber = order.Id.ToString().Substring(0, 8).ToUpper()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(PrescriptionUploadViewModel model)
        {
            var pharmacyId = GetCurrentPharmacyId();

            if (!ModelState.IsValid || model.PrescriptionFile == null)
            {
                return View(model);
            }

            try
            {
                // UploadPrescriptionAsync returns the relative file path string (Ticket 38)
                string filePath = await _fileStorageService.UploadPrescriptionAsync(model.PrescriptionFile, pharmacyId, model.OrderId);

                if (string.IsNullOrEmpty(filePath))
                {
                    ModelState.AddModelError("PrescriptionFile", "Failed to upload prescription. Please try again.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Prescription uploaded successfully. A pharmacist will review your order.";
                return RedirectToAction("Details", "Order", new { id = model.OrderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("PrescriptionFile", ex.Message ?? "An error occurred while uploading your prescription.");
                return View(model);
            }
        }

        // ==========================================
        // Pharmacist Prescription Review View (Ticket 34)
        // ==========================================

        [HttpGet]
        [HasPermission("prescription.verify")]
        public async Task<IActionResult> Review(Guid id)
        {
            var pharmacyId = GetCurrentPharmacyId();

            var prescription = await _context.Prescriptions
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .Include(p => p.Order)
                    .ThenInclude(o => o!.OrderItems)
                        .ThenInclude(oi => oi.Medicine)
                .FirstOrDefaultAsync(p => p.Id == id && p.PharmacyId == pharmacyId);

            if (prescription == null || prescription.Order == null)
            {
                return NotFound("Prescription or linked order not found.");
            }

            var order = prescription.Order;
            var storedPath = prescription.FilePath ?? string.Empty;
            var isPdf = string.Equals(Path.GetExtension(storedPath), ".pdf", StringComparison.OrdinalIgnoreCase);

            var model = new PrescriptionReviewViewModel
            {
                PrescriptionId = prescription.Id,
                OrderId = prescription.OrderId ?? order.Id,
                OrderNumber = order.Id.ToString().Substring(0, 8).ToUpper(),
                FilePath = storedPath,
                FileUrl = ResolveDocumentUrl(storedPath),
                FileType = isPdf ? "pdf" : "image",
                Status = prescription.Status ?? "PENDING",
                RejectionReason = prescription.RejectionReason,
                UploadedAt = prescription.CreatedAt,
                CustomerName = order.Customer?.FullName ?? "Walk-in / Guest Customer",
                CustomerEmail = order.Customer?.Email ?? "N/A",
                CustomerPhone = order.Customer?.PhoneNumber ?? "N/A",
                TotalAmount = order.TotalAmount,
                Items = (order.OrderItems ?? Enumerable.Empty<OrderItem>()).Select(oi => new PrescriptionOrderItemViewModel
                {
                    BrandName = oi.Medicine?.BrandName ?? "Unknown Medicine",
                    GenericName = oi.Medicine?.GenericName ?? string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    RequiresPrescription = oi.Medicine?.RequiresPrescription ?? false
                }).ToList()
            };

            return View(model);
        }

        // ==========================================
        // Pharmacist Verification API Endpoints (Ticket 39)
        // ==========================================

        [HttpPost("Prescription/Approve")]
        [ValidateAntiForgeryToken]
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