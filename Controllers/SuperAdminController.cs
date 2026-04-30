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

        public async Task<IActionResult> Users(string search, string role, int page = 1)
        {
            int pageSize = 10;
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));
            }

            var users = await usersQuery.ToListAsync();

            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                users = users.Intersect(usersInRole).ToList();
            }

            var totalItems = users.Count();
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new List<UserManagementViewModel>();

            foreach (var user in pagedUsers)
            {
                // Auto-fix for existing users who might be stuck in "Pending" status
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

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
            
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(RegisterUserBindingModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Hub = model.Hub,
                    EmailConfirmed = true // Auto-verify for admin created accounts
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Explicitly confirm email after creation to ensure it persists correctly
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);

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
        public async Task<IActionResult> UpdateProfile(string userId, string userName, string firstName, string lastName, string phoneNumber, string hub, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.UserName = userName;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;
            user.Hub = hub;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(newPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                    if (!passwordResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = "Profile updated, but password reset failed: " + string.Join(" ", passwordResult.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Users));
                    }
                }

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

        public async Task<IActionResult> Warehouses(string search, int page = 1)
        {
            int pageSize = 8;
            var query = _context.Warehouses.Where(w => !w.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.Name.Contains(search) || w.Region.Contains(search) || w.Island.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var warehouses = await query.OrderByDescending(w => w.CreatedAt)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

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

        [HttpPost]
        public async Task<IActionResult> ArchiveHub(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse != null)
            {
                warehouse.IsArchived = true;
                await _context.SaveChangesAsync();
                _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", "Archive Hub", 
                    $"Warehouse '{warehouse.Name}' archived.", AuditLogType.System);
                TempData["SuccessMessage"] = $"Warehouse '{warehouse.Name}' archived successfully.";
            }
            return RedirectToAction(nameof(Warehouses));
        }
        
        [HttpPost]
        public async Task<IActionResult> RestoreHub(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse != null)
            {
                warehouse.IsArchived = false;
                await _context.SaveChangesAsync();
                _audit.Log(User.Identity?.Name ?? "SuperAdmin", "SuperAdmin", "Restore Hub", 
                    $"Warehouse '{warehouse.Name}' restored.", AuditLogType.System);
                TempData["SuccessMessage"] = $"Warehouse '{warehouse.Name}' restored successfully.";
            }
            return RedirectToAction(nameof(Archive));
        }

        public async Task<IActionResult> Archive(string search, int page = 1)
        {
            int pageSize = 8;
            var query = _context.Warehouses.Where(w => w.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.Name.Contains(search) || w.Region.Contains(search) || w.Island.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var warehouses = await query.OrderByDescending(w => w.CreatedAt)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(warehouses);
        }

        public async Task<IActionResult> SystemLogs(string search, int page = 1)
        {
            int pageSize = 15;
            var query = _context.AuditLogs.Where(l => l.Type == AuditLogType.System).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.Action.Contains(search) || l.Detail.Contains(search) || l.Actor.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var logs = await query.OrderByDescending(l => l.Timestamp)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(logs);
        }

        public async Task<IActionResult> SecurityLogs(string search, int page = 1)
        {
            int pageSize = 15;
            var query = _context.AuditLogs.Where(l => l.Type == AuditLogType.Security).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.Action.Contains(search) || l.Detail.Contains(search) || l.Actor.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var logs = await query.OrderByDescending(l => l.Timestamp)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(logs);
        }

        public IActionResult Branding() => View();

    }
}
