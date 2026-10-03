using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels.Medicine
{
    public class MedicineListItemViewModel
    {
        public Guid Id { get; set; }
        public Guid PharmacyId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid? ManufacturerId { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;
        public bool IsRxOnly { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TotalStock { get; set; }
        public bool IsActive { get; set; }
    }

    public class MedicinePagedListViewModel
    {
        public List<MedicineListItemViewModel> Medicines { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public Guid? SelectedCategoryId { get; set; }
        public bool? SelectedRxOnly { get; set; }
    }

    public class CreateMedicineViewModel
    {
        public Guid PharmacyId { get; set; }

        [Required(ErrorMessage = "Brand name is required.")]
        [StringLength(150, ErrorMessage = "Brand name cannot exceed 150 characters.")]
        [Display(Name = "Brand Name")]
        public string BrandName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Generic name is required.")]
        [StringLength(150, ErrorMessage = "Generic name cannot exceed 150 characters.")]
        [Display(Name = "Generic Name")]
        public string GenericName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Barcode is required.")]
        [StringLength(50, ErrorMessage = "Barcode cannot exceed 50 characters.")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category selection is required.")]
        [Display(Name = "Category")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Manufacturer selection is required.")]
        [Display(Name = "Manufacturer")]
        public Guid ManufacturerId { get; set; }

        [Display(Name = "Prescription Required (Rx Only)")]
        public bool IsRxOnly { get; set; }

        [Required(ErrorMessage = "Unit of measurement is required.")]
        [StringLength(30, ErrorMessage = "Unit cannot exceed 30 characters.")]
        public string Unit { get; set; } = "Box";

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class EditMedicineViewModel : CreateMedicineViewModel
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class MedicineDetailViewModel : MedicineListItemViewModel
    {
        public DateTime CreatedAt { get; set; }
        public List<MedicineBatchSummaryViewModel> ActiveBatches { get; set; } = new();
    }

    public class MedicineBatchSummaryViewModel
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}