using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("deliveries")]
    public class Delivery
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("order_id")]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [Required]
        [Column("deliveryman_id")]
        public string DeliverymanId { get; set; } = string.Empty;

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser Deliveryman { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "PENDING"; // PENDING, DONE, CANCELLED

        [Required]
        [MaxLength(50)]
        [Column("delivery_type")]
        public string DeliveryType { get; set; } = "STANDARD"; // STANDARD, EXPRESS, SAME_DAY

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        [Column("cancellation_reason")]
        public string? CancellationReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}