using System;
using System.Threading.Tasks;
using MediGuard.Models.DTOs;

namespace MediGuard.Services
{
    public interface IMedicineService
    {
        Task<PagedResult<MedicineSearchResultDto>> SearchMedicinesAsync(
            Guid pharmacyId,
            string? term,
            Guid? categoryId,
            bool? rxOnly,
            int page,
            int pageSize);

        Task<(bool Success, string Message, Guid? MedicineId)> CreateMedicineAsync(
            Guid pharmacyId,
            CreateMedicineDto dto);

        Task<(bool Success, string Message)> UpdateMedicineAsync(
            Guid pharmacyId,
            UpdateMedicineDto dto);

        Task<MedicineSearchResultDto?> GetMedicineByIdAsync(
            Guid pharmacyId,
            Guid medicineId);
    }
}