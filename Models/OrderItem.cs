using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid MedicineId { get; set; }

        [Required]
        public Guid BatchId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal LineTotal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("MedicineId")]
        public virtual Medicine? Medicine { get; set; }

        [ForeignKey("BatchId")]
        public virtual Batch? Batch { get; set; }
    }
}