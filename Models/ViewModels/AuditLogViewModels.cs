
using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class AuditLogPageViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? SelectedUserId { get; set; }
        public string? SelectedAction { get; set; }
        public string? SearchTerm { get; set; }
        public string PharmacyName { get; set; } = string.Empty;

        public List<AuditUserOptionViewModel> Users { get; set; }
            = new();

        public List<AuditLogEntryViewModel> Entries { get; set; }
            = new();

        public int InsertCount => Entries.Count(e => e.ActionType.Contains("INSERT") || e.ActionType.Contains("CREATE"));
        public int UpdateCount => Entries.Count(e => e.ActionType.Contains("UPDATE") || e.ActionType.Contains("EDIT"));
        public int DeleteCount => Entries.Count(e => e.ActionType.Contains("DELETE") || e.ActionType.Contains("REMOVE"));
    }

    public class AuditUserOptionViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class AuditLogEntryViewModel
    {
        public string Id { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // INSERT, UPDATE, DELETE
        public string ActionType { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;

        // JSON strings; null means no stored snapshot.
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
    }
}
