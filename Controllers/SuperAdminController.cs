using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using SwiftFill.Services;
using System.Security.Claims;
using System.Collections.Generic;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _audit;

        public SuperAdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            AuditLogService audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            ViewBag.OrderCount = _context.Orders.Count();
            return View(users);
        }

        public IActionResult AuditLogs() => View();
        public IActionResult DatabaseStats() => View();

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);
                
                model.Add(new UserManagementViewModel
                {
                    User = user,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    Claims = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()
                });
            }

            ViewBag.AllHubNames = await _context.Warehouses.Select(w => w.Name).ToListAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(RegisterUserBindingModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Hub = model.Hub,
                    EmailConfirmed = true // Auto-verify for admin created accounts
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    
                    if (model.Permissions != null)
                    {
                        foreach (var perm in model.Permissions)
                        {
                            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("Permission", perm));
                        }
                    }

                    _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", "User Registration", 
                        $"New user {model.Email} registered as {model.Role}.", AuditLogType.Security);
                    
                    TempData["SuccessMessage"] = $"User {model.Email} registered successfully.";
                    return RedirectToAction(nameof(Users));
                }

                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRoles(string userId, string role, List<string> permissions)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            // Claims (Permissions)
            var currentClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = currentClaims.Where(c => c.Type == "Permission").ToList();
            foreach (var claim in permissionClaims)
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            if (permissions != null)
            {
                foreach (var perm in permissions)
                {
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("Permission", perm));
                }
            }

            _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", "Update Roles/Claims", 
                $"User {user.Email} updated: Role={role}, Permissions={string.Join(",", permissions ?? new List<string>())}.", AuditLogType.Security);

            TempData["SuccessMessage"] = $"Permissions updated for {user.Email}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string userId, string firstName, string lastName, string phoneNumber, string hub)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;
            user.Hub = hub;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Profile updated for {user.Email}.";
                return RedirectToAction(nameof(Users));
            }

            TempData["ErrorMessage"] = "Failed to update profile.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> SuspendUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            bool isCurrentlyLocked = await _userManager.IsLockedOutAsync(user);
            
            if (isCurrentlyLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"User {user.Email} has been reactivated.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = $"User {user.Email} has been suspended.";
            }

            _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", isCurrentlyLocked ? "Reactivate User" : "Suspend User", 
                $"User {user.Email} status changed.", AuditLogType.Security);

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Warehouses()
        {
            var warehouses = await _context.Warehouses.OrderByDescending(w => w.CreatedAt).ToListAsync();
            return View(warehouses);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterWarehouse(Warehouse model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.UtcNow;
                model.Status = "Operational"; // Default
                _context.Warehouses.Add(model);
                await _context.SaveChangesAsync();

                _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", "Warehouse Registration", 
                    $"New warehouse '{model.Name}' registered in {model.Region}.", AuditLogType.System);

                TempData["SuccessMessage"] = $"Warehouse '{model.Name}' registered successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to register warehouse. Please check inputs.";
            }
            return RedirectToAction(nameof(Warehouses));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWarehouse(Warehouse model)
        {
            if (ModelState.IsValid)
            {
                var warehouse = await _context.Warehouses.FindAsync(model.Id);
                if (warehouse == null) return NotFound();

                warehouse.Name = model.Name;
                warehouse.Region = model.Region;
                warehouse.Island = model.Island;
                warehouse.Capacity = model.Capacity;
                warehouse.Status = model.Status;

                await _context.SaveChangesAsync();

                _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", "Warehouse Update", 
                    $"Warehouse '{model.Name}' updated.", AuditLogType.System);

                TempData["SuccessMessage"] = $"Warehouse '{model.Name}' updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update warehouse.";
            }
            return RedirectToAction(nameof(Warehouses));
        }

        public IActionResult SystemLogs()
        {
            var logs = _audit.GetSystemLogs().ToList();
            return View(logs);
        }

        public IActionResult SecurityLogs()
        {
            var logs = _audit.GetSecurityLogs().ToList();
            return View(logs);
        }

        public IActionResult Branding() => View();

    }
}
