using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Models;
using SwiftFill.Data;

namespace SwiftFill.Controllers
{
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WarehouseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Default route
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // --- MODULE 1: DASHBOARD OVERVIEW ---
        public async Task<IActionResult> Dashboard(string? hub)
        {
            var currentHub = hub ?? "Davao Hub";
            var viewModel = await GetBaseViewModel(currentHub);
            return View(viewModel);
        }

        // --- MODULE 2: WAREHOUSE QUEUE (Master List) ---
        public async Task<IActionResult> WarehouseQueue(string? hub)
        {
            var currentHub = hub ?? "Davao Hub";
            var orders = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub && !o.IsArchived)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();
            
            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- MODULE 3: HUB TRANSFERS (Outbound Logistics) ---
        public async Task<IActionResult> HubTransfers(string? hub)
        {
            var currentHub = hub ?? "Davao Hub";
            // Orders that are packed but heading to a different region (e.g., NCR or Cebu)
            var orders = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub && 
                            o.Status == "Packed" && 
                            o.DestinationRegion != "Davao Region" && 
                            !o.IsArchived)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- MODULE 4: PICKING STATION (Inbound/Pending) ---
        public async Task<IActionResult> PickingStation(string? hub)
        {
            var currentHub = hub ?? "Davao Hub";
            var orders = await _context.Orders
                .Where(o => o.Status == "Pending" && o.CurrentLocation == currentHub && !o.IsArchived)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- MODULE 5: PACKING LINE (Processing) ---
        public async Task<IActionResult> PackingLine(string? hub)
        {
            var currentHub = hub ?? "Davao Hub";
            var orders = await _context.Orders
                .Where(o => o.Status == "Picked" && o.CurrentLocation == currentHub && !o.IsArchived)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- POST ACTIONS (The Logic) ---

        [HttpPost]
        public async Task<IActionResult> MarkPicked(string trackingId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null) {
                order.Status = "Picked";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            // Smart Redirect: Stays on the current page (Picking Station or Dashboard)
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> MarkPacked(string trackingId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null) {
                order.Status = "Packed";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> AssignRider(string trackingId, string riderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null) {
                order.AssignedRiderId = riderId;
                order.Status = "OutForDelivery";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // --- HELPER: GET VIEWMODEL DATA ---
        private async Task<AdminDashboardViewModel> GetBaseViewModel(string currentHub)
        {
            return new AdminDashboardViewModel
            {
                CurrentHub = currentHub,
                // Dashboard limited to top 50 for performance
                RecentShipments = await _context.Orders
                    .Where(o => o.CurrentLocation == currentHub && !o.IsArchived)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(50)
                    .ToListAsync(),

                PendingPickOrders = await _context.Orders.Where(o => o.Status == "Pending" && o.CurrentLocation == currentHub && !o.IsArchived).ToListAsync(),
                PickedOrders = await _context.Orders.Where(o => o.Status == "Picked" && o.CurrentLocation == currentHub && !o.IsArchived).ToListAsync(),
                PackedOrders = await _context.Orders.Where(o => o.Status == "Packed" && o.CurrentLocation == currentHub && !o.IsArchived).ToListAsync(),
                
                // Fetching all riders (Can be filtered by Hub if your ApplicationUser has a Hub property)
                AvailableRiders = await _context.Users.ToListAsync(), 
                Hubs = new List<string> { "Davao Hub", "Manila Hub", "Cebu Hub" }
            };
        }
    }
}