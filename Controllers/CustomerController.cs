using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SwiftFill.Data;
using SwiftFill.Models;
using SwiftFill.Services;
using Microsoft.EntityFrameworkCore;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrderService _orderService;

        public CustomerController(ApplicationDbContext context, OrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders.OrderByDescending(o => o.CreatedAt).ToList();
            return View(orders);
        }

        [HttpGet]
        public IActionResult Book()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Book(Order order, string? ReceiverLandmark)
        {
            if (ModelState.IsValid)
            {
                // Attach landmarks dynamically without breaking schema
                if (!string.IsNullOrWhiteSpace(ReceiverLandmark))
                    order.ReceiverAddress = $"{order.ReceiverAddress} | Landmark: {ReceiverLandmark.Trim()}";

                order.TrackingId = _orderService.GenerateTrackingId();
                order.CreatedAt = DateTime.UtcNow;
                order.Status = "Pending";
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Dynamic Pricing Engine
                decimal baseFee = 100m;
                decimal wgtFee = (decimal)order.Weight * 45m;
                decimal zoneFee = order.DestinationRegion switch
                {
                    "NCR" => 50m,
                    "Luzon" => 100m,
                    "Visayas" => 150m,
                    "Mindanao" => 200m,
                    _ => 0m
                };

                var expectedPayment = new Payment
                {
                    TrackingId = order.TrackingId,
                    Amount = baseFee + wgtFee + zoneFee,
                    Method = "Pending Hub Verification",
                    IsPaid = false
                };
                
                _context.Payments.Add(expectedPayment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Shipment registered successfully! Tracking ID: {order.TrackingId}. Proceed to branch.";
                return RedirectToAction("Index");
            }
            return View(order);
        }

        [HttpGet]
        public IActionResult Track(string trackingId)
        {
            if (string.IsNullOrEmpty(trackingId))
            {
                return View();
            }

            var order = _context.Orders.FirstOrDefault(o => o.TrackingId == trackingId);
            return View(order);
        }

        public IActionResult Returns()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetOrderDetails(string trackingId)
        {
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order == null) return Json(new { success = false });

            var returnReq = await _context.ReturnRequests.FirstOrDefaultAsync(r => r.TrackingId == trackingId);

            return Json(new {
                success = true,
                trackingId = order.TrackingId,
                status = order.Status,
                destination = order.DestinationRegion,
                itemCategory = order.ItemCategory ?? "General",
                weight = order.Weight,
                lastUpdate = order.UpdatedAt.ToString("MMM dd yyyy, HH:mm"),
                notes = order.Notes ?? "",
                returnReason = returnReq != null ? returnReq.Description ?? returnReq.Reason : ""
            });
        }
    }
}
