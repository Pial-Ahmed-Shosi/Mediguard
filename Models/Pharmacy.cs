using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class Pharmacy
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Domain { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        public bool IsMediPlusActive { get; set; } = false;
        public DateTime? MediPlusExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for multi-tenant users
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}