using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stress test for AGV station dragging in Design Mode
    /// Tests 2, 3, 5, 7 AGV stations, moving each 50+ feet, 50 iterations each
    /// </summary>
    public static class AGVStationDragTest
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== AGV Station Drag Stress Tests ===\n");

            var stationCounts = new[] { 2, 3, 5, 7 };
            bool allPassed = true;

            foreach (int count in stationCounts)
            {
                bool result = TestAGVStationDragging(count, 50);
                if (!result)
                {
                    allPassed = false;
                }
            }

            return allPassed;
        }

        private static bool TestAGVStationDragging(int stationCount, int iterations)
        {
            Console.WriteLine($"\n--- Testing {stationCount} AGV Stations ({iterations} iterations) ---");

            var hitTestService = new HitTestService();
            var layerManager = new ArchitectureLayerManager();

            var results = new List<string>();
            var successCount = 0;
            var failCount = 0;

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // Create test layout
                var layout = CreateTestLayout(stationCount);
                layout.DesignMode = true;

                // Test each station
                for (int stationIdx = 0; stationIdx < layout.AGVStations.Count; stationIdx++)
                {
                    var station = layout.AGVStations[stationIdx];
                    var originalPos = new Point(station.X, station.Y);

                    // Test hit detection at original position
                    var hitResult = hitTestService.HitTest(layout, originalPos, layerManager, false, true);

                    if (hitResult.Type != HitType.AGVStation || hitResult.AGVStation?.Id != station.Id)
                    {
                        results.Add($"[FAIL] Iter {iteration + 1}, Station {stationIdx + 1}: Hit test failed at original position");
                        failCount++;
                        continue;
                    }

                    // Move station 50+ feet (650+ pixels, assuming 12 pixels/foot)
                    var newX = station.X + 650;
                    var newY = station.Y + 650;
                    var newPos = new Point(newX, newY);

                    // Simulate drag: Update station position
                    station.X = newX;
                    station.Y = newY;

                    // Update linked waypoint if exists
                    if (!string.IsNullOrEmpty(station.LinkedWaypointId))
                    {
                        var waypoint = layout.AGVWaypoints.FirstOrDefault(w => w.Id == station.LinkedWaypointId);
                        if (waypoint != null)
                        {
                            waypoint.X = newX;
                            waypoint.Y = newY;
                        }
                    }

                    // Verify movement distance
                    var distance = Math.Sqrt(Math.Pow(newX - originalPos.X, 2) + Math.Pow(newY - originalPos.Y, 2));
                    var distanceFeet = distance / 12.0;

                    if (distanceFeet >= 50)
                    {
                        // Test hit detection at new position
                        var newHitResult = hitTestService.HitTest(layout, newPos, layerManager, false, true);

                        if (newHitResult.Type == HitType.AGVStation && newHitResult.AGVStation?.Id == station.Id)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            results.Add($"[FAIL] Iter {iteration + 1}, Station {stationIdx + 1}: Hit test failed at new position (moved {distanceFeet:F1} feet)");
                        }
                    }
                    else
                    {
                        failCount++;
                        results.Add($"[FAIL] Iter {iteration + 1}, Station {stationIdx + 1}: Movement distance only {distanceFeet:F1} feet (need 50+)");
                    }
                }
            }

            // Print results
            Console.WriteLine($"\nResults for {stationCount} stations:");
            Console.WriteLine($"Total Tests: {successCount + failCount}");
            Console.WriteLine($"Passed: {successCount} ({(successCount * 100.0 / (successCount + failCount)):F1}%)");
            Console.WriteLine($"Failed: {failCount} ({(failCount * 100.0 / (successCount + failCount)):F1}%)");

            if (failCount > 0)
            {
                Console.WriteLine("\nFirst 10 Failures:");
                foreach (var result in results.Where(r => r.StartsWith("[FAIL]")).Take(10))
                {
                    Console.WriteLine(result);
                }
            }

            // Require at least 95% success rate
            var successRate = successCount * 100.0 / (successCount + failCount);
            bool passed = successRate >= 95;

            if (passed)
            {
                Console.WriteLine($"✓ Test PASSED: Success rate {successRate:F1}% >= 95%");
            }
            else
            {
                Console.WriteLine($"✗ Test FAILED: Success rate {successRate:F1}% < 95%");
            }

            return passed;
        }

        private static LayoutData CreateTestLayout(int agvStationCount)
        {
            var layout = new LayoutData
            {
                DesignMode = true
            };

            var random = new Random(42); // Fixed seed for reproducibility

            // Create AGV stations with linked waypoints
            for (int i = 0; i < agvStationCount; i++)
            {
                var x = 100 + (i * 200) + random.Next(0, 100);
                var y = 100 + (i % 2) * 200 + random.Next(0, 100);

                var waypointId = Guid.NewGuid().ToString();

                // Create waypoint
                layout.AGVWaypoints.Add(new AGVWaypointData
                {
                    Id = waypointId,
                    X = x,
                    Y = y,
                    Name = $"Waypoint_{i + 1}",
                    WaypointType = "through"
                });

                // Create station
                layout.AGVStations.Add(new AGVStationData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"AGV Station {i + 1}",
                    X = x,
                    Y = y,
                    LinkedWaypointId = waypointId,
                    Color = "#FF0000"
                });
            }

            return layout;
        }

        public static bool Test_SimpleHitTest()
        {
            // Simple verification test
            var layout = CreateTestLayout(1);
            layout.DesignMode = true;

            var hitTestService = new HitTestService();
            var layerManager = new ArchitectureLayerManager();

            var station = layout.AGVStations[0];
            var hitPoint = new Point(station.X, station.Y);

            var hitResult = hitTestService.HitTest(layout, hitPoint, layerManager, false, true);

            bool passed = hitResult != null &&
                         hitResult.Type == HitType.AGVStation &&
                         hitResult.AGVStation != null &&
                         hitResult.AGVStation.Id == station.Id;

            if (passed)
            {
                Console.WriteLine("✓ Simple hit test passed");
            }
            else
            {
                Console.WriteLine("✗ Simple hit test failed");
            }

            return passed;
        }
    }
}
