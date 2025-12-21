using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Represents a wall segment
    /// </summary>
    public class WallData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private double _x1, _y1, _x2, _y2;
        private double _thickness = 6;
        private string _wallType = WallTypes.Standard;
        private string _color = "#444444";
        private string _dashPattern = "";
        private string _layer = "";
        private string _lineStyle = LineStyles.Solid;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public double X1
        {
            get => _x1;
            set => SetProperty(ref _x1, value);
        }

        public double Y1
        {
            get => _y1;
            set => SetProperty(ref _y1, value);
        }

        public double X2
        {
            get => _x2;
            set => SetProperty(ref _x2, value);
        }

        public double Y2
        {
            get => _y2;
            set => SetProperty(ref _y2, value);
        }

        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }

        public string WallType
        {
            get => _wallType;
            set => SetProperty(ref _wallType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Custom dash pattern as comma-separated values (e.g., "5,3" or "10,5,2,5")
        /// </summary>
        public string DashPattern
        {
            get => _dashPattern;
            set => SetProperty(ref _dashPattern, value);
        }

        /// <summary>
        /// Predefined line style: solid, dashed, dotted, dashdot, hidden
        /// </summary>
        public string LineStyle
        {
            get => _lineStyle;
            set
            {
                if (SetProperty(ref _lineStyle, value))
                {
                    // Update dash pattern based on style
                    DashPattern = LineStyles.GetDashPattern(value);
                }
            }
        }

        /// <summary>
        /// CAD layer name (from import) or layout layer
        /// </summary>
        public string Layer
        {
            get => _layer;
            set => SetProperty(ref _layer, value);
        }

        [JsonIgnore]
        public double Length => System.Math.Sqrt(
            (X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Infrastructure;
    }

    /// <summary>
    /// Predefined line styles
    /// </summary>
    public static class LineStyles
    {
        public const string Solid = "solid";
        public const string Dashed = "dashed";
        public const string Dotted = "dotted";
        public const string DashDot = "dashdot";
        public const string Hidden = "hidden";
        public const string Custom = "custom";

        public static string GetDashPattern(string style) => style switch
        {
            Dashed => "8,4",
            Dotted => "2,2",
            DashDot => "8,4,2,4",
            Hidden => "4,8",
            _ => ""
        };

        public static string[] All => new[] { Solid, Dashed, Dotted, DashDot, Hidden, Custom };
    }

    /// <summary>
    /// Represents a door or opening in a wall
    /// </summary>
    public class DoorData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _wallId = "";
        private double _position = 0.5; // 0-1 along wall (legacy - for wall-attached doors)
        private double _x, _y; // Position for standalone doors
        private double _width = 36;
        private double _height = 80;
        private double _rotation = 0; // Degrees
        private string _doorType = DoorTypes.Personnel;
        private double _swingAngle = 90; // Swing arc in degrees
        private string _swingDirection = SwingDirections.Inward; // inward, outward, sliding

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

        public string WallId
        {
            get => _wallId;
            set => SetProperty(ref _wallId, value);
        }

        public double Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
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

        public string DoorType
        {
            get => _doorType;
            set => SetProperty(ref _doorType, value);
        }

        public double SwingAngle
        {
            get => _swingAngle;
            set => SetProperty(ref _swingAngle, value);
        }

        public string SwingDirection
        {
            get => _swingDirection;
            set => SetProperty(ref _swingDirection, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Infrastructure;
    }

    /// <summary>
    /// Represents a column/pillar
    /// </summary>
    public class ColumnData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private double _x, _y;
        private double _width = 12;
        private double _height = 12;
        private string _shape = "square"; // square, round

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
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

        public string Shape
        {
            get => _shape;
            set => SetProperty(ref _shape, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Infrastructure;
    }

    /// <summary>
    /// Background image for tracing
    /// </summary>
    public class BackgroundImage : NotifyBase
    {
        private string _filePath = "";
        private string _base64Data = "";
        private double _x, _y;
        private double _width, _height;
        private double _opacity = 0.5;
        private double _scale = 1.0;
        private bool _locked = true;

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string Base64Data
        {
            get => _base64Data;
            set => SetProperty(ref _base64Data, value);
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

        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }

        public double Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        public bool Locked
        {
            get => _locked;
            set => SetProperty(ref _locked, value);
        }
    }

    /// <summary>
    /// Measurement/dimension annotation
    /// </summary>
    public class MeasurementData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private double _x1, _y1, _x2, _y2;
        private string _label = "";
        private bool _showLength = true;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public double X1
        {
            get => _x1;
            set => SetProperty(ref _x1, value);
        }

        public double Y1
        {
            get => _y1;
            set => SetProperty(ref _y1, value);
        }

        public double X2
        {
            get => _x2;
            set => SetProperty(ref _x2, value);
        }

        public double Y2
        {
            get => _y2;
            set => SetProperty(ref _y2, value);
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public bool ShowLength
        {
            get => _showLength;
            set => SetProperty(ref _showLength, value);
        }

        [JsonIgnore]
        public double Length => System.Math.Sqrt(
            (X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));
    }

    public static class WallTypes
    {
        public const string Standard = "standard";
        public const string Exterior = "exterior";
        public const string Partition = "partition";
        public const string Glass = "glass";
        public const string Safety = "safety";
    }

    public static class DoorTypes
    {
        public const string Personnel = "personnel";
        public const string Dock = "dock";
        public const string Fire = "fire";
        public const string Emergency = "emergency";
        public const string Standard = "standard"; // Legacy
        public const string Double = "double"; // Legacy
        public const string Sliding = "sliding"; // Legacy
        public const string Rollup = "rollup"; // Legacy
    }

    // SwingDirections moved to OpeningModels.cs
}
