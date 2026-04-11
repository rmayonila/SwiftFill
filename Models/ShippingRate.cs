using System;
using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    /// <summary>
    /// Admin-managed shipping fee structure per destination region.
    /// Used by both customer booking and manual admin order creation.
    /// </summary>
    public class ShippingRate
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Destination region key, e.g. "NCR", "Luzon", "Visayas", "Mindanao".</summary>
        [Required, MaxLength(64)]
        public string DestinationRegion { get; set; } = string.Empty;

        /// <summary>Flat base fee applied to every shipment for this region.</summary>
        [Required]
        public decimal BaseRate { get; set; } = 100m;

        /// <summary>Fee per kilogram of declared weight.</summary>
        [Required]
        public decimal PricePerKg { get; set; } = 45m;

        /// <summary>Additional zone surcharge for this region.</summary>
        [Required]
        public decimal ZoneSurcharge { get; set; } = 0m;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Convenience: calculate total fee for a given weight.</summary>
        public decimal Calculate(double weightKg) =>
            BaseRate + (decimal)weightKg * PricePerKg + ZoneSurcharge;
    }
}
