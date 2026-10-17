using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string OrderType { get; set; } = "POS"; // POS, B2C_ONLINE, B2B_MEDI_PLUS

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal Tax { get; set; } = 0.00m;

        [Column(TypeName = "numeric(10,2)")]
        public decimal Discount { get; set; } = 0.00m;

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(30)]
        public string PaymentStatus { get; set; } = "UNPAID"; // PAID, UNPAID, REFUNDED

        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = "CASH"; // CASH, CARD, ONLINE

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional status fields retained for cross-module compatibility
        [StringLength(50)]
        public string Status { get; set; } = "PENDING";

        [StringLength(50)]
        public string? OrderStatus { get; set; } = "PENDING";

        [StringLength(50)]
        public string? DeliveryStatus { get; set; }

        public string? CustomerId { get; set; }
        public string? DeliverymanId { get; set; }
        public Guid? SupplierPharmacyId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }

        [ForeignKey("SupplierPharmacyId")]
        public virtual Pharmacy? SupplierPharmacy { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}