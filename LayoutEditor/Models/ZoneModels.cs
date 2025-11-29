using System.Collections.ObjectModel;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Corridor for AGV/material transport
    /// </summary>
    public class CorridorData : NotifyBase
    {
        private string _id = "";
        private string _name = "";
        private double _width = 50;
        private string _color = "#E8E8E8";
        private bool _bidirectional = true;

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

        public ObservableCollection<PointData> Points { get; set; } = new();

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public bool Bidirectional
        {
            get => _bidirectional;
            set => SetProperty(ref _bidirectional, value);
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// Zone (restricted area, safety zone, etc.)
    /// </summary>
    public class ZoneData : NotifyBase
    {
        private string _id = "";
        private string _type = "restricted";
        private string _name = "";
        private ZoneVisual _visual = new();

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

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public ObservableCollection<PointData> Points { get; set; } = new();

        public ZoneVisual Visual
        {
            get => _visual;
            set => SetProperty(ref _visual, value);
        }
    }

    /// <summary>
    /// Zone visual properties
    /// </summary>
    public class ZoneVisual : NotifyBase
    {
        private string _fillColor = "#FF000022";
        private string _borderColor = "#FF0000";
        private string _borderStyle = "solid";

        public string FillColor
        {
            get => _fillColor;
            set => SetProperty(ref _fillColor, value);
        }

        public string BorderColor
        {
            get => _borderColor;
            set => SetProperty(ref _borderColor, value);
        }

        public string BorderStyle
        {
            get => _borderStyle;
            set => SetProperty(ref _borderStyle, value);
        }
    }
}
