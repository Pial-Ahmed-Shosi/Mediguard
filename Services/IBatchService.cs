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
        /// <param name="model">The batch entry view model containing user inputs.</param>
        /// <param name="pharmacyId">The unique ID of the current tenant/pharmacy.</param>
        /// <returns>A tuple indicating success status, user message, and the newly created Batch entity.</returns>
        Task<(bool Success, string Message, Batch? Batch)> CreateBatchAsync(BatchEntryViewModel model, Guid pharmacyId);

        /// <summary>
        /// Retrieves active stock batches for a given medicine and pharmacy ordered by earliest expiry date (FEFO - First-Expiry, First-Out).
        /// </summary>
        /// <param name="medicineId">The unique ID of the medicine.</param>
        /// <param name="pharmacyId">The unique ID of the current tenant/pharmacy.</param>
        /// <returns>A list of active batch DTOs.</returns>
        Task<IEnumerable<ExistingBatchDto>> GetActiveBatchesFEFOAsync(Guid medicineId, Guid pharmacyId);
    }
}