using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models;
using MediGuard.Models.ViewModels;
using MediGuard.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MediGuard.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
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

            // Server-Side Guard: Pharmacy Manager Email Domain Check
            if (model.Role == "Pharmacy Manager" || model.Role == "Manager")
            {
                var forbiddenDomains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com" };
                var domain = model.Email?.Split('@').LastOrDefault()?.ToLower();

                if (domain != null && forbiddenDomains.Contains(domain))
                {
                    ModelState.AddModelError("Email", "Managers must use an official pharmacy domain email (e.g., manager@citypharmacy.com).");
                    return View(model);
                }
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role ?? "Staff"
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
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

            // Server-Side Guard on Login: Manager Email Domain Check
            if (model.Role == "Pharmacy Manager" || model.Role == "Manager")
            {
                var forbiddenDomains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com" };
                var domain = model.Email?.Split('@').LastOrDefault()?.ToLower();

                if (domain != null && forbiddenDomains.Contains(domain))
                {
                    ModelState.AddModelError("Email", "Managers must use an official pharmacy domain email (e.g., manager@citypharmacy.com).");
                    return View(model);
                }
            }

            // 1. Try AuthService custom authentication (for custom cookie scheme)
            var (isSuccess, errorMessage, principal) = await _authService.AuthenticateUserAsync(model.Email!, model.Password);

            if (isSuccess && principal != null)
            {
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    IdentityConstants.ApplicationScheme,
                    principal,
                    authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            // 2. Fallback to ASP.NET Core Identity SignInManager
            var signInResult = await _signInManager.PasswordSignInAsync(
                model.Email!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("UserProfile", "Account");
            }

            ModelState.AddModelError(string.Empty, errorMessage ?? "Invalid login attempt.");
            return View(model);
        }

        // GET: /Account/UserProfile
        [HttpGet]
        [Authorize]
        public IActionResult UserProfile()
        {
            return View();
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}