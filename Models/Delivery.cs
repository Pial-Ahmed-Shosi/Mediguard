using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("deliveries")]
    public class Delivery
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("pharmacy_id")]
        public Guid PharmacyId { get; set; }

        [Required]
        [Column("order_id")]
        public Guid OrderId { get; set; }

        [MaxLength(450)]
        [Column("deliveryman_id")]
        public string? DeliverymanId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("delivery_type")]
        public string DeliveryType { get; set; } = "B2C"; // B2C, B2B

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "PENDING"; // PENDING, DONE, CANCELLED

        [MaxLength(255)]
        [Column("recipient_name")]
        public string? RecipientName { get; set; }

        [MaxLength(50)]
        [Column("recipient_phone")]
        public string? RecipientPhone { get; set; }

        [MaxLength(500)]
        [Column("delivery_address")]
        public string? DeliveryAddress { get; set; }

        [MaxLength(500)]
        [Column("cancellation_reason")]
        public string? CancellationReason { get; set; }

        [Column("assigned_at")]
        public DateTime? AssignedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }

        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}