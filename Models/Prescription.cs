using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("prescriptions")]
    public class Prescription
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

        [Column("customer_id")]
        public string? CustomerId { get; set; }

        [Required]
        [MaxLength(1000)]
        [Column("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("file_type")]
        public string FileType { get; set; } = "jpg"; // pdf, jpg, png

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

        [MaxLength(500)]
        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        [MaxLength(1000)]
        [Column("notes")]
        public string? Notes { get; set; }

        [MaxLength(450)]
        [Column("verified_by")]
        public string? VerifiedById { get; set; }

        [NotMapped]
        public string? VerifiedBy
        {
            get => VerifiedById;
            set => VerifiedById = value;
        }

        [Column("verified_at")]
        public DateTime? VerifiedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [ForeignKey("VerifiedById")]
        public virtual ApplicationUser? Verifier { get; set; }

        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}