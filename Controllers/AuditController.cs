using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize(Roles = "Manager")]
    public class AuditController : Controller
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ApplicationDbContext _context;

        public AuditController(
            IAuditLogService auditLogService,
            ApplicationDbContext context)
        {
            _auditLogService = auditLogService;
            _context = context;
        }

        private Guid GetCurrentTenantId()
        {
            if (HttpContext.Items["PharmacyId"] is Guid pharmacyId && pharmacyId != Guid.Empty)
            {
                return pharmacyId;
            }

            var claim = User.FindFirst("PharmacyId") ?? User.FindFirst(ClaimTypes.GroupSid);
            if (claim != null && Guid.TryParse(claim.Value, out var claimId) && claimId != Guid.Empty)
            {
                return claimId;
            }

            return Guid.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            string? userId,
            string? actionType)
        {
            var pharmacyId = GetCurrentTenantId();
            if (pharmacyId == Guid.Empty)
            {
                return Unauthorized();
            }

            var model = new AuditLogPageViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedUserId = userId,
                SelectedAction = actionType
            };

            // Load audit entries scoped strictly to the current authenticated tenant
            var logs = await _auditLogService.GetAuditLogsAsync(
                pharmacyId, startDate, endDate, userId, actionType);

            model.Entries = logs.Select(l => new AuditLogEntryViewModel
            {
                Id = l.Id.ToString(),
                Timestamp = l.Timestamp,
                UserId = l.UserId ?? string.Empty,
                UserName = l.User?.FullName ?? (string.IsNullOrEmpty(l.UserId) ? "System" : l.UserId),
                UserRole = l.User?.Role ?? "User",
                ActionType = l.Action,
                EntityName = l.EntityName,
                IpAddress = l.IpAddress ?? "N/A",
                OldValuesJson = l.OldValuesJson,
                NewValuesJson = l.NewValuesJson
            }).ToList();

            // Load tenant users for filter options
            model.Users = await _context.Users
                .AsNoTracking()
                .Where(u => u.PharmacyId == pharmacyId)
                .OrderBy(u => u.FullName)
                .Select(u => new AuditUserOptionViewModel
                {
                    UserId = u.Id,
                    DisplayName = string.IsNullOrEmpty(u.FullName) ? (u.UserName ?? u.Id) : $"{u.FullName} ({u.Role})"
                })
                .ToListAsync();

            return View(model);
        }
    }
}
