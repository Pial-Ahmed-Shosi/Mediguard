using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MediGuard.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            string rawRole = model.Role?.Trim() ?? string.Empty;
            string normalizedRole = (string.Equals(rawRole, "Regular User", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(rawRole, "Customer", StringComparison.OrdinalIgnoreCase) ||
                                     string.IsNullOrWhiteSpace(rawRole))
                                     ? "Customer"
                                     : rawRole;

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists. Please log in.");
                return View(model);
            }

            // Server-Side Guard: Pharmacy Manager Email Domain Check
            if (normalizedRole.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            {
                var forbiddenDomains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com" };
                var domain = cleanEmail.Split('@').LastOrDefault()?.ToLower();

                if (domain != null && forbiddenDomains.Contains(domain))
                {
                    ModelState.AddModelError("Email", "Managers must use an official pharmacy domain email (e.g., manager@citypharmacy.com).");
                    return View(model);
                }
            }

            var user = new ApplicationUser
            {
                UserName = cleanEmail,
                Email = cleanEmail,
                FullName = (model.FullName ?? string.Empty).Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                PersonalInfoNote = model.DeliveryAddress?.Trim(),
                Role = normalizedRole,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(normalizedRole));
                }
                await _userManager.AddToRoleAsync(user, normalizedRole);

                var additionalClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Role, normalizedRole),
                    new Claim("PharmacyId", user.PharmacyId?.ToString() ?? string.Empty),
                    new Claim("IsMediPlusActive", (user.Pharmacy?.IsMediPlusActive ?? false).ToString().ToLower())
                };

                await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, additionalClaims);

                TempData["SuccessMessage"] = "Welcome to MediGuard! Your account has been created successfully.";
                return RedirectToAction("UserProfile", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/RegisterPharmacy
        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterPharmacy()
        {
            return View(new RegisterPharmacyViewModel());
        }

        // POST: /Account/RegisterPharmacy
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPharmacy(RegisterPharmacyViewModel model)
        {
            var domainValidation = _authService.ValidateManagerEmailDomain(model.ManagerEmail, model.PharmacyDomain);
            if (!domainValidation.IsValid)
            {
                ModelState.AddModelError(nameof(model.ManagerEmail), domainValidation.ErrorMessage!);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (isSuccess, errorMessage) = await _authService.RegisterPharmacyWithManagerAsync(model);

            if (!isSuccess)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Registration failed.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Pharmacy and Manager account registered successfully. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Pharmacy)
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == cleanEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact system support.");
                return View(model);
            }

            bool isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordCorrect)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault() ?? user.Role ?? "Customer";
            if (string.Equals(primaryRole, "Regular User", StringComparison.OrdinalIgnoreCase))
            {
                primaryRole = "Customer";
            }

            // Server-Side Guard on Login: Manager Email Domain Check
            if (primaryRole.Contains("Manager", StringComparison.OrdinalIgnoreCase) && user.Pharmacy != null)
            {
                string pharmacyDomain = user.Pharmacy.Domain ?? string.Empty;
                if (!string.IsNullOrEmpty(pharmacyDomain))
                {
                    string cleanDomain = pharmacyDomain.TrimStart('@').ToLower();
                    if (!user.Email!.EndsWith("@" + cleanDomain, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(string.Empty, $"Manager email domain does not match registered pharmacy domain (@{cleanDomain}).");
                        return View(model);
                    }
                }
            }

            var additionalClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, primaryRole),
                new Claim("PharmacyId", user.PharmacyId?.ToString() ?? string.Empty),
                new Claim("IsMediPlusActive", (user.Pharmacy?.IsMediPlusActive ?? false).ToString().ToLower())
            };

            await _signInManager.SignInWithClaimsAsync(user, isPersistent: model.RememberMe, additionalClaims);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (string.Equals(primaryRole, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("UserProfile", "Account");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/UserProfile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.Pharmacy)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var orders = await _context.Orders
                .Include(o => o.Pharmacy)
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == userId || o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(15)
                .ToListAsync();

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Order)
                .Where(p => p.CustomerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(15)
                .ToListAsync();

            var model = new CustomerProfileViewModel
            {
                User = user,
                RecentOrders = orders,
                RecentPrescriptions = prescriptions,
                ActiveOrdersCount = orders.Count(o => o.DeliveryStatus == "IN_TRANSIT" || o.DeliveryStatus == "PENDING" || o.Status == "IN_TRANSIT"),
                TotalOrdersCount = orders.Count,
                PendingPrescriptionsCount = prescriptions.Count(p => p.Status == "PENDING_VERIFICATION" || p.Status == "PENDING"),
                DeliveryAddress = user.PersonalInfoNote
            };

            return View(model);
        }

        // POST: /Account/UpdateProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string deliveryAddress)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    user.FullName = fullName.Trim();
                }

                user.PhoneNumber = phoneNumber?.Trim();
                user.PersonalInfoNote = deliveryAddress?.Trim();

                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "Your profile details have been updated successfully.";
            }

            return RedirectToAction("UserProfile");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // POST / GET: /Account/Logout
        [AcceptVerbs("GET", "POST")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}