using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("medicines")]
    public class Medicine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        [StringLength(255)]
        public string BrandName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string GenericName { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }

        public Guid? ManufacturerId { get; set; }

        public bool RequiresPrescription { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } = "Tablet";

        [StringLength(100)]
        public string? Barcode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer? Manufacturer { get; set; }

        public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
    }
}
