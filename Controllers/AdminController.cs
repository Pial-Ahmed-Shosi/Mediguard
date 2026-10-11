using System;
using System.Linq;
using System.Security.Cryptography;
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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/MediPlusConfig
        [HttpGet]
        public async Task<IActionResult> MediPlusConfig(string? reason = null)
        {
            var user = await GetCurrentUserWithPharmacyAsync();
            var pharmacy = user?.Pharmacy;

            var model = new MediPlusConfigViewModel();

            if (pharmacy != null)
            {
                model.PharmacyId = pharmacy.Id;
                model.PharmacyName = pharmacy.Name;
                model.LicenseNumber = pharmacy.LicenseNumber;

                bool isActive = pharmacy.IsMediPlusActive && (pharmacy.MediPlusExpiresAt == null || pharmacy.MediPlusExpiresAt > DateTime.UtcNow);
                model.IsActive = isActive;
                model.CurrentTier = isActive ? "Medi+ B2B Tier" : "Free Tier";
                model.Status = isActive ? "Active" : (pharmacy.MediPlusExpiresAt.HasValue && pharmacy.MediPlusExpiresAt <= DateTime.UtcNow ? "Expired" : "Inactive");
                model.NextBillingDate = pharmacy.MediPlusExpiresAt ?? (isActive ? DateTime.UtcNow.AddMonths(1) : null);
                model.MonthlyFee = 49.00m;

                // Load reachable partner pharmacies
                var partners = await _context.Pharmacies
                    .Where(p => p.Id != pharmacy.Id)
                    .OrderBy(p => p.Name)
                    .Take(25)
                    .Select(p => new B2BPartnerPharmacyViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Domain = p.Domain,
                        City = p.City,
                        LicenseNumber = p.LicenseNumber,
                        IsMediPlusActive = p.IsMediPlusActive,
                        IsReachable = p.IsMediPlusActive,
                        MinimumOrderValue = 150.00m
                    })
                    .ToListAsync();

                model.PartnerPharmacies = partners;
            }

            if (!string.IsNullOrEmpty(reason) && reason == "subscription_required")
            {
                ViewBag.NoticeMessage = "Access Restricted: An active Medi+ B2B subscription is required to access wholesale inter-pharmacy features.";
            }

            return View(model);
        }

        // POST: /Admin/SaveMediPlusConfig
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMediPlusConfig([FromBody] SaveMediPlusConfigDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid configuration values." });
            }

            var user = await GetCurrentUserWithPharmacyAsync();
            var pharmacy = user?.Pharmacy;

            if (pharmacy == null)
            {
                return BadRequest(new { success = false, message = "Pharmacy profile not found." });
            }

            if (!pharmacy.IsMediPlusActive)
            {
                return BadRequest(new { success = false, message = "Cannot update B2B network settings on an inactive subscription." });
            }

            return Ok(new
            {
                success = true,
                message = "B2B connection settings successfully updated.",
                data = new
                {
                    dto.IsSupplierConnectionEnabled,
                    dto.MinimumWholesaleOrderValue,
                    dto.AutoAcceptB2BOrders
                }
            });
        }

        // POST: /Admin/SubscribeMediPlus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribeMediPlus()
        {
            var user = await GetCurrentUserWithPharmacyAsync();
            var pharmacy = user?.Pharmacy;

            if (pharmacy == null)
            {
                return BadRequest(new { success = false, message = "Pharmacy profile not found." });
            }

            pharmacy.IsMediPlusActive = true;
            pharmacy.MediPlusExpiresAt = DateTime.UtcNow.AddMonths(1);

            _context.Pharmacies.Update(pharmacy);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Welcome to Medi+ B2B! Your monthly subscription is now active.",
                tier = "Medi+ B2B Tier",
                status = "Active",
                nextBillingDate = pharmacy.MediPlusExpiresAt?.ToString("MMMM dd, yyyy")
            });
        }

        // POST: /Admin/GenerateApiKey
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateApiKey()
        {
            var user = await GetCurrentUserWithPharmacyAsync();
            var pharmacy = user?.Pharmacy;

            if (pharmacy == null)
            {
                return BadRequest(new { success = false, message = "Pharmacy profile not found." });
            }

            if (!pharmacy.IsMediPlusActive)
            {
                return BadRequest(new { success = false, message = "Medi+ B2B subscription must be active to generate API keys." });
            }

            var keyBytes = new byte[16];
            var secretBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
                rng.GetBytes(secretBytes);
            }

            string apiKey = $"mp_live_{Convert.ToHexString(keyBytes).ToLowerInvariant()}";
            string apiSecret = $"sec_{Convert.ToHexString(secretBytes).ToLowerInvariant()}";

            return Ok(new
            {
                success = true,
                message = "New B2B API Key generated successfully. Please store your secret safely.",
                apiKey,
                apiSecret,
                generatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm UTC")
            });
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
