using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize(Roles = "Manager")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffList(string search = "", string roleFilter = "", int page = 1)
        {
            Guid pharmacyId = GetCurrentTenantId();
            var model = await _userService.GetStaffListAsync(pharmacyId, search, roleFilter, page);

            return PartialView("_UserListTable", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        private Guid GetCurrentTenantId()
        {
            var claim = User.FindFirst("PharmacyId") ?? User.FindFirst("pharmacy_id");
            if (claim != null && Guid.TryParse(claim.Value, out Guid pharmacyId))
            {
                return pharmacyId;
            }

            throw new UnauthorizedAccessException("Current user tenant identity (PharmacyId) is invalid or missing.");
        }
    }
}