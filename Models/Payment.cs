using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SwiftFill.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public string TrackingId { get; set; } = string.Empty;
        
        [JsonIgnore]
        public Order? Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string Method { get; set; } = "COD"; // COD, Prepaid

        public bool IsPaid { get; set; } = false;

        public string? CollectedByUserId { get; set; }
        public ApplicationUser? CollectedByUser { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
