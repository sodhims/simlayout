using System;

namespace LayoutEditor.Data.DTOs
{
    /// <summary>
    /// Data Transfer Object for layouts stored in the database
    /// Represents a row in the Layouts table
    /// </summary>
    public class LayoutDto
    {
        /// <summary>
        /// Unique identifier for the layout
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Display name of the layout
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Width of the layout
        /// </summary>
        public double Width { get; set; } = 100.0;

        /// <summary>
        /// Height of the layout
        /// </summary>
        public double Height { get; set; } = 100.0;

        /// <summary>
        /// Unit of measurement (e.g., "meters", "feet")
        /// </summary>
        public string Unit { get; set; } = "meters";

        /// <summary>
        /// Timestamp when the layout was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the layout was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Version number for tracking changes
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Creates a new LayoutDto with default values
        /// </summary>
        public LayoutDto()
        {
        }

        /// <summary>
        /// Creates a new LayoutDto with specified values
        /// </summary>
        public LayoutDto(string id, string name, double width, double height, string unit = "meters")
        {
            Id = id;
            Name = name;
            Width = width;
            Height = height;
            Unit = unit;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            Version = 1;
        }
    }
}
