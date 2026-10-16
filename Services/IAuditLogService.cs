using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuard.Models;

namespace MediGuard.Services
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Records an audit log entry into the database.
        /// </summary>
        Task LogAsync(AuditLog log);

        /// <summary>
        /// Creates and records an audit log entry.
        /// </summary>
        Task LogAsync(
            Guid pharmacyId,
            string? userId,
            string action,
            string entityName,
            string? oldValuesJson = null,
            string? newValuesJson = null,
            string? ipAddress = null);

        /// <summary>
        /// Retrieves tenant-scoped audit logs with optional filters.
        /// </summary>
        Task<List<AuditLog>> GetAuditLogsAsync(
            Guid pharmacyId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? userId = null,
            string? actionType = null);
    }
}
