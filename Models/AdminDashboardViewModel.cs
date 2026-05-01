using System.Collections.Generic;

namespace SwiftFill.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int InTransit { get; set; }
        public int Delivered { get; set; }
        public decimal TotalRevenue { get; set; }
        
        public List<StatusDistributionItem> StatusCounts { get; set; } = new();
        public List<DailyTrendItem> DailyTrend { get; set; } = new();
        public List<RegionRevenueItem> RevenueByRegion { get; set; } = new();
        public List<Order> RecentShipments { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();

        // For Picking.cshtml
        public List<Order> PendingPickOrders { get; set; } = new();

        // For Packing.cshtml
        public List<Order> PickedOrders { get; set; } = new();

        // For Shipping.cshtml & Delivery.cshtml
        public List<Order> PackedOrders { get; set; } = new();
        public List<Order> ReturningOrders { get; set; } = new();

        // To populate the Rider Dropdown
        public List<ApplicationUser> AvailableRiders { get; set; } = new();
        public List<ManualRider> ManualRiders { get; set; } = new();

        public List<Order> LocalOrders { get; set; } = new();
        public List<Order> InterIslandOrders { get; set; } = new();

        // Stats for the Dashboard
        public int TotalInQueue { get; set; }
        public string CurrentHub { get; set; } = "Davao Hub";

        // All available hubs (e.g. Davao Hub, Manila Hub, Cebu Hub, etc.)
        public List<string> Hubs { get; set; } = new();
    }
    

    public class StatusDistributionItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DailyTrendItem
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RegionRevenueItem
    {
        public string Region { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
