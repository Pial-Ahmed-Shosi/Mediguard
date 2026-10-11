using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MediGuard.Services
{
    public class SupabaseStorageService : IFileStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB limit

        private static readonly Dictionary<string, string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".pdf", "application/pdf" }
        };

        public SupabaseStorageService(HttpClient httpClient, ApplicationDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> UploadPrescriptionAsync(IFormFile file, Guid pharmacyId, Guid orderId)
        {
            // 1. File existence validation
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was provided for upload.");
            }

            // 2. File size validation (Max 5 MB)
            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("File size exceeds the maximum allowed limit of 5 MB.");
            }

            // 3. Extension validation (.jpg, .jpeg, .png, .pdf)
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedMimeTypes.ContainsKey(extension))
            {
                throw new ArgumentException($"Invalid file extension '{extension}'. Allowed extensions are: .jpg, .jpeg, .png, .pdf.");
            }

            // 4. MIME type header validation matching extension
            string expectedMimeType = AllowedMimeTypes[extension];
            if (!string.Equals(file.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"MIME type mismatch. Provided '{file.ContentType}', expected '{expectedMimeType}' for {extension}.");
            }

            // 5. Generate storage path: prescriptions/{pharmacyId}/{orderId}_{Guid.NewGuid()}{ext}
            string relativePath = $"prescriptions/{pharmacyId}/{orderId}_{Guid.NewGuid()}{extension}";

            // 6. Read Supabase configuration settings
            string supabaseUrl = _configuration["Supabase:Url"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Supabase:Url is missing in configuration.");
            string apiKey = _configuration["Supabase:ApiKey"]
                ?? throw new InvalidOperationException("Supabase:ApiKey is missing in configuration.");
            string bucketName = _configuration["Supabase:BucketName"] ?? "prescriptions";

            // 7. Upload file to Supabase Storage via REST API
            string uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{relativePath}";

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("apikey", apiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(expectedMimeType);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorResponseBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to upload file to Supabase Storage. Status: {response.StatusCode}, Details: {errorResponseBody}");
            }

            // 8. Save Prescription record in database
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                OrderId = orderId,
                FilePath = relativePath,
                Status = "PENDING_VERIFICATION",
                CreatedAt = DateTime.UtcNow
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return relativePath;
        }
    }
}