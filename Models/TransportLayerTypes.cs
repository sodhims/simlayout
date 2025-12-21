namespace LayoutEditor.Models
{
    /// <summary>
    /// 8-layer transport architecture for layout editor.
    /// Values are sequential integers starting at 0, with Z-order increasing by layer number.
    /// </summary>
    public enum LayerType
    {
        /// <summary>
        /// Layer 0: Fixed infrastructure (walls, columns, runways, building structure)
        /// </summary>
        Infrastructure = 0,

        /// <summary>
        /// Layer 1: Spatial planning (zones, corridors, planning areas)
        /// </summary>
        Spatial = 1,

        /// <summary>
        /// Layer 2: Equipment (machines, buffers, workstations, stationary equipment)
        /// </summary>
        Equipment = 2,

        /// <summary>
        /// Layer 3: Local flow within cells (internal cell paths, cell-level transport)
        /// </summary>
        LocalFlow = 3,

        /// <summary>
        /// Layer 4: Guided transport systems (AGVs, AMRs, guided vehicles)
        /// </summary>
        GuidedTransport = 4,

        /// <summary>
        /// Layer 5: Overhead transport systems (EOT cranes, jib cranes, monorails)
        /// </summary>
        OverheadTransport = 5,

        /// <summary>
        /// Layer 6: Flexible transport systems (forklifts, tugger trains, manual transport)
        /// </summary>
        FlexibleTransport = 6,

        /// <summary>
        /// Layer 7: Pedestrian flow (walkways, safety zones, personnel movement)
        /// </summary>
        Pedestrian = 7
    }
}
