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
    }
}
