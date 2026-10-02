using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property to Pharmacy Tenant
        [ForeignKey(nameof(PharmacyId))]
        public virtual Pharmacy Pharmacy { get; set; } = null!;

        // Navigation Property to Medicines
        public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    }
}