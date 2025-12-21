using System;
using System.Text.Json.Serialization;
using System.Windows;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Terminal layout presets for easy cycling
    /// </summary>
    public static class TerminalLayouts
    {
        // Two-terminal layouts (input-output)
        public const string LeftRight = "left-right";
        public const string LeftTop = "left-top";
        public const string LeftBottom = "left-bottom";
        public const string RightLeft = "right-left";
        public const string RightTop = "right-top";
        public const string RightBottom = "right-bottom";
        public const string TopBottom = "top-bottom";
        public const string TopLeft = "top-left";
        public const string TopRight = "top-right";
        public const string BottomTop = "bottom-top";
        public const string BottomLeft = "bottom-left";
        public const string BottomRight = "bottom-right";
        
        // Single-terminal layouts (for transport stations)
        public const string Top = "top";
        public const string Bottom = "bottom";
        public const string Left = "left";
        public const string Right = "right";
        public const string Center = "center";

        /// <summary>
        /// Rotate terminal pair around the node (Ctrl+F)
        /// Keeps the angle between terminals, rotates the pair clockwise
        /// </summary>
        public static string GetNextLayout(string current)
        {
            return current switch
            {
                // 180° configurations - rotate pair
                LeftRight => TopBottom,
                TopBottom => RightLeft,
                RightLeft => BottomTop,
                BottomTop => LeftRight,
                
                // 90° configurations - rotate pair
                LeftTop => TopRight,
                TopRight => RightBottom,
                RightBottom => BottomLeft,
                BottomLeft => LeftTop,
                
                // Reverse 90° configurations - rotate pair
                TopLeft => RightTop,
                RightTop => BottomRight,
                BottomRight => LeftBottom,
                LeftBottom => TopLeft,
                
                _ => LeftRight
            };
        }

        /// <summary>
        /// Toggle to 90° mode, then cycle through 90° positions (Ctrl+Shift+F)
        /// Cycle: left-top → top-right → right-bottom → bottom-left → left-top
        /// </summary>
        public static string Toggle90Degrees(string current)
        {
            return current switch
            {
                // 180° → 90° (first press - enter 90° mode)
                LeftRight => LeftTop,
                TopBottom => TopRight,
                RightLeft => RightBottom,
                BottomTop => BottomLeft,
                
                // 90° → next 90° position (cycle clockwise)
                LeftTop => TopRight,
                TopRight => RightBottom,
                RightBottom => BottomLeft,
                BottomLeft => LeftTop,
                
                // Reverse 90° → next reverse 90° position
                TopLeft => RightTop,
                RightTop => BottomRight,
                BottomRight => LeftBottom,
                LeftBottom => TopLeft,
                
                _ => LeftTop
            };
        }

        /// <summary>
        /// Parse layout string into input/output positions
        /// </summary>
        public static (string input, string output) ParseLayout(string layout)
        {
            return layout switch
            {
                LeftRight => ("left", "right"),
                LeftTop => ("left", "top"),
                LeftBottom => ("left", "bottom"),
                RightLeft => ("right", "left"),
                RightTop => ("right", "top"),
                RightBottom => ("right", "bottom"),
                TopBottom => ("top", "bottom"),
                TopLeft => ("top", "left"),
                TopRight => ("top", "right"),
                BottomTop => ("bottom", "top"),
                BottomLeft => ("bottom", "left"),
                BottomRight => ("bottom", "right"),
                // Single-terminal layouts
                Top => ("top", "top"),
                Bottom => ("bottom", "bottom"),
                Left => ("left", "left"),
                Right => ("right", "right"),
                Center => ("center", "center"),
                _ => ("left", "right")
            };
        }

        /// <summary>
        /// Build layout string from input/output positions
        /// </summary>
        public static string BuildLayout(string input, string output)
        {
            // Handle single-terminal case
            if (input == output)
                return input;
            return $"{input}-{output}";
        }
    }

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

        /// <summary>
        /// Rotate terminal pair around the node (Ctrl+F)
        /// </summary>
        public void FlipTerminals()
        {
            var currentLayout = Visual.TerminalLayout;
            var newLayout = TerminalLayouts.GetNextLayout(currentLayout);
            Visual.TerminalLayout = newLayout;
        }

        /// <summary>
        /// Toggle between 90° and 180° terminal separation (Ctrl+Shift+F)
        /// </summary>
        public void RotateTerminals90()
        {
            var currentLayout = Visual.TerminalLayout;
            var newLayout = TerminalLayouts.Toggle90Degrees(currentLayout);
            Visual.TerminalLayout = newLayout;
        }

        /// <summary>
        /// Layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Equipment;
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
        // Optional normalized terminal coordinates (0..1) relative to icon bounding box
        private Point? _terminalInNorm = null;
        private Point? _terminalOutNorm = null;

        // Clearance zones for equipment layer (Week 3)
        private double _operationalClearanceLeft = 50;
        private double _operationalClearanceRight = 50;
        private double _operationalClearanceTop = 50;
        private double _operationalClearanceBottom = 50;
        private double _maintenanceClearanceLeft = 100;
        private double _maintenanceClearanceRight = 100;
        private double _maintenanceClearanceTop = 100;
        private double _maintenanceClearanceBottom = 100;

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

        public Point? TerminalInNorm
        {
            get => _terminalInNorm;
            set => SetProperty(ref _terminalInNorm, value);
        }

        public Point? TerminalOutNorm
        {
            get => _terminalOutNorm;
            set => SetProperty(ref _terminalOutNorm, value);
        }

        /// <summary>
        /// Combined terminal layout (convenience property for cycling)
        /// Gets/sets both InputTerminalPosition and OutputTerminalPosition
        /// </summary>
        [JsonIgnore]
        public string TerminalLayout
        {
            get => TerminalLayouts.BuildLayout(_inputTerminalPosition, _outputTerminalPosition);
            set
            {
                var (input, output) = TerminalLayouts.ParseLayout(value);
                if (_inputTerminalPosition != input || _outputTerminalPosition != output)
                {
                    _inputTerminalPosition = input;
                    _outputTerminalPosition = output;
                    OnPropertyChanged(nameof(InputTerminalPosition));
                    OnPropertyChanged(nameof(OutputTerminalPosition));
                    OnPropertyChanged(nameof(TerminalLayout));
                }
            }
        }

        // Operational clearance properties (blue zone, 50 units default)
        public double OperationalClearanceLeft
        {
            get => _operationalClearanceLeft;
            set => SetProperty(ref _operationalClearanceLeft, value);
        }

        public double OperationalClearanceRight
        {
            get => _operationalClearanceRight;
            set => SetProperty(ref _operationalClearanceRight, value);
        }

        public double OperationalClearanceTop
        {
            get => _operationalClearanceTop;
            set => SetProperty(ref _operationalClearanceTop, value);
        }

        public double OperationalClearanceBottom
        {
            get => _operationalClearanceBottom;
            set => SetProperty(ref _operationalClearanceBottom, value);
        }

        // Maintenance clearance properties (orange zone, 100 units default)
        public double MaintenanceClearanceLeft
        {
            get => _maintenanceClearanceLeft;
            set => SetProperty(ref _maintenanceClearanceLeft, value);
        }

        public double MaintenanceClearanceRight
        {
            get => _maintenanceClearanceRight;
            set => SetProperty(ref _maintenanceClearanceRight, value);
        }

        public double MaintenanceClearanceTop
        {
            get => _maintenanceClearanceTop;
            set => SetProperty(ref _maintenanceClearanceTop, value);
        }

        public double MaintenanceClearanceBottom
        {
            get => _maintenanceClearanceBottom;
            set => SetProperty(ref _maintenanceClearanceBottom, value);
        }
    }
}
