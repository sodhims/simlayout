using System;

namespace LayoutEditor.Data.DTOs
{
    /// <summary>
    /// Lightweight DTO for listing layouts without loading full element data
    /// </summary>
    public class LayoutMetadataDto
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
        public double Width { get; set; }

        /// <summary>
        /// Height of the layout
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Unit of measurement
        /// </summary>
        public string Unit { get; set; } = "meters";

        /// <summary>
        /// When the layout was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// When the layout was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Number of elements in the layout
        /// </summary>
        public int ElementCount { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public int Version { get; set; }
    }
}
