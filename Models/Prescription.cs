using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models
{
    public class Prescription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PharmacyId { get; set; }
        public string Status { get; set; } = "PENDING_VERIFICATION";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Pharmacy? Pharmacy { get; set; }
    }
}