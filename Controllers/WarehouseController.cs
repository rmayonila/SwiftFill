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
        
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(string trackingId)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.AssignedRider)
                .Include(o => o.ManualRider)
                .FirstOrDefaultAsync(o => o.TrackingId == trackingId);

            if (order == null) return NotFound();

            return PartialView("_OrderDetailsPartial", order);
        }

        // --- 1. DASHBOARD OVERVIEW ---
        public async Task<IActionResult> Dashboard()
        {
            var currentHub = GetCurrentHub();
            var viewModel = await GetDashboardViewModel(currentHub);
            return View(viewModel);
        }

        // --- 2. WAREHOUSE QUEUE (Ready to Sort/Ship) ---
        public async Task<IActionResult> WarehouseQueue(string search, int page = 1)
        {
            int pageSize = 10;
            var currentHub = GetCurrentHub();
            var query = _context.Orders
                .Include(o => o.AssignedRider)
                .Include(o => o.ManualRider)
                .Where(o => !o.IsArchived &&
                             o.CurrentLocation == currentHub &&
                             (o.Status == "Packed in Warehouse" || 
                              o.Status == "Sorted for Transfer" || 
                              o.Status == "Out for Delivery" ||
                              o.Status.StartsWith("Arrived at") ||
                              o.Status == "Returning to Sender"))
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search) || o.ReceiverAddress.Contains(search));

            var totalItems = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(orders);
        }

        // --- 3. PACKING LINE ---
        public async Task<IActionResult> PackingLine(string search, int page = 1)
        {
            int pageSize = 10;
            var currentHub = GetCurrentHub();
            var query = _context.Orders
                .Where(o => o.CurrentLocation == currentHub &&
                             (o.Status == "Pending" || o.Status == "Picked" || o.Status == "Sent to Warehouse Packing" || o.Status == "Packed in Store") &&
                             !o.IsArchived)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search));

            var totalItems = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // For Assign Rider Modal
            var allRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
            ViewBag.AvailableRiders = allRiders.Where(r => r.Hub == currentHub).ToList();
            ViewBag.ManualRiders = await _context.ManualRiders.Where(r => r.Hub == currentHub && r.IsActive).ToListAsync();

            return View(orders);
        }

        // --- 4. HUB TRANSFERS (Inbound/Outbound/Transit) ---
        public async Task<IActionResult> HubTransfers(string search, int page = 1)
        {
            int pageSize = 10;
            var currentHub = GetCurrentHub();

            var query = _context.Orders.Where(o => !o.IsArchived && (
                (o.CurrentLocation == currentHub && (o.Status == "Sorted for Transfer" || o.Status == "Packed in Warehouse" || o.Status.Contains("In Transit to") || o.Status.Contains("In Transit back to"))) ||
                (o.Status == $"In Transit to {currentHub}" || o.Status == $"In Transit back to {currentHub}")
            )).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search));

            var totalItems = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Hubs = HubRegistry.Names.ToList();

            // For Assign Rider Modal
            var allRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
            ViewBag.AvailableRiders = allRiders.Where(r => r.Hub == currentHub).ToList();
            ViewBag.ManualRiders = await _context.ManualRiders.Where(r => r.Hub == currentHub && r.IsActive).ToListAsync();
            ViewBag.CurrentHub = currentHub;

            return View(orders);
        }

        // --- 5. ORDER HISTORY (Delivered/Returned) ---
        public async Task<IActionResult> OrderHistory(string search, int page = 1)
        {
            int pageSize = 10;
            var currentHub = GetCurrentHub();
            
            // Only allow non-Davao hubs to see history (per user request)
            if (currentHub == "Davao Hub") return RedirectToAction(nameof(Dashboard));

            var query = _context.Orders
                .Include(o => o.Payment)
                .Where(o => o.CurrentLocation == currentHub && (o.Status == "Delivered" || o.Status == "Returned"))
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search));

            var totalItems = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentHub = currentHub;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(orders);
        }

        // --- POST ACTIONS ---

        [HttpPost]
        public async Task<IActionResult> MarkPicked(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null && order.CurrentLocation == currentHub)
            {
                // If it was already packed in store, picking it in warehouse marks it as "Received/Packed" in WH system
                if (order.Status == "Packed in Store")
                {
                    order.Status = "Packed in Warehouse";
                }
                else
                {
                    order.Status = "Picked";
                }
                
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
        public async Task<IActionResult> ShipToHub(string trackingId, string targetHub)
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
                bool isReturningToOrigin = order.Status.Contains("back to", StringComparison.OrdinalIgnoreCase) 
                                           && currentHub.Equals(order.OriginHub ?? "Davao Hub", StringComparison.OrdinalIgnoreCase);

                order.CurrentLocation = currentHub;
                order.Status = isReturningToOrigin ? "Returned" : $"Arrived at {currentHub}";
                order.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = isReturningToOrigin 
                    ? $"Parcel #{trackingId} has been successfully RETURNED to its origin hub!" 
                    : $"Parcel #{trackingId} received at {currentHub}!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error: This parcel is not destined for this hub.";
            }
            return RedirectToAction(nameof(HubTransfers));
        }

        [HttpPost]
        public async Task<IActionResult> ShipBackToOrigin(string trackingId)
        {
            var currentHub = GetCurrentHub();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            
            if (order != null && order.CurrentLocation == currentHub && order.Status == "Returning to Sender")
            {
                var originHub = order.OriginHub ?? "Davao Hub";
                order.Status = $"In Transit back to {originHub}";
                order.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Parcel #{trackingId} is now in transit back to its origin: {originHub}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Return shipment failed: Order must be at your hub and approved as 'Returning to Sender'.";
            }
            return Redirect(Request.Headers["Referer"].ToString());
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
                .Where(o => (o.CurrentLocation == currentHub || o.Status.Contains(currentHub)) && !o.IsArchived)
                .ToListAsync();

            var inbound = await _context.Orders
                .CountAsync(o => (o.Status == $"In Transit to {currentHub}" || o.Status == $"In Transit back to {currentHub}") && !o.IsArchived);

            var allRiders = await _userManager.GetUsersInRoleAsync("DeliveryRider");
            var filteredRiders = allRiders.Where(r => r.Hub == currentHub).ToList();

            var userRiderNames = filteredRiders.Select(r => $"{r.FirstName} {r.LastName}".ToLower()).ToList();
            var manualRiders = await _context.ManualRiders
                .Where(r => r.Hub == currentHub && r.IsActive)
                .ToListAsync();
            
            var filteredManualRiders = manualRiders
                .Where(m => !userRiderNames.Contains(m.Name.ToLower()))
                .ToList();

            return new AdminDashboardViewModel
            {
                CurrentHub = currentHub,
                RecentShipments = localOrders.Where(o => o.CurrentLocation == currentHub).OrderByDescending(o => o.UpdatedAt).Take(10).ToList(),
                PendingPickOrders = localOrders.Where(o => o.CurrentLocation == currentHub && (o.Status == "Pending" || o.Status == "Picked")).ToList(),
                PickedOrders = localOrders.Where(o => o.CurrentLocation == currentHub && o.Status == "Packed in Warehouse").ToList(),
                PackedOrders = localOrders.Where(o => o.CurrentLocation == currentHub && o.Status == "Sorted for Transfer").ToList(),
                ReturningOrders = localOrders.Where(o => o.Status.Contains("back to " + currentHub) || (o.CurrentLocation == currentHub && o.Status == "Returning to Sender")).ToList(),
                InTransit = inbound,
                AvailableRiders = filteredRiders,
                ManualRiders = filteredManualRiders,
                Hubs = HubRegistry.Names
            };
        }
    }
}