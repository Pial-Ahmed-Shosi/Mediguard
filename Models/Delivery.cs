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
        public Guid OrderId { get; set; }

        [Required]
        public string DeliverymanId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DeliveryType { get; set; } = "B2C"; // B2C, B2B

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "PENDING"; // PENDING, DONE, CANCELLED

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }
    }
}