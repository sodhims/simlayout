using System;

namespace LayoutEditor.Data.DTOs
{
    /// <summary>
    /// Data Transfer Object for elements stored in the database
    /// Represents a row in the Elements table
    /// </summary>
    public class ElementDto
    {
        /// <summary>
        /// Unique identifier for the element
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// ID of the layout this element belongs to
        /// </summary>
        public string LayoutId { get; set; } = "";

        /// <summary>
        /// Type of element (e.g., "EOTCrane", "Conveyor", "AGVPath")
        /// Maps to DbElementType constants
        /// </summary>
        public string ElementType { get; set; } = "";

        /// <summary>
        /// Layer number (0-7) corresponding to LayerType enum
        /// </summary>
        public int Layer { get; set; }

        /// <summary>
        /// Display name of the element (optional)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// JSON serialization of the full element properties
        /// Contains all element-specific data
        /// </summary>
        public string PropertiesJson { get; set; } = "";

        /// <summary>
        /// Timestamp when the element was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the element was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new ElementDto with default values
        /// </summary>
        public ElementDto()
        {
        }

        /// <summary>
        /// Creates a new ElementDto with specified values
        /// </summary>
        public ElementDto(string id, string layoutId, string elementType, int layer, string propertiesJson, string? name = null)
        {
            Id = id;
            LayoutId = layoutId;
            ElementType = elementType;
            Layer = layer;
            PropertiesJson = propertiesJson;
            Name = name;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}
