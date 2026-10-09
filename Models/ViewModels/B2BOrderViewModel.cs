using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class B2BOrderViewModel
    {
        [Required(ErrorMessage = "Purchaser pharmacy is required.")]
        public Guid PurchaserPharmacyId { get; set; }

        [Required(ErrorMessage = "Supplier pharmacy is required.")]
        public Guid SupplierPharmacyId { get; set; }

        [Required(ErrorMessage = "Shipping address is required.")]
        public string ShippingAddress { get; set; } = string.Empty;

        public string? Notes { get; set; }

        [Required(ErrorMessage = "At least one item is required in the order.")]
        public List<B2BOrderItemViewModel> Items { get; set; } = new();
    }

    public class B2BOrderItemViewModel
    {
        [Required]
        public Guid MedicineId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0.")]
        public decimal UnitPrice { get; set; }
    }
}