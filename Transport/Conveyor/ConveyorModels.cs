using System;
using System.Collections.ObjectModel;
using LayoutEditor.Transport;

namespace LayoutEditor.Transport.Conveyor
{
    #region Conveyor Station (In/Out Point)

    /// <summary>
    /// Conveyor station / workstation interface
    /// </summary>
    public class ConveyorStation : TransportStationBase
    {
        private string _interfaceType = "inline"; // inline, perpendicular, lift_transfer
        private double _conveyorHeight = 0.8;     // Height above floor
        private bool _hasStopGate;
        private bool _hasSensor;
        private bool _hasLiftTransfer;
        private double _liftStroke;
        private string _transferDirection = "both"; // left, right, both

        /// <summary>
        /// Interface type with main conveyor
        /// </summary>
        public string InterfaceType
        {
            get => _interfaceType;
            set => SetProperty(ref _interfaceType, value);
        }

        /// <summary>
        /// Conveyor surface height above floor
        /// </summary>
        public double ConveyorHeight
        {
            get => _conveyorHeight;
            set => SetProperty(ref _conveyorHeight, value);
        }

        /// <summary>
        /// Has stop/release gate
        /// </summary>
        public bool HasStopGate
        {
            get => _hasStopGate;
            set => SetProperty(ref _hasStopGate, value);
        }

        /// <summary>
        /// Has presence sensor
        /// </summary>
        public bool HasSensor
        {
            get => _hasSensor;
            set => SetProperty(ref _hasSensor, value);
        }

        /// <summary>
        /// Has vertical lift transfer
        /// </summary>
        public bool HasLiftTransfer
        {
            get => _hasLiftTransfer;
            set => SetProperty(ref _hasLiftTransfer, value);
        }

        /// <summary>
        /// Lift stroke distance
        /// </summary>
        public double LiftStroke
        {
            get => _liftStroke;
            set => SetProperty(ref _liftStroke, value);
        }

        /// <summary>
        /// Transfer direction capability
        /// </summary>
        public string TransferDirection
        {
            get => _transferDirection;
            set => SetProperty(ref _transferDirection, value);
        }
    }

    #endregion

    #region Conveyor Segment

    /// <summary>
    /// Conveyor segment (belt, roller, etc.)
    /// </summary>
    public class ConveyorSegment : TrackSegmentBase
    {
        private string _conveyorType = "belt";  // belt, roller, chain, gravity
        private double _width = 0.6;            // Conveyor width (meters)
        private double _floorHeight = 0.8;      // Height above floor
        private double _speed = 0.5;            // m/s
        private bool _isAccumulating;
        private int _accumulationZones = 1;
        private bool _hasZoning;                // Can control zones separately
        private bool _isReversible;
        private double _loadCapacity = 50;      // kg per zone/segment

        /// <summary>
        /// Conveyor type: belt, roller, chain, gravity
        /// </summary>
        public string ConveyorType
        {
            get => _conveyorType;
            set => SetProperty(ref _conveyorType, value);
        }

        /// <summary>
        /// Physical conveyor width (meters)
        /// </summary>
        public double ConveyorWidth
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// Height above floor (meters)
        /// </summary>
        public double FloorHeight
        {
            get => _floorHeight;
            set => SetProperty(ref _floorHeight, value);
        }

        /// <summary>
        /// Belt/roller speed (m/s)
        /// </summary>
        public double Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        /// <summary>
        /// Supports accumulation (zero-pressure)
        /// </summary>
        public bool IsAccumulating
        {
            get => _isAccumulating;
            set => SetProperty(ref _isAccumulating, value);
        }

        /// <summary>
        /// Number of accumulation zones
        /// </summary>
        public int AccumulationZones
        {
            get => _accumulationZones;
            set => SetProperty(ref _accumulationZones, value);
        }

        /// <summary>
        /// Has zone control capability
        /// </summary>
        public bool HasZoning
        {
            get => _hasZoning;
            set => SetProperty(ref _hasZoning, value);
        }

