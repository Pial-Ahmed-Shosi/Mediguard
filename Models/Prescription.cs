using System;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class Prescription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        public Guid? OrderId { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "PENDING_VERIFICATION";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}