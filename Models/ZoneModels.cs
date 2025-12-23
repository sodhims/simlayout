using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using LayoutEditor.Services.Constraints;

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
    public class ZoneData : NotifyBase, IConstrainedEntity
    {
        private string _id = "";
        private string _type = "storage";
        private string _name = "";
        private ZoneVisual _visual = new();
        private int? _capacity = 100;
        private int? _maxOccupancy = 0;
        private bool? _isRestricted = false;

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

        /// <summary>
        /// Storage capacity (units)
        /// </summary>
        public int? Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }

        /// <summary>
        /// Maximum occupancy (people), 0 = unlimited
        /// </summary>
        public int? MaxOccupancy
        {
            get => _maxOccupancy;
            set => SetProperty(ref _maxOccupancy, value);
        }

        /// <summary>
        /// Whether this is a restricted access zone
        /// </summary>
        public bool? IsRestricted
        {
            get => _isRestricted;
            set => SetProperty(ref _isRestricted, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Spatial;

        // IConstrainedEntity implementation
        public bool SupportsConstrainedMovement => Points != null && Points.Count >= 3;

        public IConstraint GetConstraint()
        {
            if (Points == null || Points.Count < 3)
                return null;

            var vertices = Points.Select(p => new Point(p.X, p.Y)).ToList();
            return new PolygonConstraint(vertices);
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

    /// <summary>
    /// Primary aisle for main circulation routes
    /// </summary>
    public class PrimaryAisleData : NotifyBase
    {
        private string _id = System.Guid.NewGuid().ToString();
        private string _name = "Aisle";
        private double _width = 120; // inches
        private bool _isEmergencyRoute = false;
        private string _aisleType = AisleTypes.Main;

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
        /// Centerline of the aisle as a polyline
        /// </summary>
        public ObservableCollection<PointData> Centerline { get; set; } = new();

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public bool IsEmergencyRoute
        {
            get => _isEmergencyRoute;
            set => SetProperty(ref _isEmergencyRoute, value);
        }

        public string AisleType
        {
            get => _aisleType;
            set => SetProperty(ref _aisleType, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Spatial;
    }

    public static class AisleTypes
    {
        public const string Main = "main";
        public const string Secondary = "secondary";
        public const string Emergency = "emergency";
    }

    /// <summary>
    /// Restricted area with access controls
    /// </summary>
    public class RestrictedAreaData : NotifyBase
    {
        private string _id = System.Guid.NewGuid().ToString();
        private string _name = "Restricted Area";
        private double _x, _y;
        private double _width = 100;
        private double _height = 100;
        private string _restrictionType = RestrictionTypes.AuthorizedOnly;
        private string _requiredPPE = "";

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

        public string RestrictionType
        {
            get => _restrictionType;
            set => SetProperty(ref _restrictionType, value);
        }

        public string RequiredPPE
        {
            get => _requiredPPE;
            set => SetProperty(ref _requiredPPE, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Spatial;
    }

    public static class RestrictionTypes
    {
        public const string AuthorizedOnly = "authorized_only";
        public const string Hazmat = "hazmat";
        public const string Cleanroom = "cleanroom";
        public const string HighVoltage = "high_voltage";
    }
}
