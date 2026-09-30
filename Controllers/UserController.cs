using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using MediGuard.Filters;

namespace MediGuard.Controllers
{
    [Authorize] // Enforces standard login for all actions in this controller
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // -------------------------------------------------------------------------
        // 1. Staff List Page (Initial Load)
        // -------------------------------------------------------------------------
        [HttpGet]
        [HasPermission("staff.view")]
        public async Task<IActionResult> Index()
        {
            Guid pharmacyId = GetCurrentTenantId();
            var model = await _userService.GetStaffListAsync(pharmacyId, search: "", roleFilter: "", page: 1);
            return View(model);
        }

        // -------------------------------------------------------------------------
        // 2. Fetch Staff Table (AJAX Search & Filter)
        // -------------------------------------------------------------------------
        [HttpGet]
        [HasPermission("staff.view")]
        public async Task<IActionResult> GetStaffList(string search = "", string roleFilter = "", int page = 1)
        {
            Guid pharmacyId = GetCurrentTenantId();
            var model = await _userService.GetStaffListAsync(pharmacyId, search, roleFilter, page);

            return PartialView("_UserListTable", model);
        }

        /// <summary>
        /// Alias endpoint used by user-management.js for live search and filter tabs
        /// </summary>
        [HttpGet]
        [HasPermission("staff.view")]
        public async Task<IActionResult> GetFilteredUsers(string searchTerm = "", string role = "All", string status = "All", int page = 1)
        {
            Guid pharmacyId = GetCurrentTenantId();
            string roleFilter = (role == "All") ? "" : role;
            var model = await _userService.GetStaffListAsync(pharmacyId, searchTerm, roleFilter, page);

            return PartialView("_UserListTable", model);
        }

        // -------------------------------------------------------------------------
        // 3. Create New Staff Account
        // -------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("staff.create")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data provided." });
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message, tempPassword) = await _userService.CreateStaffAsync(pharmacyId, model);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message, tempPassword });
        }

        // -------------------------------------------------------------------------
        // 4. Update Staff Permissions
        // -------------------------------------------------------------------------
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

            Guid pharmacyId = GetCurrentTenantId();

            // Passes List<int> PermissionIds to IUserService.UpdatePermissionsAsync
            var (success, message) = await _userService.UpdatePermissionsAsync(pharmacyId, model.UserId, model.PermissionIds);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }

        // -------------------------------------------------------------------------
        // Helper: Extract Tenant ID from Middleware Context
        // -------------------------------------------------------------------------
        private Guid GetCurrentTenantId()
        {
            // Reads PharmacyId populated by TenantRbacMiddleware
            if (HttpContext.Items["PharmacyId"] is Guid pharmacyId)
            {
                return pharmacyId;
            }
            // hello
            throw new UnauthorizedAccessException("Current user tenant identity (PharmacyId) is missing or invalid in context.");
        }
    }
}