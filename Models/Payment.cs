using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid PharmacyId { get; set; }

        [StringLength(150)]
        public string? GatewayTransactionId { get; set; }

        // Retained for backward/cross-module compatibility
        [StringLength(150)]
        public string? TransactionId { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "CARD"; // CARD, WALLET, BANK

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED

        [Column(TypeName = "jsonb")]
        public string? GatewayResponseJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [ForeignKey(nameof(PharmacyId))]
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}