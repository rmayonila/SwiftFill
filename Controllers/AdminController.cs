using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using SwiftFill.Services;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _audit;
        private readonly OrderService _orderService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditLogService audit, OrderService orderService)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _orderService = orderService;
        }

        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        public IActionResult Dashboard()
        {
            var model = GetDashboardViewModel();
            return View(model);
        }

        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            var model = GetDashboardViewModel();
            return Json(new {
                totalOrders = model.TotalOrders,
                pendingOrders = model.PendingOrders,
                inTransit = model.InTransit,
                delivered = model.Delivered,
                totalRevenue = model.TotalRevenue,
                statusCounts = model.StatusCounts,
                dailyTrend = model.DailyTrend,
                recentShipments = model.RecentShipments.Select(s => new {
                    s.TrackingId,
                    s.SenderName,
                    s.ReceiverName,
                    s.DestinationRegion,
                    s.Status,
                    createdAt = s.CreatedAt.ToString("MMM dd"),
                    weight = s.Weight
                })
            });
        }

        private AdminDashboardViewModel GetDashboardViewModel()
        {
            return new AdminDashboardViewModel
            {
                TotalOrders = _context.Orders.Count(o => o.Status != "Delivered" && o.Status != "Returned"),
                PendingOrders = _context.Orders.Count(o => o.Status == "Pending"),
                InTransit = _context.Orders.Count(o => (o.Status.Contains("Transit") || o.Status == "OutForDelivery")),
                Delivered = _context.Orders.Count(o => o.Status == "Delivered"),
                TotalRevenue = _context.Payments.Where(p => p.IsPaid).Sum(p => p.Amount),
                
                StatusCounts = _context.Orders.GroupBy(o => o.Status)
                    .Select(g => new StatusDistributionItem { Status = g.Key, Count = g.Count() }).ToList(),
                
                DailyTrend = _context.Orders.Where(o => o.CreatedAt >= DateTime.Now.AddDays(-7))
                    .GroupBy(o => o.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyTrendItem { Date = g.Key.ToString("MM-dd"), Count = g.Count() }).ToList(),
                
                RecentShipments = _context.Orders.OrderByDescending(o => o.CreatedAt).Take(5).ToList()
            };
        }
        
        public async Task<IActionResult> Shipments(string search, string status, string region, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Orders.Include(o => o.AssignedRider).Include(o => o.Payment).Where(o => !o.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.SenderName.Contains(search) || o.ReceiverName.Contains(search) || o.ReceiverAddress.Contains(search));

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(region) && region != "All")
                query = query.Where(o => o.DestinationRegion == region);

            var totalItems = query.Count();
            var orders = query.OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Region = region;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.PaymentMethods = await _context.PaymentMethods.Where(m => m.IsActive).ToListAsync();

            return View(orders);
        }

        public IActionResult Monitoring(string search, string status, string region, int page = 1) 
            => RedirectToAction(nameof(Shipments), new { search, status, region, page });

        [HttpPost]
        public async Task<IActionResult> ArchiveOrder(string trackingId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null)
            {
                order.IsArchived = true;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order {trackingId} archived.";
                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Archive Order",
                    $"Order {trackingId} ({order.ReceiverName} → {order.DestinationRegion}) archived.",
                    AuditLogType.System);
            }
            return RedirectToAction(nameof(Shipments));
        }

        public IActionResult Archive(string search, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Orders.Include(o => o.AssignedRider).Where(o => o.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.SenderName.Contains(search) || o.ReceiverName.Contains(search));

            var totalItems = query.Count();
            var orders = query.OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreOrder(string trackingId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null)
            {
                order.IsArchived = false;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order {trackingId} restored.";
                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Restore Order",
                    $"Order {trackingId} restored from archive.",
                    AuditLogType.System);
            }
            return RedirectToAction(nameof(Archive));
        }

        public async Task<IActionResult> EditOrder(string trackingId)
        {
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderJson(string trackingId)
        {
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order == null) return NotFound();
            return Json(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetShippingRatesJson()
        {
            var rates = await _context.ShippingRates.ToListAsync();
            return Json(rates);
        }

        [HttpPost]
        public async Task<IActionResult> EditOrder(Order model, string? PaymentMethod)
        {
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == model.TrackingId);
            if (order != null)
            {
                order.SenderName = model.SenderName;
                order.SenderAddress = model.SenderAddress;
                order.SenderPhone = model.SenderPhone;
                order.ReceiverName = model.ReceiverName;
                order.ReceiverAddress = model.ReceiverAddress;
                order.ReceiverPhone = model.ReceiverPhone;
                order.Weight = model.Weight;
                order.DestinationRegion = model.DestinationRegion;
                order.ItemCategory = model.ItemCategory;
                order.IsFragile = model.IsFragile;
                order.DeclaredValue = model.DeclaredValue;
                order.AvailPacking = model.AvailPacking;
                order.PackingFee = model.PackingFee;
                order.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(PaymentMethod))
                {
                    if (order.Payment == null)
                    {
                        order.Payment = new Payment { TrackingId = order.TrackingId };
                        _context.Payments.Add(order.Payment);
                    }

                    // Always auto-calculate from DB shipping rates + existing packing fee
                    var rates = await _context.ShippingRates.FirstOrDefaultAsync(r => r.DestinationRegion == order.DestinationRegion);
                    decimal totalAmount = 150 + (decimal)(order.Weight * 45) + order.PackingFee; // fallback
                    if (rates != null) totalAmount = rates.Calculate(order.Weight) + order.PackingFee;

                    // Keep Payment and Order records in sync
                    order.Payment.Amount = totalAmount;
                    order.ShippingFee = totalAmount;

                    order.Payment.Method = PaymentMethod;
                    if (PaymentMethod == "Cash" || PaymentMethod == "Bank Transfer" || PaymentMethod == "Prepaid")
                    {
                        if (!order.Payment.IsPaid) { order.Payment.IsPaid = true; order.Payment.PaidAt = DateTime.UtcNow; }
                    }
                    else { order.Payment.IsPaid = false; order.Payment.PaidAt = null; }
                }

                await _context.SaveChangesAsync();
                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Edit Order",
                    $"Order {model.TrackingId} edited — Receiver: {model.ReceiverName}, Region: {model.DestinationRegion}, Amount: ₱{order.Payment?.Amount:N2}.",
                    AuditLogType.System);
                TempData["SuccessMessage"] = $"Order {model.TrackingId} updated successfully.";
                return RedirectToAction(nameof(Shipments));
            }
            return View(model);
        }

        public async Task<IActionResult> Pack(string search, string status, int page = 1)
        {
            int pageSize = 10;
            // Item 6: Only show orders where the customer availed packing service
            var query = _context.Orders.Where(o => o.AvailPacking).AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }
            else
            {
                query = query.Where(o => o.Status == "Pending" || o.Status == "Picked");
            }

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search));

            var totalItems = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePacking(string trackingId, bool availPacking, decimal packingFee, string packingLocation, string sortingStatus, double? finalWeight)
        {
            try
            {
                var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
                if (order == null) return Json(new { success = false, message = "Order not found." });

                // Item 8: Update order weight if the admin provides a final measured weight
                if (finalWeight.HasValue && finalWeight.Value > 0)
                {
                    order.Weight = finalWeight.Value;
                }

                // Item 8: Recalculate total based on final weight + final packing fee
                if (order.Payment != null)
                {
                    var rates = await _context.ShippingRates.FirstOrDefaultAsync(r => r.DestinationRegion == order.DestinationRegion);
                    decimal shippingAmount = 150 + (decimal)(order.Weight * 45); // fallback
                    if (rates != null) shippingAmount = rates.Calculate(order.Weight);
                    order.Payment.Amount = shippingAmount + packingFee;
                    order.ShippingFee = order.Payment.Amount;
                }

                order.AvailPacking = availPacking;
                order.PackingFee = packingFee;
                order.PackingLocation = packingLocation;
                order.SortingStatus = sortingStatus;
                order.PackedByStaffId = _userManager.GetUserId(User);
                
                // Set status based on packing location
                if (packingLocation == "Warehouse")
                {
                    order.Status = "Sent to Warehouse Packing";
                }
                else
                {
                    order.Status = "Packed in Store";
                }
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Packing Update",
                    $"Order {trackingId} packed in warehouse by {User.Identity?.Name}. Final weight: {order.Weight}kg. Total: ₱{order.Payment?.Amount}.",
                    AuditLogType.Inventory);

                return Json(new { success = true, trackingId = trackingId, newTotal = order.Payment?.Amount ?? 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "System Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> OrderSummary(string trackingId)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.AssignedRider)
                .FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> DispatchToHub(string trackingId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null)
            {
                order.Status = $"In Transit to {order.DestinationRegion} Hub";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order {trackingId} dispatched to {order.DestinationRegion}.";
            }
            return RedirectToAction(nameof(Shipments));
        }

        [HttpPost]
        public async Task<IActionResult> ArriveAtHub(string trackingId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order != null)
            {
                order.CurrentLocation = order.DestinationRegion;
                order.Status = $"Arrived at {order.DestinationRegion} Hub";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order {trackingId} received at {order.DestinationRegion} Hub.";
            }
            return RedirectToAction(nameof(Shipments));
        }

        [HttpPost]
        public async Task<IActionResult> CreateManualOrder(Order model, string paymentMethod, string deliveryType = "DoorToDoor", string originHub = "Davao Hub", bool availPacking = false, decimal packingFee = 0, bool isFragile = false, string? ReceiverLandmark = null)
        {
            // Guard: originHub must be a real hub
            if (!SwiftFill.Models.HubRegistry.Names.Contains(originHub))
                originHub = SwiftFill.Models.HubRegistry.All[0].Name;

            // Item 5: Consistent SF-prefixed tracking ID using the shared service
            model.TrackingId = _orderService.GenerateTrackingId();
            model.IsFragile = isFragile;
            model.Status = "Pending";
            model.DeliveryType = deliveryType;    // "DoorToDoor" or "BranchPickup"
            model.OriginHub = originHub;
            model.CurrentLocation = originHub;    // Parcel starts at chosen origin hub
            model.AvailPacking = availPacking;
            model.PackingFee = packingFee;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(ReceiverLandmark))
                model.ReceiverAddress = $"{model.ReceiverAddress} | Landmark: {ReceiverLandmark.Trim()}";

            _context.Orders.Add(model);

            // Pricing from DB
            var rates = _context.ShippingRates.FirstOrDefault(r => r.DestinationRegion == model.DestinationRegion);
            decimal totalAmount = 150 + (decimal)(model.Weight * 45) + packingFee; // Fallback + packing
            if (rates != null)
            {
                totalAmount = rates.Calculate(model.Weight) + packingFee;
            }

            var payment = new Payment
            {
                TrackingId = model.TrackingId,
                Amount = totalAmount,
                Method = paymentMethod,
                IsPaid = paymentMethod == "Prepaid",
                PaidAt = paymentMethod == "Prepaid" ? DateTime.UtcNow : null
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Create Order",
                $"New order {model.TrackingId} created for {model.ReceiverName} ({deliveryType}) from {originHub} \u2192 {model.DestinationRegion}.",
                AuditLogType.System);
            TempData["SuccessMessage"] = $"Order {model.TrackingId} created · {(deliveryType == "BranchPickup" ? "Branch Pickup" : "Door-to-Door")} · Origin: {originHub}";
            return RedirectToAction(nameof(Shipments));
        }


        public IActionResult Payments(string search, string status, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Payments.Include(p => p.Order).Where(p => !p.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.TrackingId.Contains(search) || p.Order.ReceiverName.Contains(search));

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Paid") query = query.Where(p => p.IsPaid);
                else if (status == "Pending") query = query.Where(p => !p.IsPaid);
            }

            var totalItems = query.Count();
            var payments = query.OrderByDescending(p => p.IsPaid == false).ThenByDescending(p => p.PaidAt ?? p.Order.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(payments);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int paymentId)
        {
            var payment = await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment != null)
            {
                payment.IsPaid = true;
                payment.PaidAt = DateTime.UtcNow;
                payment.CollectedByUserId = _userManager.GetUserId(User);
                
                if (payment.Order != null && payment.Order.Status != "Delivered")
                {
                    // If it was COD and we're settling it, it's a good sign the delivery was successful
                    // though usually the rider marks it delivered first.
                }

                await _context.SaveChangesAsync();
                
                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Payment Settlement",
                    $"Payment for {payment.TrackingId} (₱{payment.Amount}) confirmed as received.",
                    AuditLogType.Finance);
                
                TempData["SuccessMessage"] = $"Payment for {payment.TrackingId} settled successfully.";
            }
            return RedirectToAction(nameof(Payments));
        }

        [HttpPost]
        public async Task<IActionResult> ArchivePayment(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.IsArchived = true;
                await _context.SaveChangesAsync();
                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Archive Payment", $"Payment ID {paymentId} archived.");
                TempData["SuccessMessage"] = "Payment record archived.";
            }
            return RedirectToAction(nameof(Payments));
        }

        public IActionResult Reconciliation(string search, int page = 1) 
            => RedirectToAction(nameof(Payments), new { search, page });

        public async Task<IActionResult> Returns(string search, string status, int page = 1)
        {
            // Auto-heal missing ReturnRequests from Orders marked "Returned"
            var orphanedReturns = await _context.Orders
                .Where(o => o.Status == "Returned" && !_context.ReturnRequests.Any(r => r.TrackingId == o.TrackingId))
                .ToListAsync();
            
            if (orphanedReturns.Any())
            {
                foreach (var order in orphanedReturns)
                {
                    _context.ReturnRequests.Add(new ReturnRequest 
                    {
                        TrackingId = order.TrackingId,
                        Reason = "System Auto-Sync Return",
                        Description = order.Notes ?? "Auto-generated from failed delivery state.",
                        Status = "Pending",
                        CreatedAt = order.UpdatedAt
                    });
                }
                await _context.SaveChangesAsync();
            }

            int pageSize = 10;
            var query = _context.ReturnRequests.Where(r => !r.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.TrackingId.Contains(search) || r.Reason.Contains(search));

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(r => r.Status == status);

            var totalItems = await query.CountAsync();
            var returns = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(returns);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReturn(int requestId, string actionType)
        {
            var returnReq = await _context.ReturnRequests.Include(r => r.Order).FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (returnReq != null)
            {
                if (actionType == "Notify")
                {
                    returnReq.Status = "Sender Notified";
                    if (returnReq.Order != null) returnReq.Order.Status = "Return Notified";
                }
                else if (actionType == "Approve")
                {
                    returnReq.Status = "Approved";
                    if (returnReq.Order != null) returnReq.Order.Status = "Returning to Sender";
                }
                else if (actionType == "Reject")
                {
                    returnReq.Status = "Rejected";
                    if (returnReq.Order != null) returnReq.Order.Status = "Return Hold";
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Return #{requestId} updated: {returnReq.Status}.";
            }
            return RedirectToAction(nameof(Returns));
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveReturnRequest(int requestId)
        {
            var returnReq = await _context.ReturnRequests.FindAsync(requestId);
            if (returnReq != null)
            {
                returnReq.IsArchived = true;
                await _context.SaveChangesAsync();
                _audit.Log(User.Identity?.Name ?? "Admin", "Admin", "Archive Return", $"Return request #{requestId} archived.");
                TempData["SuccessMessage"] = "Return request archived.";
            }
            return RedirectToAction(nameof(Returns));
        }

        public async Task<IActionResult> ShippingRates()
        {
            var rates = await _context.ShippingRates.ToListAsync();
            // Seed defaults if empty
            if (!rates.Any())
            {
                var defaults = new List<ShippingRate>
                {
                    new ShippingRate { DestinationRegion = "NCR",      BaseRate = 100, PricePerKg = 45, ZoneSurcharge = 50 },
                    new ShippingRate { DestinationRegion = "Luzon",    BaseRate = 100, PricePerKg = 45, ZoneSurcharge = 100 },
                    new ShippingRate { DestinationRegion = "Visayas",  BaseRate = 100, PricePerKg = 45, ZoneSurcharge = 150 },
                    new ShippingRate { DestinationRegion = "Mindanao", BaseRate = 100, PricePerKg = 45, ZoneSurcharge = 200 }
                };
                _context.ShippingRates.AddRange(defaults);
                await _context.SaveChangesAsync();
                rates = defaults;
            }
            return View(rates);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShippingRate(ShippingRate model)
        {
            var rate = await _context.ShippingRates.FirstOrDefaultAsync(r => r.DestinationRegion == model.DestinationRegion);
            if (rate != null)
            {
                rate.BaseRate = model.BaseRate;
                rate.PricePerKg = model.PricePerKg;
                rate.ZoneSurcharge = model.ZoneSurcharge;
                rate.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Rates for {model.DestinationRegion} updated successfully.";
            }
            return RedirectToAction(nameof(ShippingRates));
        }

        public IActionResult RecentActivity(string search, string status, int page = 1)
        {
            int pageSize = 15;
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.TrackingId.Contains(search) || o.ReceiverName.Contains(search) || o.CurrentLocation.Contains(search));

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(o => o.Status == status);

            var totalItems = query.Count();
            var orders = query.OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(orders);
        }

        public IActionResult Reports(int? month, int? year)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;

            var filteredOrders = _context.Orders
                .Where(o => o.CreatedAt.Month == targetMonth && o.CreatedAt.Year == targetYear);

            var model = new AdminDashboardViewModel
            {
                RevenueByRegion = filteredOrders.GroupBy(o => o.DestinationRegion)
                    .Select(g => new RegionRevenueItem { Region = g.Key, Total = (decimal)g.Count() }).ToList(),
                TotalOrders = filteredOrders.Count(),
                Delivered = filteredOrders.Count(o => o.Status == "Delivered"),
                DailyTrend = filteredOrders.GroupBy(o => o.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyTrendItem { Date = g.Key.ToString("MM-dd"), Count = g.Count() }).ToList()
            };

            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;

            return View(model);
        }

        public async Task<IActionResult> PaymentsManagement()
        {
            var methods = await _context.PaymentMethods.ToListAsync();
            return View(methods);
        }

        [HttpPost]
        public async Task<IActionResult> AddPaymentMethod(PaymentMethod method)
        {
            if (ModelState.IsValid)
            {
                _context.PaymentMethods.Add(method);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Payment method {method.Name} added.";
            }
            return RedirectToAction(nameof(PaymentsManagement));
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePaymentMethod(PaymentMethod method)
        {
            var existing = await _context.PaymentMethods.FindAsync(method.Id);
            if (existing != null)
            {
                existing.Name = method.Name;
                existing.Description = method.Description;
                existing.IsActive = method.IsActive;
                existing.IsOnline = method.IsOnline;
                existing.IconClass = method.IconClass;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Payment method {method.Name} updated.";
            }
            return RedirectToAction(nameof(PaymentsManagement));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method != null)
            {
                _context.PaymentMethods.Remove(method);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payment method removed.";
            }
            return RedirectToAction(nameof(PaymentsManagement));
        }
    }
}
