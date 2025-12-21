using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    #region Connection Types

    /// <summary>
    /// Types of connections between nodes
    /// </summary>
    public static class ConnectionTypes
    {
        // Logical connections
        public const string PartFlow = "partFlow";              // Logical material flow
        public const string ResourceBinding = "resourceBinding"; // Operator/resource assignment
        public const string ControlSignal = "controlSignal";     // Control/signal lines
        
        // Physical transport connections
        public const string TransportLink = "transportLink";     // Legacy - general transport
        public const string AGVTrack = "agvTrack";               // AGV/AMR automated routes
        public const string TTrack = "tTrack";                   // Forklift, tugger, truck routes
        public const string Conveyor = "conveyor";               // Fixed conveyor paths
        public const string General = "general";                 // Auto-generated fallback paths

        /// <summary>
        /// Get display name for UI
        /// </summary>
        public static string GetDisplayName(string type)
        {
            return type switch
            {
                PartFlow => "Part Flow",
                ResourceBinding => "Resource Binding",
                ControlSignal => "Control Signal",
                TransportLink => "Transport Link",
                AGVTrack => "AGV Track",
                TTrack => "T-Track (Forklift/Truck)",
                Conveyor => "Conveyor",
                General => "General Path",
                _ => type
            };
        }

        /// <summary>
        /// Get keyboard shortcut
        /// </summary>
        public static string GetShortcut(string type)
        {
            return type switch
            {
                PartFlow => "P",
                AGVTrack => "A",
                TTrack => "T",
                Conveyor => "C",
                General => "G",
                ResourceBinding => "R",
                ControlSignal => "S",
                _ => ""
            };
        }

        /// <summary>
        /// Check if type is a physical track (vs logical connection)
        /// </summary>
        public static bool IsPhysicalTrack(string type)
        {
            return type switch
            {
                AGVTrack => true,
                TTrack => true,
                Conveyor => true,
                General => true,
                TransportLink => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if type is a logical connection
        /// </summary>
        public static bool IsLogicalConnection(string type)
        {
            return type switch
            {
                PartFlow => true,
                ResourceBinding => true,
                ControlSignal => true,
                _ => false
            };
        }
    }

    #endregion

    /// <summary>
    /// Connection path between nodes
    /// </summary>
    public class PathData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _from = "";
        private string _to = "";
        private string _fromTerminal = "output";
        private string _toTerminal = "input";
        private string _pathType = "single";
        private string _routingMode = "direct";
        private string _connectionType = ConnectionTypes.PartFlow;
        private PathVisual _visual = new();
        private PathSimulation _simulation = new();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string From
        {
            get => _from;
            set => SetProperty(ref _from, value);
        }

        public string To
        {
            get => _to;
            set => SetProperty(ref _to, value);
        }

        public string FromTerminal
        {
            get => _fromTerminal;
            set => SetProperty(ref _fromTerminal, value);
        }

        public string ToTerminal
        {
            get => _toTerminal;
            set => SetProperty(ref _toTerminal, value);
        }

        public string PathType
        {
            get => _pathType;
            set => SetProperty(ref _pathType, value);
        }

        public string RoutingMode
        {
            get => _routingMode;
            set => SetProperty(ref _routingMode, value);
        }

        /// <summary>
        /// Connection type: partFlow, resourceBinding, transportLink, controlSignal, agvTrack, tTrack, conveyor, general
        /// </summary>
        public string ConnectionType
        {
            get => _connectionType;
            set => SetProperty(ref _connectionType, value);
        }

        public PathVisual Visual
        {
            get => _visual;
            set => SetProperty(ref _visual, value);
        }

        public PathSimulation Simulation
        {
            get => _simulation;
            set => SetProperty(ref _simulation, value);
        }

        [JsonIgnore]
        public bool IsSelected { get; set; }

        [JsonIgnore]
        public bool IsPartFlow => _connectionType == ConnectionTypes.PartFlow;

        [JsonIgnore]
        public bool IsResourceBinding => _connectionType == ConnectionTypes.ResourceBinding;

        [JsonIgnore]
        public bool IsTransportLink => _connectionType == ConnectionTypes.TransportLink;

        [JsonIgnore]
        public bool IsAGVTrack => _connectionType == ConnectionTypes.AGVTrack;

        [JsonIgnore]
        public bool IsTTrack => _connectionType == ConnectionTypes.TTrack;

        [JsonIgnore]
        public bool IsConveyor => _connectionType == ConnectionTypes.Conveyor;

        [JsonIgnore]
        public bool IsPhysicalTrack => ConnectionTypes.IsPhysicalTrack(_connectionType);

        [JsonIgnore]
        public bool IsLogicalConnection => ConnectionTypes.IsLogicalConnection(_connectionType);

        /// <summary>
        /// Transport layer assignment for 8-layer architecture.
        /// Defaults to LocalFlow but can be determined by ConnectionType.
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.LocalFlow;
    }

    /// <summary>
    /// Path visual properties
    /// </summary>
    public class PathVisual : NotifyBase
    {
        private string _color = "#888888";
        private double _thickness = 2;
        private string _style = "solid";           // solid, dashed, dotted, double-chevron, banded
        private double _arrowSize = 8;
        private double _laneSpacing = 6;

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }

        /// <summary>
        /// Line style: solid, dashed, dotted, dashdot, double-chevron, banded
        /// </summary>
        public string Style
        {
            get => _style;
            set => SetProperty(ref _style, value);
        }

        public double ArrowSize
        {
            get => _arrowSize;
            set => SetProperty(ref _arrowSize, value);
        }

        public double LaneSpacing
        {
            get => _laneSpacing;
            set => SetProperty(ref _laneSpacing, value);
        }

        public ObservableCollection<PointData> Waypoints { get; set; } = new();

        /// <summary>
        /// Apply default styling based on connection type
        /// </summary>
        public void ApplyConnectionTypeStyle(string connectionType)
        {
            switch (connectionType)
            {
                // Logical connections
                case ConnectionTypes.PartFlow:
                    Color = "#888888";       // Gray
                    Style = "solid";
                    Thickness = 2;
                    break;
                    
                case ConnectionTypes.ResourceBinding:
                    Color = "#3498DB";       // Blue
                    Style = "dashed";
                    Thickness = 2;
                    break;
                    
                case ConnectionTypes.ControlSignal:
                    Color = "#9B59B6";       // Purple
                    Style = "dashdot";
                    Thickness = 1.5;
                    break;

                // Physical tracks
                case ConnectionTypes.AGVTrack:
                    Color = "#E67E22";       // Orange
                    Style = "double-chevron";
                    Thickness = 2;
                    break;
                    
                case ConnectionTypes.TTrack:
                    Color = "#3498DB";       // Blue
                    Style = "double-chevron";
                    Thickness = 2;
                    break;
                    
                case ConnectionTypes.Conveyor:
                    Color = "#27AE60";       // Green
                    Style = "banded";
                    Thickness = 2;
                    break;
                    
                case ConnectionTypes.General:
                    Color = "#95A5A6";       // Light gray
                    Style = "double-chevron";
                    Thickness = 2;
                    break;
                    
                case ConnectionTypes.TransportLink:
                    Color = "#E67E22";       // Orange (legacy)
                    Style = "dotted";
                    Thickness = 3;
                    break;
            }
        }
    }

    /// <summary>
    /// Path simulation properties
    /// </summary>
    public class PathSimulation : NotifyBase
    {
        private double? _distance;
        private string _transportType = "conveyor";
        private double _speed = 1.0;
        private int _capacity = 10;
        private int _lanes = 1;
        private bool _bidirectional;
        private double _speedLimit = 2.0;
        private bool _isBlocked = false;

        public double? Distance
        {
            get => _distance;
            set => SetProperty(ref _distance, value);
        }

        public string TransportType
        {
            get => _transportType;
            set => SetProperty(ref _transportType, value);
        }

        public double Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public int Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }

        public int Lanes
        {
            get => _lanes;
            set => SetProperty(ref _lanes, value);
        }

        public bool Bidirectional
        {
            get => _bidirectional;
            set => SetProperty(ref _bidirectional, value);
        }

        /// <summary>
        /// Speed limit for physical tracks (m/s)
        /// </summary>
        public double SpeedLimit
        {
            get => _speedLimit;
            set => SetProperty(ref _speedLimit, value);
        }

        /// <summary>
        /// Temporarily blocked (for simulation)
        /// </summary>
        public bool IsBlocked
        {
            get => _isBlocked;
            set => SetProperty(ref _isBlocked, value);
        }
    }

    /// <summary>
    /// 2D point for waypoints
    /// </summary>
    public class PointData : NotifyBase
    {
        private double _x;
        private double _y;

        public PointData() { }

        public PointData(double x, double y)
        {
            _x = x;
            _y = y;
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
    }

    /// <summary>
    /// Path connection drawing modes
    /// </summary>
    public static class ConnectionModes
    {
        /// <summary>
        /// Single connection: Click A → Click B → Creates A→B, done
        /// </summary>
        public const string Single = "single";
        
        /// <summary>
        /// One-to-Many (Star/Fan): Click A (primary) → Click B, C, D... → Creates A→B, A→C, A→D
        /// </summary>
        public const string OneToMany = "1:N";
        
        /// <summary>
        /// Chain: Click A → B → C → D → Creates A→B, B→C, C→D in sequence
        /// </summary>
        public const string Chain = "chain";

        public static string GetDisplayName(string mode)
        {
            return mode switch
            {
                Single => "Single",
                OneToMany => "1:N (Star)",
                Chain => "Chain",
                _ => mode
            };
        }

        public static string GetNextMode(string current)
        {
            return current switch
            {
                Single => OneToMany,
                OneToMany => Chain,
                Chain => Single,
                _ => Single
            };
        }
    }
}
