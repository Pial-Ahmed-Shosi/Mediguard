// File: Services/IDashboardService.cs
using System;
using System.Threading.Tasks;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryViewModel> GetDashboardDataAsync(Guid userId, Guid pharmacyId, string role);
        void InvalidateDashboardCache(Guid userId, Guid pharmacyId);

        // Ticket 18: Expiry & Low-Stock Alerts Method
        Task<ExpiryAlertsViewModel> GetExpiryAndShortageAlertsAsync(Guid pharmacyId);
    }
}