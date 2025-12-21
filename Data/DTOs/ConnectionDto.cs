using System;

namespace LayoutEditor.Data.DTOs
{
    /// <summary>
    /// Data Transfer Object for connections stored in the database
    /// Represents a row in the Connections table
    /// </summary>
    public class ConnectionDto
    {
        /// <summary>
        /// Unique identifier for the connection
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// ID of the layout this connection belongs to
        /// </summary>
        public string LayoutId { get; set; } = "";

        /// <summary>
        /// ID of the source element
        /// </summary>
        public string SourceElementId { get; set; } = "";

        /// <summary>
        /// ID of the target element
        /// </summary>
        public string TargetElementId { get; set; } = "";

        /// <summary>
        /// Type of connection (e.g., "CraneHandoff", "CraneDropZone")
        /// Maps to DbConnectionType constants
        /// </summary>
        public string ConnectionType { get; set; } = "";

        /// <summary>
        /// JSON serialization of additional connection properties (optional)
        /// </summary>
        public string? PropertiesJson { get; set; }

        /// <summary>
        /// Timestamp when the connection was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new ConnectionDto with default values
        /// </summary>
        public ConnectionDto()
        {
        }

        /// <summary>
        /// Creates a new ConnectionDto with specified values
        /// </summary>
        public ConnectionDto(string id, string layoutId, string sourceElementId, string targetElementId,
                            string connectionType, string? propertiesJson = null)
        {
            Id = id;
            LayoutId = layoutId;
            SourceElementId = sourceElementId;
            TargetElementId = targetElementId;
            ConnectionType = connectionType;
            PropertiesJson = propertiesJson;
            CreatedDate = DateTime.UtcNow;
        }
    }
}
