using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class DeliveryDetailsDto
    {
        public Guid Id { get; set; }
        public Guid DeliveryId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderType { get; set; } = "B2C"; // B2C or B2B
        public string Status { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = "STANDARD";
        public string? ShippingAddress { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CancellationReason { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        // Recipient Metadata
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string? PharmacyName { get; set; }

        // Order Items
        public List<DeliveryOrderItemDto> Items { get; set; } = new();
    }

    public class DeliveryOrderItemDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdateDeliveryStatusDto
    {
        public Guid DeliveryId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // PENDING, DONE, CANCELLED
        public string? CancellationReason { get; set; }
    }
}
