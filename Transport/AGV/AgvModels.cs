using System;
using System.Collections.ObjectModel;
using LayoutEditor.Transport;

namespace LayoutEditor.Transport.AGV
{
    #region AGV Station

    /// <summary>
    /// AGV station (pickup, dropoff, charging, parking)
    /// </summary>
    public class AgvStation : TransportStationBase
    {
        private bool _isCharging;
        private double _chargingRate = 1.0;
        private bool _hasLiftTable;
        private double _liftHeight;
        private string _approachDirection = "any"; // any, north, south, east, west

        /// <summary>
        /// Whether this station has charging capability
        /// </summary>
        public bool IsCharging
        {
            get => _isCharging;
            set => SetProperty(ref _isCharging, value);
        }

        /// <summary>
        /// Battery charge rate (% per minute)
        /// </summary>
        public double ChargingRate
        {
            get => _chargingRate;
            set => SetProperty(ref _chargingRate, value);
        }

        /// <summary>
        /// Has lift table for load transfer
        /// </summary>
        public bool HasLiftTable
        {
            get => _hasLiftTable;
            set => SetProperty(ref _hasLiftTable, value);
        }

        /// <summary>
        /// Lift table height (mm)
        /// </summary>
        public double LiftHeight
        {
            get => _liftHeight;
            set => SetProperty(ref _liftHeight, value);
        }

        /// <summary>
        /// Required approach direction
        /// </summary>
        public string ApproachDirection
        {
            get => _approachDirection;
            set => SetProperty(ref _approachDirection, value);
        }
    }

    #endregion

    #region AGV Waypoint

    /// <summary>
    /// AGV waypoint (routing point on floor)
    /// </summary>
    public class AgvWaypoint : WaypointBase
    {
        private bool _isDecisionPoint;
        private double _speedReduction = 1.0; // 1.0 = no reduction

        /// <summary>
        /// Whether AGV should make routing decision here
        /// </summary>
        public bool IsDecisionPoint
        {
            get => _isDecisionPoint;
            set => SetProperty(ref _isDecisionPoint, value);
        }

        /// <summary>
        /// Speed reduction factor (0.5 = half speed)
        /// </summary>
        public double SpeedReduction
        {
            get => _speedReduction;
            set => SetProperty(ref _speedReduction, value);
        }
    }

    #endregion

    #region AGV Track

    /// <summary>
    /// AGV track segment (floor path)
    /// </summary>
    public class AgvTrack : TrackSegmentBase
    {
        private string _trackType = "magnetic"; // magnetic, optical, laser, natural
        private double _trackWidth = 0.8; // meters
        private bool _hasFloorMarkings = true;
        private string _surfaceType = "concrete";

        /// <summary>
        /// Guidance type: magnetic, optical, laser, natural
        /// </summary>
        public string TrackType
        {
            get => _trackType;
            set => SetProperty(ref _trackType, value);
        }

        /// <summary>
        /// Physical track width in meters
        /// </summary>
        public double TrackWidth
        {
            get => _trackWidth;
            set => SetProperty(ref _trackWidth, value);
        }

        /// <summary>
        /// Has visible floor markings
        /// </summary>
        public bool HasFloorMarkings
        {
            get => _hasFloorMarkings;
            set => SetProperty(ref _hasFloorMarkings, value);
        }

        /// <summary>
        /// Floor surface type
        /// </summary>
        public string SurfaceType
        {
            get => _surfaceType;
            set => SetProperty(ref _surfaceType, value);
        }
    }

    #endregion

    #region AGV Vehicle

    /// <summary>
    /// AGV vehicle
    /// </summary>
    public class AgvVehicle : TransporterBase
    {
        private string _vehicleType = "unit_load"; // unit_load, tow, forklift, custom
        private double _length = 1.2;
        private double _width = 0.8;
        private double _height = 0.3;
        private double _batteryCapacity = 100;
        private double _batteryLevel = 100;
        private double _batteryConsumption = 0.5; // % per minute
        private double _minBatteryLevel = 20;
        private double _loadWeight;
        private double _maxLoadWeight = 1000; // kg
        private bool _hasCollisionAvoidance = true;
        private double _safetyZoneRadius = 1.5;

        public string VehicleType
        {
            get => _vehicleType;
            set => SetProperty(ref _vehicleType, value);
        }

        public double Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public double BatteryCapacity
        {
            get => _batteryCapacity;
            set => SetProperty(ref _batteryCapacity, value);
        }

        public double BatteryLevel
        {
            get => _batteryLevel;
            set => SetProperty(ref _batteryLevel, value);
        }

        public double BatteryConsumption
        {
            get => _batteryConsumption;
            set => SetProperty(ref _batteryConsumption, value);
        }

        public double MinBatteryLevel
        {
            get => _minBatteryLevel;
            set => SetProperty(ref _minBatteryLevel, value);
        }

        public double LoadWeight
        {
            get => _loadWeight;
            set => SetProperty(ref _loadWeight, value);
        }

        public double MaxLoadWeight
        {
            get => _maxLoadWeight;
            set => SetProperty(ref _maxLoadWeight, value);
        }

        public bool HasCollisionAvoidance
        {
            get => _hasCollisionAvoidance;
            set => SetProperty(ref _hasCollisionAvoidance, value);
        }

        public double SafetyZoneRadius
        {
            get => _safetyZoneRadius;
            set => SetProperty(ref _safetyZoneRadius, value);
        }

        /// <summary>
        /// Check if battery needs charging
        /// </summary>
        public bool NeedsCharging => BatteryLevel <= MinBatteryLevel;
    }

    #endregion

    #region AGV Network

    /// <summary>
    /// AGV transport network
    /// </summary>
    public class AgvNetwork : TransportNetworkBase
    {
        private string _guidanceType = "magnetic";
        private bool _trafficManagementEnabled = true;
        private double _defaultSpeedLimit = 2.0;

        public ObservableCollection<AgvStation> Stations { get; set; } = new();
        public ObservableCollection<AgvWaypoint> Waypoints { get; set; } = new();
        public ObservableCollection<AgvTrack> Tracks { get; set; } = new();
        public ObservableCollection<AgvVehicle> Vehicles { get; set; } = new();

        public AgvNetwork()
        {
            TransportType = TransportType.AGV;
            Color = "#E67E22"; // Orange
        }

        /// <summary>
        /// Primary guidance type: magnetic, optical, laser, natural
        /// </summary>
        public string GuidanceType
        {
            get => _guidanceType;
            set => SetProperty(ref _guidanceType, value);
        }

        /// <summary>
        /// Traffic management/collision avoidance enabled
        /// </summary>
        public bool TrafficManagementEnabled
        {
            get => _trafficManagementEnabled;
            set => SetProperty(ref _trafficManagementEnabled, value);
        }

        /// <summary>
        /// Default speed limit (m/s)
        /// </summary>
        public double DefaultSpeedLimit
        {
            get => _defaultSpeedLimit;
            set => SetProperty(ref _defaultSpeedLimit, value);
        }
    }

    #endregion
}
