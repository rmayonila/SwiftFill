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
                if (roles.Contains("WarehouseStaff")) return RedirectToAction("SelectHub", "Account");
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
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "WarehouseStaff")]
        public IActionResult SelectHub()
        {
            ViewBag.Hubs = SwiftFill.Models.HubRegistry.Names;
            return View();
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "WarehouseStaff")]
        public async Task<IActionResult> SelectHub(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Please enter a hub access code.";
                return RedirectToAction(nameof(SelectHub));
            }

            var hubCode = await _context.HubAccessCodes
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper().Trim() && c.IsActive);

            if (hubCode == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired access code. Please contact your Super Admin.";
                _audit.Log(
                    actor: User.Identity?.Name ?? "Staff",
                    role: "WarehouseStaff",
                    action: "Hub Access Denied",
                    detail: $"Failed hub login attempt with code: {code}",
                    type: AuditLogType.Security
                );
                return RedirectToAction(nameof(SelectHub));
            }

            HttpContext.Session.SetString("UserHub", hubCode.HubName);

            _audit.Log(
                actor: User.Identity?.Name ?? "Staff",
                role: "WarehouseStaff",
                action: "Hub Selected",
                detail: $"Staff checked in at {hubCode.HubName} using code {hubCode.Code}",
                type: AuditLogType.Security
            );

            return RedirectToAction("Index", "Warehouse");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
