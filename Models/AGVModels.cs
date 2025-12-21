using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Waypoint types for AGV navigation
    /// </summary>
    public static class WaypointTypes
    {
        public const string Through = "through";           // Simple pass-through waypoint
        public const string Decision = "decision";         // Routing decision point
        public const string Stop = "stop";                 // Mandatory stop waypoint
        public const string SpeedChange = "speed_change";  // Speed limit change point
        public const string ChargingAccess = "charging_access"; // Access to charging station
    }

    /// <summary>
    /// AGV Waypoint for guided transport network
    /// </summary>
    public class AGVWaypointData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private double _x;
        private double _y;
        private string _waypointType = WaypointTypes.Through;
        private double _stopTime = 0.0; // seconds - for mandatory stops
        private double _speedAfter = 1.0; // m/s - speed limit after this waypoint

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
        /// Type of waypoint (through, decision, stop, speed_change, charging_access)
        /// </summary>
        public string WaypointType
        {
            get => _waypointType;
            set => SetProperty(ref _waypointType, value);
        }

        /// <summary>
        /// Mandatory stop time in seconds (for Stop waypoints)
        /// </summary>
        public double StopTime
        {
            get => _stopTime;
            set => SetProperty(ref _stopTime, value);
        }

        /// <summary>
        /// Speed limit after this waypoint in m/s (for SpeedChange waypoints)
        /// </summary>
        public double SpeedAfter
        {
            get => _speedAfter;
            set => SetProperty(ref _speedAfter, value);
        }

        /// <summary>
        /// Layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.GuidedTransport;
    }

    /// <summary>
    /// Path direction options for AGV paths
    /// </summary>
    public static class PathDirections
    {
        public const string Unidirectional = "unidirectional";
        public const string Bidirectional = "bidirectional";
    }

    /// <summary>
    /// AGV guidance path types
    /// </summary>
    public static class AGVPathTypes
    {
        public const string Wire = "wire";             // Wire-guided
        public const string Magnetic = "magnetic";     // Magnetic tape
        public const string Painted = "painted";       // Painted line
        public const string Natural = "natural";       // Natural features (vision)
    }

    /// <summary>
    /// AGV Path Segment connecting two waypoints
    /// </summary>
    public class AGVPathData : NotifyBase, IConstrainedEntity
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _fromWaypointId = "";
        private string _toWaypointId = "";
        private double _width = 1.2; // meters - vehicle width + buffer
        private double _speedLimit = 1.0; // m/s
        private string _direction = PathDirections.Unidirectional;
        private int _priority = 0; // for conflict resolution
        private string _pathType = AGVPathTypes.Magnetic;
        private string _color = "#3498DB"; // Blue

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

        /// <summary>
        /// Source waypoint ID
        /// </summary>
        public string FromWaypointId
        {
            get => _fromWaypointId;
            set => SetProperty(ref _fromWaypointId, value);
        }

        /// <summary>
        /// Destination waypoint ID
        /// </summary>
        public string ToWaypointId
        {
            get => _toWaypointId;
            set => SetProperty(ref _toWaypointId, value);
        }

        /// <summary>
        /// Path width in meters (vehicle width + buffer)
        /// </summary>
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// Speed limit in m/s
        /// </summary>
        public double SpeedLimit
        {
            get => _speedLimit;
            set => SetProperty(ref _speedLimit, value);
        }

        /// <summary>
        /// Direction: unidirectional or bidirectional
        /// </summary>
        public string Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        /// <summary>
        /// Priority for conflict resolution (higher = higher priority)
        /// </summary>
        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        /// <summary>
        /// Path guidance type (wire, magnetic, painted, natural)
        /// </summary>
        public string PathType
        {
            get => _pathType;
            set => SetProperty(ref _pathType, value);
        }

        /// <summary>
        /// Color for rendering
        /// </summary>
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.GuidedTransport;

        // IConstrainedEntity implementation
        public bool SupportsConstrainedMovement => true;

        public IConstraint GetConstraint()
        {
            // AGV path constraint requires waypoint lookup
            // Will be handled by ConstraintFactory
            return null;
        }

        /// <summary>
        /// Creates linear constraint for this AGV path given its waypoints
        /// </summary>
        public IConstraint GetConstraint(AGVWaypointData fromWaypoint, AGVWaypointData toWaypoint)
        {
            if (fromWaypoint == null || toWaypoint == null)
                return null;

            return new LinearConstraint(
                new Point(fromWaypoint.X, fromWaypoint.Y),
                new Point(toWaypoint.X, toWaypoint.Y)
            );
        }
    }

    /// <summary>
    /// AGV station types
    /// </summary>
    public static class AGVStationTypes
    {
        public const string LoadUnload = "load_unload";
        public const string LoadOnly = "load_only";
        public const string UnloadOnly = "unload_only";
        public const string Charging = "charging";
        public const string Parking = "parking";
    }

    /// <summary>
    /// AGV Station for loading, unloading, charging, or parking
    /// </summary>
    public class AGVStationData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private double _x;
        private double _y;
        private double _rotation = 0.0; // degrees
        private string _stationType = AGVStationTypes.LoadUnload;
        private string? _linkedEquipmentId; // Equipment this station serves
        private string? _linkedWaypointId; // Network access point
        private double _dockingTolerance = 0.05; // meters
        private double _serviceTime = 30.0; // seconds
        private string _color = "#E67E22"; // Orange

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
        /// Rotation in degrees (docking direction)
        /// </summary>
        public double Rotation
        {
            get => _rotation;
            set => SetProperty(ref _rotation, value);
        }

        /// <summary>
        /// Station type (load_unload, load_only, unload_only, charging, parking)
        /// </summary>
        public string StationType
        {
            get => _stationType;
            set => SetProperty(ref _stationType, value);
        }

        /// <summary>
        /// Optional: Equipment ID this station serves
        /// </summary>
        public string? LinkedEquipmentId
        {
            get => _linkedEquipmentId;
            set => SetProperty(ref _linkedEquipmentId, value);
        }

        /// <summary>
        /// Optional: Waypoint ID for network access
        /// </summary>
        public string? LinkedWaypointId
        {
            get => _linkedWaypointId;
            set => SetProperty(ref _linkedWaypointId, value);
        }

        /// <summary>
        /// Docking tolerance in meters
        /// </summary>
        public double DockingTolerance
        {
            get => _dockingTolerance;
            set => SetProperty(ref _dockingTolerance, value);
        }

        /// <summary>
        /// Service time in seconds
        /// </summary>
        public double ServiceTime
        {
            get => _serviceTime;
            set => SetProperty(ref _serviceTime, value);
        }

        /// <summary>
        /// Color for rendering
        /// </summary>
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.GuidedTransport;
    }

    /// <summary>
    /// Traffic zone types
    /// </summary>
    public static class TrafficZoneTypes
    {
        public const string Exclusive = "exclusive";     // Only one vehicle at a time
        public const string Intersection = "intersection"; // Intersection control
        public const string Passing = "passing";         // Passing lane zone
    }

    /// <summary>
    /// Traffic Zone for AGV traffic control
    /// </summary>
    public class TrafficZoneData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private List<PointData> _boundary = new();
        private int _maxVehicles = 1; // Capacity
        private string _zoneType = TrafficZoneTypes.Exclusive;
        private string _color = "#E74C3C"; // Red

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

        /// <summary>
        /// Boundary polygon points
        /// </summary>
        public List<PointData> Boundary
        {
            get => _boundary;
            set => SetProperty(ref _boundary, value);
        }

        /// <summary>
        /// Maximum vehicles allowed in zone (usually 1 for exclusive zones)
        /// </summary>
        public int MaxVehicles
        {
            get => _maxVehicles;
            set => SetProperty(ref _maxVehicles, value);
        }

        /// <summary>
        /// Zone type (exclusive, intersection, passing)
        /// </summary>
        public string ZoneType
        {
            get => _zoneType;
            set => SetProperty(ref _zoneType, value);
        }

        /// <summary>
        /// Color for rendering
        /// </summary>
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.GuidedTransport;
    }
}
