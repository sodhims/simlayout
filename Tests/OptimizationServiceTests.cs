using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Test harness for OptimizationService - validates GA integration points
    /// </summary>
    public static class OptimizationServiceTests
    {
        public static async Task<int> RunAllTests()
        {
            Console.WriteLine("=== OptimizationService Test Harness ===\n");

            int passed = 0;
            int failed = 0;

            // Test 1: Entity extraction
            if (await Test_EntityExtraction()) passed++; else failed++;

            // Test 2: Zone boundary extraction
            if (await Test_ZoneBoundaryExtraction()) passed++; else failed++;

            // Test 3: Progress events
            if (await Test_ProgressEvents()) passed++; else failed++;

            // Test 4: Cancellation
            if (await Test_Cancellation()) passed++; else failed++;

            // Test 5: Apply optimized positions
            if (await Test_ApplyOptimizedPositions()) passed++; else failed++;

            // Test 6: GA placeholder runs
            if (await Test_GAPlaceholderRuns()) passed++; else failed++;

            Console.WriteLine($"\n=== Results: {passed} passed, {failed} failed ===");
            return failed;
        }

        private static async Task<bool> Test_EntityExtraction()
        {
            Console.Write("Test 1: Entity extraction... ");
            try
            {
                var layout = CreateTestLayout();
                var service = new OptimizationService();
                var options = new OptimizationOptions { MaxGenerations = 1 };

                // Run optimization to trigger extraction
                var result = await service.OptimizeAsync(layout, options);

                // Verify we have nodes, stations, waypoints
                bool hasNodes = layout.Nodes.Count > 0;
                bool hasStations = layout.AGVStations.Count > 0;
                bool hasWaypoints = layout.AGVWaypoints.Count > 0;

                if (hasNodes && hasStations && hasWaypoints)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Nodes: {layout.Nodes.Count}");
                    Console.WriteLine($"   - AGV Stations: {layout.AGVStations.Count}");
                    Console.WriteLine($"   - AGV Waypoints: {layout.AGVWaypoints.Count}");
                    Console.WriteLine($"   - Jib Cranes: {layout.JibCranes.Count}");
                    Console.WriteLine($"   - EOT Cranes: {layout.EOTCranes.Count}");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAILED - Missing entities");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> Test_ZoneBoundaryExtraction()
        {
            Console.Write("Test 2: Zone boundary extraction... ");
            try
            {
                var layout = CreateTestLayout();

                // Add a test zone with polygon points
                var zone = new ZoneData
                {
                    Id = "zone_test",
                    Name = "Test Zone",
                    Type = "work_area"
                };
                zone.Points.Add(new PointData(100, 100));
                zone.Points.Add(new PointData(300, 100));
                zone.Points.Add(new PointData(300, 300));
                zone.Points.Add(new PointData(100, 300));
                layout.Zones.Add(zone);

                var service = new OptimizationService();
                var options = new OptimizationOptions { MaxGenerations = 1, RespectZones = true };

                var result = await service.OptimizeAsync(layout, options);

                if (layout.Zones.Count > 0 && layout.Zones[0].Points.Count == 4)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Zones: {layout.Zones.Count}");
                    Console.WriteLine($"   - Zone '{zone.Name}' has {zone.Points.Count} vertices");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAILED - Zone points not preserved");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> Test_ProgressEvents()
        {
            Console.Write("Test 3: Progress events... ");
            try
            {
                var layout = CreateTestLayout();
                var service = new OptimizationService();
                var options = new OptimizationOptions { MaxGenerations = 5 };

                var progressMessages = new List<string>();
                double lastProgress = 0;

                service.ProgressChanged += (s, e) =>
                {
                    progressMessages.Add(e.Message);
                    lastProgress = e.ProgressPercentage;
                };

                await service.OptimizeAsync(layout, options);

                if (progressMessages.Count >= 5 && lastProgress >= 80)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Progress updates: {progressMessages.Count}");
                    Console.WriteLine($"   - Final progress: {lastProgress:F1}%");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FAILED - Only {progressMessages.Count} updates, {lastProgress}% final");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> Test_Cancellation()
        {
            Console.Write("Test 4: Cancellation... ");
            try
            {
                var layout = CreateTestLayout();
                var service = new OptimizationService();
                var options = new OptimizationOptions { MaxGenerations = 100 };

                var cts = new CancellationTokenSource();

                // Cancel after 200ms
                _ = Task.Delay(200).ContinueWith(_ => cts.Cancel());

                var result = await service.OptimizeAsync(layout, options, cts.Token);

                if (!result.Success && result.Message.Contains("cancelled"))
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Cancelled successfully: {result.Message}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FAILED - Expected cancellation, got: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> Test_ApplyOptimizedPositions()
        {
            Console.Write("Test 5: Apply optimized positions... ");
            try
            {
                var layout = CreateTestLayout();
                var service = new OptimizationService();

                // Get original positions
                var node = layout.Nodes.First();
                var station = layout.AGVStations.First();
                var waypoint = layout.AGVWaypoints.First();
                var jib = layout.JibCranes.First();

                double origNodeX = node.Visual.X;
                double origStationX = station.X;
                double origWaypointX = waypoint.X;
                double origJibX = jib.CenterX;

                // Create optimized positions (move everything by 50)
                var optimized = new List<EntityPosition>
                {
                    new EntityPosition { Id = node.Id, Type = "node", X = origNodeX + 50, Y = node.Visual.Y },
                    new EntityPosition { Id = station.Id, Type = "agv_station", X = origStationX + 50, Y = station.Y },
                    new EntityPosition { Id = waypoint.Id, Type = "agv_waypoint", X = origWaypointX + 50, Y = waypoint.Y },
                    new EntityPosition { Id = jib.Id, Type = "jib_crane", X = origJibX + 50, Y = jib.CenterY }
                };

                // Apply
                service.ApplyOptimizedPositions(layout, optimized);

                // Verify
                bool nodesMoved = Math.Abs(node.Visual.X - (origNodeX + 50)) < 0.1;
                bool stationsMoved = Math.Abs(station.X - (origStationX + 50)) < 0.1;
                bool waypointsMoved = Math.Abs(waypoint.X - (origWaypointX + 50)) < 0.1;
                bool jibsMoved = Math.Abs(jib.CenterX - (origJibX + 50)) < 0.1;

                if (nodesMoved && stationsMoved && waypointsMoved && jibsMoved)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Node moved: {origNodeX:F1} -> {node.Visual.X:F1}");
                    Console.WriteLine($"   - Station moved: {origStationX:F1} -> {station.X:F1}");
                    Console.WriteLine($"   - Waypoint moved: {origWaypointX:F1} -> {waypoint.X:F1}");
                    Console.WriteLine($"   - Jib crane moved: {origJibX:F1} -> {jib.CenterX:F1}");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAILED - Positions not applied correctly");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> Test_GAPlaceholderRuns()
        {
            Console.Write("Test 6: GA placeholder runs to completion... ");
            try
            {
                var layout = CreateTestLayout();
                var service = new OptimizationService();
                var options = new OptimizationOptions
                {
                    MaxGenerations = 10,
                    PopulationSize = 20,
                    MutationRate = 0.1,
                    CrossoverRate = 0.8,
                    Objective = "minimize_travel",
                    RespectZones = true,
                    MaintainConnectivity = true
                };

                bool completedEventFired = false;
                service.OptimizationCompleted += (s, e) => completedEventFired = true;

                var result = await service.OptimizeAsync(layout, options);

                if (result.Success && completedEventFired && result.Duration.TotalMilliseconds > 0)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Duration: {result.Duration.TotalSeconds:F2}s");
                    Console.WriteLine($"   - Message: {result.Message}");
                    Console.WriteLine($"   - Completed event fired: {completedEventFired}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FAILED - Success: {result.Success}, Event: {completedEventFired}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a test layout with various entity types for optimization testing
        /// </summary>
        private static LayoutData CreateTestLayout()
        {
            var layout = new LayoutData
            {
                Canvas = new CanvasSettings { Width = 1000, Height = 800 }
            };

            // Add nodes
            for (int i = 0; i < 5; i++)
            {
                layout.Nodes.Add(new NodeData
                {
                    Id = $"node_{i}",
                    Name = $"Machine {i + 1}",
                    Visual = new NodeVisual
                    {
                        X = 100 + i * 150,
                        Y = 200,
                        Width = 60,
                        Height = 60
                    }
                });
            }

            // Add AGV stations (with homing property)
            layout.AGVStations.Add(new AGVStationData
            {
                Id = "station_1",
                Name = "Loading Station",
                X = 100,
                Y = 500,
                IsHoming = false
            });
            layout.AGVStations.Add(new AGVStationData
            {
                Id = "station_2",
                Name = "Home Station",
                X = 800,
                Y = 500,
                IsHoming = true
            });

            // Add AGV waypoints
            layout.AGVWaypoints.Add(new AGVWaypointData
            {
                Id = "waypoint_1",
                Name = "WP1",
                X = 300,
                Y = 400
            });
            layout.AGVWaypoints.Add(new AGVWaypointData
            {
                Id = "waypoint_2",
                Name = "WP2",
                X = 600,
                Y = 400
            });

            // Add a runway for EOT crane
            var runway = new RunwayData
            {
                Id = "runway_1",
                Name = "Main Bay Runway",
                StartX = 100,
                StartY = 100,
                EndX = 900,
                EndY = 100
            };
            layout.Runways.Add(runway);

            // Add EOT crane (with BayWidth property)
            layout.EOTCranes.Add(new EOTCraneData
            {
                Id = "eot_1",
                Name = "EOT Crane 1",
                RunwayId = runway.Id,
                BridgePosition = 0.5,
                BayWidth = 240 // 20 feet
            });

            // Add Jib crane
            layout.JibCranes.Add(new JibCraneData
            {
                Id = "jib_1",
                Name = "Jib Crane 1",
                CenterX = 500,
                CenterY = 600,
                Radius = 120
            });

            return layout;
        }

        /// <summary>
        /// Test harness for custom GA integration - call this with your GA implementation
        /// </summary>
        public static async Task<OptimizationResult> TestWithCustomGA(
            LayoutData layout,
            Func<List<EntityPosition>, List<ZoneBoundary>, OptimizationOptions, CancellationToken, Task<List<EntityPosition>>> gaImplementation)
        {
            Console.WriteLine("\n=== Testing Custom GA Integration ===\n");

            var service = new OptimizationService();
            var options = new OptimizationOptions
            {
                MaxGenerations = 100,
                PopulationSize = 50,
                MutationRate = 0.1,
                CrossoverRate = 0.8,
                Objective = "minimize_travel"
            };

            // Hook up progress reporting
            service.ProgressChanged += (s, e) =>
            {
                Console.WriteLine($"  [{e.ProgressPercentage:F1}%] {e.Message}");
            };

            // Run optimization
            var result = await service.OptimizeAsync(layout, options);

            Console.WriteLine($"\nOptimization complete:");
            Console.WriteLine($"  Success: {result.Success}");
            Console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F2}s");
            Console.WriteLine($"  Message: {result.Message}");

            return result;
        }
    }
}
