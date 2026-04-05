using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    public class Order
    {
        [Key]
        public string TrackingId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        [Required]
        public string SenderName { get; set; } = string.Empty;
        [Required]
        public string SenderAddress { get; set; } = string.Empty;
        [Required]
        public string SenderPhone { get; set; } = string.Empty;

        [Required]
        public string ReceiverName { get; set; } = string.Empty;
        [Required]
        public string ReceiverAddress { get; set; } = string.Empty;
        [Required]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required]
        public double Weight { get; set; }
        
        [Required]
        public string DestinationRegion { get; set; } = string.Empty; 
        
        public string? ItemCategory { get; set; }
        public double DeclaredValue { get; set; }

        public string Status { get; set; } = "Pending"; 
        
        [Required]
        public string CurrentLocation { get; set; } = "Davao Hub";
        
        public bool IsArchived { get; set; } = false;

        // --- NEW PROPERTIES FOR RIDER LOGIC ---
        public string? Notes { get; set; } 
        
        [Range(0, 3)]
        public int DeliveryAttempts { get; set; } = 0;
        
        public string? ProofImagePath { get; set; } 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- RELATIONSHIPS ---
        public string? ManifestId { get; set; }
        public Manifest? Manifest { get; set; }

        public string? AssignedRiderId { get; set; }
        public ApplicationUser? AssignedRider { get; set; }

        public Payment? Payment { get; set; }
        public ReturnRequest? ReturnRequest { get; set; }
    }
}