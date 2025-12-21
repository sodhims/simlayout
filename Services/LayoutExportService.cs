using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Models.Exports;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for exporting layout data in structured formats
    /// For MES systems, simulation tools, and planning applications
    /// </summary>
    public class LayoutExportService
    {
        /// <summary>
        /// Export material flow graph
        /// Nodes = workstations, Edges = transport connections
        /// </summary>
        public MaterialFlowGraph ExportMaterialFlow(LayoutData layout)
        {
            if (layout == null)
                return null;

            var graph = new MaterialFlowGraph
            {
                LayoutId = layout.Id,
                ExportTime = DateTime.UtcNow
            };

            // Add workstation nodes
            var workstationTypes = new[] { "Machine", "Station", "Workstation", "Assembly" };
            foreach (var node in layout.Nodes.Where(n => workstationTypes.Contains(n.Type)))
            {
                graph.Nodes.Add(new FlowNode
                {
                    Id = node.Id,
                    Name = node.Type,
                    Type = node.Type,
                    X = node.Visual.X,
                    Y = node.Visual.Y,
                    Properties = new Dictionary<string, object>
                    {
                        { "Width", node.Visual.Width },
                        { "Height", node.Visual.Height }
                    }
                });
            }

            // Add material flow edges (part flow paths)
            foreach (var path in layout.Paths.Where(p => p.ConnectionType == ConnectionTypes.PartFlow))
            {
                graph.Edges.Add(new FlowEdge
                {
                    Id = path.Id,
                    FromNodeId = path.From,
                    ToNodeId = path.To,
                    TransportType = "Manual",
                    Capacity = 1.0
                });
            }

            // Add conveyor edges
            foreach (var conveyor in layout.Conveyors)
            {
                graph.Edges.Add(new FlowEdge
                {
                    Id = conveyor.Id,
                    FromNodeId = conveyor.FromNodeId,
                    ToNodeId = conveyor.ToNodeId,
                    TransportType = "Conveyor",
                    Capacity = 10.0, // Default capacity
                    Properties = new Dictionary<string, object>
                    {
                        { "Speed", conveyor.Speed }
                    }
                });
            }

            return graph;
        }

        /// <summary>
        /// Export AGV network definition
        /// Includes paths, segments, stations, zones, and rules
        /// </summary>
        public AGVNetworkDefinition ExportAGVNetwork(LayoutData layout)
        {
            if (layout == null)
                return null;

            var network = new AGVNetworkDefinition
            {
                LayoutId = layout.Id,
                ExportTime = DateTime.UtcNow
            };

            // Export AGV paths
            var agvPaths = layout.Paths.Where(p => p.ConnectionType == ConnectionTypes.AGVTrack).ToList();
            foreach (var path in agvPaths)
            {
                var agvPath = new AGVPath
                {
                    Id = path.Id,
                    Name = $"Path {path.From} → {path.To}",
                    FromStationId = path.From,
                    ToStationId = path.To,
                    IsBidirectional = false,
                    MaxSpeed = 1.5 // Default AGV speed m/s
                };

                // Add waypoints (from and to node positions)
                var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = layout.Nodes.FirstOrDefault(n => n.Id == path.To);

                if (fromNode != null)
                {
                    agvPath.Waypoints.Add(new AGVWaypoint
                    {
                        X = fromNode.Visual.X,
                        Y = fromNode.Visual.Y,
                        Speed = 1.5
                    });
                }

                if (toNode != null)
                {
                    agvPath.Waypoints.Add(new AGVWaypoint
                    {
                        X = toNode.Visual.X,
                        Y = toNode.Visual.Y,
                        Speed = 1.5
                    });
                }

                network.Paths.Add(agvPath);

                // Create segment for this path
                if (fromNode != null && toNode != null)
                {
                    var dx = toNode.Visual.X - fromNode.Visual.X;
                    var dy = toNode.Visual.Y - fromNode.Visual.Y;
                    var length = Math.Sqrt(dx * dx + dy * dy);

                    network.Segments.Add(new AGVSegment
                    {
                        Id = $"seg-{path.Id}",
                        PathId = path.Id,
                        StartX = fromNode.Visual.X,
                        StartY = fromNode.Visual.Y,
                        EndX = toNode.Visual.X,
                        EndY = toNode.Visual.Y,
                        Length = length
                    });
                }
            }

            // Export AGV stations (nodes connected to AGV paths)
            var stationIds = agvPaths.SelectMany(p => new[] { p.From, p.To }).Distinct();
            foreach (var stationId in stationIds)
            {
                var node = layout.Nodes.FirstOrDefault(n => n.Id == stationId);
                if (node != null)
                {
                    network.Stations.Add(new AGVStation
                    {
                        Id = node.Id,
                        Name = node.Type,
                        X = node.Visual.X,
                        Y = node.Visual.Y,
                        StationType = "Pickup", // Default type
                        Capacity = 1
                    });
                }
            }

            // Export zones
            foreach (var zone in layout.Zones)
            {
                network.Zones.Add(new AGVZone
                {
                    Id = zone.Id,
                    Name = zone.Name,
                    X = zone.X,
                    Y = zone.Y,
                    Width = zone.Width,
                    Height = zone.Height,
                    MaxAGVs = 3 // Default max AGVs per zone
                });
            }

            return network;
        }

        /// <summary>
        /// Export resource capacities for simulation
        /// Includes machines, buffers, operators, and process times
        /// </summary>
        public ResourceCapacities ExportResourceCapacities(LayoutData layout)
        {
            if (layout == null)
                return null;

            var resources = new ResourceCapacities
            {
                LayoutId = layout.Id,
                ExportTime = DateTime.UtcNow
            };

            // Export machines
            var machineTypes = new[] { "Machine", "Workstation", "Assembly" };
            foreach (var node in layout.Nodes.Where(n => machineTypes.Contains(n.Type)))
            {
                resources.Machines.Add(new MachineResource
                {
                    Id = node.Id,
                    Name = node.Type,
                    Type = node.Type,
                    Capacity = 1, // Single processing capacity
                    Availability = 0.85, // 85% availability (default)
                    MTTR = 2.0, // 2 hours mean time to repair
                    MTBF = 100.0 // 100 hours mean time between failures
                });
            }

            // Export buffers
            foreach (var node in layout.Nodes.Where(n => n.Type == "Buffer"))
            {
                resources.Buffers.Add(new BufferResource
                {
                    Id = node.Id,
                    Name = "Buffer",
                    Capacity = 10, // Default buffer capacity
                    BufferType = "WIP"
                });
            }

            // Export default operator
            resources.Operators.Add(new OperatorResource
            {
                Id = "op-default",
                Name = "Default Operator",
                Skills = new List<string> { "Assembly", "Machine Operation" },
                EfficiencyFactor = 1.0
            });

            // Export default process times
            foreach (var machine in resources.Machines)
            {
                resources.ProcessTimes[$"{machine.Id}-default"] = new ProcessTime
                {
                    ProcessId = $"{machine.Id}-default",
                    MachineId = machine.Id,
                    SetupTime = 5.0, // 5 minutes
                    CycleTime = 10.0, // 10 minutes
                    TeardownTime = 2.0 // 2 minutes
                };
            }

            return resources;
        }

        /// <summary>
        /// Export crane network definition
        /// Includes cranes, coverages, handoffs, and drop zones
        /// </summary>
        public CraneNetworkDefinition ExportCraneNetwork(LayoutData layout)
        {
            if (layout == null)
                return null;

            var network = new CraneNetworkDefinition
            {
                LayoutId = layout.Id,
                ExportTime = DateTime.UtcNow
            };

            // Export cranes
            var cranes = layout.Nodes.Where(n => n.Type == "EOTCrane" || n.Type == "Crane").ToList();
            foreach (var crane in cranes)
            {
                network.Cranes.Add(new CraneDefinition
                {
                    Id = crane.Id,
                    Name = crane.Type,
                    X = crane.Visual.X,
                    Y = crane.Visual.Y,
                    Span = 50.0, // Default span (meters)
                    Height = 10.0, // Default height (meters)
                    MaxLoad = 10000.0, // Default 10 tons
                    Speed = 1.0, // Default 1 m/s
                    CraneType = "EOT"
                });

                // Create coverage area for crane
                var coverageRadius = 200.0;
                network.Coverages.Add(new CraneCoverage
                {
                    CraneId = crane.Id,
                    MinX = crane.Visual.X - coverageRadius,
                    MaxX = crane.Visual.X + coverageRadius,
                    MinY = crane.Visual.Y - coverageRadius,
                    MaxY = crane.Visual.Y + coverageRadius
                });
            }

            // Find handoff zones (areas where crane coverages overlap)
            for (int i = 0; i < cranes.Count; i++)
            {
                for (int j = i + 1; j < cranes.Count; j++)
                {
                    var crane1 = cranes[i];
                    var crane2 = cranes[j];

                    // Simplified: create handoff at midpoint
                    var handoffX = (crane1.Visual.X + crane2.Visual.X) / 2;
                    var handoffY = (crane1.Visual.Y + crane2.Visual.Y) / 2;

                    network.Handoffs.Add(new CraneHandoff
                    {
                        Id = $"handoff-{crane1.Id}-{crane2.Id}",
                        FromCraneId = crane1.Id,
                        ToCraneId = crane2.Id,
                        X = handoffX,
                        Y = handoffY
                    });
                }
            }

            // Create drop zones near each crane
            foreach (var crane in cranes)
            {
                network.DropZones.Add(new CraneDropZone
                {
                    Id = $"dropzone-{crane.Id}",
                    Name = $"Drop Zone for {crane.Id}",
                    CraneId = crane.Id,
                    X = crane.Visual.X + 20,
                    Y = crane.Visual.Y + 20,
                    Width = 30,
                    Height = 30
                });
            }

            return network;
        }
    }
}
