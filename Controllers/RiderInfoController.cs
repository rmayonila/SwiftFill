using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "WarehouseStaff,WarehouseOperator,Admin,SuperAdmin")]
    public class RiderInfoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RiderInfoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentHub()
        {
            var hub = HttpContext.Session.GetString("UserHub");
            if (string.IsNullOrEmpty(hub))
            {
                var user = await _userManager.GetUserAsync(User);
                hub = user?.Hub;
            }
            return hub ?? "Davao Hub";
        }

        public async Task<IActionResult> Index(string search, string status, int page = 1)
        {
            int pageSize = 10;
            var currentHub = await GetCurrentHub();

            // 1. Manual Riders
            var manualQuery = _context.ManualRiders
                .Where(r => r.Hub == currentHub)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                manualQuery = manualQuery.Where(r => r.Name.Contains(search) || (r.Route != null && r.Route.Contains(search)));

            if (!string.IsNullOrEmpty(status))
            {
                bool active = status == "Active";
                manualQuery = manualQuery.Where(r => r.IsActive == active);
            }

            var manualRiders = await manualQuery.OrderBy(r => r.Route).ToListAsync();

            // 2. System Riders (Only visible to SuperAdmin)
            var filteredSystemRiders = new List<ApplicationUser>();
            if (User.IsInRole("SuperAdmin"))
            {
                var allSystemRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
                filteredSystemRiders = allSystemRiders.Where(r => r.Hub == currentHub).ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    filteredSystemRiders = filteredSystemRiders.Where(r => 
                        (r.FirstName != null && r.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) || 
                        (r.LastName != null && r.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (r.Email != null && r.Email.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
                }
            }

            var totalItems = manualRiders.Count + filteredSystemRiders.Count;
            
            // Paging for the combined list? 
            // For now, let's just pass them and show them. 
            // Usually, there aren't thousands of riders per hub.
            
            ViewBag.SystemRiders = filteredSystemRiders;
            ViewBag.CurrentHub = currentHub;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(manualRiders);
        }

        [HttpPost]
        public async Task<IActionResult> AddRider(ManualRider rider)
        {
            var currentHub = await GetCurrentHub();
            rider.Hub = currentHub;
            rider.CreatedAt = DateTime.UtcNow;
            rider.IsActive = true;

            // Link to Warehouse record if it exists
            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == currentHub);
            if (warehouse != null)
            {
                rider.WarehouseId = warehouse.Id;
            }

            if (ModelState.IsValid)
            {
                _context.ManualRiders.Add(rider);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Rider added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add rider. Please check the inputs.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> EditRider(ManualRider rider)
        {
            var existing = await _context.ManualRiders.FindAsync(rider.Id);
            if (existing != null && existing.Hub == await GetCurrentHub())
            {
                existing.Name = rider.Name;
                existing.Phone = rider.Phone;
                existing.Route = rider.Route;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Rider updated successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var rider = await _context.ManualRiders.FindAsync(id);
            if (rider != null && rider.Hub == await GetCurrentHub())
            {
                rider.IsActive = !rider.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Rider {(rider.IsActive ? "Activated" : "Suspended")} successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRider(int id)
        {
            var rider = await _context.ManualRiders.FindAsync(id);
            if (rider != null && rider.Hub == await GetCurrentHub())
            {
                _context.ManualRiders.Remove(rider);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Rider removed successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetRidersForRoute(string address)
        {
            var currentHub = await GetCurrentHub();
            var riders = await _context.ManualRiders
                .Where(r => r.Hub == currentHub && r.IsActive)
                .ToListAsync();

            var addressLower = (address ?? "").ToLower();

            // Flexible matching: split the rider's route by comma and check each keyword
            // against the delivery address. "Makati City, Metro Manila" =>
            // keywords: ["makati", "manila"] (after stripping 'city'/'metro')
            // Both must appear somewhere in the address.
            var filtered = riders.Where(r =>
            {
                var routeParts = r.Route.Split(',')
                    .Select(p => p.Trim()
                        .Replace("city", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("metro", "", StringComparison.OrdinalIgnoreCase)
                        .Trim())
                    .Where(p => p.Length > 2)
                    .ToList();

                if (routeParts.Count == 0) return false;

                // ALL extracted keywords must appear in the address
                return routeParts.All(keyword => addressLower.Contains(keyword.ToLower()));
            }).ToList();

            return Json(filtered.Select(r => new { r.Id, r.Name, r.Phone, r.Route }));
        }
    }
}
