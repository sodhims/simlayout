using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Storage, buffer, and warehouse icons
    /// </summary>
    public static class StorageIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // Buffers
            ["buffer_fifo"] = new("Buffer FIFO", "M4,6 L20,6 L20,18 L4,18 Z M4,10 L20,10 M4,14 L20,14 M2,12 L4,10 L4,14 Z M20,10 L22,12 L20,14", "#F5A623"),
            ["buffer_lifo"] = new("Buffer LIFO", "M6,18 L18,18 L18,14 L6,14 Z M6,14 L18,14 L18,10 L6,10 Z M6,10 L18,10 L18,6 L6,6 Z M12,2 L8,6 M12,2 L16,6", "#F5A623"),
            ["buffer_accumulation"] = new("Accumulation Buffer", "M2,10 L22,10 L22,14 L2,14 Z M6,8 L6,16 M10,8 L10,16 M14,8 L14,16 M18,8 L18,16 M4,10 A2,2 0 1,1 4,14 M20,10 A2,2 0 1,0 20,14", "#F5A623"),
            ["buffer_lane"] = new("Lane Buffer", "M2,6 L22,6 L22,10 L2,10 Z M2,14 L22,14 L22,18 L2,18 Z M6,8 A1,1 0 1,0 8,8 M12,8 A1,1 0 1,0 14,8 M18,8 A1,1 0 1,0 20,8", "#F5A623"),
            ["buffer_serpentine"] = new("Serpentine Buffer", "M2,6 L20,6 L20,10 L4,10 L4,14 L20,14 L20,18 L2,18", "#F5A623"),
            ["buffer_rotary"] = new("Rotary Buffer", "M12,2 A10,10 0 1,0 12,22 A10,10 0 1,0 12,2 M12,6 A6,6 0 1,0 12,18 A6,6 0 1,0 12,6 M12,10 A2,2 0 1,0 12,14 M12,2 L12,6 M22,12 L18,12", "#F5A623"),
            
            // Queues
            ["queue_single"] = new("Single Queue", "M4,10 L20,10 L20,14 L4,14 Z M8,10 L8,14 M12,10 L12,14 M16,10 L16,14 M2,12 L4,12 M20,12 L22,12", "#F5A623"),
            ["queue_parallel"] = new("Parallel Queues", "M4,6 L20,6 L20,10 L4,10 Z M4,14 L20,14 L20,18 L4,18 Z M8,6 L8,10 M12,6 L12,10 M16,6 L16,10 M8,14 L8,18 M12,14 L12,18 M16,14 L16,18", "#F5A623"),
            
            // Shelves & Racks
            ["shelf_unit"] = new("Shelf Unit", "M4,2 L4,22 M20,2 L20,22 M4,2 L20,2 M4,8 L20,8 M4,14 L20,14 M4,20 L20,20 M7,4 L11,4 L11,6 L7,6 M13,10 L17,10 L17,12 L13,12", "#8E44AD"),
            ["rack_pallet"] = new("Pallet Rack", "M2,2 L2,22 M22,2 L22,22 M6,2 L6,22 M18,2 L18,22 M2,8 L22,8 M2,14 L22,14 M2,20 L22,20 M8,4 L16,4 L16,6 L8,6", "#8E44AD"),
            ["rack_cantilever"] = new("Cantilever Rack", "M6,2 L6,22 M6,6 L18,6 M6,12 L18,12 M6,18 L18,18 M8,4 L8,8 M8,10 L8,14 M8,16 L8,20 M14,4 L14,8", "#8E44AD"),
            ["rack_drive_in"] = new("Drive-In Rack", "M4,4 L4,20 M8,4 L8,20 M16,4 L16,20 M20,4 L20,20 M4,8 L20,8 M4,14 L20,14 M4,4 L20,4", "#8E44AD"),
            ["rack_flow"] = new("Flow Rack", "M4,4 L4,20 M20,4 L20,20 M4,4 L20,4 M4,8 L20,10 M4,12 L20,14 M4,16 L20,18 M4,20 L20,20", "#8E44AD"),
            ["rack_mobile"] = new("Mobile Rack", "M4,4 L4,18 L8,18 L8,4 Z M12,4 L12,18 L16,18 L16,4 Z M4,18 A2,2 0 1,0 4,22 M8,18 A2,2 0 1,0 8,22 M12,18 A2,2 0 1,0 12,22 M16,18 A2,2 0 1,0 16,22 M20,4 L20,18 A2,2 0 1,0 20,22", "#8E44AD"),
            
            // Bins & Containers
            ["bin_small"] = new("Small Bin", "M6,6 L18,6 L16,18 L8,18 Z M8,10 L16,10 M8,14 L16,14", "#8E44AD"),
            ["bin_large"] = new("Large Bin", "M4,4 L20,4 L18,20 L6,20 Z M6,8 L18,8 M6,12 L18,12 M6,16 L18,16", "#8E44AD"),
            ["container"] = new("Container", "M2,6 L22,6 L22,18 L2,18 Z M2,6 L6,2 L18,2 L22,6 M6,2 L6,6 M18,2 L18,6 M8,10 L8,14 M16,10 L16,14", "#8E44AD"),
            ["tote"] = new("Tote", "M4,8 L20,8 L18,18 L6,18 Z M6,4 L8,8 M18,4 L16,8 M6,4 L18,4 M8,12 L16,12", "#8E44AD"),
            ["pallet"] = new("Pallet", "M2,14 L22,14 M2,18 L22,18 M4,14 L4,18 M10,14 L10,18 M14,14 L14,18 M20,14 L20,18 M6,6 L18,6 L18,14 L6,14 Z", "#8E44AD"),
            ["stillage"] = new("Stillage", "M4,20 L4,4 L20,4 L20,20 M4,20 L20,20 M4,12 L20,12 M8,4 L8,20 M16,4 L16,20", "#8E44AD"),
            
            // Automated Storage
            ["asrs"] = new("AS/RS", "M2,2 L22,2 L22,22 L2,22 Z M2,8 L22,8 M2,14 L22,14 M8,2 L8,22 M14,2 L14,22 M10,10 L12,10 L12,12 L10,12 Z", "#9B59B6"),
            ["carousel_vertical"] = new("Vertical Carousel", "M6,2 L18,2 L18,22 L6,22 Z M8,4 L16,4 L16,8 L8,8 Z M8,10 L16,10 L16,14 L8,14 Z M8,16 L16,16 L16,20 L8,20 Z M4,12 L6,10 L6,14 Z", "#9B59B6"),
            ["carousel_horizontal"] = new("Horizontal Carousel", "M4,6 L20,6 L20,18 L4,18 Z M4,6 A6,6 0 0,0 4,18 M20,6 A6,6 0 0,1 20,18 M8,10 L8,14 M12,10 L12,14 M16,10 L16,14", "#9B59B6"),
            ["shuttle_system"] = new("Shuttle System", "M2,6 L22,6 M2,12 L22,12 M2,18 L22,18 M4,4 L4,20 M20,4 L20,20 M8,8 L14,8 L14,10 L8,10 Z M10,14 L16,14 L16,16 L10,16 Z", "#9B59B6"),
            ["vna_truck"] = new("VNA Truck", "M8,4 L16,4 L16,20 L8,20 Z M10,6 L14,6 L14,10 L10,10 Z M4,18 L8,18 M16,18 L20,18 M4,16 L4,20 M20,16 L20,20 M12,12 L12,16", "#9B59B6"),
            
            // Tanks & Silos
            ["tank_vertical"] = new("Vertical Tank", "M8,4 L16,4 L18,6 L18,18 L16,20 L8,20 L6,18 L6,6 Z M8,8 L16,8 M8,14 L16,14", "#3498DB"),
            ["tank_horizontal"] = new("Horizontal Tank", "M4,8 L4,16 L6,18 L18,18 L20,16 L20,8 L18,6 L6,6 Z M8,10 L8,14 M16,10 L16,14", "#3498DB"),
            ["silo"] = new("Silo", "M8,2 L16,2 L18,4 L18,18 L14,22 L10,22 L6,18 L6,4 Z M8,6 L16,6 M8,10 L16,10 M10,18 L10,20 M14,18 L14,20", "#7F8C8D"),
            ["hopper"] = new("Hopper", "M4,4 L20,4 L20,12 L14,20 L10,20 L4,12 Z M6,6 L18,6 M8,14 L16,14", "#7F8C8D"),
            ["bunker"] = new("Bunker", "M2,8 L22,8 L20,20 L4,20 Z M4,4 L20,4 L20,8 L4,8 Z M8,12 L16,12 M8,16 L16,16", "#7F8C8D"),
        };
    }
}
