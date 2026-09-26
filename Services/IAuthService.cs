using System.Security.Claims;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IAuthService
    {
        (bool IsValid, string? ErrorMessage) ValidateManagerEmailDomain(string email, string domain);
        Task<(bool IsSuccess, string? ErrorMessage, ClaimsPrincipal? Principal)> AuthenticateUserAsync(string email, string password);
        Task<(bool IsSuccess, string? ErrorMessage)> RegisterPharmacyWithManagerAsync(RegisterPharmacyViewModel model);
    }
}