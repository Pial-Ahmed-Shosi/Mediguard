using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        // Multi-Tenant context linking user to a specific Pharmacy
        public Guid? PharmacyId { get; set; }
        public virtual Pharmacy? Pharmacy { get; set; }

        public string? PersonalInfoNote { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "Staff";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}