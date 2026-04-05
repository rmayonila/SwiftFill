using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;

namespace SwiftFill.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GenerateTrackingId()
        {
            return "SF" + DateTime.Now.ToString("yyMMdd") + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
        }

        public async Task<Manifest?> CreateManifestAsync(string region)
        {
            var packedOrders = await _context.Orders
                .Where(o => o.Status == "Packed" && o.DestinationRegion == region && o.ManifestId == null)
                .ToListAsync();

            if (!packedOrders.Any()) return null;

            var manifest = new Manifest
            {
                Region = region,
                Status = "InTransit",
                CreatedAt = DateTime.UtcNow
            };

            foreach (var order in packedOrders)
            {
                order.ManifestId = manifest.ManifestId;
                order.Status = "Transit";
            }

            _context.Manifests.Add(manifest);
            await _context.SaveChangesAsync();
            return manifest;
        }
    }
}
