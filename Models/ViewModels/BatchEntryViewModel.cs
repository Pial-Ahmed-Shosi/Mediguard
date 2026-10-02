using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediGuard.Models.ViewModels
{
    public class BatchEntryViewModel
    {
        [Required(ErrorMessage = "Please select a medicine.")]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Batch number is required.")]
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters.")]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Initial quantity is required.")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1.")]
        [Display(Name = "Initial Quantity")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Purchase price per unit is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Purchase price must be greater than zero.")]
        [Display(Name = "Purchase Price Per Unit")]
        public decimal PurchasePricePerUnit { get; set; }

        [Required(ErrorMessage = "Selling price per unit is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Selling price must be greater than zero.")]
        [Display(Name = "Selling Price Per Unit")]
        public decimal SellingPricePerUnit { get; set; }

        [Required(ErrorMessage = "Manufacturing date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Manufacturing Date")]
        public DateTime ManufacturingDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Expiry date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddMonths(12);

        // Dropdown selection list
        public IEnumerable<SelectListItem> MedicineList { get; set; } = new List<SelectListItem>();

        // Pre-populated existing batches for FEFO display
        public IEnumerable<ExistingBatchDto> ExistingBatches { get; set; } = new List<ExistingBatchDto>();
    }

    public class ExistingBatchDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int RemainingQuantity { get; set; }
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; } = true;

        public int DaysUntilExpiry => (ExpiryDate.Date - DateTime.Today).Days;
    }
}