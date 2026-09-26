using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class Sale
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PharmacyId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Pharmacy? Pharmacy { get; set; }
    }
}