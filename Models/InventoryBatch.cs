using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("inventory_batches")]
    public class InventoryBatch
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        public Guid MedicineId { get; set; }

        [Required]
        [MaxLength(100)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "DATE")]
        public DateTime MfgDate { get; set; }

        [Column(TypeName = "DATE")]
        public DateTime ExpiryDate { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "ACTIVE";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(PharmacyId))]
        public virtual Pharmacy? Pharmacy { get; set; }

        [ForeignKey(nameof(MedicineId))]
        public virtual Medicine? Medicine { get; set; }
    }
}