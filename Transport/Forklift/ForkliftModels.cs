using System;
using System.Collections.ObjectModel;
using LayoutEditor.Transport;

namespace LayoutEditor.Transport.Forklift
{
    #region Forklift Station (Pickup/Dropoff)

    /// <summary>
    /// Forklift pickup/dropoff location
    /// </summary>
    public class ForkliftStation : TransportStationBase
    {
        private string _locationType = "floor";  // floor, dock, rack, staging
        private double _floorLevel;              // Height/level (for multi-level)
        private string _approachAngle = "any";   // any, front, back, side
        private double _aisleWidth = 3.5;        // Required aisle width
        private bool _requiresSpotter;
        private int _palletCapacity = 1;
        private string _palletType = "standard"; // standard, euro, custom

        /// <summary>
        /// Location type: floor, dock, rack, staging
        /// </summary>
        public string LocationType
        {
            get => _locationType;
            set => SetProperty(ref _locationType, value);
        }

        /// <summary>
        /// Floor level (for multi-story)
        /// </summary>
        public double FloorLevel
        {
            get => _floorLevel;
            set => SetProperty(ref _floorLevel, value);
        }

        /// <summary>
        /// Required approach angle
        /// </summary>
        public string ApproachAngle
        {
            get => _approachAngle;
            set => SetProperty(ref _approachAngle, value);
        }

        /// <summary>
        /// Required aisle width (meters)
        /// </summary>
        public double AisleWidth
        {
            get => _aisleWidth;
            set => SetProperty(ref _aisleWidth, value);
        }

        /// <summary>
        /// Requires spotter for safety
        /// </summary>
        public bool RequiresSpotter
        {
            get => _requiresSpotter;
            set => SetProperty(ref _requiresSpotter, value);
        }

        /// <summary>
        /// Number of pallets that can be staged
        /// </summary>
        public int PalletCapacity
        {
            get => _palletCapacity;
            set => SetProperty(ref _palletCapacity, value);
        }

        /// <summary>
        /// Pallet type requirement
        /// </summary>
        public string PalletType
        {
            get => _palletType;
            set => SetProperty(ref _palletType, value);
        }
    }

    #endregion

    #region Forklift Aisle/Route

    /// <summary>
    /// Forklift travel aisle/route
    /// </summary>
    public class ForkliftAisle : TrackSegmentBase
    {
        private string _aisleType = "main";      // main, cross, narrow
        private double _width = 3.5;             // Aisle width (meters)
        private bool _isOneWay;
        private double _maxSpeed = 2.5;          // m/s
        private string _floorType = "concrete";  // concrete, asphalt, coated
        private bool _hasPedestrianCrossing;
        private bool _requiresHorn;

        /// <summary>
        /// Aisle type: main, cross, narrow
        /// </summary>
        public string AisleType
        {
            get => _aisleType;
            set => SetProperty(ref _aisleType, value);
        }

        /// <summary>
        /// Physical aisle width (meters)
        /// </summary>
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// One-way traffic only
        /// </summary>
        public bool IsOneWay
        {
            get => _isOneWay;
            set => SetProperty(ref _isOneWay, value);
        }

        /// <summary>
        /// Maximum allowed speed (m/s)
        /// </summary>
        public double MaxSpeed
        {
            get => _maxSpeed;
            set => SetProperty(ref _maxSpeed, value);
        }

        /// <summary>
        /// Floor surface type
        /// </summary>
        public string FloorType
        {
            get => _floorType;
            set => SetProperty(ref _floorType, value);
        }

        /// <summary>
        /// Has pedestrian crossing
        /// </summary>
        public bool HasPedestrianCrossing
        {
            get => _hasPedestrianCrossing;
            set => SetProperty(ref _hasPedestrianCrossing, value);
        }

        /// <summary>
        /// Requires horn at this aisle
        /// </summary>
        public bool RequiresHorn
        {
            get => _requiresHorn;
            set => SetProperty(ref _requiresHorn, value);
        }
    }

    #endregion

    #region Forklift Intersection

    /// <summary>
    /// Forklift aisle intersection
    /// </summary>
    public class ForkliftIntersection : WaypointBase
    {
        private string _intersectionType = "cross"; // cross, T, L
        private bool _hasStopSign;
        private bool _hasConvexMirror;
        private bool _hasPedestrianCrossing;
        private string _rightOfWay = "yield";    // yield, stop, priority

        /// <summary>
        /// Intersection type: cross, T, L
        /// </summary>
        public string IntersectionType
        {
            get => _intersectionType;
            set => SetProperty(ref _intersectionType, value);
        }

        /// <summary>
        /// Has stop sign
        /// </summary>
        public bool HasStopSign
        {
            get => _hasStopSign;
            set => SetProperty(ref _hasStopSign, value);
        }

