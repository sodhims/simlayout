using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using System.Linq;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Conveyor types for material handling
    /// </summary>
    public static class ConveyorTypes
    {
        public const string Belt = "belt";
        public const string Roller = "roller";
        public const string Chain = "chain";
        public const string Overhead = "overhead";
    }

    /// <summary>
    /// Conveyor direction options
    /// </summary>
    public static class ConveyorDirections
    {
        public const string Forward = "forward";
        public const string Reverse = "reverse";
        public const string Bidirectional = "bidirectional";
    }

    /// <summary>
    /// Conveyor data model for local material flow
    /// </summary>
    public class ConveyorData : NotifyBase, IConstrainedEntity
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Conveyor";
        private List<PointData> _path = new();
        private double _width = 40.0;
        private double _speed = 0.5; // m/s
        private string _conveyorType = ConveyorTypes.Belt;
        private string _direction = ConveyorDirections.Forward;
        private bool _isAccumulating = false;
        private string? _fromNodeId;
        private string? _toNodeId;
        private string _color = "#FFA500"; // Orange

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
        /// Centerline path of the conveyor (list of points)
        /// </summary>
        public List<PointData> Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        /// <summary>
        /// Width of the conveyor in units
        /// </summary>
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// Speed in meters per second
        /// </summary>
        public double Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        /// <summary>
        /// Type of conveyor (belt, roller, chain, overhead)
        /// </summary>
        public string ConveyorType
        {
            get => _conveyorType;
            set => SetProperty(ref _conveyorType, value);
        }

        /// <summary>
        /// Direction of conveyor movement (forward, reverse, bidirectional)
        /// </summary>
        public string Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        /// <summary>
        /// True if conveyor can accumulate items
        /// </summary>
        public bool IsAccumulating
        {
            get => _isAccumulating;
            set => SetProperty(ref _isAccumulating, value);
        }

        /// <summary>
        /// Optional: ID of source equipment node
        /// </summary>
        public string? FromNodeId
        {
            get => _fromNodeId;
            set => SetProperty(ref _fromNodeId, value);
        }

        /// <summary>
        /// Optional: ID of destination equipment node
        /// </summary>
        public string? ToNodeId
        {
            get => _toNodeId;
            set => SetProperty(ref _toNodeId, value);
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
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.LocalFlow;

        // IConstrainedEntity implementation
        public bool SupportsConstrainedMovement => Path != null && Path.Count >= 2;

        public IConstraint GetConstraint()
        {
            if (Path == null || Path.Count < 2)
                return null;

            var waypoints = Path.Select(p => new Point(p.X, p.Y)).ToList();
            return new PathConstraint(waypoints);
        }
    }

    /// <summary>
    /// Direct path transfer types
    /// </summary>
    public static class TransferTypes
    {
        public const string Manual = "manual";
        public const string Robot = "robot";
        public const string Gravity = "gravity";
        public const string Pneumatic = "pneumatic";
    }

    /// <summary>
    /// Direct path for within-cell material movement
    /// </summary>
    public class DirectPathData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Transfer";
        private string _fromNodeId = "";
        private string _toNodeId = "";
        private string _transferType = TransferTypes.Manual;
        private double _transferTime = 5.0; // seconds
        private string _color = "#808080"; // Gray

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
        /// Source equipment node ID
        /// </summary>
        public string FromNodeId
        {
            get => _fromNodeId;
            set => SetProperty(ref _fromNodeId, value);
        }

        /// <summary>
        /// Destination equipment node ID
        /// </summary>
        public string ToNodeId
        {
            get => _toNodeId;
            set => SetProperty(ref _toNodeId, value);
        }

        /// <summary>
        /// Type of transfer (manual, robot, gravity, pneumatic)
        /// </summary>
        public string TransferType
        {
            get => _transferType;
            set => SetProperty(ref _transferType, value);
        }

        /// <summary>
        /// Transfer time in seconds
        /// </summary>
        public double TransferTime
        {
            get => _transferTime;
            set => SetProperty(ref _transferTime, value);
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
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.LocalFlow;
    }
}
