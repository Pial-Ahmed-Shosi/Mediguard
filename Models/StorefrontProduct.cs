using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    public class StorefrontProduct
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        public Guid? CategoryId { get; set; }

        [Required, MaxLength(150)]
        public string BrandName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string GenericName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required, Column(TypeName = "numeric(12,2)")]
        public decimal UnitPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public bool RequiresPrescription { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Pharmacy? Pharmacy { get; set; }

        public Category? Category { get; set; }
    }
}