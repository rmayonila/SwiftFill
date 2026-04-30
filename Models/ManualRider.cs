using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SwiftFill.Models
{
    public class ManualRider
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        /// <summary>Hub this rider is assigned to (e.g. "Cebu Hub").</summary>
        [MaxLength(64), ValidateNever]
        public string Hub { get; set; } = string.Empty;

        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        /// <summary>
        /// The route/area this rider covers (e.g. "Mabolo", "Lahug", "Carbon Market").
        /// Used to filter the rider list when assigning to a parcel based on receiver address.
        /// </summary>
        [Required, MaxLength(128)]
        public string Route { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
