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

        public Guid? CategoryId { get; set; }

        public Guid? ManufacturerId { get; set; }

        [Required]
        [MaxLength(150)]
        public string BrandName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string GenericName { get; set; } = string.Empty;

        public bool RequiresPrescription { get; set; } = false;

        [MaxLength(30)]
        public string Unit { get; set; } = "Tablet";

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Property wrapper for backward compatibility with views/services referencing 'Name'
        [NotMapped]
        public string Name
        {
            get => BrandName;
            set => BrandName = value;
        }

        // Foreign Key Navigation Properties
        [ForeignKey(nameof(PharmacyId))]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        [ForeignKey(nameof(ManufacturerId))]
        public virtual Manufacturer? Manufacturer { get; set; }

        // Collection Navigation Properties
        public virtual ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
        public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
    }
}