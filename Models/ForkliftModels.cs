using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    #region Forklift Aisle

    /// <summary>
    /// Forklift aisle type classifications
    /// </summary>
    public static class ForkliftAisleTypes
    {
        public const string Main = "main";
        public const string Secondary = "secondary";
        public const string Deadend = "deadend";
    }

    /// <summary>
    /// Forklift aisle with centerline path and width
    /// </summary>
    public class ForkliftAisleData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Aisle";
        private List<PointData> _centerline = new();
        private double _width = 3.0;
        private double _minTurningRadius = 2.0;
        private string _aisleType = ForkliftAisleTypes.Main;
        private string _color = "#90EE90";

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
        /// Centerline path defining the aisle route
        /// </summary>
        public List<PointData> Centerline
        {
            get => _centerline;
            set => SetProperty(ref _centerline, value);
        }

        /// <summary>
        /// Width of the aisle (meters)
        /// </summary>
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, Math.Max(1.0, value));
        }

        /// <summary>
        /// Minimum turning radius for forklifts in this aisle (meters)
        /// </summary>
        public double MinTurningRadius
        {
            get => _minTurningRadius;
            set => SetProperty(ref _minTurningRadius, Math.Max(0.5, value));
        }

        /// <summary>
        /// Aisle type: Main, Secondary, or Deadend
        /// </summary>
        public string AisleType
        {
            get => _aisleType;
            set => SetProperty(ref _aisleType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.FlexibleTransport;
    }

    #endregion

    #region Staging Area

    /// <summary>
    /// Staging area type classifications
    /// </summary>
    public static class StagingTypes
    {
        public const string Temporary = "temporary";
        public const string Buffer = "buffer";
        public const string Dock = "dock";
    }

    /// <summary>
    /// Staging area for forklift operations (pallet storage, loading docks)
    /// </summary>
    public class StagingAreaData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Staging";
        private List<PointData> _boundary = new();
        private int _capacity = 10;
        private string _stagingType = StagingTypes.Buffer;
        private string _color = "#FFD700";

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
        /// Polygon boundary defining the staging area
        /// </summary>
        public List<PointData> Boundary
        {
            get => _boundary;
            set => SetProperty(ref _boundary, value);
        }

        /// <summary>
        /// Capacity in number of pallets/units
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, Math.Max(1, value));
        }

        /// <summary>
        /// Staging type: Temporary, Buffer, or Dock
        /// </summary>
        public string StagingType
        {
            get => _stagingType;
            set => SetProperty(ref _stagingType, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.FlexibleTransport;
    }

    #endregion

    #region Crossing Zone

    /// <summary>
    /// Crossing zone type classifications
    /// </summary>
    public static class CrossingTypes
    {
        public const string SignalControlled = "signal_controlled";
        public const string PriorityBased = "priority_based";
        public const string TimeWindowed = "time_windowed";
    }

    /// <summary>
    /// Crossing zone where forklift aisle intersects with AGV path
    /// </summary>
    public class CrossingZoneData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Crossing";
        private double _x;
        private double _y;
        private string _aisleId = "";
        private string _agvPathId = "";
        private string _crossingType = CrossingTypes.PriorityBased;
        private List<PointData> _boundary = new();
        private string _color = "#FFA500";

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
        /// Center X coordinate of crossing
        /// </summary>
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        /// <summary>
        /// Center Y coordinate of crossing
        /// </summary>
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        /// <summary>
        /// ID of the forklift aisle that crosses here
        /// </summary>
        public string AisleId
        {
            get => _aisleId;
            set => SetProperty(ref _aisleId, value);
        }

        /// <summary>
        /// ID of the AGV path that crosses here
        /// </summary>
        public string AGVPathId
        {
            get => _agvPathId;
            set => SetProperty(ref _agvPathId, value);
        }

        /// <summary>
        /// Crossing control type: SignalControlled, PriorityBased, or TimeWindowed
        /// </summary>
        public string CrossingType
        {
            get => _crossingType;
            set => SetProperty(ref _crossingType, value);
        }

        /// <summary>
        /// Optional polygon boundary for the crossing zone
        /// </summary>
        public List<PointData> Boundary
        {
            get => _boundary;
            set => SetProperty(ref _boundary, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.FlexibleTransport;
    }

    #endregion
}
