using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SwiftFill.Models;

namespace SwiftFill.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SignUpAs()
        {
            TempData["SuccessMessage"] = "Successfully created account. Welcome to SwiftFill!";
            
            // Standard registrations default to Customer view in this mockup.
            // Admins/Operators are created internally.
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordAction(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "No account found with this email address.";
                return RedirectToAction("ForgotPassword");
            }

            // We would integrate the EmailSender here in the real Identity setup.
            TempData["SuccessMessage"] = $"A password reset link has been sent to {email}. Please check your inbox.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> LoginAction(string email, string password, bool rememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null) return Unauthorized();

                var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isStaff = await _userManager.IsInRoleAsync(user, "WarehouseStaff");
                var isRider = await _userManager.IsInRoleAsync(user, "DeliveryRider");
                
                TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";
                
                if (isSuperAdmin) return RedirectToAction("Index", "SuperAdmin");
                if (isAdmin) return RedirectToAction("Index", "Admin");
                if (isStaff) return RedirectToAction("Index", "Warehouse");
                if (isRider) return RedirectToAction("Index", "Rider");
                return RedirectToAction("Index", "Customer");
            }

            TempData["ErrorMessage"] = "Invalid login attempt. Please check your email and password.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
