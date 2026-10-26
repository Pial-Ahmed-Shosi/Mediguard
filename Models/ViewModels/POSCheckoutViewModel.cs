using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    /// <summary>
    /// View model representing a POS checkout cart submission (Ticket 29A / 29B).
    /// Shape matches the frontend pos-cart.js payload.
    /// </summary>
    public class POSCheckoutViewModel
    {
        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = "CASH";

        public decimal Subtotal { get; set; }

        /// <summary>
        /// Tax rate percentage (e.g. 5 for 5%).
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Overall discount amount applied to the order.
        /// </summary>
        public decimal OverallDiscount { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal AmountTendered { get; set; }

        public decimal ChangeDue { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Cart must contain at least one item.")]
        public List<POSCheckoutItemViewModel> Items { get; set; } = new();
    }

    public class POSCheckoutItemViewModel
    {
        [Required]
        public Guid MedicineId { get; set; }

        /// <summary>
        /// Optional batch hint from the frontend. The backend will use FEFO deduction
        /// regardless, but this can be stored for auditing.
        /// </summary>
        public Guid? BatchId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be positive.")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Per-item discount percentage (0–100).
        /// </summary>
        public decimal DiscountPercent { get; set; }
    }
}
