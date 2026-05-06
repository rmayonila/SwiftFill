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
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
        public string SenderPhone { get; set; } = string.Empty;

        [Required]
        public string ReceiverName { get; set; } = string.Empty;
        [Required]
        public string ReceiverAddress { get; set; } = string.Empty;
        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required]
        public double Weight { get; set; }
        
        [Required]
        public string DestinationRegion { get; set; } = string.Empty; 

        public int? ShippingRateId { get; set; }
        public ShippingRate? ShippingRate { get; set; }
        
        public string? ItemCategory { get; set; }
        public double DeclaredValue { get; set; }

        public string Status { get; set; } = "Pending"; 
        
        // "DoorToDoor" or "BranchPickup" — controls rider assignment logic
        public string DeliveryType { get; set; } = "DoorToDoor";

        /// <summary>
        /// For BranchPickup orders: the hub branch where the receiver will collect the parcel.
        /// Must match a valid hub name from HubRegistry.
        /// </summary>
        public string? PickupBranchAddress { get; set; }

        /// <summary>
        /// Final calculated shipping fee (from admin-managed ShippingRates table).
        /// 0 means fee has not yet been calculated or verified.
        /// </summary>
        public decimal ShippingFee { get; set; } = 0m;

        // Records which hub originally created/received the parcel
        public string? OriginHub { get; set; } = "Davao Hub";
        
        [Required]
        public string CurrentLocation { get; set; } = "Davao Hub";

        public int? CurrentWarehouseId { get; set; }
        public Warehouse? CurrentWarehouse { get; set; }
        
        public bool IsArchived { get; set; } = false;

        // --- NEW PROPERTIES FOR RIDER LOGIC ---
        public string? Notes { get; set; } 
        
        [Range(0, 10)]
        public int DeliveryAttempts { get; set; } = 0;
        
        public string? ProofImagePath { get; set; } 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? ManifestId { get; set; }
        public Manifest? Manifest { get; set; }

        public string? CustomerId { get; set; }
        public bool IsFragile { get; set; } = false;

        public string? AssignedRiderId { get; set; }
        public ApplicationUser? AssignedRider { get; set; }

        public int? ManualRiderId { get; set; }
        public ManualRider? ManualRider { get; set; }

        public Payment? Payment { get; set; }
        public ReturnRequest? ReturnRequest { get; set; }

        // --- NEW PACKING & SORTING LOGIC ---
        public bool AvailPacking { get; set; } = false;
        public decimal PackingFee { get; set; } = 0m;
        public string? PackedByStaffId { get; set; }
        public string? PackingLocation { get; set; } // "Store" or "Warehouse"
        public string? SortingStatus { get; set; }   // "Sorted in Warehouse" or "Pending in Store"
        public string? TruckLabel { get; set; }     // e.g., "Truck A"
    }
}