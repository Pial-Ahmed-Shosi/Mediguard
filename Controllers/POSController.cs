using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPOSService _posService;

        public POSController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPOSService posService)
        {
            _context = context;
            _userManager = userManager;
            _posService = posService;
        }

        // GET: /POS
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ──────────────────────────────────────────────────────────────
        // POST: /POS/Checkout   (Ticket 29A / 29B)
        // ──────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] POSCheckoutViewModel model)
        {
            // 1. Model State Validation
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid cart data submitted." });
            }

            // 2. Extract Tenant / User Context
            Guid pharmacyId = GetCurrentPharmacyId();
            string userId = _userManager.GetUserId(User) ?? string.Empty;

            // Fallback: if PharmacyId claim is missing, look it up from the database
            if (pharmacyId == Guid.Empty)
            {
                var currentUser = await GetCurrentUserWithPharmacyAsync();
                if (currentUser?.PharmacyId != null)
                {
                    pharmacyId = currentUser.PharmacyId.Value;
                }
                if (string.IsNullOrEmpty(userId))
                {
                    userId = currentUser?.Id ?? string.Empty;
                }
            }

            if (pharmacyId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Unable to determine pharmacy context." });
            }

            // 3. Execute Checkout Service
            try
            {
                var result = await _posService.ProcessCheckoutAsync(model, pharmacyId, userId);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                // 4. Return Success Response (Ticket 29A / 29B format)
                return Ok(new
                {
                    success = true,
                    orderId = result.OrderId,
                    receiptUrl = $"/POS/Receipt/{result.OrderId}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Server error: {ex.Message}"
                });
            }
        }

        // GET: /POS/Receipt/{id?}
        [HttpGet]
        public async Task<IActionResult> Receipt(
            Guid? id = null,
            string? receiptNumber = null,
            decimal? total = null,
            decimal? paid = null,
            string? paymentMethod = "CASH")
        {
            var user = await GetCurrentUserWithPharmacyAsync();
            var pharmacy = user?.Pharmacy;

            var model = new ReceiptViewModel();

            if (pharmacy != null)
            {
                model.PharmacyName = pharmacy.Name;
                model.PharmacyAddress = string.IsNullOrWhiteSpace(pharmacy.Address)
                    ? $"{pharmacy.City}, Medical District"
                    : $"{pharmacy.Address}, {pharmacy.City}";
                model.LicenseNumber = pharmacy.LicenseNumber;
                model.TaxId = $"TX-{pharmacy.LicenseNumber.Replace(" ", "").ToUpper()}";
            }

            model.CashierName = user?.FullName ?? (User.Identity?.Name ?? "Staff Cashier");
            model.TransactionDate = DateTime.Now;
            model.OrderType = "POS";

            // If an order ID is provided, load the full order for the receipt
            if (id.HasValue && id.Value != Guid.Empty)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Medicine)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Batch)
                    .FirstOrDefaultAsync(o => o.Id == id.Value);

                if (order != null)
                {
                    model.OrderId = order.Id;
                    model.ReceiptNumber = order.OrderNumber;
                    model.Subtotal = order.Subtotal;
                    model.TaxAmount = order.Tax;
                    model.DiscountAmount = order.Discount;
                    model.GrandTotal = order.TotalAmount;
                    model.PaymentMethod = order.PaymentMethod;

                    foreach (var oi in order.OrderItems)
                    {
                        model.Items.Add(new ReceiptItemViewModel
                        {
                            ItemName = oi.Medicine?.BrandName ?? "Unknown Item",
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            LineTotal = oi.TotalPrice,
                            BatchNumber = oi.Batch?.BatchNumber
                        });
                    }
                }
            }
            else
            {
                // Legacy query-string-based receipt (backward compatibility)
                if (!string.IsNullOrEmpty(receiptNumber))
                {
                    model.ReceiptNumber = receiptNumber;
                }

                if (total.HasValue && total.Value > 0)
                {
                    model.GrandTotal = total.Value;
                    model.Subtotal = Math.Round(total.Value / 1.05m, 2);
                    model.TaxAmount = Math.Round(total.Value - model.Subtotal, 2);
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    model.PaymentMethod = paymentMethod.ToUpperInvariant();
                }

                if (paid.HasValue && paid.Value > 0)
                {
                    model.PaidAmount = paid.Value;
                    model.ChangeAmount = Math.Max(0, paid.Value - model.GrandTotal);
                }
            }

            return View(model);
        }

        // ──────────────────────────────────────────────────────────────
        // Private Helpers
        // ──────────────────────────────────────────────────────────────

        private Guid GetCurrentPharmacyId()
        {
            var claim = User.FindFirst("PharmacyId") ?? User.FindFirst(ClaimTypes.GroupSid);
            if (claim != null && Guid.TryParse(claim.Value, out var pharmacyId))
            {
                return pharmacyId;
            }
            return Guid.Empty;
        }

        private async Task<ApplicationUser?> GetCurrentUserWithPharmacyAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _context.Users
                .Include(u => u.Pharmacy)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}

