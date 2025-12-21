using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LayoutEditor.Transport
{
    #region Enums

    /// <summary>
    /// Types of transport systems
    /// </summary>
    public enum TransportType
    {
        AGV,        // Automated Guided Vehicle (floor)
        EOT,        // Electric Overhead Transport
        Conveyor,   // Belt/roller conveyors
        Forklift,   // Manual/semi-auto forklifts
        Tugger,     // Tugger trains
        AMR         // Autonomous Mobile Robot
    }

    /// <summary>
    /// Station operation types
    /// </summary>
    public enum StationType
    {
        Pickup,
        Dropoff,
        PickupDropoff,
        Charging,
        Parking,
        Maintenance,
        Waypoint,
        Junction
    }

    /// <summary>
    /// Track routing types
    /// </summary>
    public enum TrackDirection
    {
        Unidirectional,
        Bidirectional
    }

    #endregion

    #region Base Classes

    /// <summary>
    /// Base class for all transport elements with property change notification
    /// </summary>
    public abstract class TransportElement : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _networkId = "";
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Base class for transport stations
    /// </summary>
    public abstract class TransportStationBase : TransportElement
    {
        private double _x;
        private double _y;
        private double _width = 50;
        private double _height = 50;
        private double _rotation;
        private string _color = "#9B59B6";
        private string _groupName = "";
        private StationType _stationType = StationType.Pickup;
        private int _queueCapacity = 5;
        private double _dwellTime = 10;

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

        public double Rotation
        {
            get => _rotation;
            set => SetProperty(ref _rotation, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Group name for auto-connecting stations into loops
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        public StationType StationType
        {
            get => _stationType;
            set => SetProperty(ref _stationType, value);
        }

        public int QueueCapacity
        {
            get => _queueCapacity;
            set => SetProperty(ref _queueCapacity, value);
        }

        public double DwellTime
        {
            get => _dwellTime;
            set => SetProperty(ref _dwellTime, value);
        }

        public (double X, double Y) GetCenter() => (X + Width / 2, Y + Height / 2);
    }

    /// <summary>
    /// Base class for waypoints (routing points without station functionality)
    /// </summary>
    public abstract class WaypointBase : TransportElement
    {
        private double _x;
        private double _y;
        private string _color = "#E67E22";
        private double _turnRadius;
        private bool _isJunction;

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

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public double TurnRadius
        {
            get => _turnRadius;
            set => SetProperty(ref _turnRadius, value);
        }

        public bool IsJunction
        {
            get => _isJunction;
            set => SetProperty(ref _isJunction, value);
        }
    }

    /// <summary>
    /// Base class for track segments
    /// </summary>
    public abstract class TrackSegmentBase : TransportElement
    {
        private string _from = "";
        private string _to = "";
        private double _distance;
        private double _speedLimit = 2.0;
        private TrackDirection _direction = TrackDirection.Bidirectional;
        private string _color = "";
        private int _priority;
        private bool _isBlocked;
        private double _weight = 1.0;

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

        public double Distance
        {
            get => _distance;
            set => SetProperty(ref _distance, value);
        }

        public double SpeedLimit
        {
            get => _speedLimit;
            set => SetProperty(ref _speedLimit, value);
        }

        public TrackDirection Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        [JsonIgnore]
        public bool Bidirectional
        {
            get => _direction == TrackDirection.Bidirectional;
            set => Direction = value ? TrackDirection.Bidirectional : TrackDirection.Unidirectional;
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public bool IsBlocked
        {
            get => _isBlocked;
            set => SetProperty(ref _isBlocked, value);
        }

        public double Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }
    }

    /// <summary>
    /// Base class for transport vehicles/carriers
    /// </summary>
    public abstract class TransporterBase : TransportElement
    {
        private double _speed = 1.5;
        private double _acceleration = 0.5;
        private int _capacity = 1;
        private string _color = "#E74C3C";
        private string _homeStationId = "";
        private string _currentLocationId = "";
        private double _currentX;
        private double _currentY;

        public double Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public double Acceleration
        {
            get => _acceleration;
            set => SetProperty(ref _acceleration, value);
        }

        public int Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public string HomeStationId
        {
            get => _homeStationId;
            set => SetProperty(ref _homeStationId, value);
        }

        public string CurrentLocationId
        {
            get => _currentLocationId;
            set => SetProperty(ref _currentLocationId, value);
        }

        [JsonIgnore]
        public double CurrentX
        {
            get => _currentX;
            set => SetProperty(ref _currentX, value);
        }

        [JsonIgnore]
        public double CurrentY
        {
            get => _currentY;
            set => SetProperty(ref _currentY, value);
        }
    }

    /// <summary>
    /// Base class for transport networks
    /// </summary>
    public abstract class TransportNetworkBase : TransportElement
    {
        private string _description = "";
        private bool _isVisible = true;
        private bool _isLocked;
        private string _color = "#E67E22";
        private TransportType _transportType;

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

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public TransportType TransportType
        {
            get => _transportType;
            set => SetProperty(ref _transportType, value);
        }
    }

    #endregion
}
