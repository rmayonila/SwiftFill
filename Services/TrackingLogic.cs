using SwiftFill.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SwiftFill.Services
{
    public class TrackingEventViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
        public string Icon { get; set; } = "bi-circle";
    }

    public static class TrackingLogic
    {
        public static List<TrackingEventViewModel> GetPublicTimeline(Order order)
        {
            var timeline = new List<TrackingEventViewModel>();
            var status = order.Status ?? "Pending";

            // 1. Acceptance Stage (Always present)
            timeline.Add(new TrackingEventViewModel
            {
                Title = "Shipment Accepted",
                Description = "Your package has been successfully received at our SwiftFill origin hub.",
                Date = order.CreatedAt,
                IsCompleted = true,
                IsActive = status == "Pending",
                Icon = "bi-check-circle-fill"
            });

            // 2. Logistics & Movement Flags
            bool isTransit = status.Contains("Transit");
            bool isArrived = status.Contains("Arrived");
            bool isOutForDelivery = status.Equals("Out for Delivery", StringComparison.OrdinalIgnoreCase);
            bool isDelivered = status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);
            bool isReturning = status.Contains("Return", StringComparison.OrdinalIgnoreCase);
            bool isReturned = status.Equals("Returned", StringComparison.OrdinalIgnoreCase);
            bool hasLeftOrigin = !status.Equals("Pending", StringComparison.OrdinalIgnoreCase) && !status.Equals("Picked", StringComparison.OrdinalIgnoreCase) && !status.Equals("Packed in Store", StringComparison.OrdinalIgnoreCase);

            // 2. In Transit Stage (Show if it has left origin or is further along)
            if (hasLeftOrigin || isTransit || isArrived || isOutForDelivery || isDelivered)
            {
                timeline.Add(new TrackingEventViewModel
                {
                    Title = "In Transit",
                    Description = "Shipment is currently moving between our logistics facilities.",
                    Date = isTransit ? order.UpdatedAt : (isArrived || isOutForDelivery || isDelivered ? order.CreatedAt.AddHours(12) : order.UpdatedAt),
                    IsCompleted = isArrived || isOutForDelivery || isDelivered,
                    IsActive = isTransit,
                    Icon = "bi-truck"
                });
            }

            // 3. Hub Arrival Stage (Show if arrived, out for delivery, or delivered)
            if (isArrived || isOutForDelivery || isDelivered)
            {
                string destHub = HubRegistry.ResolveDestinationHub(order.DestinationRegion);
                timeline.Add(new TrackingEventViewModel
                {
                    Title = "Arrived at Distribution Hub",
                    Description = $"Your parcel has arrived at the {destHub.Replace(" Hub", "")} facility for final mile sorting.",
                    Date = isArrived ? order.UpdatedAt : (isDelivered ? order.UpdatedAt.AddHours(-4) : order.UpdatedAt.AddHours(-2)),
                    IsCompleted = isOutForDelivery || isDelivered,
                    IsActive = isArrived,
                    Icon = "bi-geo-fill"
                });
            }

            // 4. Final Mile / Out for Delivery
            if (isOutForDelivery || isDelivered)
            {
                timeline.Add(new TrackingEventViewModel
                {
                    Title = "Out for Delivery",
                    Description = "Handed over to local delivery fleet. Rider is en route to your location.",
                    Date = isOutForDelivery ? order.UpdatedAt : order.UpdatedAt.AddHours(-2),
                    IsCompleted = isDelivered,
                    IsActive = isOutForDelivery,
                    Icon = "bi-scooter"
                });
            }

            // 5. Completion / Returns
            if (isDelivered)
            {
                timeline.Add(new TrackingEventViewModel
                {
                    Title = "Successfully Delivered",
                    Description = "The package has reached its final destination. Thank you for using SwiftFill!",
                    Date = order.UpdatedAt,
                    IsCompleted = true,
                    IsActive = true,
                    Icon = "bi-box-seam-fill"
                });
            }
            else if (isReturning || isReturned)
            {
                timeline.Add(new TrackingEventViewModel
                {
                    Title = "Returning to Sender",
                    Description = $"Delivery unsuccessful. Package is being routed back to the origin. Note: {order.Notes ?? "Failed delivery attempt."}",
                    Date = order.UpdatedAt,
                    IsCompleted = true,
                    IsActive = true,
                    Icon = "bi-arrow-return-left"
                });
            }
            else if (!isTransit && !isArrived && !isOutForDelivery && !isDelivered && !status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback for custom statuses
                timeline.Add(new TrackingEventViewModel
                {
                    Title = status,
                    Description = "Your shipment has reached a new milestone in our logistics network.",
                    Date = order.UpdatedAt,
                    IsCompleted = true,
                    IsActive = true,
                    Icon = "bi-info-circle-fill"
                });
            }

            return timeline;
        }

        private static string ResolveDestinationHub(string destinationRegion)
        {
            return HubRegistry.ResolveDestinationHub(destinationRegion);
        }
    }
}
