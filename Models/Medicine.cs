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
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("pharmacy_id")]
        public Guid PharmacyId { get; set; }

        [Column("category_id")]
        public Guid? CategoryId { get; set; }

        [Column("manufacturer_id")]
        public Guid? ManufacturerId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("brand_name")]
        public string BrandName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("generic_name")]
        public string GenericName { get; set; } = string.Empty;

        [Column("requires_prescription")]
        public bool RequiresPrescription { get; set; } = false;

        [MaxLength(30)]
        [Column("unit")]
        public string Unit { get; set; } = "Tablet";

        [MaxLength(100)]
        [Column("barcode")]
        public string? Barcode { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer? Manufacturer { get; set; }

        // Collection Navigation for FEFO Inventory Lookup
        public virtual ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
    }
}