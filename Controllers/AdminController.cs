using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;

namespace SwiftFill.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        public IActionResult Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                // Current Volume = orders NOT yet delivered or returned
                TotalOrders = _context.Orders.Count(o => o.Status != "Delivered" && o.Status != "Returned"),
                PendingOrders = _context.Orders.Count(o => o.Status == "Pending"),
                InTransit = _context.Orders.Count(o => o.Status == "Transit" || o.Status == "OutForDelivery"),
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
            return View(model);
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
            }
            return RedirectToAction(nameof(Archive));
        }

        public async Task<IActionResult> EditOrder(string trackingId)
        {
            var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            if (order == null) return NotFound();
            return View(order);
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
                order.DeclaredValue = model.DeclaredValue;
                order.UpdatedAt = DateTime.UtcNow;

                // Handle transaction mapping for Drop-offs
                if (!string.IsNullOrEmpty(PaymentMethod))
                {
                    if (order.Payment == null) 
                    {
                        order.Payment = new Payment { TrackingId = order.TrackingId, Amount = (decimal)(order.Weight * 45 + 150) };
                        _context.Payments.Add(order.Payment);
                    }
                    
                    order.Payment.Method = PaymentMethod;
                    if (PaymentMethod == "Cash" || PaymentMethod == "Bank Transfer" || PaymentMethod == "Prepaid") {
                        if (!order.Payment.IsPaid) {
                            order.Payment.IsPaid = true;
                            order.Payment.PaidAt = DateTime.UtcNow;
                        }
                    } else {
                        order.Payment.IsPaid = false;
                        order.Payment.PaidAt = null;
                    }
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order {model.TrackingId} and Payment verified.";
                return RedirectToAction(nameof(Shipments));
            }
            return View(model);
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
        public async Task<IActionResult> CreateManualOrder(Order model, string paymentMethod)
        {
            // Generate Tracking ID: SF-2026-XXXXX
            string randomSuffix = new Random().Next(10000, 99999).ToString();
            model.TrackingId = $"SF-2026-{randomSuffix}";
            model.Status = "Pending";
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Orders.Add(model);

            var payment = new Payment
            {
                TrackingId = model.TrackingId,
                Amount = (decimal)(model.Weight * 45 + 150), // Standard logistics calc
                Method = paymentMethod,
                IsPaid = paymentMethod == "Prepaid",
                PaidAt = paymentMethod == "Prepaid" ? DateTime.UtcNow : null
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Shipments));
        }

        public IActionResult Payments(string search, int page = 1)
        {
            int pageSize = 5;
            var query = _context.Payments.Where(p => p.IsPaid);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.TrackingId.Contains(search));

            var totalItems = query.Count();
            var payments = query.OrderByDescending(p => p.PaidAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(payments);
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

            int pageSize = 5;
            var query = _context.ReturnRequests.AsQueryable();

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

        public IActionResult RecentActivity()
        {
            // Just get the last 20 orders/payments/returns as activity
            var recentOrders = _context.Orders.OrderByDescending(o => o.CreatedAt).Take(10).ToList();
            return View(recentOrders);
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
                Delivered = filteredOrders.Count(o => o.Status == "Delivered")
            };

            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;

            return View(model);
        }
    }
}
