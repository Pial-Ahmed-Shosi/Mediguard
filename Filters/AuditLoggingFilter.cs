using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Services;

namespace MediGuard.Filters
{
    /// <summary>
    /// Attribute to exclude an action or controller from automated audit logging.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class IgnoreAuditLoggingAttribute : Attribute
    {
    }

    /// <summary>
    /// Global or endpoint action filter that intercepts state mutations (POST, PUT, PATCH, DELETE)
    /// and logs audit diffs asynchronously without blocking the main request pipeline.
    /// </summary>
    public class AuditLoggingFilter : IAsyncActionFilter
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLoggingFilter> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly HashSet<string> MutationMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

        public AuditLoggingFilter(
            IAuditLogService auditLogService,
            ILogger<AuditLoggingFilter> logger,
            IServiceScopeFactory scopeFactory)
        {
            _auditLogService = auditLogService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            // 1. Only intercept state-modifying HTTP methods (POST, PUT, PATCH, DELETE)
            if (!MutationMethods.Contains(request.Method))
            {
                await next();
                return;
            }

            // 2. Check if action or controller opted out of audit logging
            if (context.ActionDescriptor.EndpointMetadata.Any(em => em is IgnoreAuditLoggingAttribute))
            {
                await next();
                return;
            }

            // 3. Extract Controller and Action metadata
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? request.Method;

            string formattedAction = FormatAuditAction(request.Method, actionName, controllerName);
            string entityName = Truncate(controllerName, 100);

            // 4. Extract and sanitize request arguments for state capture
            string? newValuesJson = SerializeActionArguments(context.ActionArguments);
            string? oldValuesJson = httpContext.Items["AuditOldValues"]?.ToString();

            // 5. Extract Client and User Context
            var userId = ExtractUserId(httpContext);
            var pharmacyId = ExtractPharmacyId(httpContext);
            var ipAddress = ExtractIpAddress(httpContext);

            // 6. Execute the underlying action in the pipeline
            ActionExecutedContext executedContext;
            try
            {
                executedContext = await next();
            }
            catch (Exception)
            {
                // If the underlying action throws an unhandled exception, do not log a successful mutation
                throw;
            }

            // If action threw an unhandled exception or returned an error status code, skip logging
            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            {
                return;
            }

            if (executedContext.Result is StatusCodeResult sc && sc.StatusCode >= 400)
            {
                return;
            }

            if (executedContext.Result is ObjectResult obj && obj.StatusCode.HasValue && obj.StatusCode.Value >= 400)
            {
                return;
            }

            // Check if action enriched old or new values during execution
            if (httpContext.Items.TryGetValue("AuditOldValues", out var lateOldValues) && lateOldValues != null)
            {
                oldValuesJson = lateOldValues.ToString();
            }

            if (httpContext.Items.TryGetValue("AuditNewValues", out var lateNewValues) && lateNewValues != null)
            {
                newValuesJson = lateNewValues.ToString();
            }

            // If PharmacyId was populated or resolved during action execution, refresh it
            if (!pharmacyId.HasValue || pharmacyId.Value == Guid.Empty)
            {
                pharmacyId = ExtractPharmacyId(httpContext);
            }

            // If PharmacyId is still missing, check action arguments
            if (!pharmacyId.HasValue || pharmacyId.Value == Guid.Empty)
            {
                if (context.ActionArguments.TryGetValue("pharmacyId", out var pVal) &&
                    pVal is Guid g && g != Guid.Empty)
                {
                    pharmacyId = g;
                }
            }

            // Fallback: Check user's pharmacy or active pharmacy from DB so no mutations are dropped
            if (!pharmacyId.HasValue || pharmacyId.Value == Guid.Empty)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                        if (user?.PharmacyId != null && user.PharmacyId != Guid.Empty)
                        {
                            pharmacyId = user.PharmacyId;
                        }
                    }

                    if (!pharmacyId.HasValue || pharmacyId.Value == Guid.Empty)
                    {
                        var defaultPharmacyId = await db.Pharmacies.AsNoTracking().Select(p => p.Id).FirstOrDefaultAsync();
                        if (defaultPharmacyId != Guid.Empty)
                        {
                            pharmacyId = defaultPharmacyId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve fallback PharmacyId for audit logging.");
                }
            }

            // AuditLog requires a valid non-empty foreign key referencing Pharmacy
            if (!pharmacyId.HasValue || pharmacyId.Value == Guid.Empty)
            {
                return;
            }

