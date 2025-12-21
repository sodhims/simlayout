using System;

namespace LayoutEditor.Data.DTOs
{
    /// <summary>
    /// Data Transfer Object for zones stored in the database
    /// Represents a row in the Zones table
    /// </summary>
    public class ZoneDto
    {
        /// <summary>
        /// Unique identifier for the zone
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// ID of the layout this zone belongs to
        /// </summary>
        public string LayoutId { get; set; } = "";

        /// <summary>
        /// Display name of the zone
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Type of zone (e.g., "Functional", "Safety", "Custom")
        /// Maps to DbZoneType constants
        /// </summary>
        public string ZoneType { get; set; } = "";

        /// <summary>
        /// JSON serialization of the boundary points (optional)
        /// List of points defining the zone boundary
        /// </summary>
        public string? BoundaryJson { get; set; }

        /// <summary>
        /// JSON serialization of additional zone properties (optional)
        /// </summary>
        public string? PropertiesJson { get; set; }

        /// <summary>
        /// Timestamp when the zone was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the zone was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new ZoneDto with default values
        /// </summary>
        public ZoneDto()
        {
        }

        /// <summary>
        /// Creates a new ZoneDto with specified values
        /// </summary>
        public ZoneDto(string id, string layoutId, string name, string zoneType,
                      string? boundaryJson = null, string? propertiesJson = null)
        {
            Id = id;
            LayoutId = layoutId;
            Name = name;
            ZoneType = zoneType;
            BoundaryJson = boundaryJson;
            PropertiesJson = propertiesJson;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}
