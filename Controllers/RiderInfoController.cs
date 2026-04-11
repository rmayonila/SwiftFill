using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "WarehouseStaff,Admin,SuperAdmin")]
    public class RiderInfoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RiderInfoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentHub()
        {
            return HttpContext.Session.GetString("UserHub") ?? "Davao Hub";
        }

        public async Task<IActionResult> Index()
        {
            var currentHub = GetCurrentHub();
            if (currentHub == "Davao Hub")
            {
                return RedirectToAction("Index", "Warehouse");
            }

            var riders = await _context.ManualRiders
                .Where(r => r.Hub == currentHub)
                .OrderBy(r => r.Route)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(riders);
        }

        [HttpPost]
        public async Task<IActionResult> AddRider(ManualRider rider)
        {
            rider.Hub = GetCurrentHub();
            rider.CreatedAt = DateTime.UtcNow;
            rider.IsActive = true;

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

            // Simple filtering logic: if any part of the route name is in the address
            var filtered = riders.Where(r => address.Contains(r.Route, StringComparison.OrdinalIgnoreCase)).ToList();
            
            // If no match, return all for that hub as fallback? 
            // The prompt says "only see in that route assign address not all rider will show".
            // So if no match, maybe empty list? 
            
            return Json(filtered.Select(r => new { r.Id, r.Name, r.Phone, r.Route }));
        }
    }
}
