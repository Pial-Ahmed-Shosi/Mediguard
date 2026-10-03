using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.DTOs;

namespace MediGuard.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly ApplicationDbContext _context;

        public MedicineService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<MedicineSearchResultDto>> SearchMedicinesAsync(
            Guid pharmacyId,
            string? term,
            Guid? categoryId,
            bool? rxOnly,
            int page,
            int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _context.Medicines
                .AsNoTracking()
                .Where(m => m.PharmacyId == pharmacyId);

            // Filter: Category
            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            // Filter: Rx Only
            if (rxOnly.HasValue)
            {
                query = query.Where(m => m.RequiresPrescription == rxOnly.Value);
            }

            // Filter: Search Term (Case-insensitive across brand_name, generic_name, barcode)
            if (!string.IsNullOrWhiteSpace(term))
            {
                var searchTerm = term.Trim().ToLower();
                query = query.Where(m =>
                    m.BrandName.ToLower().Contains(searchTerm) ||
                    m.GenericName.ToLower().Contains(searchTerm) ||
                    (m.Barcode != null && m.Barcode.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var nowUtc = DateTime.UtcNow;

            // Project query with total stock aggregated from active non-expired batches
            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MedicineSearchResultDto
                {
                    Id = m.Id,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category != null ? m.Category.Name : null,
                    ManufacturerId = m.ManufacturerId,
                    ManufacturerName = m.Manufacturer != null ? m.Manufacturer.Name : null,
                    RequiresPrescription = m.RequiresPrescription,
                    Unit = m.Unit,
                    Barcode = m.Barcode,
                    TotalStock = m.InventoryBatches
                        .Where(b => b.Status == "ACTIVE" && b.ExpiryDate > nowUtc)
                        .Sum(b => (int?)b.Quantity) ?? 0,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<MedicineSearchResultDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<(bool Success, string Message, Guid? MedicineId)> CreateMedicineAsync(Guid pharmacyId, CreateMedicineDto dto)
        {
            // Validate Barcode uniqueness per tenant pharmacy
            if (!string.IsNullOrWhiteSpace(dto.Barcode))
            {
                var barcodeExists = await _context.Medicines.AnyAsync(m =>
                    m.PharmacyId == pharmacyId &&
                    m.Barcode != null &&
                    m.Barcode.ToLower() == dto.Barcode.Trim().ToLower());

                if (barcodeExists)
                {
                    return (false, "A medicine with this barcode already exists in your pharmacy.", null);
                }
            }

            var medicine = new Medicine
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                BrandName = dto.BrandName.Trim(),
                GenericName = dto.GenericName.Trim(),
                CategoryId = dto.CategoryId,
                ManufacturerId = dto.ManufacturerId,
                RequiresPrescription = dto.RequiresPrescription,
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Tablet" : dto.Unit.Trim(),
                Barcode = dto.Barcode?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return (true, "Medicine created successfully.", medicine.Id);
        }

        public async Task<(bool Success, string Message)> UpdateMedicineAsync(Guid pharmacyId, UpdateMedicineDto dto)
        {
            var medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.Id == dto.Id && m.PharmacyId == pharmacyId);
            if (medicine == null)
            {
                return (false, "Medicine record not found.");
            }

            // Validate Barcode uniqueness excluding current medicine record
            if (!string.IsNullOrWhiteSpace(dto.Barcode))
            {
                var barcodeExists = await _context.Medicines.AnyAsync(m =>
                    m.PharmacyId == pharmacyId &&
                    m.Id != dto.Id &&
                    m.Barcode != null &&
                    m.Barcode.ToLower() == dto.Barcode.Trim().ToLower());

                if (barcodeExists)
                {
                    return (false, "A medicine with this barcode already exists in your pharmacy.");
                }
            }

            medicine.BrandName = dto.BrandName.Trim();
            medicine.GenericName = dto.GenericName.Trim();
            medicine.CategoryId = dto.CategoryId;
            medicine.ManufacturerId = dto.ManufacturerId;
            medicine.RequiresPrescription = dto.RequiresPrescription;
            medicine.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Tablet" : dto.Unit.Trim();
            medicine.Barcode = dto.Barcode?.Trim();

            await _context.SaveChangesAsync();
            return (true, "Medicine updated successfully.");
        }

        public async Task<MedicineSearchResultDto?> GetMedicineByIdAsync(Guid pharmacyId, Guid medicineId)
        {
            var nowUtc = DateTime.UtcNow;

            return await _context.Medicines
                .AsNoTracking()
                .Where(m => m.PharmacyId == pharmacyId && m.Id == medicineId)
                .Select(m => new MedicineSearchResultDto
                {
                    Id = m.Id,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category != null ? m.Category.Name : null,
                    ManufacturerId = m.ManufacturerId,
                    ManufacturerName = m.Manufacturer != null ? m.Manufacturer.Name : null,
                    RequiresPrescription = m.RequiresPrescription,
                    Unit = m.Unit,
                    Barcode = m.Barcode,
                    TotalStock = m.InventoryBatches
                        .Where(b => b.Status == "ACTIVE" && b.ExpiryDate > nowUtc)
                        .Sum(b => (int?)b.Quantity) ?? 0,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}