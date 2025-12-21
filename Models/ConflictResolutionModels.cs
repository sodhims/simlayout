using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Represents a persisted conflict resolution record
    /// Stores acknowledgment and resolution information for conflicts
    /// </summary>
    public class ConflictResolutionData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _conflictType = string.Empty;
        private string _description = string.Empty;
        private DateTime _acknowledgedAt = DateTime.UtcNow;
        private string _acknowledgedBy = "User";
        private string _resolutionNotes = string.Empty;
        private bool _isResolved = false;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string ConflictType
        {
            get => _conflictType;
            set => SetProperty(ref _conflictType, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ObservableCollection<string> InvolvedElementIds { get; set; } = new ObservableCollection<string>();

        public DateTime AcknowledgedAt
        {
            get => _acknowledgedAt;
            set => SetProperty(ref _acknowledgedAt, value);
        }

        public string AcknowledgedBy
        {
            get => _acknowledgedBy;
            set => SetProperty(ref _acknowledgedBy, value);
        }

        public string ResolutionNotes
        {
            get => _resolutionNotes;
            set => SetProperty(ref _resolutionNotes, value);
        }

        public bool IsResolved
        {
            get => _isResolved;
            set => SetProperty(ref _isResolved, value);
        }

        /// <summary>
        /// Creates a ConflictResolutionData from a Conflict
        /// </summary>
        public static ConflictResolutionData FromConflict(Conflict conflict)
        {
            var resolution = new ConflictResolutionData
            {
                Id = conflict.Id,
                ConflictType = conflict.Type.ToString(),
                Description = conflict.Description,
                AcknowledgedAt = DateTime.UtcNow,
                AcknowledgedBy = "User",
                ResolutionNotes = conflict.SuggestedFix,
                IsResolved = conflict.IsAcknowledged
            };

            foreach (var elementId in conflict.InvolvedElementIds)
            {
                resolution.InvolvedElementIds.Add(elementId);
            }

            return resolution;
        }
    }

    /// <summary>
    /// Represents a connection between two elements (for crossing zones, handoffs, etc.)
    /// Used to mark validated interactions between transport layers
    /// </summary>
    public class LayerConnectionData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = string.Empty;
        private string _fromElementId = string.Empty;
        private string _toElementId = string.Empty;
        private string _connectionType = string.Empty;
        private double _x;
        private double _y;
        private string _notes = string.Empty;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FromElementId
        {
            get => _fromElementId;
            set => SetProperty(ref _fromElementId, value);
        }

        public string ToElementId
        {
            get => _toElementId;
            set => SetProperty(ref _toElementId, value);
        }

        public string ConnectionType
        {
            get => _connectionType;
            set => SetProperty(ref _connectionType, value);
        }

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // Conflict management is layer 8 (Pedestrian is 7)
        public virtual int ArchitectureLayer => 8;
    }

    /// <summary>
    /// Connection types for layer interactions
    /// </summary>
    public static class LayerConnectionTypes
    {
        public const string Crossing = "Crossing";
        public const string Handoff = "Handoff";
        public const string Overlap = "Overlap";
        public const string Adjacent = "Adjacent";
    }
}
