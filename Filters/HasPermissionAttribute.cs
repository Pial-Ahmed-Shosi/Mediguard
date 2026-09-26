using Microsoft.AspNetCore.Mvc;

namespace MediGuard.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permissionCode) : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { permissionCode };
        }
    }
}