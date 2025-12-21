using System;
using System.Collections.Generic;
using System.Windows;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Types of conflicts that can occur between layers
    /// </summary>
    public enum ConflictType
    {
        /// <summary>
        /// Pedestrian walkway intersects crane drop zone
        /// </summary>
        PedestrianUnderDropZone,

        /// <summary>
        /// Forklift/AGV crossing exists without signal control
        /// </summary>
        CrossingWithoutSignal,

        /// <summary>
        /// Walkway crosses conveyor path
        /// </summary>
        WalkwayBlocksTransport,

        /// <summary>
        /// Crane coverage areas overlap without handoff point defined
        /// </summary>
        CraneOverlapNoHandoff,

        /// <summary>
        /// Two AGV paths cross without traffic zone
        /// </summary>
        AGVPathConflict,

        /// <summary>
        /// Forklift aisle in AGV-only operational area
        /// </summary>
        ForkliftInAGVZone,

        /// <summary>
        /// Equipment blocks emergency exit path
        /// </summary>
        EmergencyExitBlocked
    }

    /// <summary>
    /// Severity level of a conflict
    /// </summary>
    public enum ConflictSeverity
    {
        /// <summary>
        /// Warning - should be reviewed but not critical
        /// </summary>
        Warning,

        /// <summary>
        /// Error - critical issue that must be resolved
        /// </summary>
        Error
    }

    /// <summary>
    /// Represents a detected conflict between layout elements
    /// </summary>
    public class Conflict
    {
        /// <summary>
        /// Unique identifier for this conflict instance
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of conflict detected
        /// </summary>
        public ConflictType Type { get; set; }

        /// <summary>
        /// Human-readable description of the conflict
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// IDs of all elements involved in this conflict
        /// </summary>
        public List<string> InvolvedElementIds { get; set; } = new List<string>();

        /// <summary>
        /// Approximate location of the conflict for visualization
        /// </summary>
        public Point Location { get; set; }

        /// <summary>
        /// Severity level of this conflict
        /// </summary>
        public ConflictSeverity Severity { get; set; }

        /// <summary>
        /// Suggested fix or resolution for this conflict
        /// </summary>
        public string SuggestedFix { get; set; } = string.Empty;

        /// <summary>
        /// Whether this conflict has been acknowledged by the user
        /// </summary>
        public bool IsAcknowledged { get; set; } = false;

        /// <summary>
        /// Timestamp when conflict was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for the conflict (optional)
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a string representation of the conflict
        /// </summary>
        public override string ToString()
        {
            return $"[{Severity}] {Type}: {Description}";
        }
    }
}
