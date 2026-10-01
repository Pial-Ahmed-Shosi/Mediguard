using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    // Staff user permission assignment modal data transport model.
    public class RoleAssignmentViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
      
        // List of active permission keys assigned to the user.
 
        public List<string> Permissions { get; set; } = new List<string>();

   
        // Compatibility alias mapping to Permissions for view/controller binding flexibility.
   
        public List<string> SelectedPermissions
        {
            get => Permissions;
            set => Permissions = value ?? new List<string>();
        }

        // Indicates if the pharmacy tenant has an active Medi+ subscription.
      
        public bool IsMediPlusActive { get; set; }

    
        // Indicates if the user currently holds b2b.mediplus.access.
    
        public bool HasMediPlusAccess { get; set; }
    }

 
    // Payload structure for POST /User/UpdatePermissions API

    public class UpdatePermissionsRequestModel
    {
        public string UserId { get; set; } = string.Empty; 
        public List<string> Permissions { get; set; } =new List<string>();
    }
}