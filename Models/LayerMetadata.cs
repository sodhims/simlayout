using System.Linq;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Metadata defining properties for each transport layer.
    /// Includes display name, description, Z-order base, default color, and default state flags.
    /// </summary>
    public class LayerMetadata
    {
        public LayerType Layer { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public int ZOrderBase { get; init; }
        public string DefaultColor { get; init; }
        public bool DefaultVisible { get; init; }
        public bool DefaultEditable { get; init; }
        public bool DefaultLocked { get; init; }

        public LayerMetadata(
            LayerType layer,
            string name,
            string description,
            int zOrderBase,
            string defaultColor,
            bool defaultVisible = true,
            bool defaultEditable = true,
            bool defaultLocked = false)
        {
            Layer = layer;
            Name = name;
            Description = description;
            ZOrderBase = zOrderBase;
            DefaultColor = defaultColor;
            DefaultVisible = defaultVisible;
            DefaultEditable = defaultEditable;
            DefaultLocked = defaultLocked;
        }

        /// <summary>
        /// Static array of all layer metadata.
        /// Z-order increases with layer number (Infrastructure lowest, Pedestrian highest).
        /// </summary>
        public static readonly LayerMetadata[] AllLayers = new[]
        {
            new LayerMetadata(
                LayerType.Infrastructure,
                "Infrastructure",
                "Fixed building infrastructure: walls, columns, runways, structural elements",
                zOrderBase: 100,
                defaultColor: "#505050"
            ),

            new LayerMetadata(
                LayerType.Spatial,
                "Spatial Planning",
                "Spatial organization: zones, corridors, planning areas, boundaries",
                zOrderBase: 200,
                defaultColor: "#3498DB"
            ),

            new LayerMetadata(
                LayerType.Equipment,
                "Equipment",
                "Production equipment: machines, buffers, workstations, stationary resources",
                zOrderBase: 300,
                defaultColor: "#2C3E50"
            ),

            new LayerMetadata(
                LayerType.LocalFlow,
                "Local Flow",
                "Cell-level material flow: internal paths, local transport within manufacturing cells",
                zOrderBase: 400,
                defaultColor: "#27AE60"
            ),

            new LayerMetadata(
                LayerType.GuidedTransport,
                "Guided Transport",
                "Automated guided transport: AGV networks, AMR paths, charging stations, waypoints",
                zOrderBase: 500,
                defaultColor: "#F39C12"
            ),

            new LayerMetadata(
                LayerType.OverheadTransport,
                "Overhead Transport",
                "Overhead material handling: EOT cranes, jib cranes, monorails, runways",
                zOrderBase: 600,
                defaultColor: "#8E44AD"
            ),

            new LayerMetadata(
                LayerType.FlexibleTransport,
                "Flexible Transport",
                "Manual and flexible transport: forklifts, tugger trains, manual handling routes",
                zOrderBase: 700,
                defaultColor: "#E67E22"
            ),

            new LayerMetadata(
                LayerType.Pedestrian,
                "Pedestrian",
                "Personnel movement: walkways, safety zones, ergonomic areas, pedestrian paths",
                zOrderBase: 800,
                defaultColor: "#E74C3C"
            )
        };

        /// <summary>
        /// Get metadata for a specific layer type.
        /// </summary>
        public static LayerMetadata GetMetadata(LayerType layer)
        {
            return AllLayers[(int)layer];
        }

        /// <summary>
        /// Get metadata by layer index (0-7).
        /// </summary>
        public static LayerMetadata GetMetadataByIndex(int index)
        {
            if (index < 0 || index >= AllLayers.Length)
                return AllLayers[0]; // Default to Infrastructure
            return AllLayers[index];
        }
    }
}
