using System;
using System.Collections.Generic;

namespace LayoutEditor.Models.Exports
{
    /// <summary>
    /// Material flow graph for simulation/planning systems
    /// Nodes represent workstations, edges represent transport connections
    /// </summary>
    public class MaterialFlowGraph
    {
        public List<FlowNode> Nodes { get; set; } = new List<FlowNode>();
        public List<FlowEdge> Edges { get; set; } = new List<FlowEdge>();
        public string LayoutId { get; set; }
        public DateTime ExportTime { get; set; }
    }

    public class FlowNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    public class FlowEdge
    {
        public string Id { get; set; }
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public string TransportType { get; set; } // AGV, Conveyor, Crane, Manual
        public double Capacity { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// AGV network definition for AGV control systems
    /// </summary>
    public class AGVNetworkDefinition
    {
        public List<AGVPath> Paths { get; set; } = new List<AGVPath>();
        public List<AGVSegment> Segments { get; set; } = new List<AGVSegment>();
        public List<AGVStation> Stations { get; set; } = new List<AGVStation>();
        public List<AGVZone> Zones { get; set; } = new List<AGVZone>();
        public AGVNetworkRules Rules { get; set; } = new AGVNetworkRules();
        public string LayoutId { get; set; }
        public DateTime ExportTime { get; set; }
    }

    public class AGVPath
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FromStationId { get; set; }
        public string ToStationId { get; set; }
        public List<AGVWaypoint> Waypoints { get; set; } = new List<AGVWaypoint>();
        public bool IsBidirectional { get; set; }
        public double MaxSpeed { get; set; }
    }

    public class AGVSegment
    {
        public string Id { get; set; }
        public string PathId { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Length { get; set; }
    }

    public class AGVStation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string StationType { get; set; } // Pickup, Dropoff, Charging, Parking
        public int Capacity { get; set; }
    }

    public class AGVZone
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int MaxAGVs { get; set; }
    }

    public class AGVWaypoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
    }

    public class AGVNetworkRules
    {
        public double DefaultSpeed { get; set; } = 1.5; // m/s
        public double DefaultAcceleration { get; set; } = 0.5; // m/s²
        public double MinimumSeparation { get; set; } = 2.0; // meters
    }

    /// <summary>
    /// Resource capacities for simulation systems
    /// </summary>
    public class ResourceCapacities
    {
        public List<MachineResource> Machines { get; set; } = new List<MachineResource>();
        public List<BufferResource> Buffers { get; set; } = new List<BufferResource>();
        public List<OperatorResource> Operators { get; set; } = new List<OperatorResource>();
        public Dictionary<string, ProcessTime> ProcessTimes { get; set; } = new Dictionary<string, ProcessTime>();
        public string LayoutId { get; set; }
        public DateTime ExportTime { get; set; }
    }

    public class MachineResource
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; } // Parallel processing capacity
        public double Availability { get; set; } // 0.0 to 1.0
        public double MTTR { get; set; } // Mean Time To Repair (hours)
        public double MTBF { get; set; } // Mean Time Between Failures (hours)
    }

    public class BufferResource
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public string BufferType { get; set; } // Input, Output, WIP
    }

    public class OperatorResource
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public double EfficiencyFactor { get; set; } = 1.0;
    }

    public class ProcessTime
    {
        public string ProcessId { get; set; }
        public string MachineId { get; set; }
        public double SetupTime { get; set; } // minutes
        public double CycleTime { get; set; } // minutes
        public double TeardownTime { get; set; } // minutes
    }

    /// <summary>
    /// Crane network definition for crane control systems
    /// </summary>
    public class CraneNetworkDefinition
    {
        public List<CraneDefinition> Cranes { get; set; } = new List<CraneDefinition>();
        public List<CraneCoverage> Coverages { get; set; } = new List<CraneCoverage>();
        public List<CraneHandoff> Handoffs { get; set; } = new List<CraneHandoff>();
        public List<CraneDropZone> DropZones { get; set; } = new List<CraneDropZone>();
        public string LayoutId { get; set; }
        public DateTime ExportTime { get; set; }
    }

    public class CraneDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Span { get; set; }
        public double Height { get; set; }
        public double MaxLoad { get; set; } // kg
        public double Speed { get; set; } // m/s
        public string CraneType { get; set; } // EOT, Gantry, Jib
    }

    public class CraneCoverage
    {
        public string CraneId { get; set; }
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
    }

    public class CraneHandoff
    {
        public string Id { get; set; }
        public string FromCraneId { get; set; }
        public string ToCraneId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class CraneDropZone
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CraneId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
