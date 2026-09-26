using Microsoft.AspNetCore.Mvc;
using MediGuardApp.Models.ViewModels;
using System.Linq;

namespace MediGuardApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Server-Side Guard: Pharmacy Manager Email Domain Check
            if (model.Role == "Pharmacy Manager")
            {
                var forbiddenDomains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com" };
                var domain = model.Email.Split('@').LastOrDefault()?.ToLower();

                if (forbiddenDomains.Contains(domain))
                {
                    ModelState.AddModelError("Email", "Managers must use an official pharmacy domain email (e.g., manager@citypharmacy.com).");
                    return View(model);
                }
            }

            // Registration Logic Here (Save to DB, Assign Roles)
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Server-Side Guard on Login
            if (model.Role == "Pharmacy Manager")
            {
                var forbiddenDomains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com" };
                var domain = model.Email.Split('@').LastOrDefault()?.ToLower();

                if (forbiddenDomains.Contains(domain))
                {
                    ModelState.AddModelError("Email", "Managers must use an official pharmacy domain email (e.g., manager@citypharmacy.com).");
                    return View(model);
                }
            }

            // Perform Authentication Logic Here
            return RedirectToAction("UserProfile", "Account");
        }

        public IActionResult UserProfile()
        {
            return View();
        }
    }
}