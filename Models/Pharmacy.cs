using System;
using System.Collections.Generic;
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

        // Existing Navigation Property (Users)
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        // Ticket 19 Navigation Properties (Inventory Classification)
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Manufacturer> Manufacturers { get; set; } = new List<Manufacturer>();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsOpen
        {
            get => PharmacyOperatingStatusStore.IsBranchOpen(Id);
            set => PharmacyOperatingStatusStore.SetBranchOpen(Id, value);
        }
    }

    public static class PharmacyOperatingStatusStore
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, bool> _branchStatus = new();

        public static bool IsBranchOpen(Guid pharmacyId)
        {
            return _branchStatus.GetOrAdd(pharmacyId, true);
        }

        public static void SetBranchOpen(Guid pharmacyId, bool isOpen)
        {
            _branchStatus[pharmacyId] = isOpen;
        }
    }
}