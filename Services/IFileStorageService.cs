using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MediGuard.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Validates, uploads a prescription file to Supabase Storage, and persists a database record.
        /// </summary>
        /// <returns>The relative storage file path stored in the database.</returns>
        Task<string> UploadPrescriptionAsync(IFormFile file, Guid pharmacyId, Guid orderId);
    }
}