        /// <summary>
        /// Has convex safety mirror
        /// </summary>
        public bool HasConvexMirror
        {
            get => _hasConvexMirror;
            set => SetProperty(ref _hasConvexMirror, value);
        }

        /// <summary>
        /// Has pedestrian crossing
        /// </summary>
        public bool HasPedestrianCrossing
        {
            get => _hasPedestrianCrossing;
            set => SetProperty(ref _hasPedestrianCrossing, value);
        }

        /// <summary>
        /// Right of way rule
        /// </summary>
        public string RightOfWay
        {
            get => _rightOfWay;
            set => SetProperty(ref _rightOfWay, value);
        }
    }

    #endregion

    #region Forklift Vehicle

    /// <summary>
    /// Forklift vehicle
    /// </summary>
    public class ForkliftVehicle : TransporterBase
    {
        private string _forkliftType = "counterbalance"; // counterbalance, reach, narrow_aisle, pallet_jack
        private double _liftCapacity = 2000;     // kg
        private double _maxLiftHeight = 5.0;     // meters
        private double _forkLength = 1.2;        // meters
        private double _turnRadius = 2.5;        // meters
        private string _powerType = "electric";  // electric, lpg, diesel
        private double _batteryLevel = 100;
        private bool _hasOperator = true;        // vs autonomous
        private string _operatorId = "";
        private bool _isLoaded;
        private double _currentLoad;

        public ForkliftVehicle()
        {
            Color = "#F39C12"; // Yellow/orange for forklift
        }

        /// <summary>
        /// Forklift type: counterbalance, reach, narrow_aisle, pallet_jack
        /// </summary>
        public string ForkliftType
        {
            get => _forkliftType;
            set => SetProperty(ref _forkliftType, value);
        }

        /// <summary>
        /// Maximum lift capacity (kg)
        /// </summary>
        public double LiftCapacity
        {
            get => _liftCapacity;
            set => SetProperty(ref _liftCapacity, value);
        }

        /// <summary>
        /// Maximum lift height (meters)
        /// </summary>
        public double MaxLiftHeight
        {
            get => _maxLiftHeight;
            set => SetProperty(ref _maxLiftHeight, value);
        }

        /// <summary>
        /// Fork length (meters)
        /// </summary>
        public double ForkLength
        {
            get => _forkLength;
            set => SetProperty(ref _forkLength, value);
        }

        /// <summary>
        /// Turning radius (meters)
        /// </summary>
        public double TurnRadius
        {
            get => _turnRadius;
            set => SetProperty(ref _turnRadius, value);
        }

        /// <summary>
        /// Power type: electric, lpg, diesel
        /// </summary>
        public string PowerType
        {
            get => _powerType;
            set => SetProperty(ref _powerType, value);
        }

        /// <summary>
        /// Battery level (%)
        /// </summary>
        public double BatteryLevel
        {
            get => _batteryLevel;
            set => SetProperty(ref _batteryLevel, value);
        }

        /// <summary>
        /// Has human operator (vs autonomous)
        /// </summary>
        public bool HasOperator
        {
            get => _hasOperator;
            set => SetProperty(ref _hasOperator, value);
        }

        /// <summary>
        /// Operator ID
        /// </summary>
        public string OperatorId
        {
            get => _operatorId;
            set => SetProperty(ref _operatorId, value);
        }

        /// <summary>
        /// Currently carrying load
        /// </summary>
        public bool IsLoaded
        {
            get => _isLoaded;
            set => SetProperty(ref _isLoaded, value);
        }

        /// <summary>
        /// Current load weight (kg)
        /// </summary>
        public double CurrentLoad
        {
            get => _currentLoad;
            set => SetProperty(ref _currentLoad, value);
        }
    }

    #endregion

    #region Forklift Network

    /// <summary>
    /// Forklift transport network
    /// </summary>
    public class ForkliftNetwork : TransportNetworkBase
    {
        private double _defaultAisleWidth = 3.5;
        private double _defaultMaxSpeed = 2.5;

        public ObservableCollection<ForkliftStation> Stations { get; set; } = new();
        public ObservableCollection<ForkliftIntersection> Intersections { get; set; } = new();
        public ObservableCollection<ForkliftAisle> Aisles { get; set; } = new();
        public ObservableCollection<ForkliftVehicle> Vehicles { get; set; } = new();

        public ForkliftNetwork()
        {
            TransportType = TransportType.Forklift;
            Color = "#F39C12"; // Yellow/orange
        }

        /// <summary>
        /// Default aisle width (meters)
        /// </summary>
        public double DefaultAisleWidth
        {
            get => _defaultAisleWidth;
            set => SetProperty(ref _defaultAisleWidth, value);
        }

        /// <summary>
        /// Default max speed (m/s)
        /// </summary>
        public double DefaultMaxSpeed
        {
            get => _defaultMaxSpeed;
            set => SetProperty(ref _defaultMaxSpeed, value);
        }
    }

    #endregion
}
