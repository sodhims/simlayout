using System;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 10A Tests: Query APIs
    /// Tests element queries, spatial queries, and relationship queries
    /// </summary>
    public static class Stage10ATests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 10A Tests: Query APIs ===\n");

            var tests = new Func<bool>[]
            {
                Test1_GetWorkstationsReturnsAll,
                Test2_GetElementsInZoneFilters,
                Test3_SpatialQueryWorks,
                Test4_NearestQueryWorks,
                Test5_ConnectionQueryWorks
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

            Console.WriteLine($"\nStage 10A Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: GetWorkstations returns all workstation-type nodes
        /// </summary>
        private static bool Test1_GetWorkstationsReturnsAll()
        {
            var layout = new LayoutData();
            var queryService = new LayoutQueryService();

            // Add various node types
            var machine1 = new NodeData { Id = "m1", Type = "Machine" };
            var machine2 = new NodeData { Id = "m2", Type = "Machine" };
            var station1 = new NodeData { Id = "s1", Type = "Station" };
            var buffer = new NodeData { Id = "b1", Type = "Buffer" }; // Not a workstation
            var assembly = new NodeData { Id = "a1", Type = "Assembly" };

            layout.Nodes.Add(machine1);
            layout.Nodes.Add(machine2);
            layout.Nodes.Add(station1);
            layout.Nodes.Add(buffer);
            layout.Nodes.Add(assembly);

            // Query workstations
            var workstations = queryService.GetWorkstations(layout);

            // Should return 4 workstations (2 machines, 1 station, 1 assembly)
            bool correctCount = workstations.Count == 4;
            bool hasMachine1 = workstations.Any(n => n.Id == "m1");
            bool hasMachine2 = workstations.Any(n => n.Id == "m2");
            bool hasStation1 = workstations.Any(n => n.Id == "s1");
            bool hasAssembly = workstations.Any(n => n.Id == "a1");
            bool doesNotHaveBuffer = !workstations.Any(n => n.Id == "b1");

            return correctCount && hasMachine1 && hasMachine2 && hasStation1 && hasAssembly && doesNotHaveBuffer;
        }

        /// <summary>
        /// Test 2: GetElementsInZone filters by zone boundaries
        /// </summary>
        private static bool Test2_GetElementsInZoneFilters()
        {
            var layout = new LayoutData();
            var queryService = new LayoutQueryService();

            // Create a zone
            var zone = new ZoneData
            {
                Id = "zone1",
                Name = "Assembly Zone",
                X = 100,
                Y = 100,
                Width = 200,
                Height = 200
            };
            layout.Zones.Add(zone);

            // Add nodes - some inside, some outside zone
            var inside1 = new NodeData { Id = "in1", Type = "Machine" };
            inside1.Visual.X = 150;
            inside1.Visual.Y = 150;

            var inside2 = new NodeData { Id = "in2", Type = "Station" };
            inside2.Visual.X = 250;
            inside2.Visual.Y = 200;

            var outside1 = new NodeData { Id = "out1", Type = "Machine" };
            outside1.Visual.X = 50; // Outside zone
            outside1.Visual.Y = 50;

            var outside2 = new NodeData { Id = "out2", Type = "Station" };
            outside2.Visual.X = 400; // Outside zone
            outside2.Visual.Y = 400;

            layout.Nodes.Add(inside1);
            layout.Nodes.Add(inside2);
            layout.Nodes.Add(outside1);
            layout.Nodes.Add(outside2);

            // Query elements in zone
            var elementsInZone = queryService.GetElementsInZone(layout, "Assembly Zone");

            // Should return only 2 elements inside the zone
            bool correctCount = elementsInZone.Count == 2;
            bool hasInside1 = elementsInZone.Any(n => n.Id == "in1");
            bool hasInside2 = elementsInZone.Any(n => n.Id == "in2");
            bool doesNotHaveOutside = !elementsInZone.Any(n => n.Id == "out1" || n.Id == "out2");

            return correctCount && hasInside1 && hasInside2 && doesNotHaveOutside;
        }

        /// <summary>
        /// Test 3: Spatial query returns elements in region
        /// </summary>
        private static bool Test3_SpatialQueryWorks()
        {
            var layout = new LayoutData();
            var queryService = new LayoutQueryService();

            // Add nodes at various positions
            var node1 = new NodeData { Id = "n1", Type = "Machine" };
            node1.Visual.X = 100;
            node1.Visual.Y = 100;
            node1.Visual.Width = 50;
            node1.Visual.Height = 50;

            var node2 = new NodeData { Id = "n2", Type = "Station" };
            node2.Visual.X = 200;
            node2.Visual.Y = 200;
            node2.Visual.Width = 50;
            node2.Visual.Height = 50;

            var node3 = new NodeData { Id = "n3", Type = "Buffer" };
            node3.Visual.X = 400;
            node3.Visual.Y = 400;
            node3.Visual.Width = 50;
            node3.Visual.Height = 50;

            layout.Nodes.Add(node1);
            layout.Nodes.Add(node2);
            layout.Nodes.Add(node3);

            // Query region (0,0) to (300,300) - should include n1 and n2
            var elementsInRegion = queryService.GetElementsInRegion(layout, 0, 0, 300, 300);

            bool correctCount = elementsInRegion.Count == 2;
            bool hasNode1 = elementsInRegion.Any(n => n.Id == "n1");
            bool hasNode2 = elementsInRegion.Any(n => n.Id == "n2");
            bool doesNotHaveNode3 = !elementsInRegion.Any(n => n.Id == "n3");

            return correctCount && hasNode1 && hasNode2 && doesNotHaveNode3;
        }

        /// <summary>
        /// Test 4: Nearest query returns correct element
        /// </summary>
        private static bool Test4_NearestQueryWorks()
        {
            var layout = new LayoutData();
            var queryService = new LayoutQueryService();

            // Add workstations at different distances
            var near = new NodeData { Id = "near", Type = "Machine" };
            near.Visual.X = 100;
            near.Visual.Y = 100;
            near.Visual.Width = 50;
            near.Visual.Height = 50;

            var far = new NodeData { Id = "far", Type = "Machine" };
            far.Visual.X = 500;
            far.Visual.Y = 500;
            far.Visual.Width = 50;
            far.Visual.Height = 50;

            layout.Nodes.Add(near);
            layout.Nodes.Add(far);

            // Query nearest to point (110, 110) - should be "near"
            var nearest = queryService.GetNearestWorkstation(layout, 110, 110);

            bool correctNearest = nearest != null && nearest.Id == "near";

            // Query nearest to point (490, 490) - should be "far"
            var nearest2 = queryService.GetNearestWorkstation(layout, 490, 490);
            bool correctNearest2 = nearest2 != null && nearest2.Id == "far";

            return correctNearest && correctNearest2;
        }

        /// <summary>
        /// Test 5: Connection query returns related elements
        /// </summary>
        private static bool Test5_ConnectionQueryWorks()
        {
            var layout = new LayoutData();
            var queryService = new LayoutQueryService();

            // Create nodes
            var station1 = new NodeData { Id = "s1", Type = "Station" };
            var station2 = new NodeData { Id = "s2", Type = "Station" };
            layout.Nodes.Add(station1);
            layout.Nodes.Add(station2);

            // Create paths connecting stations
            var path1 = new PathData
            {
                Id = "p1",
                From = "s1",
                To = "s2",
                ConnectionType = ConnectionTypes.AGVTrack
            };
            layout.Paths.Add(path1);

            var path2 = new PathData
            {
                Id = "p2",
                From = "s2",
                To = "s1",
                ConnectionType = ConnectionTypes.AGVTrack
            };
            layout.Paths.Add(path2);

            // Create conveyor
            var conveyor = new ConveyorData
            {
                Id = "c1",
                FromNodeId = "s1",
                ToNodeId = "s2"
            };
            layout.Conveyors.Add(conveyor);

            // Query transport serving station s1
            var transportForS1 = queryService.GetTransportServingStation(layout, "s1");

            // Should return 3 elements (2 paths + 1 conveyor)
            bool correctCount = transportForS1.Count == 3;

            // Query connected transport
            var connectedToS1 = queryService.GetConnectedTransport(layout, "s1");
            bool connectedCorrect = connectedToS1.Count == 3;

            return correctCount && connectedCorrect;
        }
    }
}
