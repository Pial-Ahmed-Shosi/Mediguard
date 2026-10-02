using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediGuard.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid PharmacyId { get; set; }

        public Guid? BatchId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UrgencyLevel { get; set; } = string.Empty; // "CRITICAL" or "WARNING"

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // "EXPIRY_ALERT"

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}