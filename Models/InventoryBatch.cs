using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("inventory_batches")]
    public class InventoryBatch
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("pharmacy_id")]
        public Guid PharmacyId { get; set; }

        [Required]
        [Column("medicine_id")]
        public Guid MedicineId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("batch_number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("purchase_price", TypeName = "numeric(10,2)")]
        public decimal PurchasePrice { get; set; }

        [Required]
        [Column("selling_price", TypeName = "numeric(10,2)")]
        public decimal SellingPrice { get; set; }

        [Required]
        [Column("mfg_date", TypeName = "date")]
        public DateTime MfgDate { get; set; }

        [Required]
        [Column("expiry_date", TypeName = "date")]
        public DateTime ExpiryDate { get; set; }

        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "ACTIVE"; // "ACTIVE", "EXPIRED", "DISPOSED"

        [Column("low_stock_threshold")]
        public int LowStockThreshold { get; set; } = 10;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey("MedicineId")]
        public virtual Medicine? Medicine { get; set; }
    }
}