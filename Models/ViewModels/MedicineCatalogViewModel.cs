using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediGuard.Models.ViewModels
{
    public class MedicineCatalogViewModel
    {
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Manufacturers { get; set; } = new List<SelectListItem>();
        public IEnumerable<MedicineGridItemViewModel> Medicines { get; set; } = new List<MedicineGridItemViewModel>();
    }

    public class MedicineGridItemViewModel
    {
        public Guid Id { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ManufacturerName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsRxOnly { get; set; }
        public bool RequiresPrescription { get; set; }
        public int TotalAvailableStock { get; set; }
    }
}