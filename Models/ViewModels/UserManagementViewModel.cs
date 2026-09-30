using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class UserManagementViewModel
    {
        // Search & Filter state properties (Ticket 4)
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = "All";
        public string StatusFilter { get; set; } = "All";

        // Main User Grid Collection (Preserves existing UserInfo contract across other tickets)
        public IEnumerable<UserInfo> Users { get; set; } = new List<UserInfo>();

        // Form binding model for "Add New Employee" offcanvas modal (Ticket 4)
        public AddStaffViewModel NewStaff { get; set; } = new AddStaffViewModel();
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Computed alias property ensuring compatibility whether views use .UserId or .Id
        public string Id => UserId;
    }

    public class AddStaffViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Official Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role selection is required.")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Phone is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; } = string.Empty;
    }
}