            var auditEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId.Value,
                UserId = userId,
                Action = formattedAction,
                EntityName = entityName,
                OldValuesJson = oldValuesJson,
                NewValuesJson = newValuesJson,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            // 7. Execute audit logging asynchronously in the background without blocking the primary HTTP response pipeline
            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditLogService.LogAsync(auditEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background audit logging failure for Action '{Action}' on '{Entity}'.",
                        auditEntry.Action, auditEntry.EntityName);
                }
            });
        }

        private static string FormatAuditAction(string method, string actionName, string controllerName)
        {
            string verb = actionName.ToUpperInvariant();
            string target = controllerName.ToUpperInvariant();

            if (verb.Contains(target))
            {
                return Truncate(verb, 50);
            }

            string actionPrefix = verb switch
            {
                "CREATE" or "POST" or "ADD" or "INSERT" => "INSERT",
                "EDIT" or "UPDATE" or "PUT" or "SAVE" => "UPDATE",
                "DELETE" or "REMOVE" or "DESTROY" => "DELETE",
                "DISPOSE" => "DISPOSE",
                "PATCH" => "PATCH",
                _ => verb.StartsWith("CREATE") || verb.StartsWith("INSERT") || verb.StartsWith("ADD") ? "INSERT" :
                     verb.StartsWith("EDIT") || verb.StartsWith("UPDATE") || verb.StartsWith("PUT") ? "UPDATE" :
                     verb.StartsWith("DELETE") || verb.StartsWith("REMOVE") ? "DELETE" :
                     method switch
                     {
                         "POST" => "INSERT",
                         "PUT" or "PATCH" => "UPDATE",
                         "DELETE" => "DELETE",
                         _ => verb
                     }
            };

            string result = $"{actionPrefix}_{target}";
            return Truncate(result, 50);
        }

        private static string? ExtractUserId(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("id")?.Value
                ?? user.Identity.Name;
        }

        private static Guid? ExtractPharmacyId(HttpContext context)
        {
            // Check HttpContext.Items (populated by TenantRbacMiddleware)
            if (context.Items["PharmacyId"] is Guid pGuid && pGuid != Guid.Empty)
            {
                return pGuid;
            }

            if (context.Items["PharmacyId"] is string pStr &&
                Guid.TryParse(pStr, out var parsedGuid) && parsedGuid != Guid.Empty)
            {
                return parsedGuid;
            }

            // Check Claims (JWT claims / authentication claims)
            var claimValue = context.User?.FindFirst("PharmacyId")?.Value
                ?? context.User?.FindFirst(ClaimTypes.GroupSid)?.Value;

            if (!string.IsNullOrEmpty(claimValue) &&
                Guid.TryParse(claimValue, out var claimGuid) && claimGuid != Guid.Empty)
            {
                return claimGuid;
            }

            // Check RouteData
            if (context.GetRouteData()?.Values.TryGetValue("pharmacyId", out var routeVal) == true &&
                routeVal != null &&
                Guid.TryParse(routeVal.ToString(), out var routeGuid) && routeGuid != Guid.Empty)
            {
                return routeGuid;
            }

            return null;
        }

        private static string? ExtractIpAddress(HttpContext context)
        {
            var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedHeader))
            {
                var ip = forwardedHeader.Split(',')[0].Trim();
                if (ip.Length <= 45)
                {
                    return ip;
                }
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrWhiteSpace(remoteIp) && remoteIp.Length > 45)
            {
                remoteIp = remoteIp[..45];
            }

            return remoteIp;
        }

        private static string? SerializeActionArguments(IDictionary<string, object?> arguments)
        {
            if (arguments == null || arguments.Count == 0)
            {
                return null;
            }

            try
            {
                var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (key, value) in arguments)
                {
                    // Skip stream / binary / token objects that cannot or should not be serialized
                    if (value is CancellationToken or Stream or IFormFile or IFormFileCollection)
                    {
                        continue;
                    }

                    if (IsSensitiveKey(key))
                    {
                        sanitized[key] = "[REDACTED]";
                    }
                    else
                    {
                        sanitized[key] = value;
                    }
                }

                return JsonSerializer.Serialize(sanitized);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSensitiveKey(string key)
        {
            return key.Contains("password", StringComparison.OrdinalIgnoreCase)
                || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || key.Contains("token", StringComparison.OrdinalIgnoreCase)
                || key.Contains("cvv", StringComparison.OrdinalIgnoreCase)
                || key.Contains("creditcard", StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
