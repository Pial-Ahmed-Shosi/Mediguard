using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediGuard.Models.ViewModels
{
    public class MedicineCatalogViewModel
    {
        public string SearchKeyword { get; set; }
        public int? CategoryId { get; set; }
        public int? ManufacturerId { get; set; }
        public bool RxOnly { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Manufacturers { get; set; } = new List<SelectListItem>();
        public IEnumerable<MedicineItemViewModel> Medicines { get; set; } = new List<MedicineItemViewModel>();
    }

    public class MedicineItemViewModel
    {
        public int Id { get; set; }
        public string BrandName { get; set; }
        public string GenericName { get; set; }
        public string CategoryName { get; set; }
        public string ManufacturerName { get; set; }
        public string Unit { get; set; } // e.g., Box, Strip, Bottle
        public int TotalStock { get; set; }
        public bool IsRxRequired { get; set; }
    }
}