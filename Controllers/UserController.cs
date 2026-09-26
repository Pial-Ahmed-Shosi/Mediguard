using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using MediGuard.Filters; // 1. Added namespace for custom filters

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
        // 1. Staff List Page
        // -------------------------------------------------------------------------
        [HttpGet]
        [HasPermission("staff.view")] // Requires 'staff.view' permission
        public IActionResult Index()
        {
            return View();
        }

        // -------------------------------------------------------------------------
        // 2. Fetch Staff Table (AJAX)
        // -------------------------------------------------------------------------
        [HttpGet]
        [HasPermission("staff.view")] // Requires 'staff.view' permission
        public async Task<IActionResult> GetStaffList(string search = "", string roleFilter = "", int page = 1)
        {
            Guid pharmacyId = GetCurrentTenantId();
            var model = await _userService.GetStaffListAsync(pharmacyId, search, roleFilter, page);

            return PartialView("_UserListTable", model);
        }

        // -------------------------------------------------------------------------
        // 3. Create New Staff Account
        // -------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("staff.create")] // Requires 'staff.create' permission
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
        [HasPermission("staff.permissions")] // Requires 'staff.permissions' permission
        [RequiresMediPlus]                   // Requires ACTIVE Medi+ Subscription
        public async Task<IActionResult> UpdatePermissions([FromBody] UpdatePermissionsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid permission update payload." });
            }

            Guid pharmacyId = GetCurrentTenantId();
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

            throw new UnauthorizedAccessException("Current user tenant identity (PharmacyId) is missing or invalid in context.");
        }
    }
}