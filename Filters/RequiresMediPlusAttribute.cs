using Microsoft.AspNetCore.Mvc;

namespace MediGuard.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequiresMediPlusAttribute : TypeFilterAttribute
    {
        public RequiresMediPlusAttribute() : base(typeof(RequiresMediPlusFilter))
        {
        }
    }
}