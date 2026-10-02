using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("batches")]
    public class Batch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        [StringLength(100)]
        public string BatchNumber { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int LowStockThreshold { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        // Ei Status property-ti Expiry Scanner-er CS1061 error fix korbe
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "ACTIVE"; // "ACTIVE", "EXPIRED", "FROZEN"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Property
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}