        /// <summary>
        /// Can run in reverse
        /// </summary>
        public bool IsReversible
        {
            get => _isReversible;
            set => SetProperty(ref _isReversible, value);
        }

        /// <summary>
        /// Load capacity per zone (kg)
        /// </summary>
        public double LoadCapacity
        {
            get => _loadCapacity;
            set => SetProperty(ref _loadCapacity, value);
        }
    }

    #endregion

    #region Conveyor Junction

    /// <summary>
    /// Conveyor junction/merge/divert point
    /// </summary>
    public class ConveyorJunction : WaypointBase
    {
        private string _junctionType = "merge"; // merge, divert, transfer, turntable
        private int _inputCount = 2;
        private int _outputCount = 1;
        private string _divertMethod = "pusher"; // pusher, popup_wheel, sliding_shoe
        private double _divertSpeed = 1.0;
        private bool _hasScanner;               // Barcode/RFID scanner
        private string _sortingLogic = "fifo";  // fifo, priority, destination

        /// <summary>
        /// Junction type: merge, divert, transfer, turntable
        /// </summary>
        public string JunctionType
        {
            get => _junctionType;
            set => SetProperty(ref _junctionType, value);
        }

        /// <summary>
        /// Number of input lines
        /// </summary>
        public int InputCount
        {
            get => _inputCount;
            set => SetProperty(ref _inputCount, value);
        }

        /// <summary>
        /// Number of output lines
        /// </summary>
        public int OutputCount
        {
            get => _outputCount;
            set => SetProperty(ref _outputCount, value);
        }

        /// <summary>
        /// Divert mechanism: pusher, popup_wheel, sliding_shoe
        /// </summary>
        public string DivertMethod
        {
            get => _divertMethod;
            set => SetProperty(ref _divertMethod, value);
        }

        /// <summary>
        /// Divert operation rate (per minute)
        /// </summary>
        public double DivertSpeed
        {
            get => _divertSpeed;
            set => SetProperty(ref _divertSpeed, value);
        }

        /// <summary>
        /// Has barcode/RFID scanner
        /// </summary>
        public bool HasScanner
        {
            get => _hasScanner;
            set => SetProperty(ref _hasScanner, value);
        }

        /// <summary>
        /// Sorting logic: fifo, priority, destination
        /// </summary>
        public string SortingLogic
        {
            get => _sortingLogic;
            set => SetProperty(ref _sortingLogic, value);
        }
    }

    #endregion

    #region Conveyor Network

    /// <summary>
    /// Conveyor transport network
    /// </summary>
    public class ConveyorNetwork : TransportNetworkBase
    {
        private string _systemType = "belt";    // belt, roller, mixed
        private double _defaultWidth = 0.6;
        private double _defaultSpeed = 0.5;
        private double _defaultHeight = 0.8;

        public ObservableCollection<ConveyorStation> Stations { get; set; } = new();
        public ObservableCollection<ConveyorJunction> Junctions { get; set; } = new();
        public ObservableCollection<ConveyorSegment> Segments { get; set; } = new();

        public ConveyorNetwork()
        {
            TransportType = TransportType.Conveyor;
            Color = "#27AE60"; // Green
        }

        /// <summary>
        /// Primary conveyor type
        /// </summary>
        public string SystemType
        {
            get => _systemType;
            set => SetProperty(ref _systemType, value);
        }

        /// <summary>
        /// Default conveyor width (meters)
        /// </summary>
        public double DefaultWidth
        {
            get => _defaultWidth;
            set => SetProperty(ref _defaultWidth, value);
        }

        /// <summary>
        /// Default belt speed (m/s)
        /// </summary>
        public double DefaultSpeed
        {
            get => _defaultSpeed;
            set => SetProperty(ref _defaultSpeed, value);
        }

        /// <summary>
        /// Default height above floor (meters)
        /// </summary>
        public double DefaultHeight
        {
            get => _defaultHeight;
            set => SetProperty(ref _defaultHeight, value);
        }
    }

    #endregion
}
