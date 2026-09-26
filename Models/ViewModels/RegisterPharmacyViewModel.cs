using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class RegisterPharmacyViewModel
    {
        // Pharmacy Details
        [Required(ErrorMessage = "Pharmacy name is required.")]
        [Display(Name = "Pharmacy Name")]
        public string PharmacyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pharmacy domain is required.")]
        [Display(Name = "Pharmacy Domain (e.g., citypharmacy.com)")]
        public string PharmacyDomain { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required.")]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        // Manager Account Details
        [Required(ErrorMessage = "Manager full name is required.")]
        [Display(Name = "Manager Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Manager email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Manager Official Email")]
        public string ManagerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? Phone { get; set; }

        public string? PersonalInfoNote { get; set; }
    }
}