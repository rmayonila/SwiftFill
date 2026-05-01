using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SwiftFill.Models
{
    public class ReturnRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public string TrackingId { get; set; } = string.Empty;

        [JsonIgnore]
        public Order? Order { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Declined

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? ReviewedByUserId { get; set; }
        public ApplicationUser? ReviewedByUser { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}
