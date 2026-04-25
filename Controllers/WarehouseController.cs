using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Models;
using SwiftFill.Data;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "WarehouseStaff,Admin,SuperAdmin")]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WarehouseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the current staff's hub from session.
        /// Falls back to the first hub in the registry if not set.
        /// </summary>
        private string GetCurrentHub()
        {
            var hub = HttpContext.Session.GetString("UserHub");
            if (string.IsNullOrEmpty(hub))
            {
                // Fetch from logged-in user profile
                var user = _userManager.GetUserAsync(User).Result;
                hub = user?.Hub;
            }
            return string.IsNullOrEmpty(hub) ? HubRegistry.All[0].Name : hub;
        }

        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // --- DASHBOARD ---
        public async Task<IActionResult> Dashboard()
        {
            var currentHub = GetCurrentHub();
            var viewModel = await GetDashboardViewModel(currentHub);
            return View(viewModel);
        }

        // --- WAREHOUSE QUEUE ---
        public async Task<IActionResult> WarehouseQueue()
        {
            var currentHub = GetCurrentHub();
            // Show parcels physically at this hub + inbound parcels heading here
            var orders = await _context.Orders
                .Where(o => (o.CurrentLocation == currentHub ||
                             o.Status.Contains($"Transit to {currentHub}")) && !o.IsArchived)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- HUB TRANSFERS (Outbound) ---
        public async Task<IActionResult> HubTransfers()
        {
            var currentHub = GetCurrentHub();
            // Packed orders at this hub that are destined for another hub
            var orders = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub &&
                            (o.Status == "Packed" || o.Status == "Sorted for Transfer") &&
                            !o.IsArchived)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- PICKING STATION ---
        public async Task<IActionResult> PickingStation()
        {
            var currentHub = GetCurrentHub();
            var orders = await _context.Orders
                .Where(o => o.Status == "Pending" && o.CurrentLocation == currentHub && !o.IsArchived)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- PACKING LINE ---
        public async Task<IActionResult> PackingLine()
        {
            var currentHub = GetCurrentHub();
            var orders = await _context.Orders
                .Where(o => o.Status == "Picked" && o.CurrentLocation == currentHub && !o.IsArchived)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- TRUCK TRACKING ---
        public async Task<IActionResult> TruckTracking(string truckLabel, string search)
        {
            var currentHub = GetCurrentHub();
            var query = _context.Orders.Where(o => o.CurrentLocation == currentHub && !o.IsArchived);

            if (!string.IsNullOrEmpty(truckLabel))
                query = query.Where(o => o.TruckLabel == truckLabel);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search));

            var orders = await query.OrderByDescending(o => o.UpdatedAt).ToListAsync();
            
            ViewBag.CurrentHub = currentHub;
            ViewBag.TruckLabel = truckLabel;
            ViewBag.Search = search;
            
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> AssignToTruck(string trackingId, string truckLabel)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null)
            {
                order.TruckLabel = truckLabel;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(TruckTracking), new { truckLabel });
        }

        // ============================================================
        // POST ACTIONS
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> MarkPicked(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            // Guard: only pick if the parcel is physically at this hub
            if (order != null && order.CurrentLocation == currentHub)
            {
                order.Status = "Picked";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> MarkPacked(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null && order.CurrentLocation == currentHub)
            {
                order.Status = "Packed";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        /// <summary>
        /// Ship (dispatch) a parcel from the current hub to the SPECIFIED target hub.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkShipped(string trackingId, string targetHub)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null && order.CurrentLocation == currentHub && (order.Status == "Packed" || order.Status == "Sorted for Transfer"))
            {
                order.Status = $"In Transit to {targetHub}";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        /// <summary>
        /// Staff at an intermediate hub marks a parcel as "Sorted" to prepare it for the next transfer.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkSorted(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null && order.CurrentLocation == currentHub && order.Status.StartsWith("Arrived"))
            {
                order.Status = "Sorted for Transfer";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        /// <summary>
        /// Staff at the DESTINATION hub clicks Receive.
        /// This updates CurrentLocation to THEIR hub — the key step that makes tracking accurate.
        /// From this point, they can assign a rider (Door-to-Door) or mark as ready for pickup.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkReceived(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            // Guard: only receive if this parcel is "In Transit to currentHub"
            if (order != null && order.Status.Contains($"Transit to {currentHub}"))
            {
                order.CurrentLocation = currentHub; // ← parcel physically arrives here
                order.Status = $"Arrived at {currentHub}";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> QuickReceive(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null)
            {
                order.CurrentLocation = currentHub;
                order.Status = $"Arrived at {currentHub}";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Parcel #{trackingId} received at {currentHub}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Tracking ID not found.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        /// <summary>
        /// Assign rider — only valid at the hub where the parcel has arrived (CurrentLocation == currentHub)
        /// and only for Door-to-Door orders.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignRider(string trackingId, string riderId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null &&
                order.CurrentLocation == currentHub &&
                order.Status.StartsWith("Arrived") &&
                order.DeliveryType == "DoorToDoor")
            {
                order.AssignedRiderId = riderId;
                order.Status = "Out for Delivery";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // ============================================================
        // HELPER — BUILD DASHBOARD VIEWMODEL
        // ============================================================
        private async Task<AdminDashboardViewModel> GetDashboardViewModel(string currentHub)
        {
            // Local orders: parcel is physically at this hub (can pick/pack/ship)
            var localOrders = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub && !o.IsArchived)
                .OrderByDescending(o => o.UpdatedAt)
                .Take(50)
                .ToListAsync();

            // Inbound orders: in transit heading to this hub (can receive)
            var inboundOrders = await _context.Orders
                .Where(o => o.Status.Contains($"Transit to {currentHub}") && !o.IsArchived)
                .ToListAsync();

            // Merge for the main queue table
            var allOrders = localOrders.Union(inboundOrders).OrderByDescending(o => o.UpdatedAt).ToList();

            // Riders: only fetch when there are arrived parcels needing delivery assignment
            var riders = new List<ApplicationUser>();
            var hasArrivedDoorToDoor = localOrders.Any(o => o.Status.StartsWith("Arrived") && o.DeliveryType == "DoorToDoor");
            if (hasArrivedDoorToDoor)
            {
                var allRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
                riders = allRiders.Where(r => r.Hub == currentHub).ToList();
            }

            return new AdminDashboardViewModel
            {
                CurrentHub = currentHub,
                RecentShipments = allOrders,
                PendingPickOrders = localOrders.Where(o => o.Status == "Pending").ToList(),
                PickedOrders     = localOrders.Where(o => o.Status == "Picked").ToList(),
                PackedOrders     = localOrders.Where(o => o.Status == "Packed" || o.Status == "Sorted for Transfer").ToList(),
                AvailableRiders  = riders,
                ManualRiders     = await _context.ManualRiders.Where(r => r.Hub == currentHub && r.IsActive).ToListAsync(),
                Hubs             = HubRegistry.Names
            };
        }

    }
}