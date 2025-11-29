using System;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Core node data
    /// </summary>
    public class NodeData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _type = "machine";
        private string? _templateId;
        private string _name = "";
        private string _label = "";
        private NodeVisual _visual = new();
        private SimulationParams _simulation = new();
        private int _indexInCell = -1;

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

        public string? TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
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

        public NodeVisual Visual
        {
            get => _visual;
            set => SetProperty(ref _visual, value);
        }

        public SimulationParams Simulation
        {
            get => _simulation;
            set => SetProperty(ref _simulation, value);
        }

        public int IndexInCell
        {
            get => _indexInCell;
            set => SetProperty(ref _indexInCell, value);
        }

        [JsonIgnore]
        public string TypePrefix => Type switch
        {
            NodeTypes.Machine => "M",
            NodeTypes.Buffer => "Q",
            NodeTypes.Workstation => "W",
            NodeTypes.Inspection => "I",
            NodeTypes.Source => "SRC",
            NodeTypes.Sink => "SNK",
            _ => "N"
        };

        [JsonIgnore]
        public bool IsSelected { get; set; }

        [JsonIgnore]
        public bool IsHighlighted { get; set; }
    }

    /// <summary>
    /// Node visual properties
    /// </summary>
    public class NodeVisual : NotifyBase
    {
        private double _x;
        private double _y;
        private double _width = 80;
        private double _height = 60;
        private double _rotation;
        private string _icon = "machine_generic";
        private string _color = "#4A90D9";
        private string _labelPosition = "bottom";
        private bool _labelVisible = true;
        private string _queueDirection = "horizontal";
        private double _entitySpacing = 12;
        private string _serverArrangement = "horizontal";
        private double _serverSpacing = 25;
        private string _inputTerminalPosition = "left";
        private string _outputTerminalPosition = "right";

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

        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public string LabelPosition
        {
            get => _labelPosition;
            set => SetProperty(ref _labelPosition, value);
        }

        public bool LabelVisible
        {
            get => _labelVisible;
            set => SetProperty(ref _labelVisible, value);
        }

        public string QueueDirection
        {
            get => _queueDirection;
            set => SetProperty(ref _queueDirection, value);
        }

        public double EntitySpacing
        {
            get => _entitySpacing;
            set => SetProperty(ref _entitySpacing, value);
        }

        public string ServerArrangement
        {
            get => _serverArrangement;
            set => SetProperty(ref _serverArrangement, value);
        }

        public double ServerSpacing
        {
            get => _serverSpacing;
            set => SetProperty(ref _serverSpacing, value);
        }

        public string InputTerminalPosition
        {
            get => _inputTerminalPosition;
            set => SetProperty(ref _inputTerminalPosition, value);
        }

        public string OutputTerminalPosition
        {
            get => _outputTerminalPosition;
            set => SetProperty(ref _outputTerminalPosition, value);
        }
    }
}
