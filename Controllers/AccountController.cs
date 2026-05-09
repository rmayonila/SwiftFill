using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using SwiftFill.Services;
using System.Text.Json;
using SwiftFill.Helpers;

namespace SwiftFill.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _audit;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            AuditLogService audit,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _audit = audit;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult SignUp() => View();

        [HttpPost]
        public async Task<IActionResult> SignUpAs(string username, string email, string firstName, string lastName, string password, string phoneNumber)
        {
            // Sanitization: Clean strings to prevent XSS
            username = InputSanitizer.StripScripts(username) ?? "";
            email = InputSanitizer.StripScripts(email) ?? "";
            firstName = InputSanitizer.Sanitize(firstName) ?? "";
            lastName = InputSanitizer.Sanitize(lastName) ?? "";
            phoneNumber = InputSanitizer.Sanitize(phoneNumber) ?? "";

            if (!await IsReCaptchaValid())
            {
                TempData["ErrorMessage"] = "Please complete the reCAPTCHA verification.";
                return RedirectToAction("SignUp");
            }

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
            // Sanitization
            username = InputSanitizer.StripScripts(username) ?? "";
            email = InputSanitizer.StripScripts(email) ?? "";
            firstName = InputSanitizer.Sanitize(firstName) ?? "";
            lastName = InputSanitizer.Sanitize(lastName) ?? "";
            phoneNumber = InputSanitizer.Sanitize(phoneNumber) ?? "";

            if (!await IsReCaptchaValid())
            {
                TempData["ErrorMessage"] = "Please complete the reCAPTCHA verification.";
                return RedirectToAction("RiderSignUp");
            }

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

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Customer"))
            {
                TempData["ErrorMessage"] = "This feature is for customers only. Please contact your system administration.";
                return RedirectToAction("ForgotPassword");
            }

            // Generate a 6-digit verification code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            
            // In a real app, you'd email this. For now, we store it in TempData.
            TempData["ResetEmail"] = email;
            TempData["ResetCode"] = code;

            // REAL SMTP Email Sending
            try
            {
                var subject = "SwiftFill - Password Reset Verification Code";
                var body = $@"
                    <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                        <h2 style='color: #ff8c00;'>SwiftFill Password Reset</h2>
                        <p>You requested a password reset for your SwiftFill account.</p>
                        <p>Your 6-digit verification code is:</p>
                        <div style='font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #333; margin: 20px 0;'>{code}</div>
                        <p style='color: #777; font-size: 12px;'>If you did not request this, please ignore this email.</p>
                    </div>";
                
                await _emailService.SendEmailAsync(email, subject, body);
                TempData["SuccessMessage"] = "A 6-digit verification code has been sent to your email.";
                return RedirectToAction("VerifyCode");
            }
            catch (Exception ex)
            {
                // FAILSAFE: If SMTP fails, we print the code to the terminal so the user can still proceed!
                Console.WriteLine("**************************************************");
                Console.WriteLine($"[SMTP ERROR] Could not send email: {ex.Message}");
                Console.WriteLine($"[BACKUP] YOUR VERIFICATION CODE IS: {code}");
                Console.WriteLine("**************************************************");
                
                TempData["ErrorMessage"] = "We couldn't send the email, but you can still verify if you have access to the server console. Please check the logs for your code.";
                return RedirectToAction("VerifyCode");
            }
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            var email = TempData.Peek("ResetEmail")?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCodeAction(string code)
        {
            var email = TempData.Peek("ResetEmail")?.ToString();
            var savedCode = TempData.Peek("ResetCode")?.ToString();

            if (code == savedCode)
            {
                TempData["VerificationVerified"] = "true";
                return RedirectToAction("ResetPassword", new { email = email });
            }

            TempData["ErrorMessage"] = "Invalid verification code. Please try again.";
            return RedirectToAction("VerifyCode");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email)
        {
            var isVerified = TempData.Peek("VerificationVerified")?.ToString();
            if (isVerified != "true") return RedirectToAction("ForgotPassword");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPasswordAction(string email, string token, string newPassword, string confirmPassword)
        {
            var isVerified = TempData.Peek("VerificationVerified")?.ToString();
            if (isVerified != "true") return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction("ResetPassword", new { email = email });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                TempData.Remove("ResetEmail");
                TempData.Remove("ResetCode");
                TempData.Remove("VerificationVerified");

                _audit.Log(user.UserName ?? email, "Customer", "Password Reset", "User successfully reset their password via OTP flow.", AuditLogType.Security);
                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now sign in.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("ResetPassword", new { email = email });
        }

        [HttpPost]
        public async Task<IActionResult> LoginAction(string username, string password, bool rememberMe)
        {
            if (!await IsReCaptchaValid())
            {
                var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (ipAddr == "::1") ipAddr = "127.0.0.1";

                _audit.Log(username, "Unknown", "reCAPTCHA Failed", 
                    $"Failed reCAPTCHA verification for login attempt by {username} from {ipAddr}.", 
                    AuditLogType.Security);

                TempData["ErrorMessage"] = "Please complete the reCAPTCHA verification.";
                return RedirectToAction("Login");
            }

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

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (ipAddress == "::1") ipAddress = "127.0.0.1";

                _audit.Log(
                    actor: displayName,
                    role: roleName,
                    action: "Login",
                    detail: $"{displayName} ({roleName}) signed in from {ipAddress}",
                    type: AuditLogType.Security
                );

                if (!roles.Contains("SuperAdmin"))
                {
                    TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";
                }

                if (roles.Contains("SuperAdmin")) return RedirectToAction("Index", "SuperAdmin");
                
                // Priority for Admin role, unless the username explicitly indicates they are staff
                if (roles.Contains("Admin") && (user.UserName == null || !user.UserName.ToLower().Contains("staff")))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                // Determine if user is staff based on role, hub assignment, or username containing 'staff'
                bool isStaff = roles.Contains("WarehouseStaff") || 
                               roles.Contains("Staff") || 
                               roles.Contains("WarehouseOperator") || 
                               (!roles.Contains("DeliveryRider") && !string.IsNullOrEmpty(user.Hub)) ||
                               (user.UserName?.ToLower()?.Contains("staff") == true);

                if (isStaff)
                {
                    if (!string.IsNullOrEmpty(user.Hub))
                    {
                        HttpContext.Session.SetString("UserHub", user.Hub);
                    }
                    return RedirectToAction("Dashboard", "Warehouse");
                }
                
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

            var ipAddressErr = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (ipAddressErr == "::1") ipAddressErr = "127.0.0.1";

            _audit.Log(
                actor: username,
                role: "Unknown",
                action: "Login Failed",
                detail: $"Failed login attempt for username: {username} from {ipAddressErr}",
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

        private async Task<IActionResult> GetSettingsView(ApplicationUser user, SettingsViewModel model)
        {
            var userName = user.UserName?.ToLower();
            var roles = await _userManager.GetRolesAsync(user);
            
            // Priority 1: Explicit mode from request (if authorized)
            if (!string.IsNullOrEmpty(model.PreferredView))
            {
                if (model.PreferredView == "warehouse" && (roles.Contains("WarehouseStaff") || roles.Contains("WarehouseOperator") || !string.IsNullOrEmpty(user.Hub)))
                    return View("WarehouseSettings", model);
                
                if (model.PreferredView == "admin" && (roles.Contains("Admin") || roles.Contains("SuperAdmin")))
                    return View("AdminSettings", model);
                
                if (model.PreferredView == "superadmin" && roles.Contains("SuperAdmin"))
                    return View("SuperAdminSettings", model);
            }

            // Priority 2: Explicit username overrides to fix local db role issues (now more flexible)
            if (userName != null && userName.Contains("superadmin")) return View("SuperAdminSettings", model);
            if (userName != null && userName.Contains("admin") && !userName.Contains("staff")) return View("AdminSettings", model);
            if (userName != null && userName.Contains("staff")) return View("WarehouseSettings", model);
            if (userName != null && userName.Contains("customer")) return View("CustomerSettings", model);
            
            // Priority 3: Standard role checks
            if (roles.Contains("SuperAdmin")) return View("SuperAdminSettings", model);
            if (roles.Contains("DeliveryRider")) return View("RiderSettings", model); // Check Rider BEFORE Hub to avoid stealing them to Warehouse
            
            if (roles.Contains("Admin") && (userName == null || !userName.Contains("staff"))) return View("AdminSettings", model);

            if (roles.Contains("WarehouseStaff") || roles.Contains("WarehouseOperator") || !string.IsNullOrEmpty(user.Hub)) 
                return View("WarehouseSettings", model);

            if (roles.Contains("Admin")) return View("AdminSettings", model);
            if (roles.Contains("Customer")) return View("CustomerSettings", model);
                
            return View("CustomerSettings", model); // Fallback
        }

        [HttpGet]
        public async Task<IActionResult> Settings(string? view)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var model = new SettingsViewModel
            {
                UserName = user.UserName ?? "",
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Hub = user.Hub,
                Route = user.Route,
                PreferredView = view
            };

            return await GetSettingsView(user, model);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                // Handle Username Change
                if (user.UserName != model.UserName)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(model.UserName, @"\d"))
                    {
                        TempData["ErrorMessage"] = "Username cannot contain numbers.";
                        return await GetSettingsView(user, model);
                    }

                    var existingUser = await _userManager.FindByNameAsync(model.UserName);
                    if (existingUser != null)
                    {
                        TempData["ErrorMessage"] = "Username is already taken.";
                        return await GetSettingsView(user, model);
                    }

                    var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                    if (!setUserNameResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = "Failed to update username.";
                        return await GetSettingsView(user, model);
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Hub = model.Hub;
                user.Route = model.Route;

                // Handle Email Change
                if (user.Email != model.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = "Failed to update email address.";
                        return await GetSettingsView(user, model);
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = "Failed to update profile information.";
                    return await GetSettingsView(user, model);
                }

                // Refresh sign-in to update the cookie with new username/claims
                await _signInManager.RefreshSignInAsync(user);

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        TempData["ErrorMessage"] = "Current password is required to set a new password.";
                        return await GetSettingsView(user, model);
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!changePasswordResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = string.Join(" ", changePasswordResult.Errors.Select(e => e.Description));
                        return await GetSettingsView(user, model);
                    }
                }

                _audit.Log(user.UserName ?? "User", "User", "Update Settings", "User updated their profile/password.", AuditLogType.Security);
                TempData["SuccessMessage"] = "Your settings have been updated successfully.";
                return RedirectToAction("Settings", new { view = model.PreferredView });
            }

            return await GetSettingsView(user, model);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        private async Task<bool> IsReCaptchaValid()
        {
            var response = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrWhiteSpace(response)) return false;

            var secretKey = _configuration["RecaptchaSettings:SecretKey"];
            var client = _httpClientFactory.CreateClient();
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secretKey ?? ""),
                new KeyValuePair<string, string>("response", response.ToString())
            });

            var verifyResponse = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            if (verifyResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await verifyResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                return doc.RootElement.GetProperty("success").GetBoolean();
            }
            return false;
        }
    }
}
