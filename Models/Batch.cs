using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class Batch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PharmacyId { get; set; }
        public int Quantity { get; set; }
        public int LowStockThreshold { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Pharmacy? Pharmacy { get; set; }
    }
}