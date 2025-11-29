using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
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
    }

    /// <summary>
    /// Path visual properties
    /// </summary>
    public class PathVisual : NotifyBase
    {
        private string _color = "#888888";
        private double _thickness = 2;
        private string _style = "solid";
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
}
