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
        public async Task<IActionResult> SignUpAs(string username, string email, string firstName, string lastName, string password)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    _audit.Log(username, "Customer", "Registration", "New customer registered.", AuditLogType.Security);
                    TempData["SuccessMessage"] = "Successfully created account. Welcome to SwiftFill!";
                    return RedirectToAction("Index", "Customer");
                }
                
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction("SignUp");
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
        public async Task<IActionResult> LoginAction(string username, string password, bool rememberMe)
        {
            // First, try to find the user by username or email to get the canonical UserName
            var user = await _userManager.FindByNameAsync(username) ?? await _userManager.FindByEmailAsync(username);
            
            // If user exists, use their actual UserName for the sign-in attempt
            var loginName = user?.UserName ?? username;

            var result = await _signInManager.PasswordSignInAsync(loginName, password, isPersistent: rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (user == null) user = await _userManager.FindByNameAsync(loginName);
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

                if (!roles.Contains("SuperAdmin"))
                {
                    TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";
                }

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
                actor: username,
                role: "Unknown",
                action: "Login Failed",
                detail: $"Failed login attempt for username: {username} from {HttpContext.Connection.RemoteIpAddress}",
                type: AuditLogType.Security
            );

            TempData["ErrorMessage"] = "Invalid login attempt. Please check your username and password.";
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
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var model = new SettingsViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Hub = user.Hub
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = "Failed to update profile information.";
                    return View(model);
                }

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        TempData["ErrorMessage"] = "Current password is required to set a new password.";
                        return View(model);
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!changePasswordResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = string.Join(" ", changePasswordResult.Errors.Select(e => e.Description));
                        return View(model);
                    }
                }

                _audit.Log(user.UserName ?? "User", "User", "Update Settings", "User updated their profile/password.", AuditLogType.Security);
                TempData["SuccessMessage"] = "Your settings have been updated successfully.";
                return RedirectToAction("Settings");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
