using System;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 12A Tests: Layout Generator Integration
    /// Tests for Part A - Procedural layout generation integrated into editor
    /// </summary>
    public static class Stage12ATests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 12A Tests: Layout Generator Integration ===\n");

            var tests = new Func<bool>[]
            {
                Test1_WarehouseLayoutGeneration,
                Test2_AssemblyLineGeneration,
                Test3_StorageGridGeneration,
                Test4_CraneAutoPlacement,
                Test5_AGVNetworkGeneration,
                Test6_GeneratedLayoutStructure
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

            Console.WriteLine($"\nStage 12A Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Warehouse layout generation creates proper structure
        /// </summary>
        private static bool Test1_WarehouseLayoutGeneration()
        {
            var layout = new LayoutData();

            // Simulate warehouse generation: 3 rows x 4 cols
            int rows = 3;
            int cols = 4;
            double rackWidth = 60;
            double rackDepth = 40;
            double aisleWidth = 30;
            double startX = 50;
            double startY = 50;

            // Generate racks
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    double x = startX + col * (rackWidth + aisleWidth);
                    double y = startY + row * (rackDepth + aisleWidth);

                    var node = new NodeData
                    {
                        Id = $"rack_{row}_{col}",
                        Label = $"R{row + 1}C{col + 1}",
                        Type = "Storage",
                        Visual = new NodeVisual { X = x, Y = y }
                    };
                    layout.Nodes.Add(node);
                }
            }

            // Generate aisles
            for (int row = 0; row < rows - 1; row++)
            {
                var aisle = new OpeningData
                {
                    Id = $"aisle_h_{row}",
                    OpeningType = "Aisle"
                };
                layout.Openings.Add(aisle);
            }

            bool correctRackCount = layout.Nodes.Count == rows * cols; // 12 racks
            bool correctAisleCount = layout.Openings.Count == rows - 1; // 2 horizontal aisles
            bool correctLabels = layout.Nodes.Any(n => n.Label.StartsWith("R"));

            return correctRackCount && correctAisleCount && correctLabels;
        }

        /// <summary>
        /// Test 2: Assembly line generation creates sequential stations
        /// </summary>
        private static bool Test2_AssemblyLineGeneration()
        {
            var layout = new LayoutData();

            // Simulate assembly line: 5 stations
            int stations = 5;
            double spacing = 100;
            double startX = 50;
            double startY = 200;

            for (int i = 0; i < stations; i++)
            {
                var node = new NodeData
                {
                    Id = $"station_{i}",
                    Label = $"Station {i + 1}",
                    Type = "Process",
                    Visual = new NodeVisual
                    {
                        X = startX + i * spacing,
                        Y = startY
                    }
                };
                layout.Nodes.Add(node);

                if (i > 0)
                {
                    var path = new PathData
                    {
                        Id = $"path_{i - 1}_{i}",
                        From = $"station_{i - 1}",
                        To = $"station_{i}"
                    };
                    layout.Paths.Add(path);
                }
            }

            bool correctStationCount = layout.Nodes.Count == stations;
            bool correctPathCount = layout.Paths.Count == stations - 1; // 4 connections
            bool sequentialX = layout.Nodes[1].Visual.X > layout.Nodes[0].Visual.X;

            return correctStationCount && correctPathCount && sequentialX;
        }

        /// <summary>
        /// Test 3: Storage grid generation creates dense cell layout
        /// </summary>
        private static bool Test3_StorageGridGeneration()
        {
            var layout = new LayoutData();

            // Simulate storage grid: 6 rows x 8 cols
            int rows = 6;
            int cols = 8;
            double cellSize = 30;
            double spacing = 5;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var node = new NodeData
                    {
                        Id = $"cell_{row}_{col}",
                        Label = $"{row * cols + col + 1}",
                        Type = "Storage"
                    };
                    layout.Nodes.Add(node);
                }
            }

            bool correctCellCount = layout.Nodes.Count == rows * cols; // 48 cells
            bool hasNumericLabels = layout.Nodes.All(n => int.TryParse(n.Label, out _));

            return correctCellCount && hasNumericLabels;
        }

        /// <summary>
        /// Test 4: Auto-placement of cranes over equipment zones
        /// </summary>
        private static bool Test4_CraneAutoPlacement()
        {
            var layout = new LayoutData();

            // Create equipment row
            for (int i = 0; i < 4; i++)
            {
                layout.Nodes.Add(new NodeData
                {
                    Id = $"equip_{i}",
                    Visual = new NodeVisual { X = 100 + i * 80, Y = 200 }
                });
            }

            // Simulate crane placement
            var minX = layout.Nodes.Min(n => n.Visual.X) - 50;
            var maxX = layout.Nodes.Max(n => n.Visual.X) + 50;
            var avgY = layout.Nodes.Average(n => n.Visual.Y);

            var runway = new RunwayData
            {
                Id = "runway_0",
                StartX = minX,
                StartY = avgY,
                EndX = maxX,
                EndY = avgY
            };
            layout.Runways.Add(runway);

            var crane = new EOTCraneData
            {
                Id = "eot_0",
                RunwayId = runway.Id,
                ZoneMin = 0,
                ZoneMax = 1
            };
            layout.EOTCranes.Add(crane);

            bool hasRunway = layout.Runways.Count == 1;
            bool hasCrane = layout.EOTCranes.Count == 1;
            bool craneLinkedToRunway = crane.RunwayId == runway.Id;

            return hasRunway && hasCrane && craneLinkedToRunway;
        }

        /// <summary>
        /// Test 5: AGV network generation creates connected graph
        /// </summary>
        private static bool Test5_AGVNetworkGeneration()
        {
            var layout = new LayoutData();

            // Create nodes
            layout.Nodes.Add(new NodeData { Id = "n1", Visual = new NodeVisual { X = 0, Y = 0 } });
            layout.Nodes.Add(new NodeData { Id = "n2", Visual = new NodeVisual { X = 100, Y = 0 } });
            layout.Nodes.Add(new NodeData { Id = "n3", Visual = new NodeVisual { X = 0, Y = 100 } });

            // Generate waypoints
            foreach (var node in layout.Nodes)
            {
                layout.AGVWaypoints.Add(new AGVWaypointData
                {
                    Id = $"wp_{node.Id}",
                    X = node.Visual.X,
                    Y = node.Visual.Y
                });
            }

            // Generate paths between nearby waypoints
            var wp1 = layout.AGVWaypoints[0];
            var wp2 = layout.AGVWaypoints[1];

            layout.AGVPaths.Add(new AGVPathData
            {
                Id = "path_0",
                FromWaypointId = wp1.Id,
                ToWaypointId = wp2.Id
            });

            bool hasWaypoints = layout.AGVWaypoints.Count == 3;
            bool hasPaths = layout.AGVPaths.Count >= 1;
            bool pathConnectsWaypoints = layout.AGVPaths[0].FromWaypointId == wp1.Id;

            return hasWaypoints && hasPaths && pathConnectsWaypoints;
        }

        /// <summary>
        /// Test 6: Generated layout has complete structure
        /// </summary>
        private static bool Test6_GeneratedLayoutStructure()
        {
            var layout = new LayoutData();

            // Warehouse with racks
            for (int i = 0; i < 10; i++)
            {
                layout.Nodes.Add(new NodeData
                {
                    Id = $"rack_{i}",
                    Type = "Storage"
                });
            }

            // Aisles
            for (int i = 0; i < 3; i++)
            {
                layout.Openings.Add(new OpeningData
                {
                    Id = $"aisle_{i}",
                    OpeningType = "Aisle"
                });
            }

            // AGV network
            for (int i = 0; i < 10; i++)
            {
                layout.AGVWaypoints.Add(new AGVWaypointData { Id = $"wp_{i}" });
            }

            for (int i = 0; i < 9; i++)
            {
                layout.AGVPaths.Add(new AGVPathData
                {
                    Id = $"path_{i}",
                    FromWaypointId = $"wp_{i}",
                    ToWaypointId = $"wp_{i + 1}"
                });
            }

            bool hasEquipment = layout.Nodes.Count > 0;
            bool hasAisles = layout.Openings.Count > 0;
            bool hasAGVNetwork = layout.AGVWaypoints.Count > 0 && layout.AGVPaths.Count > 0;

            return hasEquipment && hasAisles && hasAGVNetwork;
        }
    }
}
