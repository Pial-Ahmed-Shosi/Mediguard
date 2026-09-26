using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (isSuccess, errorMessage, principal) = await _authService.AuthenticateUserAsync(model.Email, model.Password);

            if (!isSuccess || principal == null)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Invalid login attempt.");
                return View(model);
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/RegisterPharmacy
        [HttpGet]
        public IActionResult RegisterPharmacy()
        {
            return View(new RegisterPharmacyViewModel());
        }

        // POST: /Account/RegisterPharmacy
        [HttpPost]
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

        // POST: /Account/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/UserProfile
        [HttpGet]
        [Authorize]
        public IActionResult UserProfile()
        {
            return View();
        }
    }
}