using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediGuard.Services
{
    public class BatchDeductionResult
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int DeductedQuantity { get; set; }
    }

    public interface IInventoryDeductionService
    {
        /// <summary>
        /// Automatically deducts requested stock from active batches following FEFO (First Expired, First Out) rules.
        /// </summary>
        Task<List<BatchDeductionResult>> DeductStockFEFOAsync(Guid pharmacyId, Guid medicineId, int requestedQty);
    }
}