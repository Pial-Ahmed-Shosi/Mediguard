using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class PrescriptionReviewViewModel
    {
        public Guid PrescriptionId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public string? RejectionReason { get; set; }
        public DateTime UploadedAt { get; set; }

        // Customer Details
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Order Summary
        public decimal TotalAmount { get; set; }
        public List<PrescriptionOrderItemViewModel> Items { get; set; } = new();
    }

    public class PrescriptionOrderItemViewModel
    {
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
        public bool RequiresPrescription { get; set; }
    }
}