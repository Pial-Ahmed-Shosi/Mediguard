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

        // ---------------------------------------------------------------
        // Ticket 28: Core Order Fields
        // ---------------------------------------------------------------

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

        /// <summary>
        /// Cashier or Customer who placed the order. Nullable for anonymous POS sales.
        /// </summary>
        public string? UserId { get; set; }

        [Column("customer_id")]
        public string? CustomerId { get; set; }

        /// <summary>
        /// Allowed values: 'POS', 'B2C_ONLINE', 'B2B_MEDI_PLUS'
        /// </summary>
        [Required]
        [StringLength(30)]
        public string OrderType { get; set; } = "POS";

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

        /// <summary>
        /// Allowed values: 'PAID', 'UNPAID', 'REFUNDED'
        /// </summary>
        [Required]
        [StringLength(30)]
        public string PaymentStatus { get; set; } = "UNPAID";

        /// <summary>
        /// Allowed values: 'CASH', 'CARD', 'ONLINE'
        /// </summary>
        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = "CASH";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional status fields retained for cross-module compatibility
        [StringLength(50)]
        public string Status { get; set; } = "PENDING";

        [StringLength(50)]
        public string? OrderStatus { get; set; } = "PENDING";

        public string? CustomerId { get; set; }
        public string? DeliverymanId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ---------------------------------------------------------------
        // Ticket 37: Delivery & B2B Extension Fields
        // ---------------------------------------------------------------

        /// <summary>
        /// Indicates whether this is a Medi+ B2B inter-pharmacy order.
        /// </summary>
        [Column("is_b2b_order")]
        public bool IsB2BOrder { get; set; } = false;

        /// <summary>
        /// Delivery status for orders requiring fulfillment.
        /// Allowed values: 'NOT_REQUIRED', 'PENDING', 'IN_TRANSIT', 'DONE', 'CANCELLED'
        /// </summary>
        [StringLength(30)]
        [Column("delivery_status")]
        public string DeliveryStatus { get; set; } = "NOT_REQUIRED";

        /// <summary>
        /// The deliveryman assigned to fulfill this order (Ticket 37).
        /// </summary>
        [Column("assigned_deliveryman_id")]
        public string? AssignedDeliverymanId { get; set; }

        /// <summary>
        /// For B2B orders, the pharmacy supplying the medicines.
        /// </summary>
        [Column("supplier_pharmacy_id")]
        public Guid? SupplierPharmacyId { get; set; }

        /// <summary>
        /// Shipping/delivery address for B2C and B2B orders.
        /// </summary>
        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Optional notes or special instructions for the order.
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

        // ---------------------------------------------------------------
        // Navigation Properties
        // ---------------------------------------------------------------

        // Ticket 28: Core navigations
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("SupplierPharmacyId")]
        public virtual Pharmacy? SupplierPharmacy { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Cross-module compatibility navigations
        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }

        // Ticket 37: Delivery & B2B navigations
        [ForeignKey("AssignedDeliverymanId")]
        public virtual ApplicationUser? AssignedDeliveryman { get; set; }

        [ForeignKey("SupplierPharmacyId")]
        public virtual Pharmacy? SupplierPharmacy { get; set; }
    }
}