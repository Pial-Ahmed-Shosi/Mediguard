using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetCategoriesByPharmacyAsync(Guid pharmacyId);
        Task<List<Category>> GetAllAsync(Guid pharmacyId);
        Task<Category?> GetByIdAsync(Guid pharmacyId, Guid id);
        Task<(bool Success, string Message, Category? Category)> CreateAsync(Guid pharmacyId, CreateCategoryViewModel model);
        Task<(bool Success, string Message)> UpdateAsync(Guid pharmacyId, EditCategoryViewModel model);
        Task<(bool Success, string Message)> DeleteAsync(Guid pharmacyId, Guid id);
        Task<(bool Success, string Message, Category? Category)> CreateQuickAsync(Guid pharmacyId, QuickCreateCategoryDto dto);
    }
}