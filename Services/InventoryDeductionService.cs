using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Services
{
    public class InventoryDeductionService : IInventoryDeductionService
    {
        private readonly ApplicationDbContext _context;

        public InventoryDeductionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BatchDeductionResult>> DeductStockFEFOAsync(Guid pharmacyId, Guid medicineId, int requestedQty)
        {
            if (requestedQty <= 0)
            {
                throw new ArgumentException("Requested quantity must be greater than zero.", nameof(requestedQty));
            }

            var today = DateTime.UtcNow.Date;

            // 1. Query active, unexpired batches sorted by earliest expiry date (FEFO)
            var activeBatches = await _context.Batches
                .Where(b => b.PharmacyId == pharmacyId &&
                            b.MedicineId == medicineId &&
                            b.Status == "ACTIVE" &&
                            b.Quantity > 0 &&
                            b.ExpiryDate.Date > today)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

            // 2. Validate total stock availability before performing any deductions
            int totalAvailableStock = activeBatches.Sum(b => b.Quantity);
            if (totalAvailableStock < requestedQty)
            {
                throw new InsufficientStockException(
                    $"Insufficient stock for medicine ID '{medicineId}'. Requested: {requestedQty}, Available active stock: {totalAvailableStock}.");
            }

            // 3. Perform FEFO deduction across active batches
            int remainingRequestedQty = requestedQty;
            var deductionSummary = new List<BatchDeductionResult>();

            foreach (var batch in activeBatches)
            {
                if (remainingRequestedQty <= 0)
                {
                    break;
                }

                int qtyToDeduct;

                if (batch.Quantity >= remainingRequestedQty)
                {
                    qtyToDeduct = remainingRequestedQty;
                    batch.Quantity -= remainingRequestedQty;
                    batch.RemainingQuantity = batch.Quantity; // Keep RemainingQuantity property in sync

                    if (batch.Quantity == 0)
                    {
                        batch.Status = "EXHAUSTED";
                    }

                    remainingRequestedQty = 0;
                }
                else
                {
                    qtyToDeduct = batch.Quantity;
                    remainingRequestedQty -= batch.Quantity;

                    batch.Quantity = 0;
                    batch.RemainingQuantity = 0;
                    batch.Status = "EXHAUSTED";
                }

                batch.UpdatedAt = DateTime.UtcNow;

                deductionSummary.Add(new BatchDeductionResult
                {
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    DeductedQuantity = qtyToDeduct
                });
            }

            // 4. Save state changes (Note: executes inside the caller's active IDbContextTransaction if invoked within checkout)
            await _context.SaveChangesAsync();

            return deductionSummary;
        }
    }
}