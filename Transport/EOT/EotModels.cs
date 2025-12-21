using System;
using System.Collections.ObjectModel;
using LayoutEditor.Transport;

namespace LayoutEditor.Transport.EOT
{
    #region EOT Station (Drop Point)

    /// <summary>
    /// EOT drop point / station (overhead)
    /// </summary>
    public class EotDropPoint : TransportStationBase
    {
        private double _floorHeight = 3.0;    // Height above floor (meters)
        private double _dropHeight = 1.0;     // How far load drops
        private double _clearanceRadius = 2.0; // Required floor clearance
        private string _dropType = "simple";   // simple, telescoping, rotating
        private double _loadCapacity = 500;    // kg
        private bool _hasHoist;
        private double _hoistSpeed = 0.5;      // m/s

        /// <summary>
        /// Height of track above floor (meters)
        /// </summary>
        public double FloorHeight
        {
            get => _floorHeight;
            set => SetProperty(ref _floorHeight, value);
        }

        /// <summary>
        /// Distance load drops from carrier
        /// </summary>
        public double DropHeight
        {
            get => _dropHeight;
            set => SetProperty(ref _dropHeight, value);
        }

        /// <summary>
        /// Required floor clearance radius
        /// </summary>
        public double ClearanceRadius
        {
            get => _clearanceRadius;
            set => SetProperty(ref _clearanceRadius, value);
        }

        /// <summary>
        /// Drop mechanism type
        /// </summary>
        public string DropType
        {
            get => _dropType;
            set => SetProperty(ref _dropType, value);
        }

        /// <summary>
        /// Maximum load capacity (kg)
        /// </summary>
        public double LoadCapacity
        {
            get => _loadCapacity;
            set => SetProperty(ref _loadCapacity, value);
        }

        /// <summary>
        /// Has powered hoist
        /// </summary>
        public bool HasHoist
        {
            get => _hasHoist;
            set => SetProperty(ref _hasHoist, value);
        }

        /// <summary>
        /// Hoist speed (m/s)
        /// </summary>
        public double HoistSpeed
        {
            get => _hoistSpeed;
            set => SetProperty(ref _hoistSpeed, value);
        }
    }

    #endregion

    #region EOT Waypoint (Overhead)

    /// <summary>
    /// EOT waypoint (overhead routing point)
    /// </summary>
    public class EotWaypoint : WaypointBase
    {
        private double _floorHeight = 3.0;
        private bool _isSwitch;          // Track switch point
        private bool _isAccumulation;    // Carriers can queue here
        private int _accumulationCapacity = 3;

        public double FloorHeight
        {
            get => _floorHeight;
            set => SetProperty(ref _floorHeight, value);
        }

        /// <summary>
        /// Is this a track switch point
        /// </summary>
        public bool IsSwitch
        {
            get => _isSwitch;
            set => SetProperty(ref _isSwitch, value);
        }

        /// <summary>
        /// Can carriers accumulate/queue here
        /// </summary>
        public bool IsAccumulation
        {
            get => _isAccumulation;
            set => SetProperty(ref _isAccumulation, value);
        }

        /// <summary>
        /// Number of carriers that can queue
        /// </summary>
        public int AccumulationCapacity
        {
            get => _accumulationCapacity;
            set => SetProperty(ref _accumulationCapacity, value);
        }
    }

    #endregion

    #region EOT Track (Overhead Rail)

    /// <summary>
    /// EOT track segment (overhead rail)
    /// </summary>
    public class EotTrack : TrackSegmentBase
    {
        private double _floorHeight = 3.0;
        private string _railType = "enclosed"; // enclosed, i_beam, monorail
        private double _railWidth = 0.15;
        private bool _isPowered = true;
        private bool _hasAccumulation;
        private int _accumulationZones;

        /// <summary>
        /// Height above floor (meters)
        /// </summary>
        public double FloorHeight
        {
            get => _floorHeight;
            set => SetProperty(ref _floorHeight, value);
        }

