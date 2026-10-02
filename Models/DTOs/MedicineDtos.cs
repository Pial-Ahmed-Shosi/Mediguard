using System;
using System.Collections.Generic;

namespace MediGuard.Models.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class MedicineSearchResultDto
    {
        public Guid Id { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid? ManufacturerId { get; set; }
        public string? ManufacturerName { get; set; }
        public bool RequiresPrescription { get; set; }
        public string Unit { get; set; } = "Tablet";
        public string? Barcode { get; set; }
        public int TotalStock { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMedicineDto
    {
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public Guid? ManufacturerId { get; set; }
        public bool RequiresPrescription { get; set; }
        public string Unit { get; set; } = "Tablet";
        public string? Barcode { get; set; }
    }

    public class UpdateMedicineDto : CreateMedicineDto
    {
        public Guid Id { get; set; }
    }
}