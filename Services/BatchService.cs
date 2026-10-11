using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public class BatchService : IBatchService
    {
        private readonly ApplicationDbContext _context;

        public BatchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, Batch? Batch)> CreateBatchAsync(BatchEntryViewModel model, Guid pharmacyId)
        {
            DateTime today = DateTime.UtcNow.Date;

            // Acceptance Criterion: Validate Expiry Date is greater than Manufacturing Date
            if (model.ExpiryDate.Date <= model.ManufacturingDate.Date)
            {
                return (false, "Expiry date must be later than the manufacturing date.", null);
            }

            // Acceptance Criterion: Reject batch entry if expiry date is equal to or prior to current system date
            if (model.ExpiryDate.Date <= today)
            {
                return (false, "Expiry date must be in the future (later than today).", null);
            }

            // Acceptance Criterion: Validate Expiry Date is at least 30 days after Manufacturing Date
            if ((model.ExpiryDate.Date - model.ManufacturingDate.Date).TotalDays < 30)
            {
                return (false, "Expiry date must be at least 30 days after the manufacturing date.", null);
            }

            // Acceptance Criterion: Association with correct pharmacy and medicine IDs & status = "ACTIVE"
            var batch = new Batch
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                MedicineId = model.MedicineId,
                BatchNumber = model.BatchNumber?.Trim() ?? string.Empty,
                Quantity = model.Quantity,
                RemainingQuantity = model.Quantity,
                PurchasePrice = model.PurchasePricePerUnit,
                SellingPrice = model.SellingPricePerUnit,
                ManufacturingDate = model.ManufacturingDate.Date,
                ExpiryDate = model.ExpiryDate.Date,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _context.Batches.Add(batch);
            await _context.SaveChangesAsync();

            return (true, "Batch created successfully.", batch);
        }

        public async Task<IEnumerable<ExistingBatchDto>> GetActiveBatchesFEFOAsync(Guid medicineId, Guid pharmacyId)
        {
            // Acceptance Criteria:
            // Filter: pharmacy_id == tenantId, medicine_id == medicineId, quantity > 0, status == "ACTIVE"
            // Sorted: .OrderBy(b => b.ExpiryDate) [FEFO Order]
            var activeBatches = await _context.Batches
                .AsNoTracking()
                .Where(b => b.PharmacyId == pharmacyId
                         && b.MedicineId == medicineId
                         && b.RemainingQuantity > 0
                         && b.Status == "ACTIVE")
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new ExistingBatchDto
                {
                    BatchId = b.Id,
                    BatchNumber = b.BatchNumber,
                    RemainingQuantity = b.RemainingQuantity,
                    ManufacturingDate = b.ManufacturingDate,
                    ExpiryDate = b.ExpiryDate,
                    SellingPrice = b.SellingPrice,
                    IsActive = b.Status == "ACTIVE"
                })
                .ToListAsync();

            return activeBatches;
        }
    }
}