using System;
using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    /// <summary>
    /// Stores the unique verification code Super Admin assigns to each hub branch.
    /// Only ONE active code per hub at a time. All staff at that physical hub share this code.
    /// </summary>
    public class HubAccessCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string HubName { get; set; } = string.Empty;

        /// <summary>6-character alphanumeric code (e.g. "DVO001").</summary>
        [Required, MaxLength(16)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }
    }
}
