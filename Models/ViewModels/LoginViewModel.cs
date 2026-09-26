using System.ComponentModel.DataAnnotations;

namespace MediGuardApp.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        // Used to enforce client/server domain rules before DB lookup
        public string Role { get; set; } = "Regular User";
    }
}