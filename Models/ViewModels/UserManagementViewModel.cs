using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public IEnumerable<UserInfo> Users { get; set; } = new List<UserInfo>();
    }

    public class UserInfo
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}