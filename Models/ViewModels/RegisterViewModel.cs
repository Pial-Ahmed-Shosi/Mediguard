using System.ComponentModel.DataAnnotations;

namespace MediGuardApp.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please select a role.")]
        public string Role { get; set; } = "Regular User";

        // --- Common Fields ---
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // --- Role-Specific Fields (Nullable for conditional validation) ---

        // Regular User
        public string? PhoneNumber { get; set; }
        public string? DeliveryAddress { get; set; }

        // Pharmacy Manager
        public string? PharmacyName { get; set; }
        public string? PharmacyLicenseNumber { get; set; }

        // Deliveryman
        public string? VehicleType { get; set; }
        public string? DrivingLicenseNumber { get; set; }

        // Admin
        public string? AdminAccessCode { get; set; }
        public string? Department { get; set; }
    }
}