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
    }
}
