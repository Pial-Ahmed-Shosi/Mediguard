namespace MediGuard.Models
{
    public class UserPermission
    {
        public Guid UserId { get; set; }

        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;

        public bool IsGranted { get; set; } = true;
    }
}