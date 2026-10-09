using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid MedicineId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        [Required]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal LineTotal { get; set; }

        [MaxLength(100)]
        public string? BatchNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("MedicineId")]
        public virtual Medicine Medicine { get; set; } = null!;
    }
}
