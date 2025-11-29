using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Manufacturing machine icons - CNC, assembly, processing equipment
    /// </summary>
    public static class MachineIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // CNC Machines
            ["cnc_mill"] = new("CNC Mill", "M3,8 L21,8 L21,20 L3,20 Z M6,8 L6,4 L18,4 L18,8 M12,10 L12,18 M8,14 L16,14", "#4A90D9"),
            ["cnc_lathe"] = new("CNC Lathe", "M2,10 L8,10 L8,14 L2,14 Z M8,12 L16,12 M16,8 L22,8 L22,16 L16,16 Z M18,10 L20,10 L20,14 L18,14", "#4A90D9"),
            ["cnc_5axis"] = new("5-Axis CNC", "M4,6 L20,6 L20,20 L4,20 Z M8,2 L16,2 L16,6 M12,8 L12,18 M8,12 L16,12 M6,16 L18,16 M10,8 L10,10 M14,8 L14,10", "#4A90D9"),
            ["cnc_router"] = new("CNC Router", "M2,8 L22,8 L22,18 L2,18 Z M6,8 L6,4 M18,8 L18,4 M12,10 L12,16 M8,13 L16,13", "#4A90D9"),
            ["cnc_edm"] = new("EDM Machine", "M4,6 L20,6 L20,20 L4,20 Z M10,2 L14,2 L14,6 M12,8 L12,12 M10,12 L14,12 L14,16 L10,16 Z", "#4A90D9"),
            ["cnc_waterjet"] = new("Waterjet Cutter", "M4,8 L20,8 L20,18 L4,18 Z M12,4 L12,8 M11,10 L13,10 L13,16 L11,16 Z M8,16 Q12,20 16,16", "#3498DB"),
            
            // Presses & Forming
            ["press_hydraulic"] = new("Hydraulic Press", "M4,2 L20,2 L20,8 L4,8 Z M10,8 L10,14 L14,14 L14,8 M4,14 L20,14 L20,22 L4,22 Z", "#E67E22"),
            ["press_punch"] = new("Punch Press", "M6,4 L18,4 L18,10 L6,10 Z M10,10 L10,14 L14,14 L14,10 M4,14 L20,14 L20,20 L4,20 Z M8,16 L8,18 M16,16 L16,18", "#E67E22"),
            ["press_brake"] = new("Press Brake", "M2,6 L22,6 L22,10 L2,10 Z M4,14 L20,14 L20,18 L4,18 Z M8,10 L8,14 M16,10 L16,14 M12,10 L12,14", "#E67E22"),
            ["roll_former"] = new("Roll Former", "M4,8 A4,4 0 1,0 4,16 M10,6 A4,4 0 1,0 10,14 M16,8 A4,4 0 1,0 16,16 M4,12 L20,12", "#E67E22"),
            ["stamping"] = new("Stamping Press", "M4,4 L20,4 L20,10 L4,10 Z M8,10 L8,14 L16,14 L16,10 M4,14 L20,14 L20,20 L4,20 Z", "#E67E22"),
            
            // Welding & Joining
            ["welder_mig"] = new("MIG Welder", "M4,8 L12,8 L12,16 L4,16 Z M12,12 L16,12 M16,8 L16,16 M18,6 L22,10 M18,10 L22,14 M18,14 L22,18", "#E67E22"),
            ["welder_spot"] = new("Spot Welder", "M8,4 L8,10 L4,14 L4,20 M16,4 L16,10 L20,14 L20,20 M8,12 L16,12 M10,10 L14,10 L14,14 L10,14 Z", "#E67E22"),
            ["welder_laser"] = new("Laser Welder", "M4,8 L14,8 L14,16 L4,16 Z M14,12 L22,12 M18,8 L22,12 L18,16 M6,10 L6,14 M10,10 L10,14", "#E74C3C"),
            ["welder_ultrasonic"] = new("Ultrasonic Welder", "M6,6 L18,6 L18,18 L6,18 Z M10,2 L14,2 L14,6 M10,18 L10,22 L14,22 L14,18 M8,10 Q12,14 16,10", "#9B59B6"),
            ["riveter"] = new("Riveter", "M10,2 L14,2 L14,8 L10,8 Z M8,8 L16,8 L16,12 L8,12 Z M10,12 L10,18 L14,18 L14,12 M6,18 L18,18 L18,22 L6,22 Z", "#7F8C8D"),
            
            // Cutting & Removal
            ["laser_cutter"] = new("Laser Cutter", "M4,8 L20,8 L20,18 L4,18 Z M12,4 L12,8 M12,10 L12,16 M8,14 L16,14 M10,12 L14,12", "#E74C3C"),
            ["plasma_cutter"] = new("Plasma Cutter", "M4,6 L20,6 L20,18 L4,18 Z M12,2 L12,6 M10,8 L14,8 L13,16 L11,16 Z M8,16 L16,16", "#E74C3C"),
            ["bandsaw"] = new("Bandsaw", "M6,4 A6,6 0 1,0 6,16 M18,4 A6,6 0 1,0 18,16 M6,10 L18,10 M4,20 L20,20", "#4A90D9"),
            ["grinder"] = new("Grinder", "M4,8 L20,8 L20,16 L4,16 Z M12,4 L12,8 M8,12 A4,4 0 1,0 16,12", "#4A90D9"),
            ["shear"] = new("Shear", "M4,4 L20,4 L20,10 L4,10 Z M4,10 L20,14 L20,20 L4,20 Z M8,6 L8,8 M16,6 L16,8", "#4A90D9"),
            
            // Surface Treatment
            ["paint_booth"] = new("Paint Booth", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 L18,8 L6,8 Z M12,10 L10,18 M12,10 L14,18", "#3498DB"),
            ["powder_coat"] = new("Powder Coating", "M4,6 L20,6 L20,18 L4,18 Z M8,10 L8,14 M12,8 L12,16 M16,10 L16,14 M6,2 L6,6 M18,2 L18,6", "#9B59B6"),
            ["plating"] = new("Plating Tank", "M4,6 L20,6 L20,18 L4,18 Z M6,4 L6,6 M18,4 L18,6 M8,8 L8,16 M12,6 L12,16 M16,8 L16,16", "#F1C40F"),
            ["heat_treat"] = new("Heat Treatment", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 L18,18 L6,18 Z M9,9 L9,11 M12,8 L12,12 M15,9 L15,11 M9,14 L15,14", "#E67E22"),
            ["deburr"] = new("Deburring", "M6,6 L18,6 L18,18 L6,18 Z M12,2 L12,6 M8,10 L16,14 M16,10 L8,14", "#4A90D9"),
            
            // Assembly
            ["assembly_station"] = new("Assembly Station", "M4,10 L20,10 L20,18 L4,18 Z M6,6 L10,6 L10,10 M14,6 L18,6 L18,10 M8,12 L8,16 M16,12 L16,16 M12,12 L12,16", "#2ECC71"),
            ["assembly_auto"] = new("Auto Assembly", "M4,8 L20,8 L20,16 L4,16 Z M8,4 L8,8 M16,4 L16,8 M6,10 L6,14 M10,10 L10,14 M14,10 L14,14 M18,10 L18,14", "#2ECC71"),
            ["torque_station"] = new("Torque Station", "M8,4 L16,4 L16,10 L8,10 Z M10,10 L10,14 L14,14 L14,10 M6,14 L18,14 L18,20 L6,20 Z M12,6 A2,2 0 1,0 12,8", "#2ECC71"),
            ["press_fit"] = new("Press Fit", "M10,2 L14,2 L14,8 L10,8 Z M8,8 L16,8 L16,12 L8,12 Z M6,14 L18,14 L18,20 L6,20 Z M10,14 L10,12 M14,14 L14,12", "#2ECC71"),
            
            // Additive
            ["printer_3d"] = new("3D Printer", "M4,6 L20,6 L20,20 L4,20 Z M6,2 L18,2 L18,6 M10,10 L14,10 L14,14 L10,14 Z M12,6 L12,10 M8,16 L16,16", "#3498DB"),
            ["printer_metal"] = new("Metal 3D Printer", "M4,4 L20,4 L20,20 L4,20 Z M8,4 L8,2 L16,2 L16,4 M10,8 L14,8 L14,14 L10,14 Z M12,4 L12,8 M7,16 L17,16", "#7F8C8D"),
            ["sla_printer"] = new("SLA Printer", "M6,4 L18,4 L18,18 L6,18 Z M4,18 L20,18 L20,22 L4,22 Z M10,8 L14,8 L14,12 L10,12 Z M12,4 L12,8", "#9B59B6"),
            
            // Injection & Molding
            ["injection_mold"] = new("Injection Mold", "M2,8 L8,8 L8,16 L2,16 Z M8,12 L10,12 M10,6 L14,6 L14,18 L10,18 Z M14,12 L16,12 M16,8 L22,8 L22,16 L16,16 Z", "#4A90D9"),
            ["blow_mold"] = new("Blow Mold", "M8,4 L16,4 L16,8 L8,8 Z M6,8 L6,16 Q6,20 12,20 Q18,20 18,16 L18,8 M10,10 L10,14 M14,10 L14,14", "#4A90D9"),
            ["die_cast"] = new("Die Casting", "M4,6 L10,6 L10,18 L4,18 Z M10,12 L14,12 M14,6 L20,6 L20,18 L14,18 Z M6,8 L8,8 L8,16 L6,16 M16,8 L18,8 L18,16 L16,16", "#E67E22"),
        };
    }

    public record IconDef(string Name, string Path, string Color, bool Filled = false);
}
