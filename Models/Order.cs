using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PharmacyId { get; set; }

        public string? CustomerId { get; set; } // ApplicationUser Id (Customer placing order)

        public string? DeliverymanId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "PENDING"; // PENDING, CANCELLED, COMPLETED

        [StringLength(50)]
        public string? DeliveryStatus { get; set; } // PENDING, ASSIGNED, IN_TRANSIT, DELIVERED

        [StringLength(50)]
        public string OrderStatus { get; set; } = "PENDING"; // PENDING, PROCESSING, READY, COMPLETED, CANCELLED

        public bool IsB2BOrder { get; set; } = false; // TRUE for B2B orders, FALSE for B2C

        public Guid? SupplierPharmacyId { get; set; } // For B2B orders: Supplier pharmacy ID

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; } = 0m;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }

        [ForeignKey("SupplierPharmacyId")]
        public virtual Pharmacy? SupplierPharmacy { get; set; }

        // Collection of order items (line items)
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}