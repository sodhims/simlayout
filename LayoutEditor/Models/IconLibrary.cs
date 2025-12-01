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

        public static string GetDefaultIcon(string nodeType) => nodeType switch
        {
            NodeTypes.Source => "source_arrow",
            NodeTypes.Sink => "sink_arrow",
            NodeTypes.Machine => "cnc_mill",
            NodeTypes.Buffer => "buffer_fifo",
            NodeTypes.Workstation => "workstation_manual",
            NodeTypes.Conveyor => "conveyor_belt",
            NodeTypes.Junction => "transfer_diverter",
            NodeTypes.Inspection => "inspection_visual",
            NodeTypes.Storage => "shelf_unit",
            NodeTypes.AgvStation => "agv",
            _ => "cnc_mill"
        };

        public static string GetDefaultColor(string nodeType) => nodeType switch
        {
            NodeTypes.Source => "#2ECC71",
            NodeTypes.Sink => "#E74C3C",
            NodeTypes.Machine => "#4A90D9",
            NodeTypes.Buffer => "#F5A623",
            NodeTypes.Workstation => "#3498DB",
            NodeTypes.Conveyor => "#7F8C8D",
            NodeTypes.Junction => "#3498DB",
            NodeTypes.Inspection => "#2ECC71",
            NodeTypes.Storage => "#8E44AD",
            NodeTypes.AgvStation => "#3498DB",
            _ => "#4A90D9"
        };

        public static Geometry GetGeometry(string iconKey)
        {
            if (Icons.TryGetValue(iconKey, out var icon))
            {
                try { return Geometry.Parse(icon.Path); }
                catch { return Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z"); }
            }
            return Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");
        }

        public static bool GetIsFilled(string iconKey)
        {
            if (Icons.TryGetValue(iconKey, out var icon))
                return icon.IsFilled;
            return false;
        }

        public static IEnumerable<string> GetSuggestedIcons(string nodeType)
        {
            var category = nodeType switch
            {
                NodeTypes.Source => "Flow",
                NodeTypes.Sink => "Flow",
                NodeTypes.Machine => "Machines",
                NodeTypes.Buffer => "Storage",
                NodeTypes.Workstation => "People",
                NodeTypes.Conveyor => "Transport",
                NodeTypes.Junction => "Transport",
                NodeTypes.Inspection => "Quality",
                NodeTypes.Storage => "Storage",
                NodeTypes.AgvStation => "Transport",
                _ => "Machines"
            };

            var icons = Icons.Where(kvp => kvp.Value.Category == category)
                .Select(kvp => kvp.Key).Take(15).ToList();

            // Filter for sources/sinks specifically
            if (nodeType == NodeTypes.Source)
                icons = Icons.Where(kvp => kvp.Key.Contains("source")).Select(kvp => kvp.Key).Take(10).ToList();
            else if (nodeType == NodeTypes.Sink)
                icons = Icons.Where(kvp => kvp.Key.Contains("sink") || kvp.Key.Contains("exit")).Select(kvp => kvp.Key).Take(10).ToList();

            return icons;
        }
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
