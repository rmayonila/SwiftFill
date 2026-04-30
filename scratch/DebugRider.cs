
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

public class DebugRider
{
    public static async Task CheckOrder(IServiceProvider services, string trackingId, string riderName)
    {
        var context = (ApplicationDbContext)services.GetService(typeof(ApplicationDbContext));
        var userManager = (UserManager<ApplicationUser>)services.GetService(typeof(UserManager<ApplicationUser>));

        var order = await context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
        var rider = (await userManager.GetUsersInRoleAsync("DeliveryRider"))
            .FirstOrDefault(u => u.FirstName + " " + u.LastName == riderName);

        Console.WriteLine($"Order: {trackingId}");
        if (order == null) {
            Console.WriteLine("Order not found!");
            return;
        }
        Console.WriteLine($"Status: {order.Status}");
        Console.WriteLine($"CurrentLocation: {order.CurrentLocation}");
        Console.WriteLine($"AssignedRiderId: {order.AssignedRiderId}");
        Console.WriteLine($"IsArchived: {order.IsArchived}");

        if (rider == null) {
            Console.WriteLine($"Rider {riderName} not found!");
            return;
        }
        Console.WriteLine($"Rider ID: {rider.Id}");
        Console.WriteLine($"Rider Hub: {rider.Hub}");
        Console.WriteLine($"Rider Route: {rider.Route}");

        bool match = order.AssignedRiderId == rider.Id;
        Console.WriteLine($"Match: {match}");
    }
}
