using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using System.IO;
using SwiftFill.Helpers;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "DeliveryRider")]
    public class RiderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SwiftFill.Services.JawgMapsService _mapsService;
        private readonly SwiftFill.Services.CloudinaryService _cloudinaryService;

        public RiderController(ApplicationDbContext context, 
                               IWebHostEnvironment environment, 
                               UserManager<ApplicationUser> userManager,
                               SwiftFill.Services.JawgMapsService mapsService,
                               SwiftFill.Services.CloudinaryService cloudinaryService)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _mapsService = mapsService;
            _cloudinaryService = cloudinaryService;
        }

        // --- 1. ACTIVE TASKS & ROUTE-LOCKED JOB POOL ---
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var rider = await _userManager.FindByIdAsync(userId!);

            Console.WriteLine($"[DEBUG] Rider Dashboard Access - User: {User.Identity?.Name}, ID: {userId}");
            
            if (rider == null) return RedirectToAction("Login", "Account");

            string riderRoute = rider.Route ?? string.Empty;
            string riderFullName = $"{rider.FirstName} {rider.LastName}";

            var riderOrders = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.AssignedRider)
                .Include(o => o.ManualRider)
                .Where(o => !o.IsArchived && o.DeliveryAttempts < 3)
                .Where(o => o.Status != "Delivered" && o.Status != "Returned")
                .ToListAsync();

            // Filter in memory for maximum reliability
            var filteredOrders = riderOrders.Where(o => 
            {
                // 1. Direct system-user assignment
                bool isDirect = o.AssignedRiderId != null && o.AssignedRiderId.Trim() == userId?.Trim();
                if (isDirect) Console.WriteLine($"[DEBUG] Direct Match Found: {o.TrackingId}");

                // 2. Manual rider name match — warehouse assigns manual_ID, this ensures the order appears for the named rider
                bool isManualNameMatch = o.ManualRider != null &&
                    string.Equals(o.ManualRider.Name.Trim(), riderFullName.Trim(), StringComparison.OrdinalIgnoreCase);
                if (isManualNameMatch) Console.WriteLine($"[DEBUG] Manual Name Match: {o.TrackingId}");

                // 3. Show in pool only if matching hub/route
                bool isPool = o.AssignedRiderId == null && o.ManualRiderId == null &&
                              o.CurrentLocation == rider.Hub && 
                              !string.IsNullOrEmpty(riderRoute) && 
                              o.ReceiverAddress.Contains(riderRoute, StringComparison.OrdinalIgnoreCase);

                return isDirect || isManualNameMatch || isPool;
            }).ToList();

            Console.WriteLine($"[DEBUG] Total Orders Found: {riderOrders.Count}, Filtered for Rider: {filteredOrders.Count}");

            ViewBag.RiderRoute = rider.Route;
            ViewBag.RiderHub = rider.Hub;
            return View(filteredOrders);
        }

        // --- 2. ACCEPT TASK (For Unassigned Route Orders) ---
        [HttpPost]
        public async Task<IActionResult> AcceptTask(string trackingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);

            if (order != null && order.AssignedRiderId == null)
            {
                order.AssignedRiderId = userId;
                order.Status = "Out for Delivery"; // Automatic status update upon acceptance
                order.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Task #{trackingId} accepted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- 3. UPDATE STATUS (DELIVER/FAIL/RETURN) ---
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string trackingId, string status, string? failReason, IFormFile? proofPhoto)
        {
            try 
            {
                // Sanitization
                trackingId = InputSanitizer.Sanitize(trackingId) ?? "";
                status = InputSanitizer.Sanitize(status) ?? "";
                failReason = InputSanitizer.Sanitize(failReason);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            
            if (order == null) return NotFound();

            // Handle Photo Upload via Cloudinary
            if (proofPhoto != null && proofPhoto.Length > 0)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(proofPhoto);
                if (imageUrl != null)
                {
                    order.ProofImagePath = imageUrl;
                }
            }

            if (status == "Delivered")
            {
                if (proofPhoto == null || proofPhoto.Length == 0)
                {
                    TempData["ErrorMessage"] = "POD Photo is required for completion.";
                    return RedirectToAction(nameof(Index));
                }

                order.Status = "Delivered";
                order.DeliveryAttempts++;
                if (order.Payment != null && order.Payment.Method == "COD") {
                    order.Payment.IsPaid = true;
                    order.Payment.PaidAt = DateTime.UtcNow;
                    order.Payment.CollectedByUserId = userId;
                }
                TempData["SuccessMessage"] = $"Order {trackingId} delivered!";
            }
            else if (status == "Failed")
            {
                order.DeliveryAttempts++;
                order.Notes = $"Attempt {order.DeliveryAttempts}: {failReason}";
                
                if (order.DeliveryAttempts >= 3) {
                    order.Status = "Returned";
                }
            }
            else if (status == "Returned")
            {
                order.Status = "Returned";
                order.Notes = $"Manual Return: {failReason}";
            }

            order.UpdatedAt = DateTime.UtcNow;

            // Handle Return Requests in DB
            if (order.Status == "Returned")
            {
                var existingReturn = await _context.ReturnRequests.FirstOrDefaultAsync(r => r.TrackingId == trackingId);
                if (existingReturn == null)
                {
                    _context.ReturnRequests.Add(new ReturnRequest 
                    {
                        TrackingId = order.TrackingId,
                        Reason = "Rider Initiated Return",
                        Description = failReason ?? order.Notes ?? "Failed delivery attempts or manual return.",
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the technical error internally
                Console.WriteLine($"[ERROR] RiderController.UpdateStatus: {ex.Message}");
                
                // Show a nice message to the user
                TempData["ErrorMessage"] = "Something went wrong while updating the order status. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // --- 4. DELIVERY HISTORY ---
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var completedOrders = await _context.Orders
                .Include(o => o.Payment)
                .Where(o => o.AssignedRiderId == userId && (o.Status == "Delivered" || o.Status == "Returned") && !o.IsArchived)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            return View(completedOrders);
        }

        // --- 5. CASH REMITTANCE ---
        public async Task<IActionResult> Remittance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var pendingPayments = await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.CollectedByUserId == userId && p.Method == "COD" && !p.IsArchived)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            return View(pendingPayments);
        }

        // --- 6. AJAX DETAILS FOR MODAL ---
        [HttpGet]
        public async Task<IActionResult> GetEta(string destination)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rider = await _userManager.FindByIdAsync(userId!);
            
            // Start from the Hub location
            string origin = rider?.Hub ?? "Davao Hub";
            var result = await _mapsService.GetDistanceAndTimeAsync(origin, destination);
            
            return Json(new { eta = result });
        }

        [HttpGet]
        public async Task<JsonResult> GetOrderDetails(string trackingId)
        {
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order == null) return Json(new { success = false });

            return Json(new {
                trackingId = order.TrackingId,
                receiverName = order.ReceiverName,
                receiverAddress = order.ReceiverAddress,
                status = order.Status,
                attempts = order.DeliveryAttempts,
                notes = order.Notes,
                amount = order.Payment?.Amount.ToString("N2") ?? "0.00",
                paymentMethod = order.Payment?.Method ?? "Unspecified",
                photo = order.ProofImagePath
            });
        }
        // --- 7. MARK AS REMITTED ---
        [HttpPost]
        public async Task<IActionResult> MarkAsRemitted()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pendingPayments = await _context.Payments
                .Where(p => p.CollectedByUserId == userId && p.Method == "COD" && !p.IsArchived)
                .ToListAsync();

            if (pendingPayments.Any())
            {
                foreach (var payment in pendingPayments)
                {
                    payment.IsArchived = true; // Archive them so they disappear from the rider's list
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Remittance recorded. Please ensure you have handed over the cash to the hub staff.";
            }
            else
            {
                TempData["ErrorMessage"] = "No pending collections found to remit.";
            }

            return RedirectToAction(nameof(Remittance));
        }
    }
}