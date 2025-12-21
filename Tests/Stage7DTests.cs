using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Data;
using LayoutEditor.Data.Services;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 7D Tests: Database Integration
    /// Tests persistence of pedestrian layer elements
    /// </summary>
    public static class Stage7DTests
    {
        private static readonly string _testDbPath = Path.Combine(Path.GetTempPath(), $"test_stage7d_{Guid.NewGuid()}.db");

        public static async Task<bool> RunAllTests()
        {
            Console.WriteLine("\n=== Stage 7D Tests: Database Integration ===\n");

            var tests = new Func<Task<bool>>[]
            {
                Test1_SaveAndLoadWalkways,
                Test2_SaveAndLoadPedestrianCrossings,
                Test3_SaveAndLoadSafetyZones,
                Test4_SaveAndLoadMixedPedestrianElements
            };

            int passed = 0;
            int failed = 0;

            for (int i = 0; i < tests.Length; i++)
            {
                try
                {
                    bool result = await tests[i]();
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

            Console.WriteLine($"\nStage 7D Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Save and load layout with walkways
        /// </summary>
        private static async Task<bool> Test1_SaveAndLoadWalkways()
        {
            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with walkways
                var layout = new LayoutData();
                layout.Id = "walkway-test";
                layout.Metadata.Name = "Walkway Test Layout";
                layout.Canvas.Width = 500.0;
                layout.Canvas.Height = 400.0;

                // Add walkways
                var walkway1 = new WalkwayData
                {
                    Id = "walk-1",
                    Name = "Main Walkway",
                    WalkwayType = WalkwayTypes.Primary,
                    Width = 30,
                    Color = "#4A90E2"
                };
                walkway1.Centerline.Add(new PointData(100, 100));
                walkway1.Centerline.Add(new PointData(200, 100));
                walkway1.Centerline.Add(new PointData(200, 200));
                layout.Walkways.Add(walkway1);

                var walkway2 = new WalkwayData
                {
                    Id = "walk-2",
                    Name = "Emergency Exit",
                    WalkwayType = WalkwayTypes.Emergency,
                    Width = 40,
                    Color = "#F5A623"
                };
                walkway2.Centerline.Add(new PointData(300, 150));
                walkway2.Centerline.Add(new PointData(400, 150));
                layout.Walkways.Add(walkway2);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("walkway-test");
                if (loaded == null) return false;

                // Verify walkways
                bool hasWalkways = loaded.Walkways.Count == 2;
                var loadedWalk1 = loaded.Walkways.FirstOrDefault(w => w.Id == "walk-1");
                bool walk1Correct = loadedWalk1 != null &&
                                   loadedWalk1.Name == "Main Walkway" &&
                                   loadedWalk1.WalkwayType == WalkwayTypes.Primary &&
                                   loadedWalk1.Width == 30 &&
                                   loadedWalk1.Centerline.Count == 3;

                var loadedWalk2 = loaded.Walkways.FirstOrDefault(w => w.Id == "walk-2");
                bool walk2Correct = loadedWalk2 != null &&
                                   loadedWalk2.WalkwayType == WalkwayTypes.Emergency &&
                                   loadedWalk2.Centerline.Count == 2;

                return hasWalkways && walk1Correct && walk2Correct;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 2: Save and load layout with pedestrian crossings
        /// </summary>
        private static async Task<bool> Test2_SaveAndLoadPedestrianCrossings()
        {
            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with crossings
                var layout = new LayoutData();
                layout.Id = "crossing-test";
                layout.Metadata.Name = "Crossing Test Layout";
                layout.Canvas.Width = 400.0;
                layout.Canvas.Height = 300.0;

                // Add crossings
                var crossing1 = new PedestrianCrossingData
                {
                    Id = "cross-1",
                    Name = "Zebra Crossing 1",
                    CrossingType = PedestrianCrossingTypes.Zebra,
                    Color = "#FFFFFF"
                };
                crossing1.Location.Add(new PointData(100, 100));
                crossing1.Location.Add(new PointData(150, 100));
                crossing1.Location.Add(new PointData(150, 120));
                crossing1.Location.Add(new PointData(100, 120));
                crossing1.CrossedEntityIds.Add("path-1");
                crossing1.CrossedEntityIds.Add("aisle-2");
                layout.PedestrianCrossings.Add(crossing1);

                var crossing2 = new PedestrianCrossingData
                {
                    Id = "cross-2",
                    Name = "Signal Crossing",
                    CrossingType = PedestrianCrossingTypes.Signal,
                    Color = "#FFD700"
                };
                crossing2.Location.Add(new PointData(250, 150));
                crossing2.Location.Add(new PointData(300, 150));
                layout.PedestrianCrossings.Add(crossing2);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("crossing-test");
                if (loaded == null) return false;

                // Verify crossings
                bool hasCrossings = loaded.PedestrianCrossings.Count == 2;
                var loadedCross1 = loaded.PedestrianCrossings.FirstOrDefault(c => c.Id == "cross-1");
                bool cross1Correct = loadedCross1 != null &&
                                    loadedCross1.Name == "Zebra Crossing 1" &&
                                    loadedCross1.CrossingType == PedestrianCrossingTypes.Zebra &&
                                    loadedCross1.Location.Count == 4 &&
                                    loadedCross1.CrossedEntityIds.Count == 2;

                var loadedCross2 = loaded.PedestrianCrossings.FirstOrDefault(c => c.Id == "cross-2");
                bool cross2Correct = loadedCross2 != null &&
                                    loadedCross2.CrossingType == PedestrianCrossingTypes.Signal;

                return hasCrossings && cross1Correct && cross2Correct;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 3: Save and load layout with safety zones
        /// </summary>
        private static async Task<bool> Test3_SaveAndLoadSafetyZones()
        {
            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with safety zones
                var layout = new LayoutData();
                layout.Id = "zone-test";
                layout.Metadata.Name = "Safety Zone Test Layout";
                layout.Canvas.Width = 600.0;
                layout.Canvas.Height = 500.0;

                // Add safety zones
                var zone1 = new SafetyZoneData
                {
                    Id = "zone-1",
                    Name = "Hard Hat Area",
                    ZoneType = SafetyZoneTypes.HardHat,
                    Color = "#FFAA00"
                };
                zone1.Boundary.Add(new PointData(100, 100));
                zone1.Boundary.Add(new PointData(200, 100));
                zone1.Boundary.Add(new PointData(200, 200));
                zone1.Boundary.Add(new PointData(100, 200));
                layout.SafetyZones.Add(zone1);

                var zone2 = new SafetyZoneData
                {
                    Id = "zone-2",
                    Name = "Keep Out Zone",
                    ZoneType = SafetyZoneTypes.KeepOut,
                    Color = "#FF0000"
                };
                zone2.Boundary.Add(new PointData(300, 150));
                zone2.Boundary.Add(new PointData(400, 150));
                zone2.Boundary.Add(new PointData(400, 250));
                zone2.Boundary.Add(new PointData(300, 250));
                layout.SafetyZones.Add(zone2);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("zone-test");
                if (loaded == null) return false;

                // Verify safety zones
                bool hasZones = loaded.SafetyZones.Count == 2;
                var loadedZone1 = loaded.SafetyZones.FirstOrDefault(z => z.Id == "zone-1");
                bool zone1Correct = loadedZone1 != null &&
                                   loadedZone1.Name == "Hard Hat Area" &&
                                   loadedZone1.ZoneType == SafetyZoneTypes.HardHat &&
                                   loadedZone1.Boundary.Count == 4;

                var loadedZone2 = loaded.SafetyZones.FirstOrDefault(z => z.Id == "zone-2");
                bool zone2Correct = loadedZone2 != null &&
                                   loadedZone2.ZoneType == SafetyZoneTypes.KeepOut &&
                                   loadedZone2.Boundary.Count == 4;

                return hasZones && zone1Correct && zone2Correct;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 4: Save and load layout with mixed pedestrian elements
        /// </summary>
        private static async Task<bool> Test4_SaveAndLoadMixedPedestrianElements()
        {
            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with all pedestrian types
                var layout = new LayoutData();
                layout.Id = "mixed-test";
                layout.Metadata.Name = "Mixed Pedestrian Test";
                layout.Canvas.Width = 800.0;
                layout.Canvas.Height = 600.0;

                // Add walkway
                var walkway = new WalkwayData { Id = "w1", Name = "Walkway 1", WalkwayType = WalkwayTypes.Primary, Width = 25 };
                walkway.Centerline.Add(new PointData(50, 50));
                walkway.Centerline.Add(new PointData(150, 50));
                layout.Walkways.Add(walkway);

                // Add crossing
                var crossing = new PedestrianCrossingData { Id = "c1", Name = "Crossing 1", CrossingType = PedestrianCrossingTypes.Unmarked };
                crossing.Location.Add(new PointData(200, 100));
                crossing.Location.Add(new PointData(250, 100));
                layout.PedestrianCrossings.Add(crossing);

                // Add safety zone
                var zone = new SafetyZoneData { Id = "z1", Name = "Zone 1", ZoneType = SafetyZoneTypes.HighVis };
                zone.Boundary.Add(new PointData(300, 150));
                zone.Boundary.Add(new PointData(400, 150));
                zone.Boundary.Add(new PointData(400, 250));
                layout.SafetyZones.Add(zone);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("mixed-test");
                if (loaded == null) return false;

                // Verify all element types present
                bool hasAllTypes = loaded.Walkways.Count == 1 &&
                                  loaded.PedestrianCrossings.Count == 1 &&
                                  loaded.SafetyZones.Count == 1;

                // Verify element count matches
                int elementCount = await service.GetElementCountAsync("mixed-test");
                bool countCorrect = elementCount == 3;

                return hasAllTypes && countCorrect;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        private static void CleanupDatabase()
        {
            try
            {
                if (File.Exists(_testDbPath))
                {
                    // Force garbage collection to release database file
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Delete(_testDbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
