using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Quality, inspection, and testing icons
    /// </summary>
    public static class QualityIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // Inspection Stations
            ["inspection_visual"] = new("Visual Inspection", "M4,8 L20,8 L20,16 L4,16 Z M12,10 A2,2 0 1,0 12,14 M8,12 L10,12 M14,12 L16,12 M2,12 L4,12 M20,12 L22,12", "#2ECC71"),
            ["inspection_auto"] = new("Auto Inspection", "M4,6 L20,6 L20,18 L4,18 Z M8,8 A3,3 0 1,0 8,14 M14,8 L18,8 L18,10 M14,12 L18,12 M14,14 L16,14", "#2ECC71"),
            ["inspection_xray"] = new("X-Ray Inspection", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L16,6 L16,14 L8,14 Z M10,16 L10,18 M14,16 L14,18 M12,8 L12,12 M10,10 L14,10", "#9B59B6"),
            ["inspection_cmm"] = new("CMM", "M4,4 L20,4 M4,4 L4,20 M4,20 L20,20 M10,8 L10,16 M10,12 L16,12 M16,12 L16,16 M16,16 L14,18 M16,16 L18,18", "#3498DB"),
            
            // Testing Equipment
            ["test_leak"] = new("Leak Tester", "M6,6 L18,6 L18,18 L6,18 Z M10,8 L14,8 L14,12 L10,12 Z M12,12 L12,16 M8,14 Q12,18 16,14", "#3498DB"),
            ["test_pressure"] = new("Pressure Tester", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 L12,12 L16,12 M8,12 L10,12 M12,14 L12,16", "#E67E22"),
            ["test_electrical"] = new("Electrical Tester", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L16,6 L16,10 L8,10 Z M8,12 L10,12 M8,14 L10,14 M12,12 L14,12 M12,14 L14,14 M10,16 L14,16 L14,18 L10,18", "#F5A623"),
            ["test_functional"] = new("Functional Tester", "M4,6 L20,6 L20,18 L4,18 Z M6,8 L18,8 L18,12 L6,12 Z M8,14 L10,14 L10,16 L8,16 M12,14 A1,1 0 1,0 14,14 M16,14 A1,1 0 1,0 18,14", "#2ECC71"),
            ["test_endurance"] = new("Endurance Tester", "M4,8 L20,8 L20,16 L4,16 Z M8,4 L8,8 M16,4 L16,8 M6,10 L6,14 M10,10 L10,14 M14,10 L14,14 M18,10 L18,14 M12,8 L12,4 A2,2 0 1,1 14,4", "#E67E22"),
            ["test_vibration"] = new("Vibration Tester", "M4,14 L20,14 L20,18 L4,18 Z M6,10 L6,14 M18,10 L18,14 M8,10 L8,14 M16,10 L16,14 M10,6 L14,6 L14,14 L10,14 Z M6,8 Q12,4 18,8", "#E67E22"),
            ["test_climate"] = new("Climate Chamber", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 L18,16 L6,16 Z M8,8 L8,10 M16,8 L16,10 M10,12 L14,12 M8,18 L10,18 M14,18 L16,18", "#3498DB"),
            
            // Measurement
            ["gauge_dial"] = new("Dial Gauge", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 L12,12 L16,10 M10,14 L14,14 M10,16 L14,16", "#7F8C8D"),
            ["gauge_digital"] = new("Digital Gauge", "M4,8 L20,8 L20,16 L4,16 Z M6,10 L18,10 L18,14 L6,14 Z M8,11 L8,13 M10,11 L10,13 M13,11 L13,13 M15,11 L15,13 M17,11 L17,13", "#7F8C8D"),
            ["caliper"] = new("Caliper", "M2,10 L22,10 M2,14 L22,14 M2,10 L2,18 M10,6 L10,18 M6,14 L6,18 M14,10 L14,6 M18,10 L18,8", "#7F8C8D"),
            ["micrometer"] = new("Micrometer", "M4,10 L4,14 L10,14 L10,10 Z M10,12 L18,12 M18,8 L18,16 M20,10 L22,10 M20,14 L22,14 M18,10 A2,2 0 0,1 18,14", "#7F8C8D"),
            ["scale_weight"] = new("Weighing Scale", "M4,18 L20,18 L20,22 L4,22 Z M6,14 L18,14 L18,18 L6,18 Z M12,6 A4,4 0 1,0 12,14 M12,4 L12,6 M10,10 L14,10", "#7F8C8D"),
            
            // Quality Gates
            ["gate_pass"] = new("Pass Gate", "M4,4 L20,4 L20,20 L4,20 Z M8,10 L10,12 L16,6 M8,16 L16,16", "#2ECC71"),
            ["gate_fail"] = new("Fail Gate", "M4,4 L20,4 L20,20 L4,20 Z M8,8 L16,16 M16,8 L8,16 M8,18 L16,18", "#E74C3C"),
            ["gate_rework"] = new("Rework Gate", "M4,4 L20,4 L20,20 L4,20 Z M8,12 A4,4 0 1,0 16,12 M14,8 L16,12 L14,16 M8,16 L16,16", "#F5A623"),
            ["gate_scrap"] = new("Scrap Gate", "M6,6 L18,6 L16,20 L8,20 Z M4,6 L20,6 M10,4 L14,4 M9,10 L9,16 M12,10 L12,16 M15,10 L15,16", "#E74C3C"),
            
            // SPC & Monitoring
            ["spc_chart"] = new("SPC Chart", "M4,4 L4,20 L20,20 M6,16 L10,12 L14,14 L18,8 M6,10 L18,10 M6,14 L18,14", "#4A90D9"),
            ["andon"] = new("Andon Light", "M8,2 L16,2 L16,22 L8,22 Z M10,4 L14,4 L14,8 L10,8 Z M10,10 L14,10 L14,14 L10,14 Z M10,16 L14,16 L14,20 L10,20 Z", "#E74C3C"),
            ["monitor"] = new("Monitor Display", "M4,4 L20,4 L20,16 L4,16 Z M6,6 L18,6 L18,14 L6,14 Z M10,18 L14,18 L14,20 L10,20 M8,20 L16,20", "#4A90D9"),
            ["label_printer"] = new("Label Printer", "M4,6 L20,6 L20,14 L4,14 Z M6,8 L18,8 M6,10 L14,10 M6,12 L12,12 M16,14 L16,20 L20,20 L20,14", "#7F8C8D"),
            ["marking"] = new("Marking Station", "M6,4 L18,4 L18,12 L6,12 Z M12,12 L12,20 M10,16 L14,16 M8,20 L16,20 M8,6 L16,6 M8,8 L12,8 M8,10 L10,10", "#7F8C8D"),
        };
    }
}
