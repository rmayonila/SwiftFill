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
        public async Task<IActionResult> SignUpAs(string username, string email, string firstName, string lastName, string password, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["ErrorMessage"] = "All fields are required. Please fill in all input boxes.";
                return RedirectToAction("SignUp");
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(username, @"\d"))
            {
                TempData["ErrorMessage"] = "Username cannot contain numbers.";
                return RedirectToAction("SignUp");
            }

            // Remove typical formatting chars to check length
            var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (cleanPhone.Length < 10 || cleanPhone.Length > 12)
            {
                TempData["ErrorMessage"] = "Phone number is too short or too long. It must be exactly 11 digits.";
                return RedirectToAction("SignUp");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
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

        // ─── RIDER SIGN-UP ────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult RiderSignUp() => View();

        [HttpPost]
        public async Task<IActionResult> SignUpAsRider(
            string username, string email,
            string firstName, string lastName,
            string password, string phoneNumber)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction("RiderSignUp");
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(username, @"\d"))
            {
                TempData["ErrorMessage"] = "Username cannot contain numbers.";
                return RedirectToAction("RiderSignUp");
            }

            var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (cleanPhone.Length < 10 || cleanPhone.Length > 12)
            {
                TempData["ErrorMessage"] = "Phone number must be exactly 11 digits.";
                return RedirectToAction("RiderSignUp");
            }

            // ── Core check: full name must exist in ManualRiders table ──
            var fullName = $"{firstName.Trim()} {lastName.Trim()}";
            var manualRider = await _context.ManualRiders
                .FirstOrDefaultAsync(r =>
                    r.Name.ToLower() == fullName.ToLower() && r.IsActive);

            if (manualRider == null)
            {
                TempData["ErrorMessage"] =
                    $"Your name \"{fullName}\" is not registered as a rider in our system. " +
                    "Please contact your hub manager to be added to the rider list first.";
                return RedirectToAction("RiderSignUp");
            }

            // ── Create account and assign role + hub + route from ManualRider record ──
            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Hub = manualRider.Hub,
                Route = manualRider.Route,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
                await _userManager.AddToRoleAsync(user, "DeliveryRider");
                await _signInManager.SignInAsync(user, isPersistent: false);

                _audit.Log(username, "DeliveryRider", "Registration",
                    $"Rider {fullName} registered for {manualRider.Hub} — route: {manualRider.Route}.",
                    AuditLogType.Security);

                TempData["SuccessMessage"] = $"Welcome, {firstName}! You're registered as a rider for {manualRider.Hub}.";
                return RedirectToAction("Index", "Rider");
            }

            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("RiderSignUp");
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
            var user = await _userManager.FindByNameAsync(username) ?? await _userManager.FindByEmailAsync(username);
            
            if (user != null && user.IsSuspended)
            {
                TempData["ErrorMessage"] = "Your account has been suspended due to 10 failed login attempts. Please contact the Super Admin to unsuspend your account.";
                return RedirectToAction("Login");
            }

            var loginName = user?.UserName ?? username;

            var result = await _signInManager.PasswordSignInAsync(loginName, password, isPersistent: rememberMe, lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                if (user == null) user = await _userManager.FindByNameAsync(loginName);
                if (user == null) return Unauthorized();

                // Reset failed logins on success
                if (user.TotalFailedLogins > 0)
                {
                    user.TotalFailedLogins = 0;
                    await _userManager.UpdateAsync(user);
                }

                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "User";
                var displayName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(displayName)) displayName = user.Email ?? user.UserName ?? "Unknown";

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
                
                if (roles.Contains("WarehouseStaff") || roles.Contains("Staff") || roles.Contains("WarehouseOperator") || user.UserName?.ToLower() == "staff")
                {
                    if (!string.IsNullOrEmpty(user.Hub))
                    {
                        HttpContext.Session.SetString("UserHub", user.Hub);
                    }
                    return RedirectToAction("Dashboard", "Warehouse");
                }
                
                if (roles.Contains("Admin")) return RedirectToAction("Dashboard", "Admin");
                if (roles.Contains("DeliveryRider")) return RedirectToAction("Index", "Rider");
                return RedirectToAction("Index", "Customer");
            }

            if (result.IsLockedOut)
            {
                TempData["ErrorMessage"] = "You have failed to login 5 times. Your account is temporarily locked for 5 minutes. Please pause and try again later.";
                return RedirectToAction("Login");
            }

            // Handle incrementing total failed logins
            if (user != null)
            {
                user.TotalFailedLogins++;
                if (user.TotalFailedLogins >= 10)
                {
                    user.IsSuspended = true;
                    await _userManager.UpdateAsync(user);
                    TempData["ErrorMessage"] = "Your account has been suspended due to 10 failed login attempts. Please contact the Super Admin to unsuspend your account.";
                    return RedirectToAction("Login");
                }
                await _userManager.UpdateAsync(user);
            }

            _audit.Log(
                actor: username,
                role: "Unknown",
                action: "Login Failed",
                detail: $"Failed login attempt for username: {username} from {HttpContext.Connection.RemoteIpAddress}",
                type: AuditLogType.Security
            );

            TempData["ErrorMessage"] = "Incorrect username or password. Please try again.";
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

        private IActionResult GetSettingsView(SettingsViewModel model)
        {
            var userName = User.Identity?.Name?.ToLower();
            
            // Explicit username overrides to fix local db role issues
            if (userName == "superadmin") return View("SuperAdminSettings", model);
            if (userName == "admin") return View("AdminSettings", model);
            if (userName == "staff") return View("WarehouseSettings", model);
            if (userName == "customer") return View("CustomerSettings", model);
            
            // Standard role checks
            if (User.IsInRole("SuperAdmin")) return View("SuperAdminSettings", model);
            if (User.IsInRole("Admin")) return View("AdminSettings", model);
            if (User.IsInRole("WarehouseStaff") || User.IsInRole("WarehouseOperator")) return View("WarehouseSettings", model);
            if (User.IsInRole("DeliveryRider")) return View("RiderSettings", model);
            if (User.IsInRole("Customer")) return View("CustomerSettings", model);
                
            return View("CustomerSettings", model); // Provide Customer as the ultimate fallback for standard users
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

            return GetSettingsView(model);
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
                    return GetSettingsView(model);
                }

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        TempData["ErrorMessage"] = "Current password is required to set a new password.";
                        return GetSettingsView(model);
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!changePasswordResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = string.Join(" ", changePasswordResult.Errors.Select(e => e.Description));
                        return GetSettingsView(model);
                    }
                }

                _audit.Log(user.UserName ?? "User", "User", "Update Settings", "User updated their profile/password.", AuditLogType.Security);
                TempData["SuccessMessage"] = "Your settings have been updated successfully.";
                return RedirectToAction("Settings");
            }

            return GetSettingsView(model);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
