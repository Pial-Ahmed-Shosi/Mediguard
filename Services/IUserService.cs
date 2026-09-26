using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IUserService
    {
        Task<StaffPagedListViewModel> GetStaffListAsync(Guid pharmacyId, string search, string roleFilter, int page, int pageSize = 10);
        Task<(bool Success, string Message, string? TempPassword)> CreateStaffAsync(Guid pharmacyId, CreateStaffViewModel model);
        Task<(bool Success, string Message)> UpdatePermissionsAsync(Guid pharmacyId, string userId, List<int> permissionIds);
    }
}