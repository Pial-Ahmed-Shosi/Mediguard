using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels.Medicine;

namespace MediGuard.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly ApplicationDbContext _context;

        public MedicineService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MedicinePagedListViewModel> SearchMedicinesAsync(
            Guid pharmacyId,
            string? term,
            Guid? categoryId,
            bool? rxOnly,
            int page = 1,
            int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var utcNow = DateTime.UtcNow;

            // Scope query to the current pharmacy tenant
            var query = _context.Medicines
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.Manufacturer)
                .Include(m => m.Batches)
                .Where(m => m.PharmacyId == pharmacyId);

            // Case-insensitive search across BrandName, GenericName, and Barcode
            if (!string.IsNullOrWhiteSpace(term))
            {
                var termLower = term.Trim().ToLower();
                query = query.Where(m =>
                    (m.BrandName != null && m.BrandName.ToLower().Contains(termLower)) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(termLower)) ||
                    (m.Barcode != null && m.Barcode.ToLower().Contains(termLower))
                );
            }

            // Filter by Category
            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            // Filter by Prescription requirement using mapped database property (RequiresPrescription)
            if (rxOnly.HasValue)
            {
                query = query.Where(m => m.RequiresPrescription == rxOnly.Value);
            }

            // Calculate total matching records before applying pagination
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination and project stock calculated from active non-expired batches
            var items = await query
                .OrderBy(m => m.BrandName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MedicineListItemViewModel
                {
                    Id = m.Id,
                    PharmacyId = m.PharmacyId,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    Barcode = m.Barcode ?? string.Empty,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category != null ? m.Category.Name : string.Empty,
                    ManufacturerId = m.ManufacturerId,
                    ManufacturerName = m.Manufacturer != null ? m.Manufacturer.Name : string.Empty,
                    IsRxOnly = m.RequiresPrescription,
                    Unit = m.Unit,
                    Price = m.Price,
                    IsActive = m.IsActive,
                    TotalStock = m.Batches
                        .Where(b => b.Status == "ACTIVE" && b.ExpiryDate > utcNow)
                        .Sum(b => (int?)b.Quantity) ?? 0
                })
                .ToListAsync();

            return new MedicinePagedListViewModel
            {
                Medicines = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = term ?? string.Empty,
                SelectedCategoryId = categoryId,
                SelectedRxOnly = rxOnly
            };
        }

        public async Task<MedicineDetailViewModel?> GetMedicineByIdAsync(Guid id, Guid pharmacyId)
        {
            var utcNow = DateTime.UtcNow;

            var medicine = await _context.Medicines
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.Manufacturer)
                .Include(m => m.Batches)
                .FirstOrDefaultAsync(m => m.Id == id && m.PharmacyId == pharmacyId);

            if (medicine == null)
                return null;

            return new MedicineDetailViewModel
            {
                Id = medicine.Id,
                PharmacyId = medicine.PharmacyId,
                BrandName = medicine.BrandName,
                GenericName = medicine.GenericName,
                Barcode = medicine.Barcode ?? string.Empty,
                CategoryId = medicine.CategoryId,
                CategoryName = medicine.Category?.Name ?? string.Empty,
                ManufacturerId = medicine.ManufacturerId,
                ManufacturerName = medicine.Manufacturer?.Name ?? string.Empty,
                IsRxOnly = medicine.RequiresPrescription,
                Unit = medicine.Unit,
                Price = medicine.Price,
                IsActive = medicine.IsActive,
                CreatedAt = medicine.CreatedAt,
                TotalStock = medicine.Batches
                    .Where(b => b.Status == "ACTIVE" && b.ExpiryDate > utcNow)
                    .Sum(b => (int?)b.Quantity) ?? 0,
                ActiveBatches = medicine.Batches
                    .Where(b => b.Status == "ACTIVE" && b.ExpiryDate > utcNow)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new MedicineBatchSummaryViewModel
                    {
                        BatchId = b.Id,
                        BatchNumber = b.BatchNumber,
                        Quantity = b.Quantity,
                        ExpiryDate = b.ExpiryDate,
                        Status = b.Status
                    })
                    .ToList()
            };
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, Guid pharmacyId, Guid? excludeMedicineId = null)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return true;

            var normalizedBarcode = barcode.Trim().ToLower();

            var query = _context.Medicines
                .AsNoTracking()
                .Where(m => m.PharmacyId == pharmacyId && m.Barcode != null && m.Barcode.ToLower() == normalizedBarcode);

            if (excludeMedicineId.HasValue && excludeMedicineId.Value != Guid.Empty)
            {
                query = query.Where(m => m.Id != excludeMedicineId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<(bool Success, string Message, Guid? Id)> CreateMedicineAsync(CreateMedicineViewModel model, Guid pharmacyId)
        {
            bool isUnique = await IsBarcodeUniqueAsync(model.Barcode, pharmacyId);
            if (!isUnique)
            {
                return (false, $"Barcode '{model.Barcode}' is already assigned to another medicine in this pharmacy.", null);
            }

            var medicine = new Medicine
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                BrandName = model.BrandName.Trim(),
                GenericName = model.GenericName.Trim(),
                Barcode = model.Barcode.Trim(),
                CategoryId = model.CategoryId,
                ManufacturerId = model.ManufacturerId,
                RequiresPrescription = model.IsRxOnly,
                Unit = model.Unit.Trim(),
                Price = model.Price,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return (true, "Medicine created successfully.", medicine.Id);
        }

        public async Task<(bool Success, string Message)> UpdateMedicineAsync(EditMedicineViewModel model, Guid pharmacyId)
        {
            var medicine = await _context.Medicines
                .FirstOrDefaultAsync(m => m.Id == model.Id && m.PharmacyId == pharmacyId);

            if (medicine == null)
            {
                return (false, "Medicine record not found or unauthorized access.");
            }

            bool isUnique = await IsBarcodeUniqueAsync(model.Barcode, pharmacyId, model.Id);
            if (!isUnique)
            {
                return (false, $"Barcode '{model.Barcode}' is already assigned to another medicine in this pharmacy.");
            }

            medicine.BrandName = model.BrandName.Trim();
            medicine.GenericName = model.GenericName.Trim();
            medicine.Barcode = model.Barcode.Trim();
            medicine.CategoryId = model.CategoryId;
            medicine.ManufacturerId = model.ManufacturerId;
            medicine.RequiresPrescription = model.IsRxOnly;
            medicine.Unit = model.Unit.Trim();
            medicine.Price = model.Price;
            medicine.IsActive = model.IsActive;
            medicine.UpdatedAt = DateTime.UtcNow;

            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();

            return (true, "Medicine updated successfully.");
        }
    }
}