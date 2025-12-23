using System.Collections.ObjectModel;
using LayoutEditor.Models;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Root container for all layout data
    /// </summary>
    public partial class LayoutData : NotifyBase
    {
        private string _id = System.Guid.NewGuid().ToString();
        private LayoutMetadata _metadata = new();
        private CanvasSettings _canvas = new();
        private DisplaySettings _display = new();
        private BackgroundImage? _background;
        private LayerManager _layerManager = new();
        private bool _frictionlessMode = false;
        private bool _designMode = false;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public LayoutMetadata Metadata
        {
            get => _metadata;
            set => SetProperty(ref _metadata, value);
        }

        public CanvasSettings Canvas
        {
            get => _canvas;
            set => SetProperty(ref _canvas, value);
        }

        public DisplaySettings Display
        {
            get => _display;
            set => SetProperty(ref _display, value);
        }

        public BackgroundImage? Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        public LayerManager LayerManager
        {
            get => _layerManager;
            set => SetProperty(ref _layerManager, value);
        }

        /// <summary>
        /// Frictionless mode: enables constrained movement for cranes, AGVs, etc.
        /// </summary>
        public bool FrictionlessMode
        {
            get => _frictionlessMode;
            set => SetProperty(ref _frictionlessMode, value);
        }

        /// <summary>
        /// Design mode: unlocks all entities for free movement and editing
        /// </summary>
        public bool DesignMode
        {
            get => _designMode;
            set => SetProperty(ref _designMode, value);
        }

        // Core simulation elements
        public ObservableCollection<NodeData> Nodes { get; set; } = new();
        public ObservableCollection<PathData> Paths { get; set; } = new();
        public ObservableCollection<GroupData> Groups { get; set; } = new();

        // Local flow elements (Week 3)
        public ObservableCollection<ConveyorData> Conveyors { get; set; } = new();
        public ObservableCollection<DirectPathData> DirectPaths { get; set; } = new();

        // Guided transport elements (Week 4 - AGV)
        public ObservableCollection<AGVWaypointData> AGVWaypoints { get; set; } = new();
        public ObservableCollection<AGVPathData> AGVPaths { get; set; } = new();
        public ObservableCollection<AGVStationData> AGVStations { get; set; } = new();
        public ObservableCollection<TrafficZoneData> TrafficZones { get; set; } = new();

        // Building elements
        public ObservableCollection<WallData> Walls { get; set; } = new();
        public ObservableCollection<DoorData> Doors { get; set; } = new(); // Legacy - migrate to Openings
        public ObservableCollection<OpeningData> Openings { get; set; } = new(); // New generalized opening model
        public ObservableCollection<ColumnData> Columns { get; set; } = new();
        public ObservableCollection<MeasurementData> Measurements { get; set; } = new();

        // Zones and corridors
        public ObservableCollection<ZoneData> Zones { get; set; } = new();
        public ObservableCollection<CorridorData> Corridors { get; set; } = new();
        public ObservableCollection<PrimaryAisleData> PrimaryAisles { get; set; } = new();
        public ObservableCollection<RestrictedAreaData> RestrictedAreas { get; set; } = new();

        // Transport (AGV/forklift)
        public ObservableCollection<TransportStationData> TransportStations { get; set; } = new();
        public ObservableCollection<WaypointData> Waypoints { get; set; } = new();
        public ObservableCollection<TransporterTrackData> TransporterTracks { get; set; } = new();
        public ObservableCollection<TransporterData> Transporters { get; set; } = new();
        public ObservableCollection<TransportNetworkData> TransportNetworks { get; set; } = new();

        // Cranes
        public ObservableCollection<RunwayData> Runways { get; set; } = new();
        public ObservableCollection<EOTCraneData> EOTCranes { get; set; } = new();
        public ObservableCollection<JibCraneData> JibCranes { get; set; } = new();
        public ObservableCollection<HandoffPointData> HandoffPoints { get; set; } = new();
        public ObservableCollection<DropZoneData> DropZones { get; set; } = new();

        // Forklifts
        public ObservableCollection<ForkliftAisleData> ForkliftAisles { get; set; } = new();
        public ObservableCollection<StagingAreaData> StagingAreas { get; set; } = new();
        public ObservableCollection<CrossingZoneData> CrossingZones { get; set; } = new();

        // Templates
        public ObservableCollection<TemplateData> Templates { get; set; } = new();

        // Pedestrian elements (Layer 7)
        public ObservableCollection<WalkwayData> Walkways { get; set; } = new();
        public ObservableCollection<PedestrianCrossingData> PedestrianCrossings { get; set; } = new();
        public ObservableCollection<SafetyZoneData> SafetyZones { get; set; } = new();
        public ObservableCollection<ZoneData> PedestrianMesh { get; set; } = new(); // Generated walkable areas

        // Conflict management (Layer 8)
        public ObservableCollection<ConflictResolutionData> ConflictResolutions { get; set; } = new();
        public ObservableCollection<LayerConnectionData> LayerConnections { get; set; } = new();

                #region Transport Marker System

        /// <summary>
        /// Transport markers (backbone junctions) - orange diamonds
        /// </summary>
        public ObservableCollection<TransportMarker> TransportMarkers { get; set; } = new();

        /// <summary>
        /// Transport links (machine terminal → transport path)
        /// </summary>
        public ObservableCollection<TransportLink> TransportLinks { get; set; } = new();

        /// <summary>
        /// Transport groups (resource collections)
        /// </summary>
        public ObservableCollection<TransportGroup> TransportGroups { get; set; } = new();

        #endregion
    }

    /// <summary>
    /// A saved template (group of nodes/paths)
    /// </summary>
    public class TemplateData : NotifyBase
    {
        private string _id = System.Guid.NewGuid().ToString();
        private string _name = "Template";
        private string _category = "Custom";
        private string _description = "";

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

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public System.Collections.Generic.List<NodeData> Nodes { get; set; } = new();
        public System.Collections.Generic.List<PathData> Paths { get; set; } = new();
        public System.Collections.Generic.List<GroupData> Groups { get; set; } = new();
    }
}
