using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MediGuard.Filters
{
    public class RequiresMediPlusFilter : IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1. Enforce Authentication
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return Task.CompletedTask;
            }

            // 2. Read Subscription Status from Middleware Context
            var isMediPlusActive = httpContext.Items["IsMediPlusActive"] as bool? ?? false;

            if (!isMediPlusActive)
            {
                // Detect AJAX / API requests vs traditional page navigation
                var isJsonRequest = httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                                 || (httpContext.Request.Headers["Accept"].ToString().Contains("application/json"));

                if (isJsonRequest)
                {
                    context.Result = new ForbidResult();
                }
                else
                {
                    context.Result = new RedirectToActionResult("MediPlusConfig", "Admin", new { reason = "subscription_required" });
                }
            }

            return Task.CompletedTask;
        }
    }
}