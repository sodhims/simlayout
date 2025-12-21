using System;
using System.Collections.ObjectModel;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Walkway type enumeration
    /// </summary>
    public static class WalkwayTypes
    {
        public const string Primary = "Primary";
        public const string Secondary = "Secondary";
        public const string Emergency = "Emergency";
    }

    /// <summary>
    /// Pedestrian crossing type enumeration
    /// </summary>
    public static class PedestrianCrossingTypes
    {
        public const string Zebra = "Zebra";
        public const string Signal = "Signal";
        public const string Unmarked = "Unmarked";
    }

    /// <summary>
    /// Safety zone type enumeration
    /// </summary>
    public static class SafetyZoneTypes
    {
        public const string KeepOut = "KeepOut";
        public const string HardHat = "HardHat";
        public const string HighVis = "HighVis";
        public const string Restricted = "Restricted";
    }

    /// <summary>
    /// Pedestrian walkway model
    /// </summary>
    public class WalkwayData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Walkway";
        private double _width = 1.5; // meters - typical walkway width
        private string _walkwayType = WalkwayTypes.Primary;
        private string _color = "#2ECC71"; // Green

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
        /// Centerline points defining the walkway path
        /// </summary>
        public ObservableCollection<PointData> Centerline { get; set; } = new();

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public string WalkwayType
        {
            get => _walkwayType;
            set => SetProperty(ref _walkwayType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Layer property returns Pedestrian
        /// </summary>
        public virtual LayerType ArchitectureLayer => LayerType.Pedestrian;
    }

    /// <summary>
    /// Pedestrian crossing model
    /// </summary>
    public class PedestrianCrossingData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Crossing";
        private string _crossingType = PedestrianCrossingTypes.Zebra;
        private string _color = "#F39C12"; // Orange

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
        /// Location polygon or point defining the crossing area
        /// </summary>
        public ObservableCollection<PointData> Location { get; set; } = new();

        public string CrossingType
        {
            get => _crossingType;
            set => SetProperty(ref _crossingType, value);
        }

        /// <summary>
        /// IDs of paths/aisles that this crossing crosses
        /// </summary>
        public ObservableCollection<string> CrossedEntityIds { get; set; } = new();

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Layer property returns Pedestrian
        /// </summary>
        public virtual LayerType ArchitectureLayer => LayerType.Pedestrian;
    }

    /// <summary>
    /// Safety zone model for pedestrian areas
    /// </summary>
    public class SafetyZoneData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Safety Zone";
        private string _zoneType = SafetyZoneTypes.Restricted;
        private string _color = "#E74C3C"; // Red

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
        /// Boundary polygon defining the safety zone
        /// </summary>
        public ObservableCollection<PointData> Boundary { get; set; } = new();

        public string ZoneType
        {
            get => _zoneType;
            set => SetProperty(ref _zoneType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Layer property returns Pedestrian
        /// </summary>
        public virtual LayerType ArchitectureLayer => LayerType.Pedestrian;
    }
}
