using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class RegisterPharmacyViewModel
    {
        // --- Pharmacy Business Info ---
        [Required(ErrorMessage = "Organisation Name is required")]
        public string OrganisationName { get; set; }

        [Required(ErrorMessage = "License Number is required")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Business Address is required")]
        public string BusinessAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Required(ErrorMessage = "Tax ID is required")]
        public string TaxID { get; set; }

        [Required(ErrorMessage = "Pharmacy Email Domain is required (e.g. citypharmacy.com)")]
        public string PharmacyEmailDomain { get; set; }

        // --- Manager Profile Details ---
        [Required(ErrorMessage = "Manager Full Name is required")]
        public string ManagerFullName { get; set; }

        [Required(ErrorMessage = "Personal Phone Number is required")]
        [Phone]
        public string PersonalPhoneNumber { get; set; }

        public string PersonalContactNote { get; set; }

        public string EmployeeID { get; set; }

        [Required(ErrorMessage = "Manager Authentication Email is required")]
        [EmailAddress]
        public string ManagerAuthenticationEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number and one special symbol.")]
        public string Password { get; set; }
    }
}