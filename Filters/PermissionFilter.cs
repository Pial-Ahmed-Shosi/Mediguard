using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MediGuard.Filters
{
    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionCode;

        public PermissionFilter(string permissionCode)
        {
            _permissionCode = permissionCode;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1. Enforce Authentication
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return Task.CompletedTask;
            }

            // 2. Extract Compiled Permissions from Middleware Context
            var userPermissions = httpContext.Items["UserPermissions"] as HashSet<string>
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 3. Block request with 403 Forbidden if permission code is missing
            if (!userPermissions.Contains(_permissionCode))
            {
                context.Result = new ForbidResult();
            }

            return Task.CompletedTask;
        }
    }
}