        /// <summary>
        /// Rail type: enclosed, i_beam, monorail
        /// </summary>
        public string RailType
        {
            get => _railType;
            set => SetProperty(ref _railType, value);
        }

        /// <summary>
        /// Physical rail width (meters)
        /// </summary>
        public double RailWidth
        {
            get => _railWidth;
            set => SetProperty(ref _railWidth, value);
        }

        /// <summary>
        /// Power rail track (vs gravity/manual)
        /// </summary>
        public bool IsPowered
        {
            get => _isPowered;
            set => SetProperty(ref _isPowered, value);
        }

        /// <summary>
        /// Has accumulation capability
        /// </summary>
        public bool HasAccumulation
        {
            get => _hasAccumulation;
            set => SetProperty(ref _hasAccumulation, value);
        }

        /// <summary>
        /// Number of accumulation zones
        /// </summary>
        public int AccumulationZones
        {
            get => _accumulationZones;
            set => SetProperty(ref _accumulationZones, value);
        }
    }

    #endregion

    #region EOT Carrier

    /// <summary>
    /// EOT carrier (overhead trolley)
    /// </summary>
    public class EotCarrier : TransporterBase
    {
        private string _carrierType = "standard"; // standard, rotating, tilting
        private double _hookHeight = 0.5;
        private double _loadCapacity = 500; // kg
        private double _currentLoad;
        private bool _isLoaded;
        private string _loadType = "";

        public EotCarrier()
        {
            Color = "#3498DB"; // Blue for EOT
        }

        /// <summary>
        /// Carrier type: standard, rotating, tilting
        /// </summary>
        public string CarrierType
        {
            get => _carrierType;
            set => SetProperty(ref _carrierType, value);
        }

        /// <summary>
        /// Hook/attachment height below carrier
        /// </summary>
        public double HookHeight
        {
            get => _hookHeight;
            set => SetProperty(ref _hookHeight, value);
        }

        /// <summary>
        /// Maximum load capacity (kg)
        /// </summary>
        public double LoadCapacity
        {
            get => _loadCapacity;
            set => SetProperty(ref _loadCapacity, value);
        }

        /// <summary>
        /// Current load weight (kg)
        /// </summary>
        public double CurrentLoad
        {
            get => _currentLoad;
            set => SetProperty(ref _currentLoad, value);
        }

        /// <summary>
        /// Is carrier currently loaded
        /// </summary>
        public bool IsLoaded
        {
            get => _isLoaded;
            set => SetProperty(ref _isLoaded, value);
        }

        /// <summary>
        /// Type of load being carried
        /// </summary>
        public string LoadType
        {
            get => _loadType;
            set => SetProperty(ref _loadType, value);
        }
    }

    #endregion

    #region EOT Network

    /// <summary>
    /// EOT transport network (overhead system)
    /// </summary>
    public class EotNetwork : TransportNetworkBase
    {
        private double _defaultFloorHeight = 3.0;
        private string _systemType = "power_and_free"; // power_and_free, monorail, enclosed

        public ObservableCollection<EotDropPoint> DropPoints { get; set; } = new();
        public ObservableCollection<EotWaypoint> Waypoints { get; set; } = new();
        public ObservableCollection<EotTrack> Tracks { get; set; } = new();
        public ObservableCollection<EotCarrier> Carriers { get; set; } = new();

        public EotNetwork()
        {
            TransportType = TransportType.EOT;
            Color = "#3498DB"; // Blue
        }

        /// <summary>
        /// Default track height above floor
        /// </summary>
        public double DefaultFloorHeight
        {
            get => _defaultFloorHeight;
            set => SetProperty(ref _defaultFloorHeight, value);
        }

        /// <summary>
        /// System type: power_and_free, monorail, enclosed
        /// </summary>
        public string SystemType
        {
            get => _systemType;
            set => SetProperty(ref _systemType, value);
        }
    }

    #endregion
}
