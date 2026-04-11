using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using SwiftFill.Services;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _audit;

        public SuperAdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            AuditLogService audit)
        {
            _userManager = userManager;
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
            return View(users);
        }

        public IActionResult Warehouses() => View();

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

        // ============================================================
        // HUB ACCESS CODES
        // ============================================================

        public async Task<IActionResult> HubCodes()
        {
            var codes = await _context.HubAccessCodes
                .OrderBy(h => h.HubName)
                .ToListAsync();

            ViewBag.Hubs = HubRegistry.Names;
            ViewBag.ActiveHubs = codes.Where(c => c.IsActive).Select(c => c.HubName).ToList();
            return View(codes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHubCode(string hubName, string code)
        {
            if (string.IsNullOrWhiteSpace(hubName) || string.IsNullOrWhiteSpace(code))
            {
                TempData["ErrorMessage"] = "Hub name and code are required.";
                return RedirectToAction(nameof(HubCodes));
            }

            if (!HubRegistry.Names.Contains(hubName))
            {
                TempData["ErrorMessage"] = "Invalid hub name.";
                return RedirectToAction(nameof(HubCodes));
            }

            // Check for duplicate code across ALL hubs
            var codeExists = await _context.HubAccessCodes
                .AnyAsync(h => h.Code.ToUpper() == code.ToUpper().Trim() && h.IsActive);
            if (codeExists)
            {
                TempData["ErrorMessage"] = $"Code \"{code.ToUpper()}\" is already in use by another hub. Choose a different code.";
                return RedirectToAction(nameof(HubCodes));
            }

            // Deactivate any existing active code for this hub (one active code per hub)
            var existing = await _context.HubAccessCodes
                .Where(h => h.HubName == hubName && h.IsActive)
                .ToListAsync();
            foreach (var old in existing)
                old.IsActive = false;

            // Create new code
            var newCode = new HubAccessCode
            {
                HubName = hubName,
                Code = code.ToUpper().Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };
            _context.HubAccessCodes.Add(newCode);
            await _context.SaveChangesAsync();

            _audit.Log(
                actor: User.Identity?.Name ?? "SuperAdmin",
                role: "SuperAdmin",
                action: "Create Hub Code",
                detail: $"New access code created for {hubName} by {User.Identity?.Name}.",
                type: AuditLogType.Security
            );

            TempData["SuccessMessage"] = $"Access code for {hubName} set successfully.";
            return RedirectToAction(nameof(HubCodes));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHubCode(int id)
        {
            var code = await _context.HubAccessCodes.FindAsync(id);
            if (code != null)
            {
                code.IsActive = false;
                await _context.SaveChangesAsync();
                _audit.Log(
                    actor: User.Identity?.Name ?? "SuperAdmin",
                    role: "SuperAdmin",
                    action: "Revoke Hub Code",
                    detail: $"Access code for {code.HubName} revoked.",
                    type: AuditLogType.Security
                );
                TempData["SuccessMessage"] = $"Access code for {code.HubName} revoked.";
            }
            return RedirectToAction(nameof(HubCodes));
        }

        /// <summary>Generate a random 8-character alphanumeric code suggestion (AJAX).</summary>
        [HttpGet]
        public IActionResult GenerateCodeSuggestion()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new Random();
            var suggestion = new string(Enumerable.Range(0, 8)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            return Json(new { code = suggestion });
        }
    }
}
