using Microsoft.AspNetCore.Authorization;
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

        public RiderInfoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentHub()
        {
            var hub = HttpContext.Session.GetString("UserHub");
            if (string.IsNullOrEmpty(hub))
            {
                // We don't have UserManager injected here, but we can use HttpContext if needed.
                // However, the SelectHub logic now sets this session during login.
                // As a fallback for this controller, we'll just check the session.
            }
            return hub ?? "Davao Hub";
        }

        public async Task<IActionResult> Index(string search, string status, int page = 1)
        {
            int pageSize = 10;
            var currentHub = GetCurrentHub();

            var query = _context.ManualRiders
                .Where(r => r.Hub == currentHub)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.Name.Contains(search) || r.Route.Contains(search));

            if (!string.IsNullOrEmpty(status))
            {
                bool active = status == "Active";
                query = query.Where(r => r.IsActive == active);
            }

            var totalItems = await query.CountAsync();
            var riders = await query.OrderBy(r => r.Route)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(riders);
        }

        [HttpPost]
        public async Task<IActionResult> AddRider(ManualRider rider)
        {
            var currentHub = GetCurrentHub();
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
            if (existing != null && existing.Hub == GetCurrentHub())
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
            if (rider != null && rider.Hub == GetCurrentHub())
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
            if (rider != null && rider.Hub == GetCurrentHub())
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
            var currentHub = GetCurrentHub();
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
