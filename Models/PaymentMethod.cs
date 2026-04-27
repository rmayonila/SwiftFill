using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsOnline { get; set; } = false; // Whether it requires online verification

        public string? IconClass { get; set; } = "bi-cash";
    }
}
