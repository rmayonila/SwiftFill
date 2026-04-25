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

            // 1. Acceptance Stage (Always present if order exists)
            timeline.Add(new TrackingEventViewModel
            {
                Title = "Shipment Accepted",
                Description = "Shipment has been accepted in SwiftFill",
                Date = order.CreatedAt,
                IsCompleted = true,
                IsActive = order.Status == "Pending",
                Icon = "bi-check-circle-fill"
            });

            // 2. Transit / Sorting Stage
            // We skip "Picked" and "Packed" for customers.
            
            bool isTransit = order.Status.Contains("Transit");
            bool isArrived = order.Status.Contains("Arrived");
            bool isOutForDelivery = order.Status == "Out for Delivery";
            bool isDelivered = order.Status == "Delivered";
            bool isReturned = order.Status == "Returned";

            // If it's passed initial acceptance but not yet at final delivery
            if (!order.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                string originIsland = HubRegistry.GetIsland(order.OriginHub ?? "Davao Hub") ?? "Mindanao";
                string destHub = HubRegistry.ResolveDestinationHub(order.DestinationRegion);
                string destIsland = HubRegistry.GetIsland(destHub) ?? "Luzon";

                if (isArrived)
                {
                    if (order.Status.Contains(destHub))
                    {
                        // CASE: Arrived at final destination hub
                        timeline.Add(new TrackingEventViewModel
                        {
                            Title = $"{destHub.Replace(" Hub", "").ToUpper()} DISTRIBUTION TEAM",
                            Description = $"Shipment received at {destHub.Replace(" Hub", "")} distribution team. Expect a delivery within the day.",
                            Date = order.UpdatedAt,
                            IsCompleted = true,
                            IsActive = !isOutForDelivery && !isDelivered && !isReturned,
                            Icon = "bi-geo-alt-fill"
                        });
                    }
                    else
                    {
                        // CASE: Arrived at intermediate hub
                        string currentHubName = order.Status.Replace("Arrived at ", "");
                        timeline.Add(new TrackingEventViewModel
                        {
                            Title = "Intermediate Sorting",
                            Description = $"Parcel received and currently being sorted at {currentHubName} ({HubRegistry.GetIsland(currentHubName)}).",
                            Date = order.UpdatedAt,
                            IsCompleted = true,
                            IsActive = true,
                            Icon = "bi-geo-fill"
                        });
                    }
                }
                else if (isTransit)
                {
                    if (order.Status.Contains(destHub))
                    {
                        // CASE: In transit to final destination hub
                        timeline.Add(new TrackingEventViewModel
                        {
                            Title = $"{destHub.Replace(" Hub", "").ToUpper()} DISTRIBUTION TEAM",
                            Description = "Next update for shipment will come within 3 to 6 days.",
                            Date = order.UpdatedAt,
                            IsCompleted = false,
                            IsActive = true,
                            Icon = "bi-truck"
                        });
                    }
                    else
                    {
                        // CASE: In transit to an intermediate hub
                        string targetHubName = order.Status.Replace("In Transit to ", "");
                        timeline.Add(new TrackingEventViewModel
                        {
                            Title = "Transport in Progress",
                            Description = $"Shipment is being transferred to {targetHubName} for further sorting.",
                            Date = order.UpdatedAt,
                            IsCompleted = true,
                            IsActive = true,
                            Icon = "bi-truck"
                        });
                    }
                }
                else if (order.Status == "Sorted for Transfer")
                {
                     timeline.Add(new TrackingEventViewModel
                    {
                        Title = "Sorted & Ready",
                        Description = "Shipment has been sorted and is awaiting the next transport schedule.",
                        Date = order.UpdatedAt,
                        IsCompleted = true,
                        IsActive = true,
                        Icon = "bi-box-seam"
                    });
                }
            }

            // 3. Final Mile
            if (isOutForDelivery || isDelivered || isReturned)
            {
                if (isOutForDelivery)
                {
                    timeline.Add(new TrackingEventViewModel
                    {
                        Title = "Out for Delivery",
                        Description = "Handed over to local delivery fleet. Rider assigned.",
                        Date = order.UpdatedAt,
                        IsCompleted = true,
                        IsActive = true,
                        Icon = "bi-scooter"
                    });
                }
                
                if (isDelivered)
                {
                    timeline.Add(new TrackingEventViewModel
                    {
                        Title = "Successfully Delivered",
                        Description = "The package has reached its final destination.",
                        Date = order.UpdatedAt,
                        IsCompleted = true,
                        IsActive = true,
                        Icon = "bi-box-seam-fill"
                    });
                }
                else if (isReturned)
                {
                    timeline.Add(new TrackingEventViewModel
                    {
                        Title = "Returned to Sender",
                        Description = $"Package delivery failed. {order.Notes}",
                        Date = order.UpdatedAt,
                        IsCompleted = true,
                        IsActive = true,
                        Icon = "bi-arrow-return-left"
                    });
                }
            }
            else if (!isDelivered && !isReturned)
            {
                 timeline.Add(new TrackingEventViewModel
                {
                    Title = "Final Delivery",
                    Description = "Awaiting final recipient confirmation.",
                    IsCompleted = false,
                    IsActive = false,
                    Icon = "bi-house-heart"
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
