using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using SwiftFill.Services;

namespace SwiftFill.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _audit;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            AuditLogService audit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _audit = audit;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult SignUp() => View();

        [HttpPost]
        public IActionResult SignUpAs()
        {
            TempData["SuccessMessage"] = "Successfully created account. Welcome to SwiftFill!";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordAction(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "No account found with this email address.";
                return RedirectToAction("ForgotPassword");
            }
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

                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "User";
                var displayName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(displayName)) displayName = user.Email ?? user.UserName ?? "Unknown";

                // ── Security log: successful login ──
                _audit.Log(
                    actor: displayName,
                    role: roleName,
                    action: "Login",
                    detail: $"{displayName} ({roleName}) signed in from {HttpContext.Connection.RemoteIpAddress}",
                    type: AuditLogType.Security
                );

                TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";

                if (roles.Contains("SuperAdmin")) return RedirectToAction("Index", "SuperAdmin");
                if (roles.Contains("Admin")) return RedirectToAction("Index", "Admin");
                if (roles.Contains("WarehouseStaff"))
                {
                    if (!string.IsNullOrEmpty(user.Hub))
                    {
                        HttpContext.Session.SetString("UserHub", user.Hub);
                    }
                    return RedirectToAction("Index", "Warehouse");
                }
                if (roles.Contains("DeliveryRider")) return RedirectToAction("Index", "Rider");
                return RedirectToAction("Index", "Customer");
            }

            // ── Security log: failed login ──
            _audit.Log(
                actor: email,
                role: "Unknown",
                action: "Login Failed",
                detail: $"Failed login attempt for email: {email} from {HttpContext.Connection.RemoteIpAddress}",
                type: AuditLogType.Security
            );

            TempData["ErrorMessage"] = "Invalid login attempt. Please check your email and password.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var name = User.Identity?.Name ?? "Unknown";
            _audit.Log(
                actor: name,
                role: "User",
                action: "Logout",
                detail: $"{name} signed out.",
                type: AuditLogType.Security
            );
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
