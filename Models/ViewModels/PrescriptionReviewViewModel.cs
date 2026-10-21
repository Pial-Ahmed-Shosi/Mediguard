using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class PrescriptionReviewViewModel
    {
        public Guid PrescriptionId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        // File Path Reference (raw stored path)
        public string PrescriptionFilePath { get; set; } = string.Empty;
        public string FilePath
        {
            get => PrescriptionFilePath;
            set => PrescriptionFilePath = value;
        }

        // Browsable URL the viewer uses (resolved by the controller)
        public string? FileUrl { get; set; }

        public string FileType { get; set; } = "image"; // "image" or "pdf"
        public bool IsPdf => string.Equals(FileType, "pdf", StringComparison.OrdinalIgnoreCase);

        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
        public bool IsDecided =>
            string.Equals(Status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, "REJECTED", StringComparison.OrdinalIgnoreCase);

        public string OrderStatus { get; set; } = "PENDING";
        public string? CustomerNotes { get; set; }
        public string? RejectionReason { get; set; }

        // Submission Timestamps
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime UploadedAt
        {
            get => SubmittedAt;
            set => SubmittedAt = value;
        }

        // Customer Profile Details
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Order Summary
        public decimal TotalAmount { get; set; }

        // Order Line Items
        public List<PrescriptionOrderItemViewModel> OrderItems { get; set; } = new();
        public List<PrescriptionOrderItemViewModel> Items
        {
            get => OrderItems;
            set => OrderItems = value;
        }
    }

    public class PrescriptionOrderItemViewModel
    {
        public Guid MedicineId { get; set; }

        public string MedicineName { get; set; } = string.Empty;
        public string BrandName
        {
            get => MedicineName;
            set => MedicineName = value;
        }

        public string GenericName { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;
        public decimal LineTotal => TotalPrice;

        public bool IsRxRequired { get; set; } = true;
        public bool RequiresPrescription
        {
            get => IsRxRequired;
            set => IsRxRequired = value;
        }
    }
}