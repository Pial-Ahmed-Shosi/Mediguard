using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("prescriptions")]
    public class Prescription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        public string? CustomerId { get; set; }

        public Guid? PharmacyId { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FileType { get; set; } = "jpg"; // pdf, jpg, png

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public string? VerifiedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [ForeignKey("VerifiedById")]
        public virtual ApplicationUser? VerifiedBy { get; set; }

        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}