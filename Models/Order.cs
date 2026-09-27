using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PharmacyId { get; set; }
        public string? DeliverymanId { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Pharmacy? Pharmacy { get; set; }
        public ApplicationUser? Deliveryman { get; set; }
    }
}