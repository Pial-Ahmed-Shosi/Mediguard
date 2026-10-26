using System;
using System.Threading.Tasks;
using MediGuard.Models.DTOs;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    /// <summary>
    /// Service contract for POS checkout processing (Ticket 29A).
    /// </summary>
    public interface IPOSService
    {
        /// <summary>
        /// Processes a complete POS checkout within an atomic database transaction.
        /// Validates stock, deducts inventory via FEFO, persists order + line items, and returns result.
        /// </summary>
        /// <param name="model">The checkout payload from the POS frontend.</param>
        /// <param name="pharmacyId">The current tenant pharmacy ID.</param>
        /// <param name="userId">The current cashier user ID.</param>
        /// <returns>A <see cref="POSCheckoutResult"/> indicating success/failure with order details.</returns>
        Task<POSCheckoutResult> ProcessCheckoutAsync(POSCheckoutViewModel model, Guid pharmacyId, string userId);
    }
}
