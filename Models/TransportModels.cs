using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LayoutEditor.Models
{
    #region Station Types

    /// <summary>
    /// Types of transport stations
    /// </summary>
    public static class StationTypes
    {
        public const string Pickup = "pickup";
        public const string Dropoff = "dropoff";
        public const string Home = "home";
        public const string Buffer = "buffer";
        public const string Crossing = "crossing";
        public const string Waypoint = "waypoint";
        public const string Charging = "charging";
        public const string Maintenance = "maintenance";
    }

    /// <summary>
    /// Types of transporters
    /// </summary>
    public static class TransporterTypes
    {
        public const string AGV = "agv";
        public const string AMR = "amr";
        public const string Forklift = "forklift";
        public const string Tugger = "tugger";
        public const string Conveyor = "conveyor";
    }

    #endregion

    #region Transport Network (Container)

    /// <summary>
    /// A named transport network containing stations, waypoints, tracks, and transporters.
    /// Multiple networks can coexist (e.g., "MainFloor", "Warehouse", "OutdoorYard").
    /// </summary>
    public class TransportNetworkData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Network";
        private string _description = "";
        private bool _isVisible = true;
        private bool _isLocked = false;
        private TransportNetworkVisual _visual = new();
        private ObservableCollection<TransportStationData> _stations = new();
        private ObservableCollection<WaypointData> _waypoints = new();
        private ObservableCollection<TrackSegmentData> _segments = new();
        private ObservableCollection<TransporterData> _transporters = new();

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

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => SetProperty(ref _isLocked, value);
        }

        public TransportNetworkVisual Visual
        {
            get => _visual;
            set => SetProperty(ref _visual, value);
        }

        /// <summary>
        /// Stations in this network
        /// </summary>
        public ObservableCollection<TransportStationData> Stations
        {
            get => _stations;
            set => SetProperty(ref _stations, value);
        }

        /// <summary>
        /// Waypoints in this network
        /// </summary>
        public ObservableCollection<WaypointData> Waypoints
        {
            get => _waypoints;
            set => SetProperty(ref _waypoints, value);
        }

        /// <summary>
        /// Track segments in this network (directly on network, not nested in tracks)
        /// </summary>
        public ObservableCollection<TrackSegmentData> Segments
        {
            get => _segments;
            set => SetProperty(ref _segments, value);
        }

        /// <summary>
        /// Transporters assigned to this network
        /// </summary>
        public ObservableCollection<TransporterData> Transporters
        {
            get => _transporters;
            set => SetProperty(ref _transporters, value);
        }

        #region Network Analysis Methods

        /// <summary>
        /// Get all point IDs (stations + waypoints) in the network
        /// </summary>
        public IEnumerable<string> GetAllPointIds()
        {
            foreach (var s in Stations)
                yield return s.Id;
            foreach (var w in Waypoints)
                yield return w.Id;
        }

        /// <summary>
        /// Get segments connected to a specific point
        /// </summary>
        public IEnumerable<TrackSegmentData> GetConnectedSegments(string pointId)
        {
            return Segments.Where(s => s.From == pointId || s.To == pointId);
        }

        /// <summary>
        /// Check if a point is connected to the network
        /// </summary>
        public bool IsPointConnected(string pointId)
        {
            return Segments.Any(s => s.From == pointId || s.To == pointId);
        }

        /// <summary>
        /// Get orphaned points (not connected to any segment)
        /// </summary>
        public IEnumerable<string> GetOrphanedPoints()
        {
            var connectedIds = new HashSet<string>();
            foreach (var seg in Segments)
            {
                connectedIds.Add(seg.From);
                connectedIds.Add(seg.To);
            }

            foreach (var id in GetAllPointIds())
            {
                if (!connectedIds.Contains(id))
                    yield return id;
            }
        }

        /// <summary>
        /// Check if two points are directly connected
        /// </summary>
        public bool ArePointsConnected(string pointA, string pointB)
        {
            return Segments.Any(s =>
                (s.From == pointA && s.To == pointB) ||
                (s.From == pointB && s.To == pointA && s.Bidirectional));
        }

        /// <summary>
        /// Get segment between two points (if exists)
        /// </summary>
        public TrackSegmentData? GetSegmentBetween(string pointA, string pointB)
        {
            return Segments.FirstOrDefault(s =>
                (s.From == pointA && s.To == pointB) ||
                (s.From == pointB && s.To == pointA));
        }

        /// <summary>
        /// Validate network connectivity and return issues
        /// </summary>
        public List<NetworkValidationIssue> ValidateNetwork()
        {
            var issues = new List<NetworkValidationIssue>();

            // Check for orphaned points
            foreach (var orphanId in GetOrphanedPoints())
            {
                var station = Stations.FirstOrDefault(s => s.Id == orphanId);
                var waypoint = Waypoints.FirstOrDefault(w => w.Id == orphanId);
                var name = station?.Name ?? waypoint?.Name ?? orphanId;
                
                issues.Add(new NetworkValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message = $"'{name}' is not connected to any track segment",
                    ElementId = orphanId,
                    IssueType = ValidationIssueType.OrphanedPoint
                });
            }

            // Check for segments referencing non-existent points
            var allPointIds = new HashSet<string>(GetAllPointIds());
            foreach (var seg in Segments)
            {
                if (!allPointIds.Contains(seg.From))
                {
                    issues.Add(new NetworkValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Message = $"Segment references unknown start point: {seg.From}",
                        ElementId = seg.Id,
                        IssueType = ValidationIssueType.InvalidReference
                    });
                }
                if (!allPointIds.Contains(seg.To))
                {
                    issues.Add(new NetworkValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Message = $"Segment references unknown end point: {seg.To}",
                        ElementId = seg.Id,
                        IssueType = ValidationIssueType.InvalidReference
                    });
                }
            }

            // Check for home stations
            if (!Stations.Any(s => s.Simulation.StationType == StationTypes.Home))
            {
                issues.Add(new NetworkValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Network has no Home station defined",
                    IssueType = ValidationIssueType.MissingHomeStation
                });
            }

            // Check for transporters without valid home
            foreach (var t in Transporters)
            {
                if (string.IsNullOrEmpty(t.HomeStationId) || 
                    !Stations.Any(s => s.Id == t.HomeStationId))
                {
                    issues.Add(new NetworkValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"Transporter '{t.Name}' has no valid home station",
                        ElementId = t.Id,
                        IssueType = ValidationIssueType.InvalidHomeStation
                    });
                }
            }

            // Check connectivity (all points reachable from first point)
            var allPoints = GetAllPointIds().ToList();
            if (allPoints.Count > 1 && Segments.Count > 0)
            {
                var visited = new HashSet<string>();
                var queue = new Queue<string>();
                queue.Enqueue(allPoints.First());

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (visited.Contains(current)) continue;
                    visited.Add(current);

                    foreach (var seg in GetConnectedSegments(current))
                    {
                        var next = seg.From == current ? seg.To : seg.From;
                        if (!visited.Contains(next))
                        {
                            // For non-bidirectional, only traverse if going correct direction
                            if (seg.Bidirectional || seg.From == current)
                            {
                                queue.Enqueue(next);
                            }
                        }
                    }
                }

                var unreachable = allPoints.Where(p => !visited.Contains(p) && IsPointConnected(p)).ToList();
                if (unreachable.Any())
                {
                    issues.Add(new NetworkValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"{unreachable.Count} point(s) are not reachable from the main network",
                        IssueType = ValidationIssueType.DisconnectedSubgraph
                    });
                }
            }

            return issues;
        }

        #endregion
    }

    public class TransportNetworkVisual : NotifyBase
    {
        private string _color = "#E67E22";  // Orange for transport networks
        private double _trackWidth = 30;
        private double _opacity = 1.0;
        private bool _showDirections = true;
        private bool _showLabels = true;
        private string _lineStyle = "solid";

        public string Color { get => _color; set => SetProperty(ref _color, value); }
        public double TrackWidth { get => _trackWidth; set => SetProperty(ref _trackWidth, value); }
        public double Opacity { get => _opacity; set => SetProperty(ref _opacity, value); }
        public bool ShowDirections { get => _showDirections; set => SetProperty(ref _showDirections, value); }
        public bool ShowLabels { get => _showLabels; set => SetProperty(ref _showLabels, value); }
        public string LineStyle { get => _lineStyle; set => SetProperty(ref _lineStyle, value); }
    }

    #endregion

    #region Validation Types

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum ValidationIssueType
    {
        OrphanedPoint,
        InvalidReference,
        MissingHomeStation,
        InvalidHomeStation,
        DisconnectedSubgraph,
        DuplicateSegment,
        SelfReference
    }

    public class NetworkValidationIssue
    {
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = "";
        public string? ElementId { get; set; }
        public ValidationIssueType IssueType { get; set; }
    }

    #endregion

    #region Transport Station

    /// <summary>
    /// A transport station where AGVs/transporters can pick up, drop off, or wait
    /// </summary>
    public class TransportStationData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _type = "transportStation";
        private string _name = "Station";
        private string _label = "";
        private string _networkId = "";
        private TransportStationVisual _visual = new();
        private TransportStationSimulation _simulation = new();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        /// <summary>
        /// ID of the network this station belongs to
        /// </summary>
        public string NetworkId
        {
            get => _networkId;
            set => SetProperty(ref _networkId, value);
        }

        private string _groupName = "";
        
        /// <summary>
        /// Group name for auto-connecting stations into loops.
        /// Stations with the same group name can be auto-linked.
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSelected { get; set; }

        public TransportStationVisual Visual
        {
            get => _visual;
            set => SetProperty(ref _visual, value);
        }

        public TransportStationSimulation Simulation
        {
            get => _simulation;
            set => SetProperty(ref _simulation, value);
        }

        /// <summary>
        /// Get center position of station
        /// </summary>
        public (double X, double Y) GetCenter()
        {
            return (Visual.X + Visual.Width / 2, Visual.Y + Visual.Height / 2);
        }
    }

    public class TransportStationVisual : NotifyBase
    {
        private double _x, _y;
        private double _width = 50;
        private double _height = 50;
        private double _rotation = 0;
        private string _color = "#9B59B6";
        private string _icon = "default";
        private bool _showLabel = true;
        private string _terminalLayout = "left-right";  // left-right, right-left, top-bottom, bottom-top, top, bottom, left, right, center
        private bool _showTerminals = true;

        public double X { get => _x; set => SetProperty(ref _x, value); }
        public double Y { get => _y; set => SetProperty(ref _y, value); }
        public double Width { get => _width; set => SetProperty(ref _width, value); }
        public double Height { get => _height; set => SetProperty(ref _height, value); }
        public double Rotation { get => _rotation; set => SetProperty(ref _rotation, value); }
        public string Color { get => _color; set => SetProperty(ref _color, value); }
        public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
        public bool ShowLabel { get => _showLabel; set => SetProperty(ref _showLabel, value); }
        
        /// <summary>
        /// Terminal layout: left-right, right-left, top-bottom, bottom-top, top, bottom, left, right, center
        /// </summary>
        public string TerminalLayout { get => _terminalLayout; set => SetProperty(ref _terminalLayout, value); }
        
        /// <summary>
        /// Whether to show terminal indicators
        /// </summary>
        public bool ShowTerminals { get => _showTerminals; set => SetProperty(ref _showTerminals, value); }
    }

    public class TransportStationSimulation : NotifyBase
    {
        private string _stationType = StationTypes.Pickup;
        private int _queueCapacity = 5;
        private double _dwellTime = 10;
        private bool _isBlocking = false;
        private double _loadTime = 5;
        private double _unloadTime = 5;
        private int _priority = 0;

        /// <summary>
        /// Type: pickup, dropoff, home, buffer, crossing, charging, maintenance
        /// </summary>
        public string StationType
        {
            get => _stationType;
            set => SetProperty(ref _stationType, value);
        }

        /// <summary>
        /// How many transporters can queue at this station
        /// </summary>
        public int QueueCapacity
        {
            get => _queueCapacity;
            set => SetProperty(ref _queueCapacity, value);
        }

        /// <summary>
        /// Default time spent at station (seconds)
        /// </summary>
        public double DwellTime
        {
            get => _dwellTime;
            set => SetProperty(ref _dwellTime, value);
        }

        /// <summary>
        /// If true, blocks track while occupied
        /// </summary>
        public bool IsBlocking
        {
            get => _isBlocking;
            set => SetProperty(ref _isBlocking, value);
        }

        /// <summary>
        /// Time to load an item (seconds)
        /// </summary>
        public double LoadTime
        {
            get => _loadTime;
            set => SetProperty(ref _loadTime, value);
        }

        /// <summary>
        /// Time to unload an item (seconds)
        /// </summary>
        public double UnloadTime
        {
            get => _unloadTime;
            set => SetProperty(ref _unloadTime, value);
        }

        /// <summary>
        /// Priority for job assignment (higher = more priority)
        /// </summary>
        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }
    }

    #endregion

    #region Waypoint

    /// <summary>
    /// A waypoint for track routing (not a station, just a routing point)
    /// </summary>
    public class WaypointData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _type = "waypoint";
        private string _name = "Waypoint";
        private string _networkId = "";
        private double _x, _y;
        private string _color = "#95A5A6";
        private double _turnRadius = 0;
        private bool _isJunction = false;

        public string Id { get => _id; set => SetProperty(ref _id, value); }
        public string Type { get => _type; set => SetProperty(ref _type, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        
        /// <summary>
        /// ID of the network this waypoint belongs to
        /// </summary>
        public string NetworkId { get => _networkId; set => SetProperty(ref _networkId, value); }
        
        public double X { get => _x; set => SetProperty(ref _x, value); }
        public double Y { get => _y; set => SetProperty(ref _y, value); }
        public string Color { get => _color; set => SetProperty(ref _color, value); }
        
        /// <summary>
        /// Minimum turn radius at this waypoint (0 = sharp turn allowed)
        /// </summary>
        public double TurnRadius { get => _turnRadius; set => SetProperty(ref _turnRadius, value); }
        
        /// <summary>
        /// If true, this waypoint is a junction where paths split
        /// </summary>
        public bool IsJunction { get => _isJunction; set => SetProperty(ref _isJunction, value); }
    }

    #endregion

    #region Transporter Track (Legacy - for backward compatibility)

    /// <summary>
    /// A track that transporters follow (collection of segments).
    /// LEGACY: New code should use TransportNetworkData.Segments directly.
    /// </summary>
    public class TransporterTrackData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Track";
        private TrackVisual _visual = new();
        private ObservableCollection<TrackSegmentData> _segments = new();

        public string Id { get => _id; set => SetProperty(ref _id, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public TrackVisual Visual { get => _visual; set => SetProperty(ref _visual, value); }
        public ObservableCollection<TrackSegmentData> Segments { get => _segments; set => SetProperty(ref _segments, value); }
    }

    public class TrackVisual : NotifyBase
    {
        private string _color = "#3498DB";
        private double _width = 30;
        private bool _showDirections = true;
        private string _lineStyle = "solid";

        public string Color { get => _color; set => SetProperty(ref _color, value); }
        public double Width { get => _width; set => SetProperty(ref _width, value); }
        public bool ShowDirections { get => _showDirections; set => SetProperty(ref _showDirections, value); }
        public string LineStyle { get => _lineStyle; set => SetProperty(ref _lineStyle, value); }
    }

    #endregion

    #region Track Segment

    /// <summary>
    /// A single segment of a track connecting two points
    /// </summary>
    public class TrackSegmentData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _networkId = "";
        private string _from = "";
        private string _to = "";
        private double _distance = 0;
        private bool _bidirectional = true;
        private double _speedLimit = 2.0;
        private int _laneCount = 1;
        private string _color = "";
        private int _priority = 0;
        private bool _isBlocked = false;
        private double _weight = 1.0;

        public string Id { get => _id; set => SetProperty(ref _id, value); }
        
        /// <summary>
        /// ID of the network this segment belongs to
        /// </summary>
        public string NetworkId { get => _networkId; set => SetProperty(ref _networkId, value); }
        
        /// <summary>
        /// ID of start station/waypoint
        /// </summary>
        public string From { get => _from; set => SetProperty(ref _from, value); }
        
        /// <summary>
        /// ID of end station/waypoint
        /// </summary>
        public string To { get => _to; set => SetProperty(ref _to, value); }
        
        /// <summary>
        /// Distance in layout units (calculated from positions if 0)
        /// </summary>
        public double Distance { get => _distance; set => SetProperty(ref _distance, value); }
        
        /// <summary>
        /// If true, transporters can travel both directions
        /// </summary>
        public bool Bidirectional { get => _bidirectional; set => SetProperty(ref _bidirectional, value); }
        
        /// <summary>
        /// Speed limit in m/s
        /// </summary>
        public double SpeedLimit { get => _speedLimit; set => SetProperty(ref _speedLimit, value); }
        
        /// <summary>
        /// Number of parallel lanes
        /// </summary>
        public int LaneCount { get => _laneCount; set => SetProperty(ref _laneCount, value); }
        
        /// <summary>
        /// Override color for this segment (empty = use network color)
        /// </summary>
        public string Color { get => _color; set => SetProperty(ref _color, value); }
        
        /// <summary>
        /// Routing priority (higher = preferred)
        /// </summary>
        public int Priority { get => _priority; set => SetProperty(ref _priority, value); }
        
        /// <summary>
        /// If true, segment is temporarily blocked
        /// </summary>
        public bool IsBlocked { get => _isBlocked; set => SetProperty(ref _isBlocked, value); }
        
        /// <summary>
        /// Routing weight multiplier (higher = more costly, less preferred)
        /// </summary>
        public double Weight { get => _weight; set => SetProperty(ref _weight, value); }
    }

    #endregion

    #region Transporter (AGV)

    /// <summary>
    /// A transporter/AGV that travels on tracks
    /// </summary>
    public class TransporterData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "AGV";
        private string _networkId = "";
        private string _homeStationId = "";
        private double _speed = 1.5;
        private double _acceleration = 0.5;
        private int _capacity = 1;
        private string _color = "#E74C3C";
        private string _transporterType = TransporterTypes.AGV;
        private double _length = 1.0;
        private double _width = 0.8;
        private double _batteryCapacity = 100;
        private double _batteryConsumption = 1;

        public string Id { get => _id; set => SetProperty(ref _id, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        
        /// <summary>
        /// ID of the network this transporter operates on
        /// </summary>
        public string NetworkId { get => _networkId; set => SetProperty(ref _networkId, value); }
        
        /// <summary>
        /// ID of home/charging station
        /// </summary>
        public string HomeStationId { get => _homeStationId; set => SetProperty(ref _homeStationId, value); }
        
        /// <summary>
        /// Travel speed in m/s
        /// </summary>
        public double Speed { get => _speed; set => SetProperty(ref _speed, value); }
        
        /// <summary>
        /// Acceleration in m/s²
        /// </summary>
        public double Acceleration { get => _acceleration; set => SetProperty(ref _acceleration, value); }
        
        /// <summary>
        /// Number of items can carry
        /// </summary>
        public int Capacity { get => _capacity; set => SetProperty(ref _capacity, value); }
        
        public string Color { get => _color; set => SetProperty(ref _color, value); }
        
        /// <summary>
        /// Type: agv, amr, forklift, tugger, conveyor
        /// </summary>
        public string TransporterType { get => _transporterType; set => SetProperty(ref _transporterType, value); }
        
        /// <summary>
        /// Length in meters
        /// </summary>
        public double Length { get => _length; set => SetProperty(ref _length, value); }
        
        /// <summary>
        /// Width in meters
        /// </summary>
        public double Width { get => _width; set => SetProperty(ref _width, value); }
        
        /// <summary>
        /// Battery capacity in kWh
        /// </summary>
        public double BatteryCapacity { get => _batteryCapacity; set => SetProperty(ref _batteryCapacity, value); }
        
        /// <summary>
        /// Battery consumption per meter traveled
        /// </summary>
        public double BatteryConsumption { get => _batteryConsumption; set => SetProperty(ref _batteryConsumption, value); }
    }

    #endregion

    #region Layout Extensions

    /// <summary>
    /// Extension to LayoutData for transport elements.
    /// Add these properties to your LayoutData class.
    /// </summary>
    public class TransportLayoutData
    {
        /// <summary>
        /// Named transport networks (new model)
        /// </summary>
        public ObservableCollection<TransportNetworkData> TransportNetworks { get; set; } = new();
        
        // Legacy collections for backward compatibility
        public ObservableCollection<TransportStationData> TransportStations { get; set; } = new();
        public ObservableCollection<WaypointData> Waypoints { get; set; } = new();
        public ObservableCollection<TransporterTrackData> TransporterTracks { get; set; } = new();
        public ObservableCollection<TransporterData> Transporters { get; set; } = new();
    }

    #endregion

    #region Network Preset Colors

    /// <summary>
    /// Preset colors for transport networks
    /// </summary>
    public static class NetworkColors
    {
        public static readonly string[] Presets = new[]
        {
            "#E67E22",  // Orange (default)
            "#3498DB",  // Blue
            "#9B59B6",  // Purple
            "#1ABC9C",  // Teal
            "#E74C3C",  // Red
            "#2ECC71",  // Green
            "#F39C12",  // Yellow
            "#34495E",  // Dark gray
            "#16A085",  // Sea green
            "#8E44AD",  // Violet
        };

        public static string GetNextColor(int networkIndex)
        {
            return Presets[networkIndex % Presets.Length];
        }
    }

    #endregion
}
