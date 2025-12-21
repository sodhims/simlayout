using System;
using System.Collections.Generic;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Layout template types
    /// </summary>
    public static class LayoutTemplateTypes
    {
        public const string JobShop = "job_shop";
        public const string CellularManufacturing = "cellular_manufacturing";
        public const string FlowShop = "flow_shop";
        public const string FixedPosition = "fixed_position";
        public const string ProcessLayout = "process_layout";
        public const string ProductLayout = "product_layout";
        public const string Warehouse = "warehouse";
        public const string AssemblyLine = "assembly_line";
    }

    /// <summary>
    /// Template for generating layouts
    /// </summary>
    public class LayoutTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string TemplateType { get; set; } = LayoutTemplateTypes.JobShop;
        public string Description { get; set; } = "";

        // Template parameters as key-value pairs
        public Dictionary<string, string> Parameters { get; set; } = new();

        // Generation rules
        public string GenerationRules { get; set; } = ""; // JSON or script

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Job shop template parameters
    /// </summary>
    public class JobShopTemplate
    {
        public int WorkCenters { get; set; } = 6;
        public double Spacing { get; set; } = 150;
        public string Arrangement { get; set; } = "circular"; // "circular", "grid", "u_shape"
        public bool AddMaterialHandling { get; set; } = true;
        public bool AddToolCribs { get; set; } = true;
        public int ToolCribs { get; set; } = 2;
    }

    /// <summary>
    /// Cellular manufacturing template parameters
    /// </summary>
    public class CellularManufacturingTemplate
    {
        public int Cells { get; set; } = 4;
        public int MachinesPerCell { get; set; } = 5;
        public double CellSpacing { get; set; } = 200;
        public string CellShape { get; set; } = "u_shape"; // "u_shape", "linear", "circular"
        public bool SharedResources { get; set; } = true;
    }

    /// <summary>
    /// Flow shop template parameters
    /// </summary>
    public class FlowShopTemplate
    {
        public int Stages { get; set; } = 5;
        public int MachinesPerStage { get; set; } = 3;
        public double StageSpacing { get; set; } = 120;
        public double MachineSpacing { get; set; } = 60;
        public bool AddBuffers { get; set; } = true;
        public bool AddConveyors { get; set; } = true;
    }

    /// <summary>
    /// Fixed position template parameters
    /// </summary>
    public class FixedPositionTemplate
    {
        public int Products { get; set; } = 2;
        public int ToolsPerProduct { get; set; } = 8;
        public double ProductSpacing { get; set; } = 300;
        public double ToolRadius { get; set; } = 150;
    }
}
