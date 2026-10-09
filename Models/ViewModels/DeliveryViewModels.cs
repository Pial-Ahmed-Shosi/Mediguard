using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuardApp.Models.ViewModels
{
    public class UpdateDeliveryStatusDto
    {
        [Required]
        public Guid DeliveryId { get; set; }

        [Required]
        public string NewStatus { get; set; } = string.Empty; // "DONE" or "CANCELLED"

        public string? Reason { get; set; }
    }

    public class DeliveryDetailsDto
    {
        public Guid DeliveryId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderType { get; set; } = "B2C"; // "B2C" or "B2B"
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CancellationReason { get; set; }
        public decimal TotalAmount { get; set; }

        // Recipient Metadata (B2C & B2B)
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? PharmacyName { get; set; } // Populated for B2B

        // Order Items Breakdown
        public List<DeliveryOrderItemDto> Items { get; set; } = new();
    }

    public class DeliveryOrderItemDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}