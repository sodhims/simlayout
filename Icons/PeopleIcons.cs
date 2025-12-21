using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// People, operators, and workstation icons
    /// </summary>
    public static class PeopleIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // People
            ["operator"] = new("Operator", "M12,2 A3,3 0 1,0 12,8 M12,8 L12,14 M8,10 L16,10 M12,14 L8,22 M12,14 L16,22", "#3498DB"),
            ["operator_seated"] = new("Seated Operator", "M12,2 A3,3 0 1,0 12,8 M12,8 L12,14 M8,10 L16,10 M10,14 L6,14 L6,20 M14,14 L18,14 L18,20 M8,20 L16,20", "#3498DB"),
            ["technician"] = new("Technician", "M12,2 A3,3 0 1,0 12,8 M12,8 L12,14 M8,10 L16,10 M12,14 L8,22 M12,14 L16,22 M6,8 L4,10 L4,14 M18,8 L20,10 L20,14", "#9B59B6"),
            ["supervisor"] = new("Supervisor", "M12,2 A3,3 0 1,0 12,8 M12,8 L12,14 M8,10 L16,10 M12,14 L8,22 M12,14 L16,22 M8,2 L16,2 M12,0 L12,2", "#E67E22"),
            ["team"] = new("Team", "M8,4 A2,2 0 1,0 8,8 M8,8 L8,12 M6,10 L10,10 M8,12 L6,16 M8,12 L10,16 M16,4 A2,2 0 1,0 16,8 M16,8 L16,12 M14,10 L18,10 M16,12 L14,16 M16,12 L18,16 M12,10 L12,20", "#3498DB"),
            ["forklift_driver"] = new("Forklift Driver", "M12,4 A2,2 0 1,0 12,8 M12,8 L12,12 M10,10 L14,10 M8,12 L16,12 L16,18 L8,18 Z M10,14 L14,14 L14,16 L10,16", "#F5A623"),
            
            // Workstations
            ["workstation_manual"] = new("Manual Workstation", "M4,10 L20,10 L20,18 L4,18 Z M6,6 L10,6 L10,10 M14,6 L18,6 L18,10 M12,6 A2,2 0 1,0 12,10 M8,12 L8,16 M16,12 L16,16", "#3498DB"),
            ["workstation_computer"] = new("Computer Workstation", "M4,6 L20,6 L20,14 L4,14 Z M6,8 L18,8 L18,12 L6,12 Z M10,14 L10,18 L14,18 L14,14 M8,18 L16,18 L16,20 L8,20", "#3498DB"),
            ["workstation_standing"] = new("Standing Workstation", "M4,8 L20,8 L20,12 L4,12 Z M6,12 L6,20 M18,12 L18,20 M6,20 L18,20 M10,4 L14,4 L14,8 L10,8", "#3498DB"),
            ["workbench"] = new("Workbench", "M4,12 L20,12 L20,16 L4,16 Z M6,16 L6,22 M18,16 L18,22 M8,8 L16,8 L16,12 L8,12 Z M10,4 L14,4 L14,8", "#7F8C8D"),
            ["assembly_bench"] = new("Assembly Bench", "M2,12 L22,12 L22,16 L2,16 Z M4,16 L4,20 M20,16 L20,20 M6,8 L10,8 L10,12 M14,8 L18,8 L18,12 M12,4 L12,12", "#2ECC71"),
            
            // Desks & Offices
            ["desk"] = new("Desk", "M4,10 L20,10 L20,14 L4,14 Z M6,14 L6,20 M18,14 L18,20 M8,6 L16,6 L16,10 L8,10 Z", "#7F8C8D"),
            ["control_desk"] = new("Control Desk", "M2,8 L22,8 L22,14 L2,14 Z M4,10 L6,10 M8,10 L10,10 M12,10 L14,10 M16,10 L20,10 L20,12 L16,12 Z M4,14 L4,18 M20,14 L20,18", "#4A90D9"),
            ["office"] = new("Office", "M4,4 L20,4 L20,20 L4,20 Z M4,4 L12,2 L20,4 M8,10 L12,10 L12,14 L8,14 Z M14,12 L18,12 M14,14 L18,14 M14,16 L16,16", "#7F8C8D"),
            
            // Break & Support Areas
            ["break_room"] = new("Break Room", "M4,4 L20,4 L20,20 L4,20 Z M8,8 L16,8 L16,14 L8,14 Z M10,14 L10,18 M14,14 L14,18 M6,10 L6,12 M18,10 L18,12", "#2ECC71"),
            ["restroom"] = new("Restroom", "M4,4 L20,4 L20,20 L4,20 Z M8,8 L8,12 M8,10 L6,10 M8,10 L10,10 M8,12 L6,16 M8,12 L10,16 M16,8 L16,10 L14,14 L18,14 L16,10 M16,14 L14,18 M16,14 L18,18", "#7F8C8D"),
            ["locker"] = new("Locker Room", "M4,4 L10,4 L10,20 L4,20 Z M14,4 L20,4 L20,20 L14,20 Z M6,8 L8,8 M6,12 L8,12 M16,8 L18,8 M16,12 L18,12 M7,16 A1,1 0 1,0 7,18 M17,16 A1,1 0 1,0 17,18", "#7F8C8D"),
            ["first_aid"] = new("First Aid", "M6,4 L18,4 L18,20 L6,20 Z M12,8 L12,16 M8,12 L16,12", "#E74C3C"),
            ["safety_station"] = new("Safety Station", "M12,2 L20,8 L20,20 L4,20 L4,8 Z M12,2 L12,8 L20,8 M8,12 L16,12 M12,10 L12,18 M10,16 L14,16", "#E74C3C"),
            
            // Training & Meeting
            ["training_area"] = new("Training Area", "M4,4 L20,4 L20,12 L4,12 Z M6,6 L18,6 L18,10 L6,10 Z M6,14 L10,14 L10,18 L6,18 M14,14 L18,14 L18,18 L14,18 M10,16 L14,16", "#3498DB"),
            ["meeting_room"] = new("Meeting Room", "M4,6 L20,6 L20,18 L4,18 Z M8,10 L16,10 L16,14 L8,14 Z M6,10 L6,14 M18,10 L18,14 M10,6 L10,10 M14,6 L14,10", "#3498DB"),
            ["huddle_area"] = new("Huddle Area", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 A4,4 0 1,0 12,16 A4,4 0 1,0 12,8 M12,10 L12,14 M10,12 L14,12", "#3498DB"),
        };
    }
}
