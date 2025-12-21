using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Transport, conveyor, and material handling icons
    /// </summary>
    public static class TransportIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // ===== AGV STATIONS =====
            ["station_pickup"] = new("Pickup Station", "M12,4 L12,20 M6,10 L12,4 L18,10", "#27AE60"),
            ["station_dropoff"] = new("Dropoff Station", "M12,4 L12,20 M6,14 L12,20 L18,14", "#E74C3C"),
            ["station_home"] = new("Home/Charging", "M12,4 L22,12 L22,22 L2,22 L2,12 Z M10,22 L10,16 L14,16 L14,22", "#F39C12", true),
            ["station_buffer"] = new("Buffer Station", "M4,6 L20,6 M4,12 L20,12 M4,18 L20,18", "#9B59B6"),
            ["station_crossing"] = new("Track Crossing", "M4,4 L20,20 M20,4 L4,20", "#3498DB"),
            ["waypoint"] = new("Waypoint", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 A4,4 0 1,0 12,16 A4,4 0 1,0 12,8", "#95A5A6"),
            
            // ===== CONVEYORS =====
            ["conveyor_belt"] = new("Belt Conveyor", "M4,10 L20,10 L20,14 L4,14 Z M2,12 A2,2 0 1,0 6,12 A2,2 0 1,0 2,12 M18,12 A2,2 0 1,0 22,12 A2,2 0 1,0 18,12 M8,10 L8,14 M12,10 L12,14 M16,10 L16,14", "#7F8C8D"),
            ["conveyor_roller"] = new("Roller Conveyor", "M4,8 L20,8 M4,16 L20,16 M6,8 L6,16 M10,8 L10,16 M14,8 L14,16 M18,8 L18,16 M4,8 A2,2 0 1,1 4,12 M20,8 A2,2 0 1,0 20,12", "#7F8C8D"),
            ["conveyor_chain"] = new("Chain Conveyor", "M4,10 L20,10 M4,14 L20,14 M4,10 L4,14 M8,10 L8,14 M12,10 L12,14 M16,10 L16,14 M20,10 L20,14 M6,8 L6,10 M14,8 L14,10 M10,14 L10,16 M18,14 L18,16", "#7F8C8D"),
            ["conveyor_screw"] = new("Screw Conveyor", "M4,8 L4,16 L20,16 L20,8 Z M6,12 Q8,8 10,12 Q12,16 14,12 Q16,8 18,12", "#7F8C8D"),
            ["conveyor_overhead"] = new("Overhead Conveyor", "M4,4 L20,4 M4,4 L4,10 M20,4 L20,10 M12,4 L12,10 M4,10 L6,10 L6,16 L4,16 M12,10 L14,10 L14,16 L12,16 M18,10 L20,10 L20,16 L18,16", "#7F8C8D"),
            ["conveyor_gravity"] = new("Gravity Conveyor", "M2,6 L22,14 M4,4 L4,8 M8,6 L8,10 M12,8 L12,12 M16,10 L16,14 M20,12 L20,16", "#7F8C8D"),
            ["conveyor_spiral"] = new("Spiral Conveyor", "M12,2 A8,8 0 0,1 20,10 A8,8 0 0,1 12,18 A8,8 0 0,1 4,10 M12,6 A4,4 0 0,1 16,10 A4,4 0 0,1 12,14", "#7F8C8D"),
            ["conveyor_incline"] = new("Incline Conveyor", "M2,18 L20,6 M4,20 L22,8 M6,16 L6,18 M10,12 L10,14 M14,10 L14,12 M18,6 L18,8", "#7F8C8D"),
            
            // ===== VEHICLES =====
            ["agv"] = new("AGV", "M4,8 L20,8 L20,16 L4,16 Z M6,16 A2,2 0 1,0 6,20 M18,16 A2,2 0 1,0 18,20 M8,10 L16,10 L16,14 L8,14 Z M11,6 L13,6 L13,8 L11,8 Z", "#3498DB"),
            ["amr"] = new("AMR", "M6,6 L18,6 L20,10 L20,16 L4,16 L4,10 Z M6,16 A2,2 0 1,0 6,20 M18,16 A2,2 0 1,0 18,20 M10,8 L14,8 L14,12 L10,12 Z M8,2 L8,6 M16,2 L16,6", "#3498DB"),
            ["forklift"] = new("Forklift", "M6,6 L14,6 L14,16 L6,16 Z M14,12 L20,12 L20,18 L14,18 M20,12 L22,12 L22,6 L20,6 M6,16 A2,2 0 1,0 6,20 M14,16 A2,2 0 1,0 14,20 M8,8 L12,8 L12,12 L8,12 Z", "#F5A623"),
            ["tugger"] = new("Tugger", "M4,8 L12,8 L12,16 L4,16 Z M4,16 A2,2 0 1,0 4,20 M12,16 A2,2 0 1,0 12,20 M12,12 L16,12 M16,10 L20,10 L20,14 L16,14 Z", "#F5A623"),
            ["pallet_jack"] = new("Pallet Jack", "M4,10 L8,10 L8,18 L4,18 Z M8,14 L18,14 M18,12 L22,12 L22,16 L18,16 M6,18 A2,2 0 1,0 6,22 M20,10 L20,12 M20,16 L20,18", "#F5A623"),
            ["cart"] = new("Cart", "M4,6 L20,6 L20,14 L4,14 Z M6,14 A2,2 0 1,0 6,18 M18,14 A2,2 0 1,0 18,18 M8,4 L8,6 M16,4 L16,6", "#7F8C8D"),
            ["dolly"] = new("Dolly", "M6,4 L18,4 L18,14 L6,14 Z M6,14 A2,2 0 1,0 6,18 M18,14 A2,2 0 1,0 18,18 M10,18 A2,2 0 1,0 10,22 M14,18 A2,2 0 1,0 14,22", "#7F8C8D"),
            
            // ===== CRANES & HOISTS =====
            ["crane_overhead"] = new("Overhead Crane", "M2,4 L22,4 M2,4 L2,8 M22,4 L22,8 M8,4 L8,8 L16,8 L16,4 M12,8 L12,14 M10,14 L14,14 L14,18 L10,18 Z", "#E67E22"),
            ["crane_gantry"] = new("Gantry Crane", "M4,20 L4,6 L8,6 L8,20 M16,20 L16,6 L20,6 L20,20 M4,6 L20,6 M10,6 L10,12 L14,12 L14,6 M12,12 L12,16", "#E67E22"),
            ["crane_jib"] = new("Jib Crane", "M4,20 L4,4 M4,6 L18,6 M18,6 L18,10 M16,10 L20,10 L20,16 L16,16 Z M4,4 L6,4 M4,8 L6,8", "#E67E22"),
            ["hoist"] = new("Hoist", "M8,2 L16,2 L16,6 L8,6 Z M12,6 L12,12 M8,12 L16,12 L16,18 L8,18 Z M10,14 L14,14 M10,16 L14,16", "#E67E22"),
            ["lift_table"] = new("Lift Table", "M4,18 L20,18 L20,22 L4,22 Z M6,14 L8,18 M18,14 L16,18 M6,14 L18,14 M8,10 L16,10 L16,14 L8,14 Z", "#E67E22"),
            ["elevator"] = new("Elevator", "M6,2 L18,2 L18,22 L6,22 Z M8,4 L16,4 L16,18 L8,18 Z M12,6 L12,8 M12,16 L10,14 L14,14 Z", "#7F8C8D"),
            
            // ===== TRANSFER SYSTEMS =====
            ["transfer_shuttle"] = new("Shuttle", "M2,10 L22,10 M2,14 L22,14 M8,8 L16,8 L16,16 L8,16 Z M10,10 L10,14 M14,10 L14,14", "#3498DB"),
            ["transfer_turntable"] = new("Turntable", "M12,4 A8,8 0 1,0 12,20 A8,8 0 1,0 12,4 M12,8 A4,4 0 1,0 12,16 A4,4 0 1,0 12,8 M12,4 L12,8 M12,16 L12,20 M4,12 L8,12 M16,12 L20,12", "#3498DB"),
            ["transfer_diverter"] = new("Diverter", "M2,12 L10,12 M10,8 L10,16 M10,8 L18,4 M10,16 L18,20 M10,12 L22,12", "#3498DB"),
            ["transfer_merge"] = new("Merge", "M2,6 L10,12 M2,18 L10,12 M10,12 L22,12 M10,10 L10,14", "#3498DB"),
            ["transfer_sortation"] = new("Sortation", "M2,12 L8,12 M8,12 L14,6 L22,6 M8,12 L14,12 L22,12 M8,12 L14,18 L22,18", "#3498DB"),
            ["crossover"] = new("Crossover", "M2,8 L22,8 M2,16 L22,16 M8,2 L8,22 M16,2 L16,22 M8,8 L8,16 M16,8 L16,16", "#3498DB"),
            
            // ===== TRACKS & RAILS =====
            ["rail_track"] = new("Rail Track", "M4,10 L20,10 M4,14 L20,14 M6,8 L6,16 M10,8 L10,16 M14,8 L14,16 M18,8 L18,16", "#7F8C8D"),
            ["monorail"] = new("Monorail", "M4,6 L20,6 M8,6 L8,12 L6,12 L6,16 L10,16 L10,12 L8,12 M16,6 L16,12 L14,12 L14,16 L18,16 L18,12 L16,12", "#7F8C8D"),
            
            // ===== PIPES & TUBES =====
            ["pipe"] = new("Pipe", "M2,10 L22,10 M2,14 L22,14 M2,10 A2,2 0 0,0 2,14 M22,10 A2,2 0 0,1 22,14", "#3498DB"),
            ["pneumatic_tube"] = new("Pneumatic Tube", "M4,8 L4,16 M20,8 L20,16 M4,12 L20,12 M8,10 L8,14 M12,10 L12,14 M16,10 L16,14 M2,8 L6,8 M2,16 L6,16 M18,8 L22,8 M18,16 L22,16", "#3498DB"),
        };
    }
}
