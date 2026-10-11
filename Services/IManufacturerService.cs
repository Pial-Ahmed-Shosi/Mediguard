using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IManufacturerService
    {
        Task<IEnumerable<Manufacturer>> GetManufacturersByPharmacyAsync(Guid pharmacyId);
        Task<List<Manufacturer>> GetAllAsync(Guid pharmacyId);
        Task<Manufacturer?> GetByIdAsync(Guid pharmacyId, Guid id);
        Task<(bool Success, string Message, Manufacturer? Manufacturer)> CreateAsync(Guid pharmacyId, CreateManufacturerViewModel model);
        Task<(bool Success, string Message)> UpdateAsync(Guid pharmacyId, EditManufacturerViewModel model);
        Task<(bool Success, string Message)> DeleteAsync(Guid pharmacyId, Guid id);
        Task<(bool Success, string Message, Manufacturer? Manufacturer)> CreateQuickAsync(Guid pharmacyId, QuickCreateManufacturerDto dto);
    }
}