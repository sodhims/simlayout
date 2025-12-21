using System;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 10B Tests: Structured Exports
    /// Tests material flow, AGV network, resource, and crane exports
    /// </summary>
    public static class Stage10BTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 10B Tests: Structured Exports ===\n");

            var tests = new Func<bool>[]
            {
                Test1_MaterialFlowExportWorks,
                Test2_AGVNetworkExportWorks,
                Test3_ResourceExportWorks,
                Test4_CraneNetworkExportWorks
            };

            int passed = 0;
            int failed = 0;

            for (int i = 0; i < tests.Length; i++)
            {
                try
                {
                    bool result = tests[i]();
                    if (result)
                    {
                        passed++;
                        Console.WriteLine($"✓ Test {i + 1} passed");
                    }
                    else
                    {
                        failed++;
                        Console.WriteLine($"✗ Test {i + 1} failed");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"✗ Test {i + 1} failed with exception: {ex.Message}");
                }
            }

            Console.WriteLine($"\nStage 10B Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Material flow export creates correct graph structure
        /// </summary>
        private static bool Test1_MaterialFlowExportWorks()
        {
            var layout = new LayoutData();
            var exportService = new LayoutExportService();

            // Create workstation nodes
            var machine1 = new NodeData { Id = "m1", Type = "Machine" };
            machine1.Visual.X = 100;
            machine1.Visual.Y = 100;

            var machine2 = new NodeData { Id = "m2", Type = "Machine" };
            machine2.Visual.X = 200;
            machine2.Visual.Y = 200;

            var station = new NodeData { Id = "s1", Type = "Station" };
            station.Visual.X = 300;
            station.Visual.Y = 300;

            layout.Nodes.Add(machine1);
            layout.Nodes.Add(machine2);
            layout.Nodes.Add(station);

            // Create material flow paths
            var flow1 = new PathData
            {
                Id = "f1",
                From = "m1",
                To = "m2",
                ConnectionType = ConnectionTypes.PartFlow
            };

            var flow2 = new PathData
            {
                Id = "f2",
                From = "m2",
                To = "s1",
                ConnectionType = ConnectionTypes.PartFlow
            };

            layout.Paths.Add(flow1);
            layout.Paths.Add(flow2);

            // Create conveyor
            var conveyor = new ConveyorData
            {
                Id = "c1",
                FromNodeId = "m1",
                ToNodeId = "s1"
            };
            conveyor.Speed = 1.0;
            layout.Conveyors.Add(conveyor);

            // Export material flow
            var graph = exportService.ExportMaterialFlow(layout);

            // Verify graph structure
            bool hasCorrectNodeCount = graph.Nodes.Count == 3;
            bool hasCorrectEdgeCount = graph.Edges.Count == 3; // 2 part flows + 1 conveyor

            bool hasM1Node = graph.Nodes.Any(n => n.Id == "m1");
            bool hasM2Node = graph.Nodes.Any(n => n.Id == "m2");
            bool hasS1Node = graph.Nodes.Any(n => n.Id == "s1");

            bool hasFlow1Edge = graph.Edges.Any(e => e.Id == "f1");
            bool hasFlow2Edge = graph.Edges.Any(e => e.Id == "f2");
            bool hasConveyorEdge = graph.Edges.Any(e => e.Id == "c1" && e.TransportType == "Conveyor");

            bool hasLayoutId = !string.IsNullOrEmpty(graph.LayoutId);

            return hasCorrectNodeCount && hasCorrectEdgeCount &&
                   hasM1Node && hasM2Node && hasS1Node &&
                   hasFlow1Edge && hasFlow2Edge && hasConveyorEdge &&
                   hasLayoutId;
        }

        /// <summary>
        /// Test 2: AGV network export includes all paths
        /// </summary>
        private static bool Test2_AGVNetworkExportWorks()
        {
            var layout = new LayoutData();
            var exportService = new LayoutExportService();

            // Create stations
            var station1 = new NodeData { Id = "st1", Type = "Station" };
            station1.Visual.X = 100;
            station1.Visual.Y = 100;

            var station2 = new NodeData { Id = "st2", Type = "Station" };
            station2.Visual.X = 200;
            station2.Visual.Y = 200;

            var station3 = new NodeData { Id = "st3", Type = "Station" };
            station3.Visual.X = 300;
            station3.Visual.Y = 100;

            layout.Nodes.Add(station1);
            layout.Nodes.Add(station2);
            layout.Nodes.Add(station3);

            // Create AGV paths
            var agvPath1 = new PathData
            {
                Id = "agv1",
                From = "st1",
                To = "st2",
                ConnectionType = ConnectionTypes.AGVTrack
            };

            var agvPath2 = new PathData
            {
                Id = "agv2",
                From = "st2",
                To = "st3",
                ConnectionType = ConnectionTypes.AGVTrack
            };

            layout.Paths.Add(agvPath1);
            layout.Paths.Add(agvPath2);

            // Create a zone
            var zone = new ZoneData
            {
                Id = "z1",
                Name = "AGV Zone 1",
                X = 50,
                Y = 50,
                Width = 300,
                Height = 200
            };
            layout.Zones.Add(zone);

            // Export AGV network
            var network = exportService.ExportAGVNetwork(layout);

            // Verify network structure
            bool hasCorrectPathCount = network.Paths.Count == 2;
            bool hasCorrectSegmentCount = network.Segments.Count == 2;
            bool hasCorrectStationCount = network.Stations.Count == 3;
            bool hasCorrectZoneCount = network.Zones.Count == 1;

            bool hasPath1 = network.Paths.Any(p => p.Id == "agv1");
            bool hasPath2 = network.Paths.Any(p => p.Id == "agv2");

            bool hasSt1Station = network.Stations.Any(s => s.Id == "st1");
            bool hasSt2Station = network.Stations.Any(s => s.Id == "st2");
            bool hasSt3Station = network.Stations.Any(s => s.Id == "st3");

            bool hasZone = network.Zones.Any(z => z.Id == "z1");

            bool hasRules = network.Rules != null && network.Rules.DefaultSpeed > 0;

            return hasCorrectPathCount && hasCorrectSegmentCount &&
                   hasCorrectStationCount && hasCorrectZoneCount &&
                   hasPath1 && hasPath2 &&
                   hasSt1Station && hasSt2Station && hasSt3Station &&
                   hasZone && hasRules;
        }

        /// <summary>
        /// Test 3: Resource export includes machines, buffers, and capacities
        /// </summary>
        private static bool Test3_ResourceExportWorks()
        {
            var layout = new LayoutData();
            var exportService = new LayoutExportService();

            // Create machines
            var machine1 = new NodeData { Id = "m1", Type = "Machine" };
            var machine2 = new NodeData { Id = "m2", Type = "Workstation" };
            var assembly = new NodeData { Id = "a1", Type = "Assembly" };

            layout.Nodes.Add(machine1);
            layout.Nodes.Add(machine2);
            layout.Nodes.Add(assembly);

            // Create buffers
            var buffer1 = new NodeData { Id = "b1", Type = "Buffer" };
            var buffer2 = new NodeData { Id = "b2", Type = "Buffer" };

            layout.Nodes.Add(buffer1);
            layout.Nodes.Add(buffer2);

            // Export resources
            var resources = exportService.ExportResourceCapacities(layout);

            // Verify resource structure
            bool hasCorrectMachineCount = resources.Machines.Count == 3;
            bool hasCorrectBufferCount = resources.Buffers.Count == 2;
            bool hasOperators = resources.Operators.Count > 0;
            bool hasProcessTimes = resources.ProcessTimes.Count > 0;

            bool hasMachine1 = resources.Machines.Any(m => m.Id == "m1");
            bool hasMachine2 = resources.Machines.Any(m => m.Id == "m2");
            bool hasAssembly = resources.Machines.Any(m => m.Id == "a1");

            bool hasBuffer1 = resources.Buffers.Any(b => b.Id == "b1");
            bool hasBuffer2 = resources.Buffers.Any(b => b.Id == "b2");

            // Check machine properties
            var machine = resources.Machines.FirstOrDefault();
            bool machineHasCapacity = machine != null && machine.Capacity > 0;
            bool machineHasAvailability = machine != null && machine.Availability > 0 && machine.Availability <= 1;

            return hasCorrectMachineCount && hasCorrectBufferCount &&
                   hasOperators && hasProcessTimes &&
                   hasMachine1 && hasMachine2 && hasAssembly &&
                   hasBuffer1 && hasBuffer2 &&
                   machineHasCapacity && machineHasAvailability;
        }

        /// <summary>
        /// Test 4: Crane network export includes coverage and handoffs
        /// </summary>
        private static bool Test4_CraneNetworkExportWorks()
        {
            var layout = new LayoutData();
            var exportService = new LayoutExportService();

            // Create cranes
            var crane1 = new NodeData { Id = "cr1", Type = "EOTCrane" };
            crane1.Visual.X = 100;
            crane1.Visual.Y = 100;

            var crane2 = new NodeData { Id = "cr2", Type = "EOTCrane" };
            crane2.Visual.X = 300;
            crane2.Visual.Y = 100;

            var crane3 = new NodeData { Id = "cr3", Type = "Crane" };
            crane3.Visual.X = 200;
            crane3.Visual.Y = 300;

            layout.Nodes.Add(crane1);
            layout.Nodes.Add(crane2);
            layout.Nodes.Add(crane3);

            // Export crane network
            var network = exportService.ExportCraneNetwork(layout);

            // Verify network structure
            bool hasCorrectCraneCount = network.Cranes.Count == 3;
            bool hasCorrectCoverageCount = network.Coverages.Count == 3; // One per crane
            bool hasHandoffs = network.Handoffs.Count > 0; // Should have handoffs between cranes
            bool hasDropZones = network.DropZones.Count == 3; // One per crane

            bool hasCrane1 = network.Cranes.Any(c => c.Id == "cr1");
            bool hasCrane2 = network.Cranes.Any(c => c.Id == "cr2");
            bool hasCrane3 = network.Cranes.Any(c => c.Id == "cr3");

            bool hasCoverage1 = network.Coverages.Any(c => c.CraneId == "cr1");
            bool hasCoverage2 = network.Coverages.Any(c => c.CraneId == "cr2");

            bool hasDropZone1 = network.DropZones.Any(d => d.CraneId == "cr1");
            bool hasDropZone2 = network.DropZones.Any(d => d.CraneId == "cr2");

            // Check crane properties
            var crane = network.Cranes.FirstOrDefault();
            bool craneHasProperties = crane != null && crane.Span > 0 && crane.MaxLoad > 0;

            // Check coverage properties
            var coverage = network.Coverages.FirstOrDefault();
            bool coverageHasBounds = coverage != null &&
                                     coverage.MaxX > coverage.MinX &&
                                     coverage.MaxY > coverage.MinY;

            return hasCorrectCraneCount && hasCorrectCoverageCount &&
                   hasHandoffs && hasDropZones &&
                   hasCrane1 && hasCrane2 && hasCrane3 &&
                   hasCoverage1 && hasCoverage2 &&
                   hasDropZone1 && hasDropZone2 &&
                   craneHasProperties && coverageHasBounds;
        }
    }
}
