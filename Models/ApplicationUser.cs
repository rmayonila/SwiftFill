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
        // NOTE: Marked NotMapped for now to avoid runtime SQL errors until an EF migration
        // is applied to add this column to the database. Remove this attribute after
        // running a migration that adds the column.
        [NotMapped]
        public string? Hub { get; set; }
    }
}
