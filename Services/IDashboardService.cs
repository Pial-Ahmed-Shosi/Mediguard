using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryViewModel> GetDashboardDataAsync(Guid userId, Guid pharmacyId, string role);
        void InvalidateDashboardCache(Guid userId, Guid pharmacyId);
    }
}