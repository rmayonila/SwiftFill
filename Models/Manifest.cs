using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    public class Manifest
    {
        [Key]
        public string ManifestId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        [Required]
        public string Region { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, InTransit, Received

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
