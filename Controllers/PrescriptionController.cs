using System;
using System.Collections.Generic;
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

namespace MediGuard.Controllers
{
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

            var model = new PrescriptionReviewViewModel
            {
                PrescriptionId = prescription.Id,
                OrderId = prescription.OrderId ?? order.Id,
                OrderNumber = order.Id.ToString().Substring(0, 8).ToUpper(),
                FilePath = prescription.FilePath ?? string.Empty,
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

        [HttpPost("Prescription/Reject")]
        [HasPermission("prescription.verify")]
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