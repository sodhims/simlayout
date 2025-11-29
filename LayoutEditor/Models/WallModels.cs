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

        [JsonIgnore]
        public double Length => System.Math.Sqrt(
            (X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));
    }

    /// <summary>
    /// Represents a door or opening in a wall
    /// </summary>
    public class DoorData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _wallId = "";
        private double _position = 0.5; // 0-1 along wall
        private double _width = 36;
        private string _doorType = DoorTypes.Standard;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
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

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public string DoorType
        {
            get => _doorType;
            set => SetProperty(ref _doorType, value);
        }
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
        public const string Standard = "standard";
        public const string Double = "double";
        public const string Sliding = "sliding";
        public const string Rollup = "rollup";
        public const string Emergency = "emergency";
    }
}
