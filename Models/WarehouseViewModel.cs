using System.Collections.Generic;

namespace SwiftFill.Models
{
    public class WarehouseViewModel
    {
        // For Picking.cshtml
        public List<Order> PendingPickOrders { get; set; } = new();

        // For Packing.cshtml
        public List<Order> PickedOrders { get; set; } = new();

        // For Shipping.cshtml & Delivery.cshtml
        public List<Order> PackedOrders { get; set; } = new();

        // To populate the Rider Dropdown
        public List<ApplicationUser> AvailableRiders { get; set; } = new();

        // Stats for the Dashboard
        public int TotalInQueue { get; set; }
        public string CurrentHub { get; set; } = "Davao Hub";
    }
}