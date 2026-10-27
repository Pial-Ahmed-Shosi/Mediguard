using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using MediGuard.Filters;

namespace MediGuard.Controllers
{
    [Authorize] // Enforces standard login for all actions in this controller
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        public UserController(IUserService userService, ApplicationDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // Staff List Page (Initial Load)
        [HttpGet]
        [HasPermission("staff.view")]
        public async Task<IActionResult> Index()
        {
            Guid pharmacyId = await GetCurrentTenantIdAsync();
            var model = await _userService.GetStaffListAsync(pharmacyId, search: "", roleFilter: "", page: 1);
            return View(model);
        }

        // Fetch Staff Table (AJAX Search & Filter)
        [HttpGet]
        [HasPermission("staff.view")]
        public async Task<IActionResult> GetStaffList(string search = "", string roleFilter = "", int page = 1)
        {
            Guid pharmacyId = await GetCurrentTenantIdAsync();
            var model = await _userService.GetStaffListAsync(pharmacyId, search, roleFilter, page);

            return PartialView("_UserListTable", model.Users);
        }

        // Alias endpoint used by user-management.js for live search and filter tabs
        [HttpGet]
        [HasPermission("staff.view")]
        public async Task<IActionResult> GetFilteredUsers(string searchTerm = "", string role = "All", string status = "All", int page = 1)
        {
            Guid pharmacyId = await GetCurrentTenantIdAsync();
            string roleFilter = (role == "All") ? "" : role;
            var model = await _userService.GetStaffListAsync(pharmacyId, searchTerm, roleFilter, page);

            return PartialView("_UserListTable", model.Users);
        }

        // Form POST: Add Staff from Offcanvas
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("staff.create")]
        public async Task<IActionResult> AddStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Index));
            }

            Guid pharmacyId = await GetCurrentTenantIdAsync();
            var (success, message, tempPassword) = await _userService.CreateStaffAsync(pharmacyId, model);

            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = $"{message} (Temporary password: {tempPassword})";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX POST: Create New Staff Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("staff.create")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data provided." });
            }

            Guid pharmacyId = await GetCurrentTenantIdAsync();
            var (success, message, tempPassword) = await _userService.CreateStaffAsync(pharmacyId, model);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message, tempPassword });
        }

        // 4. Update Staff Permissions
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("staff.permissions")]
        [RequiresMediPlus]
        public async Task<IActionResult> UpdatePermissions([FromBody] UpdatePermissionsViewModel model)
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(new { success = false, message = "Invalid permission update payload." });
            }

            Guid pharmacyId = await GetCurrentTenantIdAsync();

            var (success, message) = await _userService.UpdatePermissionsAsync(pharmacyId, model.UserId, model.PermissionIds);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }

        // Helper Method to Retrieve Current Tenant (Pharmacy) ID with resilient fallback
        private async Task<Guid> GetCurrentTenantIdAsync()
        {
            if (HttpContext.Items["PharmacyId"] is Guid pharmacyId && pharmacyId != Guid.Empty)
            {
                return pharmacyId;
            }

            var claimVal = User.FindFirst("PharmacyId")?.Value;
            if (Guid.TryParse(claimVal, out Guid claimPharmacyId) && claimPharmacyId != Guid.Empty)
            {
                return claimPharmacyId;
            }

            var defaultPharmacy = await _context.Pharmacies.FirstOrDefaultAsync();
            if (defaultPharmacy != null)
            {
                return defaultPharmacy.Id;
            }

            throw new UnauthorizedAccessException("Current user tenant identity (PharmacyId) is missing or invalid in context.");
        }
    }
}