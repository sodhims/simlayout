using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Utility, infrastructure, and facility icons
    /// </summary>
    public static class UtilityIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // Utilities
            ["electrical_panel"] = new("Electrical Panel", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L16,6 L16,10 L8,10 Z M8,12 L10,12 M8,14 L10,14 M8,16 L10,16 M12,12 A1,1 0 1,0 14,12 M12,14 A1,1 0 1,0 14,14 M16,12 A1,1 0 1,0 18,12", "#F5A623"),
            ["compressor"] = new("Air Compressor", "M4,8 L16,8 L16,16 L4,16 Z M16,12 L20,12 M20,10 L20,14 M20,10 L22,10 M20,14 L22,14 M8,10 A2,2 0 1,0 8,14 M12,10 L12,14", "#3498DB"),
            ["hvac"] = new("HVAC Unit", "M4,6 L20,6 L20,18 L4,18 Z M8,8 L16,8 L16,12 L8,12 Z M10,14 L10,16 M14,14 L14,16 M12,10 A2,2 0 1,0 14,10", "#3498DB"),
            ["chiller"] = new("Chiller", "M4,4 L20,4 L20,20 L4,20 Z M8,6 A4,4 0 1,0 8,14 M16,6 A4,4 0 1,0 16,14 M6,16 L18,16 M6,18 L18,18", "#3498DB"),
            ["pump"] = new("Pump", "M4,12 A4,4 0 1,0 12,12 A4,4 0 1,0 4,12 M12,12 L20,12 M20,10 L20,14 M20,10 L22,10 M20,14 L22,14 M8,12 L8,8 M8,12 L8,16", "#3498DB"),
            ["transformer"] = new("Transformer", "M8,4 L8,20 M16,4 L16,20 M8,6 Q12,8 8,10 Q12,12 8,14 Q12,16 8,18 M16,6 Q12,8 16,10 Q12,12 16,14 Q12,16 16,18", "#F5A623"),
            ["ups"] = new("UPS", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L16,6 L16,10 L8,10 Z M10,12 L14,12 L14,18 L10,18 Z M10,14 L14,14", "#F5A623"),
            
            // Safety Equipment
            ["fire_extinguisher"] = new("Fire Extinguisher", "M10,4 L14,4 L14,8 L10,8 Z M8,8 L16,8 L16,18 L8,18 Z M12,2 L12,4 M10,18 L10,20 L14,20 L14,18 M12,10 L12,16", "#E74C3C"),
            ["emergency_stop"] = new("Emergency Stop", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 A4,4 0 1,0 12,16 A4,4 0 1,0 12,8", "#E74C3C", true),
            ["safety_shower"] = new("Safety Shower", "M10,4 L14,4 L14,8 L18,8 L18,10 L6,10 L6,8 L10,8 Z M12,10 L12,14 M8,14 L16,14 Q16,20 12,20 Q8,20 8,14", "#2ECC71"),
            ["eyewash"] = new("Eye Wash", "M4,10 L20,10 L20,14 L4,14 Z M8,14 L8,18 M16,14 L16,18 M8,10 L8,6 Q12,4 16,6 L16,10 M12,14 L12,20", "#2ECC71"),
            ["barrier"] = new("Safety Barrier", "M2,8 L22,8 L22,12 L2,12 Z M4,4 L4,8 M10,4 L10,8 M14,4 L14,8 M20,4 L20,8 M4,12 L4,16 M20,12 L20,16", "#F5A623"),
            ["guard"] = new("Machine Guard", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 M6,10 L18,10 M6,14 L18,14 M6,18 L18,18 M8,4 L8,20 M12,4 L12,20 M16,4 L16,20", "#F5A623"),
            
            // Floors & Areas
            ["floor_area"] = new("Floor Area", "M4,4 L20,4 L20,20 L4,20 Z M4,4 L20,20 M4,20 L20,4", "#CCCCCC"),
            ["restricted"] = new("Restricted Area", "M4,4 L20,4 L20,20 L4,20 Z M8,8 L16,16 M16,8 L8,16 M6,6 L18,6 L18,18 L6,18 Z", "#E74C3C"),
            ["clean_room"] = new("Clean Room", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 L18,18 L6,18 Z M10,10 L14,10 L14,14 L10,14 Z", "#3498DB"),
            ["loading_zone"] = new("Loading Zone", "M4,4 L20,4 L20,20 L4,20 Z M4,8 L20,8 M4,12 L20,12 M4,16 L20,16 M8,4 L8,20 M16,4 L16,20", "#F5A623"),
            ["aisle"] = new("Aisle", "M8,2 L8,22 M16,2 L16,22 M8,6 L16,6 M8,12 L16,12 M8,18 L16,18 M10,4 L14,4 M10,10 L14,10 M10,16 L14,16", "#7F8C8D"),
            
            // Doors & Access
            ["door"] = new("Door", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L16,6 L16,18 L8,18 Z M14,12 L16,12", "#7F8C8D"),
            ["door_auto"] = new("Auto Door", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L11,6 L11,18 L8,18 Z M13,6 L16,6 L16,18 L13,18 Z M11,10 L13,12 L11,14 M13,10 L11,12 L13,14", "#7F8C8D"),
            ["airlock"] = new("Airlock", "M2,6 L10,6 L10,18 L2,18 Z M14,6 L22,6 L22,18 L14,18 Z M10,12 L14,12 M4,10 L8,10 M4,14 L8,14 M16,10 L20,10 M16,14 L20,14", "#3498DB"),
            ["gate_security"] = new("Security Gate", "M4,4 L4,20 M20,4 L20,20 M4,4 L20,4 M4,20 L20,20 M8,8 L16,8 L16,16 L8,16 Z M12,12 A2,2 0 1,0 14,12", "#7F8C8D"),
            
            // Columns & Structures
            ["column"] = new("Column", "M8,4 L16,4 L16,20 L8,20 Z M10,6 L14,6 M10,18 L14,18 M10,10 L14,10 M10,14 L14,14", "#7F8C8D"),
            ["wall"] = new("Wall", "M4,8 L20,8 L20,16 L4,16 Z", "#7F8C8D", true),
            ["mezzanine"] = new("Mezzanine", "M2,16 L22,16 M4,16 L4,8 M20,16 L20,8 M4,8 L20,8 M8,8 L8,4 L16,4 L16,8 M6,12 L18,12", "#7F8C8D"),
            ["stairs"] = new("Stairs", "M4,20 L8,20 L8,16 L12,16 L12,12 L16,12 L16,8 L20,8 L20,4 M4,20 L4,16 L8,16 M8,16 L8,12 L12,12 M12,12 L12,8 L16,8 M16,8 L16,4 L20,4", "#7F8C8D"),
            
            // Misc
            ["trash"] = new("Trash Bin", "M6,6 L18,6 L16,20 L8,20 Z M4,6 L20,6 M10,4 L14,4 M9,10 L9,16 M12,10 L12,16 M15,10 L15,16", "#7F8C8D"),
            ["recycling"] = new("Recycling", "M12,4 L8,10 L10,10 L6,16 L12,16 L12,20 M12,4 L16,10 L14,10 L18,16 L12,16 M8,16 L8,20 L16,20 L16,16", "#2ECC71"),
            ["clock"] = new("Time Clock", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 L12,12 L16,12 M12,6 L12,7 M12,17 L12,18 M6,12 L7,12 M17,12 L18,12", "#7F8C8D"),
            ["wifi"] = new("WiFi Point", "M12,18 A2,2 0 1,0 12,22 M8,14 A6,6 0 0,1 16,14 M4,10 A10,10 0 0,1 20,10", "#3498DB"),
        };
    }
}
