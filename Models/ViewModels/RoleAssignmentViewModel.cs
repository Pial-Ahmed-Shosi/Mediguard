using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    /// <summary>
    /// Staff user permission assignment modal data transport model.
    /// </summary>
    public class RoleAssignmentViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        /// <summary>
        /// List of active permission keys assigned to the user.
        /// </summary>
        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// Compatibility alias mapping to Permissions for view/controller binding flexibility.
        /// </summary>
        public List<string> SelectedPermissions
        {
            get => Permissions;
            set => Permissions = value ?? new List<string>();
        }

        /// <summary>
        /// Indicates if the pharmacy tenant has an active Medi+ subscription.
        /// </summary>
        public bool IsMediPlusActive { get; set; }

        /// <summary>
        /// Indicates if the user currently holds b2b.mediplus.access.
        /// </summary>
        public bool HasMediPlusAccess { get; set; }
    }

    /// <summary>
    /// Payload structure for POST /User/UpdatePermissions API
    /// </summary>
    public class UpdatePermissionsRequestModel
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
}