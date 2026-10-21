using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediGuard.Data;
using MediGuard.Models;

namespace MediGuard.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IServiceScopeFactory scopeFactory,
            ILogger<AuditLogService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task LogAsync(AuditLog log)
        {
            if (log == null)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                dbContext.AuditLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Safely handle audit recording failures so underlying business operations are never disrupted
                _logger.LogWarning(ex, "Failed to persist audit log record for Action '{Action}' on Entity '{EntityName}' (Pharmacy: {PharmacyId}).",
                    log.Action, log.EntityName, log.PharmacyId);
            }
        }

        public async Task LogAsync(
            Guid pharmacyId,
            string? userId,
            string action,
            string entityName,
            string? oldValuesJson = null,
            string? newValuesJson = null,
            string? ipAddress = null)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                UserId = userId,
                Action = action,
                EntityName = entityName,
                OldValuesJson = oldValuesJson,
                NewValuesJson = newValuesJson,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await LogAsync(log);
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(
            Guid pharmacyId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? userId = null,
            string? actionType = null,
            string? searchTerm = null)
        {
            if (pharmacyId == Guid.Empty)
            {
                return new List<AuditLog>();
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var query = dbContext.AuditLogs
                    .Include(a => a.User)
                    .AsNoTracking()
                    .Where(a => a.PharmacyId == pharmacyId);

                if (startDate.HasValue)
                {
                    var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                    query = query.Where(a => a.Timestamp >= startUtc);
                }

                if (endDate.HasValue)
                {
                    var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.Timestamp <= endUtc);
                }

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    query = query.Where(a => a.UserId == userId);
                }

                if (!string.IsNullOrWhiteSpace(actionType))
                {
                    var upperAction = actionType.Trim().ToUpperInvariant();
                    if (upperAction == "INSERT" || upperAction == "CREATE")
                    {
                        query = query.Where(a => a.Action.ToUpper().Contains("INSERT") || a.Action.ToUpper().Contains("CREATE"));
                    }
                    else if (upperAction == "UPDATE" || upperAction == "EDIT")
                    {
                        query = query.Where(a => a.Action.ToUpper().Contains("UPDATE") || a.Action.ToUpper().Contains("EDIT"));
                    }
                    else if (upperAction == "DELETE" || upperAction == "REMOVE")
                    {
                        query = query.Where(a => a.Action.ToUpper().Contains("DELETE") || a.Action.ToUpper().Contains("REMOVE"));
                    }
                    else
                    {
                        query = query.Where(a => a.Action.ToUpper().Contains(upperAction));
                    }
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLower();
                    query = query.Where(a => a.EntityName.ToLower().Contains(term) ||
                                             a.Action.ToLower().Contains(term) ||
                                             (a.IpAddress != null && a.IpAddress.ToLower().Contains(term)));
                }

                return await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(500)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for PharmacyId: {PharmacyId}.", pharmacyId);
                return new List<AuditLog>();
            }
        }
    }
}
