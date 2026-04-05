using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using System.IO;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "DeliveryRider")]
    public class RiderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RiderController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // --- 1. ACTIVE TASKS (DASHBOARD) ---
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Keeps orders in the list if they aren't Delivered/Returned AND haven't hit 3 attempts
            var riderOrders = await _context.Orders
                .Include(o => o.Payment)
                .Where(o => o.AssignedRiderId == userId && 
                           o.Status != "Delivered" && 
                           o.Status != "Returned" && 
                           o.DeliveryAttempts < 3 && 
                           !o.IsArchived)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            return View(riderOrders);
        }

        // --- 2. UPDATE STATUS (DELIVER/FAIL/RETURN) ---
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string trackingId, string status, string? failReason, IFormFile? proofPhoto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            
            if (order == null) return NotFound();

            // Handle Photo Upload
            if (proofPhoto != null && proofPhoto.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/proofs");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = $"{trackingId}_{DateTime.Now.Ticks}{Path.GetExtension(proofPhoto.FileName)}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await proofPhoto.CopyToAsync(fileStream);
                }
                order.ProofImagePath = "/uploads/proofs/" + fileName;
            }

            if (status == "Delivered")
            {
                order.Status = "Delivered";
                order.DeliveryAttempts++;
                if (order.Payment != null) {
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
                
                // Auto-Return if 3 attempts reached
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

        // --- 3. DELIVERY HISTORY ---
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

        // --- 4. CASH REMITTANCE ---
        public async Task<IActionResult> Remittance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.UtcNow.Date;

            var totalCollected = await _context.Payments
                .Where(p => p.CollectedByUserId == userId && p.PaidAt >= today && p.Method == "COD")
                .SumAsync(p => p.Amount);

            return View(totalCollected);
        }

        // --- 5. AJAX DETAILS FOR MODAL ---
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
                photo = order.ProofImagePath
            });
        }
    }
}