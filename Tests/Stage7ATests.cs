using System;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 7A Tests: Pedestrian Models
    /// Tests walkway, crossing, and safety zone models
    /// </summary>
    public static class Stage7ATests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 7A Tests: Pedestrian Models ===\n");

            var tests = new Func<bool>[]
            {
                Test1_WalkwayModelComplete,
                Test2_PedestrianCrossingModelComplete,
                Test3_SafetyZoneModelComplete,
                Test4_CollectionsInLayoutData
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

            Console.WriteLine($"\nStage 7A Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Walkway model has all required properties
        /// </summary>
        private static bool Test1_WalkwayModelComplete()
        {
            var walkway = new WalkwayData();

            // Verify all properties are accessible
            bool hasId = !string.IsNullOrEmpty(walkway.Id);
            bool hasName = walkway.Name != null;
            bool hasCenterline = walkway.Centerline != null;
            bool hasWidth = walkway.Width > 0;
            bool hasType = !string.IsNullOrEmpty(walkway.WalkwayType);
            bool hasLayer = walkway.ArchitectureLayer == LayerType.Pedestrian;

            // Test property setters
            walkway.Id = "test-walkway-1";
            walkway.Name = "Main Walkway";
            walkway.Width = 2.0;
            walkway.WalkwayType = WalkwayTypes.Primary;

            bool settersWork = walkway.Id == "test-walkway-1" &&
                               walkway.Name == "Main Walkway" &&
                               walkway.Width == 2.0 &&
                               walkway.WalkwayType == WalkwayTypes.Primary;

            // Test centerline collection
            walkway.Centerline.Add(new PointData(10, 20));
            walkway.Centerline.Add(new PointData(30, 40));
            bool centerlineWorks = walkway.Centerline.Count == 2;

            return hasId && hasName && hasCenterline && hasWidth &&
                   hasType && hasLayer && settersWork && centerlineWorks;
        }

        /// <summary>
        /// Test 2: PedestrianCrossing model has all required properties
        /// </summary>
        private static bool Test2_PedestrianCrossingModelComplete()
        {
            var crossing = new PedestrianCrossingData();

            // Verify all properties are accessible
            bool hasId = !string.IsNullOrEmpty(crossing.Id);
            bool hasName = crossing.Name != null;
            bool hasLocation = crossing.Location != null;
            bool hasType = !string.IsNullOrEmpty(crossing.CrossingType);
            bool hasCrossedEntities = crossing.CrossedEntityIds != null;
            bool hasLayer = crossing.ArchitectureLayer == LayerType.Pedestrian;

            // Test property setters
            crossing.Id = "test-crossing-1";
            crossing.Name = "Zebra Crossing";
            crossing.CrossingType = PedestrianCrossingTypes.Zebra;

            bool settersWork = crossing.Id == "test-crossing-1" &&
                               crossing.Name == "Zebra Crossing" &&
                               crossing.CrossingType == PedestrianCrossingTypes.Zebra;

            // Test location collection
            crossing.Location.Add(new PointData(15, 25));
            crossing.Location.Add(new PointData(35, 45));
            bool locationWorks = crossing.Location.Count == 2;

            // Test crossed entities collection
            crossing.CrossedEntityIds.Add("path-1");
            crossing.CrossedEntityIds.Add("aisle-2");
            bool crossedEntitiesWorks = crossing.CrossedEntityIds.Count == 2;

            return hasId && hasName && hasLocation && hasType &&
                   hasCrossedEntities && hasLayer && settersWork &&
                   locationWorks && crossedEntitiesWorks;
        }

        /// <summary>
        /// Test 3: SafetyZone model has all required properties
        /// </summary>
        private static bool Test3_SafetyZoneModelComplete()
        {
            var zone = new SafetyZoneData();

            // Verify all properties are accessible
            bool hasId = !string.IsNullOrEmpty(zone.Id);
            bool hasName = zone.Name != null;
            bool hasBoundary = zone.Boundary != null;
            bool hasType = !string.IsNullOrEmpty(zone.ZoneType);
            bool hasLayer = zone.ArchitectureLayer == LayerType.Pedestrian;

            // Test property setters
            zone.Id = "test-zone-1";
            zone.Name = "Hard Hat Area";
            zone.ZoneType = SafetyZoneTypes.HardHat;

            bool settersWork = zone.Id == "test-zone-1" &&
                               zone.Name == "Hard Hat Area" &&
                               zone.ZoneType == SafetyZoneTypes.HardHat;

            // Test boundary collection
            zone.Boundary.Add(new PointData(0, 0));
            zone.Boundary.Add(new PointData(100, 0));
            zone.Boundary.Add(new PointData(100, 100));
            zone.Boundary.Add(new PointData(0, 100));
            bool boundaryWorks = zone.Boundary.Count == 4;

            return hasId && hasName && hasBoundary && hasType &&
                   hasLayer && settersWork && boundaryWorks;
        }

        /// <summary>
        /// Test 4: LayoutData has pedestrian collections and can add/remove items
        /// </summary>
        private static bool Test4_CollectionsInLayoutData()
        {
            var layout = new LayoutData();

            // Verify collections exist
            bool hasWalkways = layout.Walkways != null;
            bool hasCrossings = layout.PedestrianCrossings != null;
            bool hasSafetyZones = layout.SafetyZones != null;

            // Test adding walkways
            var walkway = new WalkwayData { Id = "w1", Name = "Walkway 1" };
            layout.Walkways.Add(walkway);
            bool walkwayAdded = layout.Walkways.Count == 1 && layout.Walkways[0].Id == "w1";

            // Test removing walkways
            layout.Walkways.Remove(walkway);
            bool walkwayRemoved = layout.Walkways.Count == 0;

            // Test adding crossings
            var crossing = new PedestrianCrossingData { Id = "c1", Name = "Crossing 1" };
            layout.PedestrianCrossings.Add(crossing);
            bool crossingAdded = layout.PedestrianCrossings.Count == 1 && layout.PedestrianCrossings[0].Id == "c1";

            // Test removing crossings
            layout.PedestrianCrossings.Remove(crossing);
            bool crossingRemoved = layout.PedestrianCrossings.Count == 0;

            // Test adding safety zones
            var zone = new SafetyZoneData { Id = "z1", Name = "Zone 1" };
            layout.SafetyZones.Add(zone);
            bool zoneAdded = layout.SafetyZones.Count == 1 && layout.SafetyZones[0].Id == "z1";

            // Test removing safety zones
            layout.SafetyZones.Remove(zone);
            bool zoneRemoved = layout.SafetyZones.Count == 0;

            return hasWalkways && hasCrossings && hasSafetyZones &&
                   walkwayAdded && walkwayRemoved &&
                   crossingAdded && crossingRemoved &&
                   zoneAdded && zoneRemoved;
        }
    }
}
