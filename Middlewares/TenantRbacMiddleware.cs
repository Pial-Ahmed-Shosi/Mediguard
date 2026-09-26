using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;

namespace MediGuard.Middlewares
{
    public class TenantRbacMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantRbacMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = userManager.GetUserId(context.User);

                // Parse the string ID into a Guid
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out Guid userGuid))
                {
                    // 1. Fetch user along with linked Pharmacy tenant
                    var user = await dbContext.Users
                        .Include(u => u.Pharmacy)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userIdStr);

                    if (user != null)
                    {
                        // Populate PharmacyId and Medi+ Subscription Flag in HttpContext
                        context.Items["PharmacyId"] = user.PharmacyId;
                        context.Items["IsMediPlusActive"] = user.Pharmacy?.IsMediPlusActive ?? false;

                        // 2. Fetch User Permissions (Role default + Explicit user grants)
                        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        // Load default permissions based on User Role
                        if (!string.IsNullOrEmpty(user.Role))
                        {
                            var rolePermissions = await dbContext.RolePermissions
                                .Include(rp => rp.Role)
                                .Include(rp => rp.Permission)
                                .Where(rp => rp.Role.Name == user.Role)
                                .Select(rp => rp.Permission.Code)
                                .ToListAsync();

                            foreach (var code in rolePermissions)
                            {
                                permissions.Add(code);
                            }
                        }

                        // Load explicit user-specific permissions (overrides/grants)
                        // Compares Guid up.UserId directly with userGuid (Guid)
                        var userPermissions = await dbContext.UserPermissions
                            .Include(up => up.Permission)
                            .Where(up => up.UserId == userGuid && up.IsGranted)
                            .Select(up => up.Permission.Code)
                            .ToListAsync();

                        foreach (var code in userPermissions)
                        {
                            permissions.Add(code);
                        }

                        // Store compiled permissions set in HttpContext
                        context.Items["UserPermissions"] = permissions;
                    }
                }
            }

            await _next(context);
        }
    }
}