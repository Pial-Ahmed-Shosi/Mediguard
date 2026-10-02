using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IBatchService
    {
        /// <summary>
        /// Validates, constructs, and persists a new batch lot into the database with ACTIVE status.
        /// </summary>
        Task<(bool Success, string Message, Batch? Batch)> CreateBatchAsync(BatchEntryViewModel model, Guid pharmacyId);

        /// <summary>
        /// Retrieves active stock batches for a given medicine and pharmacy using FEFO (First-Expiry, First-Out) ordering.
        /// </summary>
        Task<IEnumerable<ExistingBatchDto>> GetActiveBatchesFEFOAsync(Guid medicineId, Guid pharmacyId);
    }
}