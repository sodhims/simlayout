using System;
using System.Collections.Generic;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Root container for the entire layout
    /// </summary>
    public class LayoutData
    {
        public string Version { get; set; } = "2.3";
        public LayoutMetadata Metadata { get; set; } = new();
        public CanvasSettings Canvas { get; set; } = new();
        public DisplaySettings Display { get; set; } = new();
        public LayerManager LayerManager { get; set; } = new();
        public List<TemplateData> Templates { get; set; } = new();
        public List<ResourcePool> ResourcePools { get; set; } = new();
        public List<PartTypeData> PartTypes { get; set; } = new();
        public List<CorridorData> Corridors { get; set; } = new();
        public List<NodeData> Nodes { get; set; } = new();
        public List<PathData> Paths { get; set; } = new();
        public List<ZoneData> Zones { get; set; } = new();
        public List<GroupData> Groups { get; set; } = new();
        
        // New layers
        public List<WallData> Walls { get; set; } = new();
        public List<DoorData> Doors { get; set; } = new();
        public List<ColumnData> Columns { get; set; } = new();
        public List<MeasurementData> Measurements { get; set; } = new();
        public BackgroundImage? Background { get; set; }
    }

    /// <summary>
    /// Factory for creating default layouts and nodes
    /// </summary>
    public static class LayoutFactory
    {
        public static LayoutData CreateDefault()
        {
            var layout = new LayoutData
            {
                Version = "2.3",
                Metadata = new LayoutMetadata
                {
                    Name = "New Layout",
                    Author = Environment.UserName,
                    Created = DateTime.Now,
                    Modified = DateTime.Now
                },
                Canvas = new CanvasSettings
                {
                    Width = 1200,
                    Height = 800,
                    GridSize = 20,
                    ShowGrid = true,
                    SnapToGrid = true
                },
                Display = new DisplaySettings()
            };
            
            // Initialize default layers
            layout.LayerManager.InitializeDefaultLayers();
            
            return layout;
        }

        public static NodeData CreateNode(string type, double x, double y)
        {
            return new NodeData
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Name = GetDefaultName(type),
                Label = GetDefaultLabel(type),
                Visual = new NodeVisual
                {
                    X = x,
                    Y = y,
                    Width = 80,
                    Height = 60,
                    Color = GetDefaultColor(type),
                    Icon = GetDefaultIcon(type)
                },
                Simulation = new SimulationParams()
            };
        }

        private static string GetDefaultName(string type) => type switch
        {
            NodeTypes.Source => "Source",
            NodeTypes.Sink => "Sink",
            NodeTypes.Machine => "Machine",
            NodeTypes.Buffer => "Buffer",
            NodeTypes.Workstation => "Workstation",
            NodeTypes.Inspection => "Inspection",
            NodeTypes.Storage => "Storage",
            _ => "Node"
        };

        private static string GetDefaultLabel(string type) => type switch
        {
            NodeTypes.Source => "IN",
            NodeTypes.Sink => "OUT",
            _ => ""
        };

        private static string GetDefaultColor(string type) => type switch
        {
            NodeTypes.Source => "#2ECC71",
            NodeTypes.Sink => "#E74C3C",
            NodeTypes.Machine => "#3498DB",
            NodeTypes.Buffer => "#F5A623",
            NodeTypes.Workstation => "#9B59B6",
            NodeTypes.Inspection => "#1ABC9C",
            NodeTypes.Storage => "#95A5A6",
            _ => "#4A90D9"
        };

        private static string GetDefaultIcon(string type) => type switch
        {
            NodeTypes.Source => "source_arrow",
            NodeTypes.Sink => "sink_arrow",
            NodeTypes.Machine => "machine_generic",
            NodeTypes.Buffer => "buffer_queue",
            NodeTypes.Workstation => "workstation",
            NodeTypes.Inspection => "inspection",
            NodeTypes.Storage => "storage_rack",
            _ => "machine_generic"
        };
    }

    /// <summary>
    /// Template data for reusable node configurations
    /// </summary>
    public class TemplateData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<NodeData> Nodes { get; set; } = new();
        public List<PathData> Paths { get; set; } = new();
    }

    /// <summary>
    /// Resource pool for shared resources
    /// </summary>
    public class ResourcePool
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string ResourceType { get; set; } = "operator";
        public int Capacity { get; set; } = 1;
        public int Available { get; set; } = 1;
        public List<string> AssignedNodes { get; set; } = new();
    }

    /// <summary>
    /// Part type definition
    /// </summary>
    public class PartTypeData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#888888";
        public Dictionary<string, double> Attributes { get; set; } = new();
    }
}
