using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDashboardService _dashboardService;
        private readonly IPOSService _posService;
        private readonly IPrescriptionService _prescriptionService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDashboardService dashboardService,
            IPOSService posService,
            IPrescriptionService prescriptionService)
        {
            _context = context;
            _userManager = userManager;
            _dashboardService = dashboardService;
            _posService = posService;
            _prescriptionService = prescriptionService;
        }

        // GET: /Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? userIdStr = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Pharmacy)
                .FirstOrDefaultAsync(u => u.Id == userIdStr);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string role = user.Role ?? User.FindFirstValue(ClaimTypes.Role) ?? "Staff";

            // If Customer user, redirect to Customer Profile & Dashboard
            if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Regular User", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("UserProfile", "Account");
            }

            // Resolve Pharmacy context for Manager / Staff
            Guid pharmacyId = user.PharmacyId ?? Guid.Empty;
            if (pharmacyId == Guid.Empty)
            {
                var claimVal = User.FindFirst("PharmacyId")?.Value;
                if (!Guid.TryParse(claimVal, out pharmacyId) || pharmacyId == Guid.Empty)
                {
                    // Fallback to first available pharmacy branch in DB
                    var defaultPharmacy = await _context.Pharmacies.FirstOrDefaultAsync();
                    if (defaultPharmacy != null)
                    {
                        pharmacyId = defaultPharmacy.Id;
                        user.PharmacyId = pharmacyId;
                        await _userManager.UpdateAsync(user);
                    }
                    else
                    {
                        // Create default pharmacy branch
                        var newPharmacy = new Pharmacy
                        {
                            Id = Guid.NewGuid(),
                            Name = "MediGuard Central Pharmacy",
                            LicenseNumber = "MG-CENTRAL-001",
                            Domain = "mediguard.com",
                            Address = "12/A Medical District, Dhanmondi",
                            City = "Dhaka",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Pharmacies.Add(newPharmacy);
                        await _context.SaveChangesAsync();
                        pharmacyId = newPharmacy.Id;
                        user.PharmacyId = pharmacyId;
                        await _userManager.UpdateAsync(user);
                    }
                }
            }

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyId);
            if (pharmacy == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var tomorrowUtc = todayUtc.AddDays(1);
            var startOfMonthUtc = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc), DateTimeKind.Utc);
            var thirtyDaysFromNow = todayUtc.AddDays(30);

            // 1. Strict Scope: Branch Inventory Items (Batches)
            var batches = await _context.Batches
                .AsNoTracking()
                .Include(b => b.Medicine)
                .Where(b => b.PharmacyId == pharmacyId)
                .OrderBy(b => b.Medicine != null ? b.Medicine.BrandName : "")
                .ToListAsync();

            var inventoryDtos = batches.Select(b => new InventoryItemDto
            {
                BatchId = b.Id,
                MedicineId = b.MedicineId,
                MedicineName = b.Medicine?.BrandName ?? "Unnamed Medicine",
                GenericName = b.Medicine?.GenericName ?? "N/A",
                BatchNumber = b.BatchNumber,
                Quantity = b.Quantity,
                LowStockThreshold = b.LowStockThreshold > 0 ? b.LowStockThreshold : 10,
                PurchasePrice = b.PurchasePrice,
                SellingPrice = b.SellingPrice,
                ManufacturingDate = b.ManufacturingDate,
                ExpiryDate = b.ExpiryDate,
                Status = b.Status
            }).ToList();

            var expiringBatches = batches
                .Where(b => b.ExpiryDate <= thirtyDaysFromNow && b.ExpiryDate >= todayUtc && b.Quantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .ToList();

            var lowStockBatches = batches
                .Where(b => b.Quantity <= (b.LowStockThreshold > 0 ? b.LowStockThreshold : 10))
                .OrderBy(b => b.Quantity)
                .ToList();

            var branchMedicines = await _context.Medicines
                .AsNoTracking()
                .Where(m => m.PharmacyId == pharmacyId && m.IsActive)
                .OrderBy(m => m.BrandName)
                .ToListAsync();

            // 2. Incoming Online Orders Queue for this branch
            var incomingOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.AssignedDeliveryman)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Medicine)
                .Where(o => o.PharmacyId == pharmacyId && o.OrderType == "B2C_ONLINE")
                .OrderByDescending(o => o.CreatedAt)
                .Take(30)
                .ToListAsync();

            // 3. Pending Prescriptions for this branch
            var pendingPrescriptions = await _context.Prescriptions
                .AsNoTracking()
                .Include(p => p.Order)
                .Include(p => p.Customer)
                .Where(p => p.PharmacyId == pharmacyId && p.Status == "PENDING_VERIFICATION")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 4. Active Delivery Riders for this branch
            var riders = await _context.Users
                .AsNoTracking()
                .Where(u => u.PharmacyId == pharmacyId && u.Role == "Deliveryman" && u.IsActive)
                .ToListAsync();

            // 5. Offline Counter Sales (Today & Month)
            var todaySales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.PharmacyId == pharmacyId && s.CreatedAt >= todayUtc && s.CreatedAt < tomorrowUtc)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            decimal dailyDrawerTotal = todaySales.Sum(s => s.TotalAmount);

            var monthlySalesSum = await _context.Sales
                .Where(s => s.PharmacyId == pharmacyId && s.CreatedAt >= startOfMonthUtc)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var monthlyPaidOrdersSum = await _context.Orders
                .Where(o => o.PharmacyId == pharmacyId && o.CreatedAt >= startOfMonthUtc && o.PaymentStatus == "PAID")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            decimal monthlyRevenueTotal = monthlySalesSum + monthlyPaidOrdersSum;

            decimal prepaidOnlineTotal = incomingOrders
                .Where(o => o.PaymentStatus == "PAID")
                .Sum(o => o.TotalAmount);

            decimal pendingCodTotal = incomingOrders
                .Where(o => o.PaymentMethod == "COD" && o.PaymentStatus != "PAID")
                .Sum(o => o.TotalAmount);

            int totalOnlineOrdersMonth = await _context.Orders
                .CountAsync(o => o.PharmacyId == pharmacyId && o.CreatedAt >= startOfMonthUtc);

            int totalPosSalesMonth = await _context.Sales
                .CountAsync(s => s.PharmacyId == pharmacyId && s.CreatedAt >= startOfMonthUtc);

            var viewModel = new PharmacyManagerDashboardViewModel
            {
                ManagerUser = user,
                Pharmacy = pharmacy,
                IsPharmacyOpen = pharmacy.IsOpen,
                DailyDrawerTotal = dailyDrawerTotal,
                MonthlyRevenueTotal = monthlyRevenueTotal,
                PrepaidOnlineTotal = prepaidOnlineTotal,
                PendingCodTotal = pendingCodTotal,
                TotalOnlineOrdersMonth = totalOnlineOrdersMonth,
                TotalPosSalesMonth = totalPosSalesMonth,
                BranchInventory = inventoryDtos,
                ExpiringBatches = expiringBatches,
                LowStockBatches = lowStockBatches,
                BranchMedicines = branchMedicines,
                IncomingOrders = incomingOrders,
                PendingPrescriptions = pendingPrescriptions,
                ActiveDeliveryRiders = riders,
                TodayOfflineSales = todaySales
            };

            return View("~/Views/Dashboard/Index.cshtml", viewModel);
        }

        // POST: /Dashboard/ToggleOperatingStatus
        [HttpPost]
        public async Task<IActionResult> ToggleOperatingStatus([FromBody] ToggleOperatingStatusRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.PharmacyId == null) return Unauthorized(new { message = "Branch context missing." });

            PharmacyOperatingStatusStore.SetBranchOpen(user.PharmacyId.Value, request.IsOpen);
            return Ok(new { success = true, isOpen = request.IsOpen });
        }

        // POST: /Dashboard/RestockBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestockBatch(RestockBatchRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.PharmacyId == null) return RedirectToAction("Login", "Account");

            Guid pharmacyId = user.PharmacyId.Value;

            if (request.QuantityAdded <= 0)
            {
                TempData["ErrorMessage"] = "Quantity added must be greater than zero.";
                return RedirectToAction(nameof(Index));
            }

            Medicine? medicine = null;
            if (request.MedicineId.HasValue && request.MedicineId.Value != Guid.Empty)
            {
                medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.Id == request.MedicineId.Value && m.PharmacyId == pharmacyId);
            }

            if (medicine == null && !string.IsNullOrWhiteSpace(request.MedicineName))
            {
                medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.PharmacyId == pharmacyId && m.BrandName.ToLower() == request.MedicineName.Trim().ToLower());
                if (medicine == null)
                {
                    medicine = new Medicine
                    {
                        Id = Guid.NewGuid(),
                        PharmacyId = pharmacyId,
                        BrandName = request.MedicineName.Trim(),
                        GenericName = !string.IsNullOrWhiteSpace(request.GenericName) ? request.GenericName.Trim() : request.MedicineName.Trim(),
                        Price = request.RetailUnitPrice,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Medicines.Add(medicine);
                    await _context.SaveChangesAsync();
                }
            }

            if (medicine == null)
            {
                TempData["ErrorMessage"] = "Please select or enter a valid medicine name.";
                return RedirectToAction(nameof(Index));
            }

            // Find existing batch or create a new one
            string cleanBatch = request.BatchNumber.Trim().ToUpperInvariant();
            var existingBatch = await _context.Batches.FirstOrDefaultAsync(b => b.PharmacyId == pharmacyId && b.MedicineId == medicine.Id && b.BatchNumber == cleanBatch);

            if (existingBatch != null)
            {
                existingBatch.Quantity += request.QuantityAdded;
                existingBatch.RemainingQuantity += request.QuantityAdded;
                existingBatch.PurchasePrice = request.PurchaseCost > 0 ? request.PurchaseCost : existingBatch.PurchasePrice;
                existingBatch.SellingPrice = request.RetailUnitPrice > 0 ? request.RetailUnitPrice : existingBatch.SellingPrice;
                if (request.ExpiryDate > DateTime.MinValue) existingBatch.ExpiryDate = DateTime.SpecifyKind(request.ExpiryDate, DateTimeKind.Utc);
                existingBatch.Status = "ACTIVE";
                existingBatch.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newBatch = new Batch
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = pharmacyId,
                    MedicineId = medicine.Id,
                    BatchNumber = cleanBatch,
                    Quantity = request.QuantityAdded,
                    RemainingQuantity = request.QuantityAdded,
                    PurchasePrice = request.PurchaseCost,
                    SellingPrice = request.RetailUnitPrice,
                    LowStockThreshold = 10,
                    ManufacturingDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                    ExpiryDate = request.ExpiryDate > DateTime.MinValue ? DateTime.SpecifyKind(request.ExpiryDate, DateTimeKind.Utc) : DateTime.UtcNow.AddYears(2),
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Batches.Add(newBatch);
            }

            // Keep medicine price updated
            if (request.RetailUnitPrice > 0)
            {
                medicine.Price = request.RetailUnitPrice;
            }

            await _context.SaveChangesAsync();
            _dashboardService.InvalidateDashboardCache(Guid.Parse(user.Id), pharmacyId);

            TempData["SuccessMessage"] = $"Stock for '{medicine.BrandName}' (Batch #{cleanBatch}) increased by {request.QuantityAdded} units.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/UpdateOrderStatus
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.PharmacyId == null) return Unauthorized(new { message = "Branch context missing." });

            Guid pharmacyId = user.PharmacyId.Value;
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.PharmacyId == pharmacyId);

            if (order == null) return NotFound(new { message = "Order not found in this pharmacy branch." });

            string action = (request.Action ?? "").ToUpperInvariant();

            switch (action)
            {
                case "ACCEPT":
                    order.Status = "APPROVED";
                    break;
                case "PREPARE":
                    order.Status = "IN_PREPARATION";
                    order.DeliveryStatus = "IN_PREPARATION";
                    break;
                case "READY_PICKUP":
                    order.Status = "READY_FOR_PICKUP";
                    order.DeliveryStatus = "READY_FOR_PICKUP";
                    break;
                case "ASSIGN_RIDER":
                    if (string.IsNullOrWhiteSpace(request.DeliverymanId))
                    {
                        return BadRequest(new { message = "Please select a delivery rider." });
                    }
                    order.AssignedDeliverymanId = request.DeliverymanId;
                    order.DeliverymanId = request.DeliverymanId;
                    order.Status = "IN_TRANSIT";
                    order.DeliveryStatus = "IN_TRANSIT";
                    break;
                case "COMPLETE":
                    order.Status = "COMPLETED";
                    order.DeliveryStatus = "COMPLETED";
                    order.PaymentStatus = "PAID";
                    break;
                default:
                    return BadRequest(new { message = "Invalid action." });
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newStatus = order.Status, newDeliveryStatus = order.DeliveryStatus });
        }

        // POST: /Dashboard/ReviewPrescription
        [HttpPost]
        public async Task<IActionResult> ReviewPrescription([FromBody] ReviewPrescriptionRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.PharmacyId == null) return Unauthorized(new { message = "Branch context missing." });

            var rx = await _context.Prescriptions.FirstOrDefaultAsync(p => p.Id == request.PrescriptionId && p.PharmacyId == user.PharmacyId.Value);
            if (rx == null) return NotFound(new { message = "Prescription record not found." });

            if (string.Equals(request.Decision, "APPROVE", StringComparison.OrdinalIgnoreCase))
            {
                rx.Status = "APPROVED";
                rx.VerifiedById = user.Id;
                rx.RejectionReason = null;
            }
            else
            {
                rx.Status = "REJECTED";
                rx.VerifiedById = user.Id;
                rx.RejectionReason = string.IsNullOrWhiteSpace(request.Notes) ? "Prescription does not meet requirements." : request.Notes.Trim();
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = rx.Status });
        }
    }

    public class ToggleOperatingStatusRequest
    {
        public bool IsOpen { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public Guid OrderId { get; set; }
        public string Action { get; set; } = string.Empty; // ACCEPT, PREPARE, READY_PICKUP, ASSIGN_RIDER, COMPLETE
        public string? DeliverymanId { get; set; }
    }

    public class ReviewPrescriptionRequest
    {
        public Guid PrescriptionId { get; set; }
        public string Decision { get; set; } = "APPROVE"; // APPROVE or REJECT
        public string? Notes { get; set; }
    }
}