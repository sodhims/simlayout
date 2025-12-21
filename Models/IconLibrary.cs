using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using LayoutEditor.Icons;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Central library aggregating all icon categories from Icons folder
    /// </summary>
    public static class IconLibrary
    {
        public static readonly Dictionary<string, IconDefinition> Icons;

        static IconLibrary()
        {
            Icons = new Dictionary<string, IconDefinition>();
            
            // Load all icon categories from separate files
            LoadCategory(MachineIcons.All, "Machines");
            LoadCategory(TransportIcons.All, "Transport");
            LoadCategory(StorageIcons.All, "Storage");
            LoadCategory(RobotIcons.All, "Robots");
            LoadCategory(QualityIcons.All, "Quality");
            LoadCategory(SourceSinkIcons.All, "Flow");
            LoadCategory(PeopleIcons.All, "People");
            LoadCategory(UtilityIcons.All, "Utility");
        }

        private static void LoadCategory(Dictionary<string, IconDef> icons, string category)
        {
            foreach (var kvp in icons)
            {
                Icons[kvp.Key] = new IconDefinition
                {
                    Name = kvp.Value.Name,
                    Category = category,
                    Path = kvp.Value.Path,
                    DefaultColor = kvp.Value.Color,
                    IsFilled = kvp.Value.Filled
                };
            }
        }

        public static IEnumerable<string> GetCategories() =>
            Icons.Values.Select(i => i.Category).Distinct().OrderBy(c => c);

        public static IEnumerable<KeyValuePair<string, IconDefinition>> GetByCategory(string category) =>
            Icons.Where(kvp => kvp.Value.Category == category);

        /// <summary>
        /// Gets the default icon key for a node type
        /// </summary>
        public static string GetDefaultIcon(string nodeType) => nodeType?.ToLower() switch
        {
            // Flow
            NodeTypes.Source or "source" => "source_arrow",
            NodeTypes.Sink or "sink" => "sink_arrow",
            
            // Processing
            NodeTypes.Machine or "machine" => "cnc_mill",
            "workstation" => "workstation_manual",
            "assembly" => "assembly_station",
            "inspection" => "inspection_visual",
            "robot" => "operator",  // From PeopleIcons since RobotIcons might not have this
            "printer3d" => "printer_3d",
            
            // Storage
            NodeTypes.Buffer or "buffer" => "buffer_fifo",
            NodeTypes.Storage or "storage" => "shelf_unit",
            "rack" => "shelf_unit",
            "pallet" => "pallet_jack",
            
            // Transport
            NodeTypes.Conveyor or "conveyor" => "conveyor_belt",
            NodeTypes.Junction or "junction" => "transfer_diverter",
            NodeTypes.AgvStation or "agv_station" => "agv",
            "agv" => "agv",
            "elevator" => "elevator",
            "dock" => "sink_dock",
            
            // People
            "operator" => "operator",
            "crew" => "team",
            
            // Decision/Routing
            "decision" => "transfer_diverter",
            
            _ => "cnc_mill"
        };

        /// <summary>
        /// Gets the default icon key for a transport station type
        /// </summary>
        public static string GetStationIcon(string stationType) => stationType?.ToLower() switch
        {
            "pickup" => "station_pickup",
            "dropoff" => "station_dropoff",
            "home" => "station_home",
            "buffer" => "station_buffer",
            "crossing" => "station_crossing",
            "waypoint" => "waypoint",
            _ => "station_pickup"
        };

        /// <summary>
        /// Gets the default color for a node type
        /// </summary>
        public static string GetDefaultColor(string nodeType) => nodeType?.ToLower() switch
        {
            NodeTypes.Source or "source" => "#2ECC71",
            NodeTypes.Sink or "sink" => "#E74C3C",
            NodeTypes.Machine or "machine" => "#4A90D9",
            NodeTypes.Buffer or "buffer" => "#F5A623",
            "workstation" => "#3498DB",
            NodeTypes.Conveyor or "conveyor" => "#7F8C8D",
            NodeTypes.Junction or "junction" => "#3498DB",
            "decision" => "#9B59B6",
            NodeTypes.Inspection or "inspection" => "#2ECC71",
            NodeTypes.Storage or "storage" => "#8E44AD",
            NodeTypes.AgvStation or "agv_station" => "#3498DB",
            "agv" => "#3498DB",
            "assembly" => "#16A085",
            "robot" => "#E67E22",
            "printer3d" => "#3498DB",
            "rack" => "#8E44AD",
            "pallet" => "#95A5A6",
            "operator" => "#3498DB",
            "crew" => "#3498DB",
            "elevator" => "#E74C3C",
            "dock" => "#27AE60",
            _ => "#4A90D9"
        };

        /// <summary>
        /// Gets the default color for a transport station type
        /// </summary>
        public static string GetStationColor(string stationType) => stationType?.ToLower() switch
        {
            "pickup" => "#27AE60",
            "dropoff" => "#E74C3C",
            "home" => "#F39C12",
            "buffer" => "#9B59B6",
            "crossing" => "#3498DB",
            "waypoint" => "#95A5A6",
            _ => "#9B59B6"
        };

        /// <summary>
        /// Parses a geometry path string into a Geometry object
        /// </summary>
        public static Geometry GetGeometry(string iconKey)
        {
            if (Icons.TryGetValue(iconKey, out var icon))
            {
                try { return Geometry.Parse(icon.Path); }
                catch { return Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z"); }
            }
            return Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");
        }

        /// <summary>
        /// Gets whether an icon should be filled vs stroked
        /// </summary>
        public static bool GetIsFilled(string iconKey)
        {
            if (Icons.TryGetValue(iconKey, out var icon))
                return icon.IsFilled;
            return false;
        }

        /// <summary>
        /// Gets suggested icons for a node type (for icon picker dropdown)
        /// </summary>
        public static IEnumerable<string> GetSuggestedIcons(string nodeType)
        {
            var category = nodeType?.ToLower() switch
            {
                NodeTypes.Source or "source" => "Flow",
                NodeTypes.Sink or "sink" => "Flow",
                NodeTypes.Machine or "machine" => "Machines",
                NodeTypes.Buffer or "buffer" => "Storage",
                "workstation" => "People",
                NodeTypes.Conveyor or "conveyor" => "Transport",
                NodeTypes.Junction or "junction" => "Transport",
                NodeTypes.Inspection or "inspection" => "Quality",
                NodeTypes.Storage or "storage" => "Storage",
                NodeTypes.AgvStation or "agv_station" => "Transport",
                "robot" => "Machines",
                "assembly" => "Machines",
                _ => "Machines"
            };

            var icons = Icons.Where(kvp => kvp.Value.Category == category)
                .Select(kvp => kvp.Key).Take(15).ToList();

            // Filter for sources/sinks specifically
            if (nodeType == NodeTypes.Source || nodeType == "source")
                icons = Icons.Where(kvp => kvp.Key.Contains("source")).Select(kvp => kvp.Key).Take(10).ToList();
            else if (nodeType == NodeTypes.Sink || nodeType == "sink")
                icons = Icons.Where(kvp => kvp.Key.Contains("sink") || kvp.Key.Contains("exit")).Select(kvp => kvp.Key).Take(10).ToList();

            return icons;
        }

        /// <summary>
        /// Gets the display name for a node type
        /// </summary>
        public static string GetNodeTypeName(string nodeType) => nodeType?.ToLower() switch
        {
            "source" => "Source",
            "sink" => "Sink",
            "machine" => "Machine",
            "buffer" => "Buffer",
            "workstation" => "Workstation",
            "conveyor" => "Conveyor",
            "junction" => "Junction",
            "decision" => "Decision",
            "inspection" => "Inspection",
            "storage" => "Storage",
            "agv_station" => "AGV Station",
            "agv" => "AGV",
            "assembly" => "Assembly",
            "robot" => "Robot",
            "printer3d" => "3D Printer",
            "rack" => "Rack",
            "pallet" => "Pallet",
            "operator" => "Operator",
            "crew" => "Crew",
            "elevator" => "Elevator",
            "dock" => "Loading Dock",
            _ => nodeType ?? "Node"
        };

        /// <summary>
        /// Gets the display name for a transport station type
        /// </summary>
        public static string GetStationTypeName(string stationType) => stationType?.ToLower() switch
        {
            "pickup" => "Pickup Station",
            "dropoff" => "Dropoff Station",
            "home" => "Home/Charging",
            "buffer" => "Buffer Station",
            "crossing" => "Track Crossing",
            "waypoint" => "Waypoint",
            _ => stationType ?? "Station"
        };
    }

    public class IconDefinition
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Path { get; set; } = "";
        public string DefaultColor { get; set; } = "#4A90D9";
        public bool IsFilled { get; set; } = false;
    }
}
