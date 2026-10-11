
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.ViewModels;

namespace MediGuard.Controllers
{
    [Authorize(Roles = "Manager")]
    public class AuditController : Controller
    {
        [HttpGet]
        public IActionResult Index(
            DateTime? startDate,
            DateTime? endDate,
            string? userId,
            string? actionType)
        {
            var model = new AuditLogPageViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedUserId = userId,
                SelectedAction = actionType
            };

            // TODO: Load audit entries and user options from the
            // existing tenant-scoped audit service/repository.
            //
            // IMPORTANT:
            // Do not load all tenants' audit records here.
            // Derive the current tenant from the authenticated
            // user's trusted server-side identity/claims.

            return View(model);
        }
    }
}
