using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize(Roles = "Pharmacy Manager,Manager,Admin")]
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

        private async Task<Guid> GetCurrentTenantIdAsync()
        {
            // 1. Check HttpContext.Items populated by TenantRbacMiddleware
            if (HttpContext.Items["PharmacyId"] is Guid pharmacyId && pharmacyId != Guid.Empty)
            {
                return pharmacyId;
            }

            if (HttpContext.Items["PharmacyId"] is string pStr &&
                Guid.TryParse(pStr, out var parsedGuid) && parsedGuid != Guid.Empty)
            {
                return parsedGuid;
            }

            // 2. Check Claims
            var claim = User.FindFirst("PharmacyId") ?? User.FindFirst(ClaimTypes.GroupSid);
            if (claim != null && Guid.TryParse(claim.Value, out var claimId) && claimId != Guid.Empty)
            {
                return claimId;
            }

            // 3. Check User's database record
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user?.PharmacyId != null && user.PharmacyId != Guid.Empty)
                {
                    return user.PharmacyId.Value;
                }
            }

            // 4. Fallback for Admin: assign first pharmacy so Admin can audit system events
            if (User.IsInRole("Admin"))
            {
                var firstPharmacyId = await _context.Pharmacies.AsNoTracking().Select(p => p.Id).FirstOrDefaultAsync();
                return firstPharmacyId;
            }

            return Guid.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            string? userId,
            string? actionType,
            string? searchTerm)
        {
            var pharmacyId = await GetCurrentTenantIdAsync();
            if (pharmacyId == Guid.Empty)
            {
                return Unauthorized();
            }

            var pharmacy = await _context.Pharmacies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pharmacyId);

            var model = new AuditLogPageViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedUserId = userId,
                SelectedAction = actionType,
                SearchTerm = searchTerm,
                PharmacyName = pharmacy?.Name ?? "Main Pharmacy"
            };

            // Load audit entries scoped strictly to the current authenticated tenant
            var logs = await _auditLogService.GetAuditLogsAsync(
                pharmacyId, startDate, endDate, userId, actionType, searchTerm);

            model.Entries = logs.Select(l => new AuditLogEntryViewModel
            {
                Id = l.Id.ToString(),
                Timestamp = l.Timestamp,
                UserId = l.UserId ?? string.Empty,
                UserName = !string.IsNullOrEmpty(l.User?.FullName)
                    ? l.User.FullName
                    : (!string.IsNullOrEmpty(l.User?.UserName)
                        ? l.User.UserName
                        : (string.IsNullOrEmpty(l.UserId) ? "System" : $"User #{l.UserId[..Math.Min(8, l.UserId.Length)]}")),
                UserRole = l.User?.Role ?? (string.IsNullOrEmpty(l.UserId) ? "System" : "Staff"),
                ActionType = l.Action,
                EntityName = l.EntityName,
                IpAddress = l.IpAddress ?? "N/A",
                OldValuesJson = l.OldValuesJson,
                NewValuesJson = l.NewValuesJson
            }).ToList();

            // Load tenant users for filter options
            var pharmacyUsers = await _context.Users
                .AsNoTracking()
                .Where(u => u.PharmacyId == pharmacyId)
                .OrderBy(u => u.FullName)
                .Select(u => new AuditUserOptionViewModel
                {
                    UserId = u.Id,
                    DisplayName = string.IsNullOrEmpty(u.FullName) ? (u.UserName ?? u.Id) : $"{u.FullName} ({u.Role})"
                })
                .ToListAsync();

            model.Users = pharmacyUsers;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            DateTime? startDate,
            DateTime? endDate,
            string? userId,
            string? actionType,
            string? searchTerm)
        {
            var pharmacyId = await GetCurrentTenantIdAsync();
            if (pharmacyId == Guid.Empty)
            {
                return Unauthorized();
            }

            var pharmacy = await _context.Pharmacies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pharmacyId);
            var logs = await _auditLogService.GetAuditLogsAsync(
                pharmacyId, startDate, endDate, userId, actionType, searchTerm);

            var csvBuilder = new StringBuilder();
            // CSV Header
            csvBuilder.AppendLine("Timestamp (UTC),User Name,Role,Action,Entity / Module,IP Address,Old Values Snapshot,New Values Snapshot");

            foreach (var log in logs)
            {
                string userName = !string.IsNullOrEmpty(log.User?.FullName)
                    ? log.User.FullName
                    : (!string.IsNullOrEmpty(log.User?.UserName)
                        ? log.User.UserName
                        : (string.IsNullOrEmpty(log.UserId) ? "System" : log.UserId));
                string role = log.User?.Role ?? "System";
                string timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                string action = log.Action;
                string entity = log.EntityName;
                string ip = log.IpAddress ?? "N/A";
                string oldVals = log.OldValuesJson ?? string.Empty;
                string newVals = log.NewValuesJson ?? string.Empty;

                csvBuilder.AppendLine(
                    $"{EscapeCsv(timestamp)},{EscapeCsv(userName)},{EscapeCsv(role)},{EscapeCsv(action)},{EscapeCsv(entity)},{EscapeCsv(ip)},{EscapeCsv(oldVals)},{EscapeCsv(newVals)}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvBuilder.ToString())).ToArray();
            string cleanPharmacyName = string.Join("_", (pharmacy?.Name ?? "Pharmacy").Split(Path.GetInvalidFileNameChars()));
            string fileName = $"Audit_Report_{cleanPharmacyName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "\"\"";
            }

            // Escape double quotes by doubling them and wrapping in quotes
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
    }
}
