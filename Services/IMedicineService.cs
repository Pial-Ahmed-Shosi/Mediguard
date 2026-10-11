using System;
using System.Threading.Tasks;
using MediGuard.Models.ViewModels.Medicine;

namespace MediGuard.Services
{
    public interface IMedicineService
    {
        Task<MedicinePagedListViewModel> SearchMedicinesAsync(
            Guid pharmacyId,
            string? term,
            Guid? categoryId,
            bool? rxOnly,
            int page = 1,
            int pageSize = 10);

        Task<MedicineDetailViewModel?> GetMedicineByIdAsync(Guid id, Guid pharmacyId);

        Task<bool> IsBarcodeUniqueAsync(string barcode, Guid pharmacyId, Guid? excludeMedicineId = null);

        Task<(bool Success, string Message, Guid? Id)> CreateMedicineAsync(CreateMedicineViewModel model, Guid pharmacyId);

        Task<(bool Success, string Message)> UpdateMedicineAsync(EditMedicineViewModel model, Guid pharmacyId);
    }
}