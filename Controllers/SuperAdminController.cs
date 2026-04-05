using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SuperAdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            ViewBag.OrderCount = _context.Orders.Count();
            return View(users);
        }

        public IActionResult AuditLogs()
        {
            // Mock audit logs
            return View();
        }

        public IActionResult DatabaseStats()
        {
            // Mock DB stats
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        public IActionResult Warehouses()
        {
            return View();
        }

        public IActionResult SystemLogs()
        {
            return View();
        }

        public IActionResult SecurityLogs()
        {
            return View();
        }

        public IActionResult Branding()
        {
            return View();
        }
    }
}
