using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Models;
using SwiftFill.Data;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "WarehouseStaff,WarehouseOperator,Admin,SuperAdmin")]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WarehouseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCurrentHub()
        {
            var hub = HttpContext.Session.GetString("UserHub");
            if (string.IsNullOrEmpty(hub))
            {
                var user = _userManager.GetUserAsync(User).Result;
                hub = user?.Hub;
            }
            // Fallback to Davao Hub if no session or user hub is found
            return string.IsNullOrEmpty(hub) ? "Davao Hub" : hub;
        }

        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // --- 1. DASHBOARD OVERVIEW ---
        public async Task<IActionResult> Dashboard()
        {
            var currentHub = GetCurrentHub();
            var viewModel = await GetDashboardViewModel(currentHub);
            return View(viewModel);
        }

        // --- 2. WAREHOUSE QUEUE (Ready to Sort/Ship) ---
        public async Task<IActionResult> WarehouseQueue()
        {
            var currentHub = GetCurrentHub();
            var orders = await _context.Orders
                .Include(o => o.AssignedRider)
                .Include(o => o.ManualRider)
                .Where(o => !o.IsArchived &&
                             o.CurrentLocation == currentHub &&
                             (o.Status == "Packed in Warehouse" || 
                              o.Status == "Sorted for Transfer" || 
                              o.Status == "Out for Delivery" ||
                              o.Status.StartsWith("Arrived at")))
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            var allRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
            var filteredRiders = allRiders.Where(r => r.Hub == currentHub).ToList();
            var userRiderNames = filteredRiders.Select(r => $"{r.FirstName} {r.LastName}".ToLower()).ToList();
            
            var manualRiders = await _context.ManualRiders
                .Where(r => r.Hub == currentHub && r.IsActive)
                .ToListAsync();

            ViewBag.AvailableRiders = filteredRiders;
            ViewBag.ManualRiders = manualRiders.Where(m => !userRiderNames.Contains(m.Name.ToLower())).ToList();
            ViewBag.CurrentHub = currentHub;
            ViewBag.Hubs = HubRegistry.Names;
            return View(orders);
        }

        // --- 3. PACKING LINE ---
        public async Task<IActionResult> PackingLine()
        {
            var currentHub = GetCurrentHub();
            var orders = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub &&
                             (o.Status == "Pending" || o.Status == "Picked") &&
                             !o.IsArchived)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            return View(orders);
        }

        // --- 4. HUB TRANSFERS (Inbound/Outbound/Transit) ---
        public async Task<IActionResult> HubTransfers()
        {
            var currentHub = GetCurrentHub();

            var outbound = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub &&
                            (o.Status == "Sorted for Transfer" || o.Status == "Packed in Warehouse") &&
                            !o.IsArchived)
                .ToListAsync();

            var inTransit = await _context.Orders
                .Where(o => o.CurrentLocation == currentHub && 
                            o.Status.Contains("In Transit to") &&
                            !o.IsArchived)
                .ToListAsync();

            var inbound = await _context.Orders
                .Where(o => o.Status == $"In Transit to {currentHub}" && !o.IsArchived)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            ViewBag.Hubs = HubRegistry.Names.ToList();
            ViewBag.Outbound = outbound;
            ViewBag.InTransit = inTransit;
            ViewBag.InboundParcels = inbound;

            // Merging outbound and transit for the main view list
            return View(outbound.Union(inTransit).OrderByDescending(o => o.UpdatedAt).ToList());
        }

        // --- POST ACTIONS ---

        [HttpPost]
        public async Task<IActionResult> MarkPicked(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
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
                order.Status = "Packed in Warehouse";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Parcel #{trackingId} is now packed.";
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> MarkSortedForDestination(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);

            // FIX: Added 'Arrived' check so Cebu/Manila can sort inbound parcels
            if (order != null && order.CurrentLocation == currentHub &&
               (order.Status == "Packed in Warehouse" || 
                order.Status == "Packed" || 
                order.Status.Contains("Arrived at")))
            {
                order.Status = "Sorted for Transfer";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Parcel #{trackingId} is sorted for transfer.";
            }
            else
            {
                TempData["ErrorMessage"] = "Sorting failed: Order must be Packed or Arrived at this hub.";
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> MarkShipped(string trackingId, string targetHub)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null && order.CurrentLocation == currentHub)
            {
                order.Status = $"In Transit to {targetHub}";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Parcel #{trackingId} is now in transit to {targetHub}.";
            }
            return RedirectToAction(nameof(HubTransfers));
        }

        [HttpPost]
        public async Task<IActionResult> MarkReceived(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);

            // Dynamic check: Does the status contain "Transit" and the name of the current hub?
            if (order != null && order.Status.Contains("Transit", StringComparison.OrdinalIgnoreCase) 
                           && order.Status.Contains(currentHub, StringComparison.OrdinalIgnoreCase))
            {
                order.CurrentLocation = currentHub;
                order.Status = $"Arrived at {currentHub}";
                order.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Parcel #{trackingId} received at {currentHub}!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error: This parcel is not destined for this hub.";
            }
            return RedirectToAction(nameof(HubTransfers));
        }

        [HttpPost]
        public async Task<IActionResult> QuickReceive(string trackingId)
        {
            return await MarkReceived(trackingId);
        }

        [HttpPost]
        public async Task<IActionResult> AssignRider(string trackingId, string riderId)
        {
            var currentHub = GetCurrentHub();
            Console.WriteLine($"[DEBUG] AssignRider - Tracking: {trackingId}, RiderID: {riderId}, CurrentHub: {currentHub}");
            
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null && order.CurrentLocation == currentHub && 
                (order.Status.Contains("Arrived") || order.Status == "Packed in Warehouse" || 
                 order.Status == "Sorted for Transfer" || order.Status == "Out for Delivery"))
            {
                Console.WriteLine($"[DEBUG] Valid Order found for assignment. Current Status: {order.Status}");
                if (riderId.StartsWith("user_"))
                {
                    order.AssignedRiderId = riderId.Replace("user_", "").Trim();
                    order.ManualRiderId = null;
                    Console.WriteLine($"[DEBUG] Assigned to USER Rider: {order.AssignedRiderId}");
                }
                else if (riderId.StartsWith("manual_"))
                {
                    if (int.TryParse(riderId.Replace("manual_", ""), out int mId))
                    {
                        order.ManualRiderId = mId;
                        order.AssignedRiderId = null;
                        Console.WriteLine($"[DEBUG] Assigned to MANUAL Rider: {mId}");
                    }
                }

                order.Status = "Out for Delivery";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Rider assigned successfully.";
            }
            else
            {
                string reason = order == null ? "Not Found" : (order.CurrentLocation != currentHub ? "Hub Mismatch" : "Status Invalid");
                Console.WriteLine($"[DEBUG] AssignRider FAILED. Reason: {reason}");
                
                if (order == null) TempData["ErrorMessage"] = "Order not found.";
                else if (order.CurrentLocation != currentHub) TempData["ErrorMessage"] = $"Order location mismatch. Order is at {order.CurrentLocation}, but you are at {currentHub}.";
                else TempData["ErrorMessage"] = $"Order status ({order.Status}) is not valid for assignment.";
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // --- HELPER: VIEWMODEL BUILDER ---
        private async Task<AdminDashboardViewModel> GetDashboardViewModel(string currentHub)
        {
            var localOrders = await _context.Orders
                .Include(o => o.AssignedRider)
                .Include(o => o.ManualRider)
                .Where(o => o.CurrentLocation == currentHub && !o.IsArchived)
                .ToListAsync();

            var allRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
            var filteredRiders = allRiders.Where(r => r.Hub == currentHub).ToList();

            var userRiderNames = filteredRiders.Select(r => $"{r.FirstName} {r.LastName}".ToLower()).ToList();
            var manualRiders = await _context.ManualRiders
                .Where(r => r.Hub == currentHub && r.IsActive)
                .ToListAsync();
            
            // Filter out manual riders that already have a system user account to prevent double-assignment confusion
            var filteredManualRiders = manualRiders
                .Where(m => !userRiderNames.Contains(m.Name.ToLower()))
                .ToList();

            return new AdminDashboardViewModel
            {
                CurrentHub = currentHub,
                RecentShipments = localOrders.OrderByDescending(o => o.UpdatedAt).Take(10).ToList(),
                PendingPickOrders = localOrders.Where(o => o.Status == "Pending").ToList(),
                PickedOrders = localOrders.Where(o => o.Status == "Picked").ToList(),
                PackedOrders = localOrders.Where(o => o.Status == "Packed in Warehouse" || o.Status == "Sorted for Transfer").ToList(),
                AvailableRiders = filteredRiders,
                ManualRiders = filteredManualRiders,
                Hubs = HubRegistry.Names
            };
        }
    }
}