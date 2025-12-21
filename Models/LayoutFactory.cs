using System.Linq;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Factory for creating layout instances and elements
    /// </summary>
    public static class LayoutFactory
    {
        /// <summary>
        /// Creates a new default layout with standard settings
        /// </summary>
        public static LayoutData CreateDefault()
        {
            var layout = new LayoutData
            {
                Metadata = new LayoutMetadata
                {
                    Name = "Untitled Layout",
                    Units = "meters",
                    PixelsPerUnit = 20.0
                },
                Canvas = new CanvasSettings
                {
                    Width = 2000,
                    Height = 1500,
                    GridSize = 20,
                    SnapToGrid = true,
                    SnapMode = SnapMode.Grid,
                    ShowGrid = true
                },
                Display = new DisplaySettings
                {
                    ShowGrid = true,
                    ShowRulers = true,
                    ShowLabels = true,
                    ShowMinimap = false,
                    Layers = new LayerVisibility
                    {
                        BackgroundImage = true,
                        Background = true,
                        Walls = true,
                        Corridors = true,
                        Zones = true,
                        Paths = true,
                        Nodes = true,
                        Labels = true,
                        Measurements = true
                    }
                },
                LayerManager = new LayerManager()
            };

            layout.LayerManager.InitializeDefaultLayers();

            return layout;
        }

        /// <summary>
        /// Creates a node of the specified type at position
        /// </summary>
        public static NodeData CreateNode(string nodeType, double x = 0, double y = 0)
        {
            var node = new NodeData
            {
                Id = System.Guid.NewGuid().ToString(),
                Type = nodeType,
                Name = GetDefaultName(nodeType),
                Visual = new NodeVisual
                {
                    X = x,
                    Y = y,
                    Width = GetDefaultWidth(nodeType),
                    Height = GetDefaultHeight(nodeType),
                    Color = GetDefaultColor(nodeType)
                }
            };

            ConfigureSimulationDefaults(node);
            return node;
        }

        /// <summary>
        /// Creates a node with layout context (layout used for naming uniqueness)
        /// </summary>
        public static NodeData CreateNode(string nodeType, double x, double y, LayoutData layout)
        {
            var node = CreateNode(nodeType, x, y);
            
            // Generate unique name based on existing nodes
            if (layout != null)
            {
                var baseName = GetDefaultName(nodeType);
                var existingCount = layout.Nodes.Count(n => n.Type == nodeType);
                node.Name = $"{baseName}-{existingCount + 1}";
            }
            
            return node;
        }

        /// <summary>
        /// Creates a node with custom name
        /// </summary>
        public static NodeData CreateNode(string nodeType, string name, double x, double y)
        {
            var node = CreateNode(nodeType, x, y);
            node.Name = name;
            return node;
        }

        private static string GetDefaultName(string nodeType)
        {
            // Capitalize first letter
            if (string.IsNullOrEmpty(nodeType)) return "Node";
            return char.ToUpper(nodeType[0]) + nodeType.Substring(1);
        }

        private static double GetDefaultWidth(string nodeType)
        {
            return nodeType switch
            {
                NodeTypes.Buffer => 40,
                "decision" => 50,
                "merge" => 50,
                _ => 60
            };
        }

        private static double GetDefaultHeight(string nodeType)
        {
            return nodeType switch
            {
                NodeTypes.Buffer => 40,
                "decision" => 50,
                "merge" => 50,
                _ => 40
            };
        }

        private static string GetDefaultColor(string nodeType)
        {
            return nodeType switch
            {
                NodeTypes.Machine => "#4A90D9",
                NodeTypes.Buffer => "#F5A623",
                NodeTypes.Workstation => "#7ED321",
                NodeTypes.Inspection => "#9013FE",
                NodeTypes.Source => "#50E3C2",
                NodeTypes.Sink => "#D0021B",
                "decision" => "#F8E71C",
                "merge" => "#BD10E0",
                _ => "#888888"
            };
        }

        private static void ConfigureSimulationDefaults(NodeData node)
        {
            node.Simulation = new SimulationParams();

            switch (node.Type)
            {
                case NodeTypes.Machine:
                    node.Simulation.Servers = 1;
                    node.Simulation.Capacity = 1;
                    node.Simulation.ProcessTime = new DistributionData { Distribution = "constant", Value = 5 };
                    break;
                case NodeTypes.Buffer:
                    node.Simulation.Capacity = 10;
                    node.Simulation.QueueDiscipline = "FIFO";
                    break;
                case NodeTypes.Source:
                    node.Simulation.InterarrivalTime = new DistributionData { Distribution = "exponential", Mean = 5 };
                    break;
                case NodeTypes.Inspection:
                    node.Simulation.ProcessTime = new DistributionData { Distribution = "constant", Value = 2 };
                    break;
            }
        }

        /// <summary>
        /// Creates a layout from an existing layout (clone)
        /// </summary>
        public static LayoutData Clone(LayoutData source)
        {
            var json = Helpers.JsonHelper.Serialize(source);
            return Helpers.JsonHelper.Deserialize<LayoutData>(json) ?? CreateDefault();
        }
    }
}
