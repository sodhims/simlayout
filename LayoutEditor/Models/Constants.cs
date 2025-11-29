namespace LayoutEditor.Models
{
    /// <summary>
    /// Node type constants
    /// </summary>
    public static class NodeTypes
    {
        public const string Source = "source";
        public const string Sink = "sink";
        public const string Machine = "machine";
        public const string Buffer = "buffer";
        public const string Storage = "storage";
        public const string Workstation = "workstation";
        public const string Inspection = "inspection";
        public const string Conveyor = "conveyor";
        public const string Junction = "junction";
        public const string AgvStation = "agv_station";
    }

    /// <summary>
    /// Path type constants
    /// </summary>
    public static class PathTypes
    {
        public const string Single = "single";
        public const string Double = "double";
    }

    /// <summary>
    /// Routing mode constants
    /// </summary>
    public static class RoutingModes
    {
        public const string Direct = "direct";
        public const string Manhattan = "manhattan";
        public const string Corridor = "corridor";
    }

    /// <summary>
    /// Transport type constants
    /// </summary>
    public static class TransportTypes
    {
        public const string Conveyor = "conveyor";
        public const string AGV = "agv";
        public const string Manual = "manual";
        public const string Crane = "crane";
    }

    /// <summary>
    /// Buffer discipline constants
    /// </summary>
    public static class BufferDisciplines
    {
        public const string FIFO = "fifo";
        public const string LIFO = "lifo";
        public const string Priority = "priority";
    }

    /// <summary>
    /// Cell type constants for canonical naming
    /// </summary>
    public static class CellTypes
    {
        public const string MachiningCell = "machining_cell";
        public const string AssemblyCell = "assembly_cell";
        public const string InspectionCell = "inspection_cell";
        public const string PackingCell = "packing_cell";
        public const string StorageCell = "storage_cell";
        public const string BufferCell = "buffer_cell";
    }

    /// <summary>
    /// Terminal type constants
    /// </summary>
    public static class TerminalTypes
    {
        public const string Entry = "entry";
        public const string Exit = "exit";
    }

    /// <summary>
    /// Distribution type constants
    /// </summary>
    public static class DistributionTypes
    {
        public const string Constant = "constant";
        public const string Exponential = "exponential";
        public const string Normal = "normal";
        public const string Uniform = "uniform";
        public const string Triangular = "triangular";
    }
}
