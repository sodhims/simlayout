using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    #region Transport Marker (Backbone Junction)

    /// <summary>
    /// Transport marker - permanent junction point on transport backbone
    /// Visual: Orange diamond
    /// </summary>
    public class TransportMarker : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Marker";
        private string _networkId = "";
        private double _x;
        private double _y;
        private string _markerType = MarkerTypes.Junction;
        private string _color = "#E67E22"; // Orange
        private double _size = 24;
        private bool _isSelected;
        private bool _isHighlighted;

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

        public string NetworkId
        {
            get => _networkId;
            set => SetProperty(ref _networkId, value);
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

        /// <summary>
        /// Marker type: junction, terminus, crossing
        /// </summary>
        public string MarkerType
        {
            get => _markerType;
            set => SetProperty(ref _markerType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public double Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        [JsonIgnore]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        /// <summary>
        /// Groups that have paths through this marker (for legend display)
        /// </summary>
        [JsonIgnore]
        public List<string> PassingGroupIds { get; set; } = new();
    }

    public static class MarkerTypes
    {
        public const string Junction = "junction";
        public const string Terminus = "terminus";
        public const string Crossing = "crossing";
    }

    #endregion

    #region Transport Group (Resource Collection)

    /// <summary>
    /// Transport group - collection of transport resources for a zone
    /// Visual: Dockable box with colored header
    /// </summary>
    public class TransportGroup : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "T01";
        private string _displayName = "Transport Group 1";
        private string _color = "#E67E22";
        private string _groupType = GroupTypes.Transport;
        private double _boxX;  // Position of dockable box
        private double _boxY;
        private bool _isExpanded = true;
        private bool _isSelected;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Short name with prefix: T01, C01, M01, etc.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Full display name
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>
        /// Group color (used in legends)
        /// </summary>
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Group type determines what can be dropped into it
        /// </summary>
        public string GroupType
        {
            get => _groupType;
            set => SetProperty(ref _groupType, value);
        }

        /// <summary>
        /// Box position X (for dockable panel)
        /// </summary>
        public double BoxX
        {
            get => _boxX;
            set => SetProperty(ref _boxX, value);
        }

        /// <summary>
        /// Box position Y (for dockable panel)
        /// </summary>
        public double BoxY
        {
            get => _boxY;
            set => SetProperty(ref _boxY, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Member element IDs (stations, vehicles, waypoints)
        /// </summary>
        public ObservableCollection<string> MemberIds { get; set; } = new();

        /// <summary>
        /// Marker IDs that this group's path passes through
        /// </summary>
        public ObservableCollection<string> PathMarkerIds { get; set; } = new();

        /// <summary>
        /// Check if an element type can be added to this group
        /// </summary>
        public bool CanAccept(string elementType)
        {
            return GroupType switch
            {
                GroupTypes.Transport => elementType is "transportStation" or "transporter" or "waypoint" or "agvStation" or "forklift",
                GroupTypes.Cell => false, // Cells have their own system
                GroupTypes.Storage => elementType is "storage" or "warehouse" or "rack",
                _ => false
            };
        }
    }

    public static class GroupTypes
    {
        public const string Transport = "transport";  // T prefix
        public const string Cell = "cell";            // C prefix
        public const string Storage = "storage";      // S prefix
        public const string Queue = "queue";          // Q prefix
    }

    #endregion

    #region Transport Link (Machine → Transport)

    /// <summary>
    /// Link from production element to transport backbone
    /// Visual: Thin dashed line
    /// </summary>
    public class TransportLink : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _fromNodeId = "";
        private string _fromTerminal = "output";
        private string _toMarkerId = "";      // If linked to marker
        private string _toSegmentId = "";     // If linked to segment (creates waypoint)
        private double _connectionPointX;     // Point on segment where link connects
        private double _connectionPointY;
        private string _linkType = LinkTypes.Pickup;
        private string _color = "#888888";
        private bool _isSelected;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Source node (machine, queue, cell)
        /// </summary>
        public string FromNodeId
        {
            get => _fromNodeId;
            set => SetProperty(ref _fromNodeId, value);
        }

        /// <summary>
        /// Terminal on source node: input, output
        /// </summary>
        public string FromTerminal
        {
            get => _fromTerminal;
            set => SetProperty(ref _fromTerminal, value);
        }

        /// <summary>
        /// Target marker (if linked directly to marker)
        /// </summary>
        public string ToMarkerId
        {
            get => _toMarkerId;
            set => SetProperty(ref _toMarkerId, value);
        }

        /// <summary>
        /// Target segment (if linked to segment, creates implicit waypoint)
        /// </summary>
        public string ToSegmentId
        {
            get => _toSegmentId;
            set => SetProperty(ref _toSegmentId, value);
        }

        /// <summary>
        /// Connection point on segment (if ToSegmentId is set)
        /// </summary>
        public double ConnectionPointX
        {
            get => _connectionPointX;
            set => SetProperty(ref _connectionPointX, value);
        }

        public double ConnectionPointY
        {
            get => _connectionPointY;
            set => SetProperty(ref _connectionPointY, value);
        }

        /// <summary>
        /// Link type: pickup, dropoff, both
        /// </summary>
        public string LinkType
        {
            get => _linkType;
            set => SetProperty(ref _linkType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public static class LinkTypes
    {
        public const string Pickup = "pickup";
        public const string Dropoff = "dropoff";
        public const string Both = "both";
    }

    #endregion

    #region Element Prefixes

    /// <summary>
    /// Standard prefixes for element naming
    /// </summary>
    public static class ElementPrefixes
    {
        public const string Transport = "T";
        public const string Cell = "C";
        public const string Machine = "M";
        public const string Storage = "S";
        public const string Queue = "Q";
        public const string Workstation = "W";
        public const string Inspection = "I";
        public const string Packaging = "P";
        public const string Resource = "R";
        public const string Dock = "D";
        public const string Assembly = "A";
        public const string Crane = "K";
        public const string Utility = "U";

        private static readonly Dictionary<string, int> _counters = new();

        /// <summary>
        /// Get next name for element type: T01, T02, etc.
        /// </summary>
        public static string GetNextName(string prefix)
        {
            if (!_counters.ContainsKey(prefix))
                _counters[prefix] = 0;
            
            _counters[prefix]++;
            return $"{prefix}{_counters[prefix]:D2}";
        }

        /// <summary>
        /// Reset counter for prefix
        /// </summary>
        public static void ResetCounter(string prefix)
        {
            _counters[prefix] = 0;
        }

        /// <summary>
        /// Set counter to specific value (for loading layouts)
        /// </summary>
        public static void SetCounter(string prefix, int value)
        {
            _counters[prefix] = value;
        }
    }

    #endregion

    #region Group Colors

    /// <summary>
    /// Preset colors for groups (for legend display)
    /// </summary>
    public static class GroupColors
    {
        public static readonly string[] Palette = new[]
        {
            "#E74C3C",  // Red
            "#3498DB",  // Blue
            "#2ECC71",  // Green
            "#9B59B6",  // Purple
            "#F39C12",  // Orange/Yellow
            "#1ABC9C",  // Teal
            "#E91E63",  // Pink
            "#00BCD4",  // Cyan
            "#FF5722",  // Deep Orange
            "#607D8B",  // Blue Gray
            "#8BC34A",  // Light Green
            "#673AB7",  // Deep Purple
        };

        private static int _index = 0;

        public static string GetNextColor()
        {
            var color = Palette[_index % Palette.Length];
            _index++;
            return color;
        }

        public static void Reset()
        {
            _index = 0;
        }
    }

    #endregion
}
