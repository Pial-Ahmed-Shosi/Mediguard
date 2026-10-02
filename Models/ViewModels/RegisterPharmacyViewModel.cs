using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class RegisterPharmacyViewModel
    {
        // --- Pharmacy / Organisation Details ---
        [Required(ErrorMessage = "Pharmacy / Organisation name is required.")]
        [Display(Name = "Pharmacy / Organisation Name")]
        public string PharmacyName { get; set; } = string.Empty;

        // Alias for backwards compatibility
        public string OrganisationName
        {
            get => PharmacyName;
            set => PharmacyName = value;
        }

        [Required(ErrorMessage = "Pharmacy domain is required.")]
        [Display(Name = "Pharmacy Domain (e.g., citypharmacy.com)")]
        public string PharmacyDomain { get; set; } = string.Empty;

        // Alias for backwards compatibility
        public string PharmacyEmailDomain
        {
            get => PharmacyDomain;
            set => PharmacyDomain = value;
        }

        [Required(ErrorMessage ="License number is required.")]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business address is required.")]
        [Display(Name = "Business Address")]
        public string Address { get; set; } = string.Empty;

        // Alias for backwards compatibility 
        public string BusinessAddress
        {
            get => Address;
            set => Address = value;
        }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tax ID is required.")]
        [Display(Name = "Tax ID")]
        public string TaxID { get; set; } = string.Empty;

        // --- Manager Profile Details ---

        [Required(ErrorMessage = "Manager full name is required.")]
        [Display(Name = "Manager Full Name")]
        public string FullName { get; set; } = string.Empty;

        // Alias for backwards compatibility
        public string ManagerFullName
        {
            get => FullName;
            set => FullName = value;
        }

        [Required(ErrorMessage = "Manager official email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Manager Official Email")]
        public string ManagerEmail { get; set; } = string.Empty;

        // Alias for backwards compatibility
        public string ManagerAuthenticationEmail
        {
            get => ManagerEmail;
            set => ManagerEmail = value;
        }

        [Phone(ErrorMessage = "Invalid phone number.")]
        [Display(Name = "Personal Phone Number")]
        public string? Phone { get; set; }

        // Alias for backwards compatibility
        public string? PersonalPhoneNumber
        {
            get => Phone;
            set => Phone = value;
        }

        public string? PersonalInfoNote { get; set; }

        // Alias for backwards compatibility
        public string? PersonalContactNote
        {
            get => PersonalInfoNote;
            set => PersonalInfoNote = value;
        }

        public string? EmployeeID { get; set; }

        // --- Authentication Details ---

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special symbol.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}