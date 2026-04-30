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
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, OrderService orderService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _orderService = orderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.CustomerId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                order.CustomerId = user.Id;

                // Attach landmarks dynamically without breaking schema
                if (!string.IsNullOrWhiteSpace(ReceiverLandmark))
                    order.ReceiverAddress = $"{order.ReceiverAddress} | Landmark: {ReceiverLandmark.Trim()}";

                order.TrackingId = _orderService.GenerateTrackingId();
                order.CreatedAt = DateTime.UtcNow;
                order.Status = "Pending";
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Dynamic Pricing Engine from DB
                var rates = await _context.ShippingRates.FirstOrDefaultAsync(r => r.DestinationRegion == order.DestinationRegion);
                decimal baseFee = 100m;
                decimal wgtFee = (decimal)order.Weight * 45m;
                decimal zoneFee = 0m;
                
                if (rates != null)
                {
                    baseFee = rates.BaseRate;
                    wgtFee = (decimal)order.Weight * rates.PricePerKg;
                    zoneFee = rates.ZoneSurcharge;
                }

                // Apply packing fee if they want SwiftFill to pack it
                decimal packingFee = 0m;
                if (order.AvailPacking)
                {
                    packingFee = 50m; // Flat 50 fee for packing
                    order.PackingFee = packingFee;
                    order.PackingLocation = "Store";
                }

                var expectedPayment = new Payment
                {
                    TrackingId = order.TrackingId,
                    Amount = baseFee + wgtFee + zoneFee + packingFee,
                    Method = "Pending Hub Verification",
                    IsPaid = false
                };
                
                order.ShippingFee = expectedPayment.Amount;
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
                deliveryType = order.DeliveryType,
                pickupBranch = order.PickupBranchAddress ?? "",
                returnReason = returnReq != null ? returnReq.Description ?? returnReq.Reason : ""
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetRates()
        {
            var rates = await _context.ShippingRates.ToListAsync();
            return Json(rates);
        }
    }
}
