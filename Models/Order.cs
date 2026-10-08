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

        public string? DeliverymanId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "PENDING"; // PENDING, CANCELLED, COMPLETED

        [StringLength(50)]
        public string? DeliveryStatus { get; set; } // PENDING, ASSIGNED, IN_TRANSIT, DELIVERED

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("DeliverymanId")]
        public virtual ApplicationUser? Deliveryman { get; set; }
    }
}