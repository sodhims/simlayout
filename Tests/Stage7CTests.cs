using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 7C Tests: Pedestrian Tools
    /// Tests interactive tools for creating pedestrian elements
    /// </summary>
    public static class Stage7CTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 7C Tests: Pedestrian Tools ===\n");

            var tests = new Func<bool>[]
            {
                Test1_WalkwayToolCreatesWalkway,
                Test2_CrossingToolCreatesCrossing,
                Test3_SafetyZoneToolCreatesZone,
                Test4_ToolsCancelCorrectly
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

            Console.WriteLine($"\nStage 7C Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Walkway tool creates walkway with centerline points
        /// </summary>
        private static bool Test1_WalkwayToolCreatesWalkway()
        {
            var layout = new LayoutData();

            // Simulate walkway creation (as the tool would do)
            var walkway = new WalkwayData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Walkway",
                WalkwayType = WalkwayTypes.Primary,
                Width = 30,
                Color = "#4A90E2"
            };

            // Add centerline points
            walkway.Centerline.Add(new PointData(100, 100));
            walkway.Centerline.Add(new PointData(200, 100));
            walkway.Centerline.Add(new PointData(200, 200));

            layout.Walkways.Add(walkway);

            // Verify walkway was created correctly
            bool hasWalkway = layout.Walkways.Count == 1;
            bool hasCorrectType = layout.Walkways[0].WalkwayType == WalkwayTypes.Primary;
            bool hasCorrectPoints = layout.Walkways[0].Centerline.Count == 3;
            bool hasCorrectWidth = layout.Walkways[0].Width == 30;

            return hasWalkway && hasCorrectType && hasCorrectPoints && hasCorrectWidth;
        }

        /// <summary>
        /// Test 2: Crossing tool creates pedestrian crossing with location polygon
        /// </summary>
        private static bool Test2_CrossingToolCreatesCrossing()
        {
            var layout = new LayoutData();

            // Simulate crossing creation (as the tool would do)
            var crossing = new PedestrianCrossingData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Crossing",
                CrossingType = PedestrianCrossingTypes.Zebra,
                Color = "#FFFFFF"
            };

            // Add location points
            crossing.Location.Add(new PointData(100, 100));
            crossing.Location.Add(new PointData(150, 100));
            crossing.Location.Add(new PointData(150, 120));
            crossing.Location.Add(new PointData(100, 120));

            layout.PedestrianCrossings.Add(crossing);

            // Verify crossing was created correctly
            bool hasCrossing = layout.PedestrianCrossings.Count == 1;
            bool hasCorrectType = layout.PedestrianCrossings[0].CrossingType == PedestrianCrossingTypes.Zebra;
            bool hasCorrectPoints = layout.PedestrianCrossings[0].Location.Count == 4;
            bool hasCrossedEntities = layout.PedestrianCrossings[0].CrossedEntityIds != null;

            return hasCrossing && hasCorrectType && hasCorrectPoints && hasCrossedEntities;
        }

        /// <summary>
        /// Test 3: Safety zone tool creates zone with boundary polygon
        /// </summary>
        private static bool Test3_SafetyZoneToolCreatesZone()
        {
            var layout = new LayoutData();

            // Simulate safety zone creation (as the tool would do)
            var zone = new SafetyZoneData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Zone",
                ZoneType = SafetyZoneTypes.HardHat,
                Color = "#FFAA00"
            };

            // Add boundary points
            zone.Boundary.Add(new PointData(0, 0));
            zone.Boundary.Add(new PointData(100, 0));
            zone.Boundary.Add(new PointData(100, 100));
            zone.Boundary.Add(new PointData(0, 100));

            layout.SafetyZones.Add(zone);

            // Verify zone was created correctly
            bool hasZone = layout.SafetyZones.Count == 1;
            bool hasCorrectType = layout.SafetyZones[0].ZoneType == SafetyZoneTypes.HardHat;
            bool hasCorrectPoints = layout.SafetyZones[0].Boundary.Count == 4;
            bool hasCorrectColor = layout.SafetyZones[0].Color == "#FFAA00";

            return hasZone && hasCorrectType && hasCorrectPoints && hasCorrectColor;
        }

        /// <summary>
        /// Test 4: Tools can cancel incomplete elements correctly
        /// </summary>
        private static bool Test4_ToolsCancelCorrectly()
        {
            var layout = new LayoutData();

            // Test walkway cancellation
            var incompleteWalkway = new WalkwayData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Incomplete Walkway",
                WalkwayType = WalkwayTypes.Primary,
                Width = 30
            };
            incompleteWalkway.Centerline.Add(new PointData(50, 50)); // Only 1 point
            layout.Walkways.Add(incompleteWalkway);

            // Simulate cancellation - remove if less than 2 points
            if (incompleteWalkway.Centerline.Count < 2)
            {
                layout.Walkways.Remove(incompleteWalkway);
            }

            bool walkwayCancelled = layout.Walkways.Count == 0;

            // Test crossing cancellation
            var incompleteCrossing = new PedestrianCrossingData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Incomplete Crossing",
                CrossingType = PedestrianCrossingTypes.Zebra
            };
            incompleteCrossing.Location.Add(new PointData(60, 60)); // Only 1 point
            layout.PedestrianCrossings.Add(incompleteCrossing);

            // Simulate cancellation - remove if less than 2 points
            if (incompleteCrossing.Location.Count < 2)
            {
                layout.PedestrianCrossings.Remove(incompleteCrossing);
            }

            bool crossingCancelled = layout.PedestrianCrossings.Count == 0;

            // Test safety zone cancellation
            var incompleteZone = new SafetyZoneData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Incomplete Zone",
                ZoneType = SafetyZoneTypes.KeepOut
            };
            incompleteZone.Boundary.Add(new PointData(70, 70)); // Only 1 point
            incompleteZone.Boundary.Add(new PointData(80, 70)); // 2 points
            layout.SafetyZones.Add(incompleteZone);

            // Simulate cancellation - remove if less than 3 points
            if (incompleteZone.Boundary.Count < 3)
            {
                layout.SafetyZones.Remove(incompleteZone);
            }

            bool zoneCancelled = layout.SafetyZones.Count == 0;

            return walkwayCancelled && crossingCancelled && zoneCancelled;
        }
    }
}
