using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Source (entry) and sink (exit) icons
    /// </summary>
    public static class SourceSinkIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // Sources - Material Entry
            ["source_arrow"] = new("Source Arrow", "M2,12 L16,12 M12,6 L18,12 L12,18 M18,8 L22,12 L18,16", "#2ECC71"),
            ["source_funnel"] = new("Funnel Input", "M4,4 L20,4 L20,8 L14,14 L14,20 L10,20 L10,14 L4,8 Z", "#2ECC71", true),
            ["source_truck"] = new("Truck Arrival", "M2,10 L2,16 L6,16 M6,14 A2,2 0 1,0 10,14 M10,16 L14,16 M14,14 A2,2 0 1,0 18,14 M18,16 L20,16 L20,12 L16,10 L14,10 L14,16 M2,10 L14,10 M20,10 L22,8", "#2ECC71"),
            ["source_dock"] = new("Dock Door In", "M4,4 L20,4 L20,20 L4,20 Z M8,8 L16,8 L16,18 L8,18 Z M12,18 L12,20 M12,4 L12,8 M2,12 L4,10 L4,14 Z", "#2ECC71"),
            ["source_pallet"] = new("Pallet In", "M2,16 L22,16 L22,20 L2,20 Z M4,16 L4,20 M12,16 L12,20 M20,16 L20,20 M6,8 L18,8 L18,16 L6,16 Z M2,12 L6,12 M4,10 L6,12 L4,14", "#2ECC71"),
            ["source_raw"] = new("Raw Material", "M8,4 L16,4 L18,8 L18,18 L16,20 L8,20 L6,18 L6,8 Z M8,8 L16,8 M8,12 L16,12 M8,16 L16,16 M2,12 L6,12", "#2ECC71"),
            ["source_warehouse"] = new("From Warehouse", "M4,8 L12,4 L20,8 L20,20 L4,20 Z M4,8 L4,20 M8,10 L8,18 M16,10 L16,18 M12,8 L12,20 M2,14 L4,12 L4,16 Z", "#2ECC71"),
            ["source_kanban"] = new("Kanban Signal", "M4,6 L20,6 L20,18 L4,18 Z M4,10 L20,10 M4,14 L20,14 M2,12 L4,12 M6,8 A1,1 0 1,0 8,8 M6,12 A1,1 0 1,0 8,12 M6,16 A1,1 0 1,0 8,16", "#2ECC71"),
            ["source_supplier"] = new("Supplier Delivery", "M2,8 L10,8 L10,16 L2,16 Z M10,12 L14,12 M14,10 L18,6 L22,6 L22,18 L18,18 L14,14 M14,10 L14,14 L18,14", "#2ECC71"),
            
            // Sinks - Material Exit
            ["sink_arrow"] = new("Exit Arrow", "M6,12 L20,12 M16,6 L22,12 L16,18 M2,8 L6,12 L2,16", "#E74C3C"),
            ["sink_flag"] = new("Exit Flag", "M6,4 L6,20 M6,4 L18,4 L18,12 L6,12", "#E74C3C"),
            ["sink_truck"] = new("Truck Departure", "M2,10 L2,16 L6,16 M6,14 A2,2 0 1,0 10,14 M10,16 L14,16 M14,14 A2,2 0 1,0 18,14 M18,16 L20,16 L20,12 L16,10 L14,10 L14,16 M2,10 L14,10 M18,8 L22,12 L18,16", "#E74C3C"),
            ["sink_dock"] = new("Dock Door Out", "M4,4 L20,4 L20,20 L4,20 Z M8,8 L16,8 L16,18 L8,18 Z M12,18 L12,20 M12,4 L12,8 M20,10 L22,12 L20,14", "#E74C3C"),
            ["sink_shipping"] = new("Shipping", "M4,6 L20,6 L18,18 L6,18 Z M4,6 L8,2 L16,2 L20,6 M10,10 L14,10 M10,14 L14,14 M20,12 L22,12", "#E74C3C"),
            ["sink_customer"] = new("To Customer", "M6,4 L18,4 L18,20 L6,20 Z M10,8 L14,8 L14,12 L10,12 Z M12,12 L12,16 M10,16 L14,16 M18,10 L22,12 L18,14", "#E74C3C"),
            ["sink_warehouse"] = new("To Warehouse", "M4,8 L12,4 L20,8 L20,20 L4,20 Z M4,8 L4,20 M8,10 L8,18 M16,10 L16,18 M12,8 L12,20 M20,12 L22,14 L20,16", "#E74C3C"),
            ["sink_dispose"] = new("Disposal", "M6,6 L18,6 L16,20 L8,20 Z M4,6 L20,6 M10,4 L14,4 M9,10 L9,16 M12,10 L12,16 M15,10 L15,16 M18,12 L22,12", "#E74C3C"),
            
            // Rework & Return
            ["rework_loop"] = new("Rework Loop", "M12,4 A8,8 0 1,0 20,12 M16,8 L20,12 L16,16 M12,4 L12,8 M8,4 L12,4 L12,8", "#F5A623"),
            ["return_path"] = new("Return Path", "M20,8 L12,8 L12,16 L4,16 M8,12 L4,16 L8,20 M16,4 L20,8 L16,12", "#F5A623"),
            ["reject"] = new("Reject Chute", "M4,4 L14,4 L14,12 L20,12 L20,20 L14,20 L14,12 M4,4 L4,12 L14,12 M8,8 L10,8 M16,16 L18,16", "#E74C3C"),
            ["scrap_bin"] = new("Scrap Bin", "M6,6 L18,6 L16,20 L8,20 Z M4,6 L20,6 M10,4 L14,4 M8,10 L16,10 M8,14 L16,14", "#E74C3C"),
            
            // Transfer Points
            ["transfer_in"] = new("Transfer In", "M2,12 L8,12 M4,8 L8,12 L4,16 M10,8 L18,8 L18,16 L10,16 Z M12,10 L16,10 M12,12 L16,12 M12,14 L16,14", "#3498DB"),
            ["transfer_out"] = new("Transfer Out", "M6,8 L14,8 L14,16 L6,16 Z M8,10 L12,10 M8,12 L12,12 M8,14 L12,14 M16,12 L22,12 M18,8 L22,12 L18,16", "#3498DB"),
            ["handoff"] = new("Handoff Point", "M4,8 L10,8 L10,16 L4,16 Z M14,8 L20,8 L20,16 L14,16 Z M10,12 L14,12 M11,10 L13,12 L11,14", "#3498DB"),
            ["staging"] = new("Staging Area", "M4,6 L20,6 L20,18 L4,18 Z M4,10 L20,10 M4,14 L20,14 M8,6 L8,18 M12,6 L12,18 M16,6 L16,18", "#3498DB"),
        };
    }
}
