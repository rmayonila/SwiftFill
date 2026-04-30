using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftFill.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Hub assignment for riders/staff (e.g. "Davao Hub", "Manila Hub", "Cebu Hub")
        public string? Hub { get; set; }

        /// <summary>
        /// Delivery route keyword assigned to this rider (e.g. "Mabolo", "Buhangin").
        /// Matches the Route field in ManualRiders — used to filter order assignments.
        /// </summary>
        public string? Route { get; set; }

        public bool IsSuspended { get; set; } = false;
        public int TotalFailedLogins { get; set; } = 0;
    }
}
