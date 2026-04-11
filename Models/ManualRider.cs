using System;
using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    /// <summary>
    /// A manually-entered rider record for non-Davao hub transfer hubs.
    /// These riders are NOT system users — they are added by warehouse staff
    /// for the purpose of local delivery assignment within a hub's service area.
    /// </summary>
    public class ManualRider
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        /// <summary>Hub this rider is assigned to (e.g. "Cebu Hub").</summary>
        [Required, MaxLength(64)]
        public string Hub { get; set; } = string.Empty;

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
