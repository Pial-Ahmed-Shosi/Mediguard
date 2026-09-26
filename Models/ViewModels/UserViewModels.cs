using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role selection is required.")]
        public string Role { get; set; } = string.Empty; // Pharmacist, Cashier, or Deliveryman
    }

    public class StaffUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty; // Identity User Id is string
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid PharmacyId { get; set; } // Tenant Pharmacy ID is Guid
        public bool IsActive { get; set; }
        public List<int> AssignedPermissionIds { get; set; } = new();
    }

    public class StaffPagedListViewModel
    {
        public List<StaffUserListItemViewModel> Users { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; } = string.Empty;
        public string RoleFilter { get; set; } = string.Empty;
    }

    public class UpdatePermissionsViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty; // User Id is string

        public List<int> PermissionIds { get; set; } = new();
    }
}