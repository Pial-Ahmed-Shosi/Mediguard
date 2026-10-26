using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [NotMapped]
        public string? ProfileImageUrl
        {
            get => GetMetadata().ProfileImageUrl;
            set { var m = GetMetadata(); m.ProfileImageUrl = value; SaveMetadata(m); }
        }

        [NotMapped]
        public string? DeliveryAddress
        {
            get => GetMetadata().DeliveryAddress;
            set { var m = GetMetadata(); m.DeliveryAddress = value; SaveMetadata(m); }
        }

        [NotMapped]
        public string? EmergencyContact
        {
            get => GetMetadata().EmergencyContact;
            set { var m = GetMetadata(); m.EmergencyContact = value; SaveMetadata(m); }
        }

        [NotMapped]
        public string? VehicleDetails
        {
            get => GetMetadata().VehicleDetails;
            set { var m = GetMetadata(); m.VehicleDetails = value; SaveMetadata(m); }
        }

        [NotMapped]
        public List<SavedPaymentMethodDto> SavedPaymentMethods
        {
            get => GetMetadata().SavedPaymentMethods ?? new();
            set { var m = GetMetadata(); m.SavedPaymentMethods = value; SaveMetadata(m); }
        }

        public UserProfileMetadata GetMetadata()
        {
            if (string.IsNullOrWhiteSpace(PersonalInfoNote))
            {
                return new UserProfileMetadata();
            }

            if (PersonalInfoNote.TrimStart().StartsWith("{"))
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<UserProfileMetadata>(PersonalInfoNote) ?? new UserProfileMetadata();
                }
                catch
                {
                    return new UserProfileMetadata { DeliveryAddress = PersonalInfoNote };
                }
            }

            return new UserProfileMetadata { DeliveryAddress = PersonalInfoNote };
        }

        public void SaveMetadata(UserProfileMetadata meta)
        {
            PersonalInfoNote = System.Text.Json.JsonSerializer.Serialize(meta);
        }
    }

    public class UserProfileMetadata
    {
        public string? ProfileImageUrl { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? EmergencyContact { get; set; }
        public string? VehicleDetails { get; set; }
        public List<SavedPaymentMethodDto> SavedPaymentMethods { get; set; } = new();
    }

    public class SavedPaymentMethodDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Type { get; set; } = "CARD"; // "CARD", "BKASH", "NAGAD", "ROCKET"
        public string Title { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? Expiry { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}