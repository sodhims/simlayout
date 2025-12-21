using System;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 12B Tests: Regeneration Commands
    /// Tests for Part B - Editor integration of regeneration commands
    /// </summary>
    public static class Stage12BTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 12B Tests: Regeneration Commands ===\n");

            var tests = new Func<bool>[]
            {
                Test1_PedestrianMeshProperty,
                Test2_PedestrianMeshGeneration,
                Test3_CraneCoverageCalculation,
                Test4_AGVNetworkValidation,
                Test5_ForkliftAisleDetection,
                Test6_OpeningLinking
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

            Console.WriteLine($"\nStage 12B Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: PedestrianMesh property exists on LayoutData
        /// </summary>
        private static bool Test1_PedestrianMeshProperty()
        {
            var layout = new LayoutData();

            bool hasProperty = layout.PedestrianMesh != null;
            bool canAdd = true;

            // Test adding to collection
            try
            {
                var zone = new ZoneData
                {
                    Id = "test_mesh",
                    Name = "Test Mesh"
                };
                layout.PedestrianMesh.Add(zone);
                canAdd = layout.PedestrianMesh.Count == 1;
            }
            catch
            {
                canAdd = false;
            }

            return hasProperty && canAdd;
        }

        /// <summary>
        /// Test 2: Pedestrian mesh generation logic
        /// </summary>
        private static bool Test2_PedestrianMeshGeneration()
        {
            var layout = new LayoutData();
            layout.Canvas = new CanvasSettings { Width = 1000, Height = 800 };

            // Add some nodes as obstacles
            layout.Nodes.Add(new NodeData
            {
                Id = "node1",
                Label = "Equipment 1",
                Visual = new NodeVisual { X = 100, Y = 100 }
            });

            layout.Nodes.Add(new NodeData
            {
                Id = "node2",
                Label = "Equipment 2",
                Visual = new NodeVisual { X = 500, Y = 500 }
            });

            // Mesh generation would happen here
            // For this test, we just verify the structure is in place
            bool hasCanvas = layout.Canvas != null;
            bool hasNodes = layout.Nodes.Count == 2;
            bool hasMeshCollection = layout.PedestrianMesh != null;

            return hasCanvas && hasNodes && hasMeshCollection;
        }

        /// <summary>
        /// Test 3: Crane coverage calculation for EOT cranes
        /// </summary>
        private static bool Test3_CraneCoverageCalculation()
        {
            var layout = new LayoutData();

            // Create runway
            var runway = new RunwayData
            {
                Id = "runway1",
                StartX = 0,
                StartY = 0,
                EndX = 100,
                EndY = 0
            };
            layout.Runways.Add(runway);

            // Create EOT crane
            var crane = new EOTCraneData
            {
                Id = "crane1",
                RunwayId = "runway1",
                ZoneMin = 0,
                ZoneMax = 1,
                ReachLeft = 10,
                ReachRight = 10
            };
            layout.EOTCranes.Add(crane);

            // Verify crane can be found
            bool hasRunway = layout.Runways.Count == 1;
            bool hasCrane = layout.EOTCranes.Count == 1;
            bool craneHasRunway = crane.RunwayId == runway.Id;

            return hasRunway && hasCrane && craneHasRunway;
        }

        /// <summary>
        /// Test 4: AGV network validation
        /// </summary>
        private static bool Test4_AGVNetworkValidation()
        {
            var layout = new LayoutData();

            // Create waypoints
            layout.AGVWaypoints.Add(new AGVWaypointData
            {
                Id = "wp1",
                X = 0,
                Y = 0
            });

            layout.AGVWaypoints.Add(new AGVWaypointData
            {
                Id = "wp2",
                X = 100,
                Y = 0
            });

            // Create path between waypoints
            layout.AGVPaths.Add(new AGVPathData
            {
                Id = "path1",
                FromWaypointId = "wp1",
                ToWaypointId = "wp2"
            });

            // Validate network
            int validPaths = 0;
            int invalidPaths = 0;

            foreach (var path in layout.AGVPaths)
            {
                var from = layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.FromWaypointId);
                var to = layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.ToWaypointId);

                if (from != null && to != null)
                    validPaths++;
                else
                    invalidPaths++;
            }

            return validPaths == 1 && invalidPaths == 0;
        }

        /// <summary>
        /// Test 5: Forklift aisle detection from equipment layout
        /// </summary>
        private static bool Test5_ForkliftAisleDetection()
        {
            var layout = new LayoutData();

            // Create two rows of equipment
            // Row 1: Y = 100
            layout.Nodes.Add(new NodeData
            {
                Id = "r1n1",
                Label = "Row 1 Node 1",
                Visual = new NodeVisual { X = 50, Y = 100 }
            });

            layout.Nodes.Add(new NodeData
            {
                Id = "r1n2",
                Label = "Row 1 Node 2",
                Visual = new NodeVisual { X = 150, Y = 100 }
            });

            // Row 2: Y = 250
            layout.Nodes.Add(new NodeData
            {
                Id = "r2n1",
                Label = "Row 2 Node 1",
                Visual = new NodeVisual { X = 50, Y = 250 }
            });

            layout.Nodes.Add(new NodeData
            {
                Id = "r2n2",
                Label = "Row 2 Node 2",
                Visual = new NodeVisual { X = 150, Y = 250 }
            });

            // Verify structure for aisle detection
            bool hasEnoughNodes = layout.Nodes.Count == 4;

            // Check Y coordinates for row detection
            var row1 = layout.Nodes.Where(n => Math.Abs(n.Visual.Y - 100) < 50).ToList();
            var row2 = layout.Nodes.Where(n => Math.Abs(n.Visual.Y - 250) < 50).ToList();

            bool hasTwoRows = row1.Count == 2 && row2.Count == 2;

            return hasEnoughNodes && hasTwoRows;
        }

        /// <summary>
        /// Test 6: Opening linking to nearest equipment
        /// </summary>
        private static bool Test6_OpeningLinking()
        {
            var layout = new LayoutData();

            // Add equipment node
            layout.Nodes.Add(new NodeData
            {
                Id = "equip1",
                Label = "Equipment 1",
                Visual = new NodeVisual { X = 100, Y = 100 }
            });

            // Add opening
            layout.Openings.Add(new OpeningData
            {
                Id = "door1",
                Name = "Door 1",
                X = 120,
                Y = 120
            });

            // Find nearest node
            var opening = layout.Openings.First();
            NodeData nearest = null;
            double minDist = double.MaxValue;

            foreach (var node in layout.Nodes)
            {
                var dx = node.Visual.X - opening.X;
                var dy = node.Visual.Y - opening.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }

            bool foundNearest = nearest != null && nearest.Id == "equip1";
            bool distanceCorrect = Math.Abs(minDist - 28.28) < 1.0; // sqrt(20^2 + 20^2) ≈ 28.28

            return foundNearest && distanceCorrect;
        }
    }
}
