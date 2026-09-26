using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        // Forbidden public email providers for manager accounts
        private static readonly HashSet<string> PublicEmailDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com",
            "icloud.com", "protonmail.com", "aol.com", "yandex.com", "zoho.com"
        };

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public (bool IsValid, string? ErrorMessage) ValidateManagerEmailDomain(string email, string domain)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return (false, "A valid manager email address is required.");
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                return (false, "Pharmacy domain is required.");
            }

            string cleanDomain = domain.TrimStart('@').Trim().ToLower();
            string emailDomain = email.Split('@')[1].Trim().ToLower();

            if (PublicEmailDomains.Contains(emailDomain))
            {
                return (false, $"Managers must use an official pharmacy domain email (e.g., manager@{cleanDomain}). Public domains like {emailDomain} are forbidden.");
            }

            if (!email.EndsWith("@" + cleanDomain, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Manager email domain must match the pharmacy domain (@{cleanDomain}).");
            }

            return (true, null);
        }

        public async Task<(bool IsSuccess, string? ErrorMessage, ClaimsPrincipal? Principal)> AuthenticateUserAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Pharmacy)
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return (false, "Invalid email or password.", null);
            }

            if (!user.IsActive)
            {
                return (false, "Your account is inactive. Please contact system support.", null);
            }

            bool isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                return (false, "Invalid email or password.", null);
            }

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault() ?? user.Role ?? "Regular User";

            // Enforce domain check on login for managers
            if (primaryRole.Contains("Manager", StringComparison.OrdinalIgnoreCase) && user.Pharmacy != null)
            {
                string pharmacyDomain = user.Pharmacy.Domain ?? string.Empty;
                if (!string.IsNullOrEmpty(pharmacyDomain))
                {
                    string cleanDomain = pharmacyDomain.TrimStart('@').ToLower();
                    if (!user.Email!.EndsWith("@" + cleanDomain, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, $"Manager email domain does not match the registered pharmacy domain (@{cleanDomain}).", null);
                    }
                }
            }

            bool isMediPlusActive = user.Pharmacy?.IsMediPlusActive ?? false;
            string displayName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email!;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, primaryRole),
                new Claim("PharmacyId", user.PharmacyId?.ToString() ?? string.Empty),
                new Claim("IsMediPlusActive", isMediPlusActive.ToString().ToLower())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return (true, null, new ClaimsPrincipal(identity));
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> RegisterPharmacyWithManagerAsync(RegisterPharmacyViewModel model)
        {
            var domainCheck = ValidateManagerEmailDomain(model.ManagerEmail, model.PharmacyDomain);
            if (!domainCheck.IsValid)
            {
                return (false, domainCheck.ErrorMessage);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.ManagerEmail);
            if (existingUser != null)
            {
                return (false, "A user with this email address already exists.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pharmacy = new Pharmacy
                {
                    Id = Guid.NewGuid(),
                    Name = model.PharmacyName,
                    Domain = model.PharmacyDomain.TrimStart('@').Trim().ToLower(),
                    LicenseNumber = model.LicenseNumber,
                    Address = model.Address,
                    City = model.City,
                    IsMediPlusActive = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Pharmacies.Add(pharmacy);
                await _context.SaveChangesAsync();

                if (!await _roleManager.RoleExistsAsync("Pharmacy Manager"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Pharmacy Manager"));
                }

                var managerUser = new ApplicationUser
                {
                    UserName = model.ManagerEmail.ToLower(),
                    Email = model.ManagerEmail.ToLower(),
                    FullName = model.FullName,
                    PhoneNumber = model.Phone,
                    PersonalInfoNote = model.PersonalInfoNote,
                    PharmacyId = pharmacy.Id,
                    Role = "Pharmacy Manager",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(managerUser, model.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return (false, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }

                var roleResult = await _userManager.AddToRoleAsync(managerUser, "Pharmacy Manager");
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return (false, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"An error occurred during registration: {ex.Message}");
            }
        }
    }
}