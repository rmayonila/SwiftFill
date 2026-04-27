using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Island { get; set; } = string.Empty;

        public int Capacity { get; set; } = 100;
        
        public string Status { get; set; } = "Operational";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;
    }
}
