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
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        [Column("order_number")]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("order_type")]
        public string OrderType { get; set; } = "POS"; // POS, ONLINE, B2B

        [Column("is_b2b_order")]
        public bool IsB2BOrder { get; set; } = false;

        [Required]
        [Column("pharmacy_id")]
        public Guid PharmacyId { get; set; }

        [Column("supplier_pharmacy_id")]
        public Guid? SupplierPharmacyId { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("customer_id")]
        public string? CustomerId { get; set; }

        [Column("deliveryman_id")]
        public string? DeliverymanId { get; set; }

        [Column("assigned_deliveryman_id")]
        public string? AssignedDeliverymanId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("delivery_status")]
        public string DeliveryStatus { get; set; } = "NOT_REQUIRED"; // NOT_REQUIRED, PENDING, IN_TRANSIT, DELIVERED, CANCELLED

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("tax")]
        public decimal Tax { get; set; } = 0.00m;

        [Column("discount")]
        public decimal Discount { get; set; } = 0.00m;

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "UNPAID"; // UNPAID, PAID, REFUNDED

        [Required]
        [MaxLength(30)]
        [Column("payment_method")]
        public string PaymentMethod { get; set; } = "CASH"; // CASH, CARD, ONLINE

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("SupplierPharmacyId")]
        public virtual Pharmacy? SupplierPharmacy { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }

        [ForeignKey("AssignedDeliverymanId")]
        public virtual ApplicationUser? AssignedDeliveryman { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}