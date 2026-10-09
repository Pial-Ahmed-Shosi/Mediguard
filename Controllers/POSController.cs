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

namespace MediGuard.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public POSController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /POS
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /POS/Receipt
        [HttpGet]
        public async Task<IActionResult> Receipt(
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

            return View(model);
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
