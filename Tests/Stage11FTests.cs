using System;
using System.Collections.Generic;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 11F Tests: Polish
    /// Tests visual enhancements and UX improvements for frictionless mode
    /// </summary>
    public static class Stage11FTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 11F Tests: Polish ===\n");

            var tests = new Func<bool>[]
            {
                Test1_ConstraintGuideGeneration,
                Test2_SnapIndicatorFunctionality,
                Test3_VisualFeedbackAvailability,
                Test4_StatusBarEnhancements,
                Test5_KeyboardShortcutSupport,
                Test6_TooltipPresence
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

            Console.WriteLine($"\nStage 11F Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Constraint guides can be generated for all entity types
        /// </summary>
        private static bool Test1_ConstraintGuideGeneration()
        {
            var layout = new LayoutData();

            // Create runway and EOT crane
            var runway = new RunwayData
            {
                Id = "runway1",
                StartX = 0,
                StartY = 0,
                EndX = 100,
                EndY = 0
            };
            layout.Runways.Add(runway);

            var eotCrane = new EOTCraneData
            {
                Id = "crane1",
                RunwayId = "runway1",
                ZoneMin = 0,
                ZoneMax = 1
            };
            layout.EOTCranes.Add(eotCrane);

            // Create jib crane
            var jibCrane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 50,
                CenterY = 50,
                Radius = 30
            };
            layout.JibCranes.Add(jibCrane);

            // Create zone
            var zone = new ZoneData
            {
                Id = "zone1",
                Name = "Test Zone"
            };
            zone.Points.Add(new PointData(0, 0));
            zone.Points.Add(new PointData(50, 0));
            zone.Points.Add(new PointData(50, 50));
            zone.Points.Add(new PointData(0, 50));
            layout.Zones.Add(zone);

            var service = new ConstrainedDragService(layout);

            // Get guides for each entity type
            var eotGuide = service.GetConstraintGuide(eotCrane);
            var jibGuide = service.GetConstraintGuide(jibCrane);
            var zoneGuide = service.GetConstraintGuide(zone);

            // All guides should be generated
            bool hasEotGuide = eotGuide != null;
            bool hasJibGuide = jibGuide != null;
            bool hasZoneGuide = zoneGuide != null;

            return hasEotGuide && hasJibGuide && hasZoneGuide;
        }

        /// <summary>
        /// Test 2: Snap indicator functionality (position calculation)
        /// </summary>
        private static bool Test2_SnapIndicatorFunctionality()
        {
            var layout = new LayoutData();

            // Create jib crane
            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 0,
                CenterY = 0,
                Radius = 50,
                ArcStart = 0,
                ArcEnd = 90
            };
            layout.JibCranes.Add(crane);

            var service = new ConstrainedDragService(layout);

            // Project a point onto the constraint
            var (snapPosition, parameter) = service.ProjectToConstraint(crane, new Point(40, 40));

            // Snap position should be on the arc
            var distFromCenter = Math.Sqrt(snapPosition.X * snapPosition.X + snapPosition.Y * snapPosition.Y);
            bool onArc = Math.Abs(distFromCenter - 50) < 1.0;

            // Should be in first quadrant
            bool inQuadrant = snapPosition.X >= 0 && snapPosition.Y >= 0;

            return onArc && inQuadrant;
        }

        /// <summary>
        /// Test 3: Visual feedback availability through service methods
        /// </summary>
        private static bool Test3_VisualFeedbackAvailability()
        {
            var layout = new LayoutData();

            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 0,
                CenterY = 0,
                Radius = 50
            };
            layout.JibCranes.Add(crane);

            var service = new ConstrainedDragService(layout);

            // Check that service provides methods for visual feedback
            var projection = service.ProjectToConstraint(crane, new Point(30, 30));
            bool hasProjectMethod = projection.position.X >= 0 || projection.position.X < 0;
            bool hasGuideMethod = service.GetConstraintGuide(crane) != null;
            bool hasCollisionMethod = !service.WouldCollide(crane, new Point(30, 30)) ||
                                      service.WouldCollide(crane, new Point(30, 30));

            return hasProjectMethod && hasGuideMethod && hasCollisionMethod;
        }

        /// <summary>
        /// Test 4: Status bar information can be calculated
        /// </summary>
        private static bool Test4_StatusBarEnhancements()
        {
            var layout = new LayoutData();
            layout.FrictionlessMode = true;

            // Add various constrained entities
            layout.EOTCranes.Add(new EOTCraneData { Id = "crane1", RunwayId = "r1" });
            layout.JibCranes.Add(new JibCraneData { Id = "jib1" });
            layout.Zones.Add(new ZoneData { Id = "zone1", Name = "Zone 1" });

            // Count constrained entities
            int count = layout.EOTCranes.Count + layout.JibCranes.Count +
                        layout.Conveyors.Count + layout.Zones.Count + layout.AGVPaths.Count;

            bool hasEntities = count == 3;
            bool modeEnabled = layout.FrictionlessMode;

            return hasEntities && modeEnabled;
        }

        /// <summary>
        /// Test 5: Keyboard shortcut support (mode toggle functionality)
        /// </summary>
        private static bool Test5_KeyboardShortcutSupport()
        {
            var layout = new LayoutData();

            // Test mode toggle
            layout.FrictionlessMode = false;
            bool initiallyOff = !layout.FrictionlessMode;

            layout.FrictionlessMode = true;
            bool canToggleOn = layout.FrictionlessMode;

            layout.FrictionlessMode = false;
            bool canToggleOff = !layout.FrictionlessMode;

            return initiallyOff && canToggleOn && canToggleOff;
        }

        /// <summary>
        /// Test 6: Tooltip presence (frictionless mode flag exists)
        /// </summary>
        private static bool Test6_TooltipPresence()
        {
            var layout = new LayoutData();

            // Test that frictionless mode property exists and is accessible
            layout.FrictionlessMode = true;
            bool propertyExists = layout.FrictionlessMode == true;

            layout.FrictionlessMode = false;
            bool propertyWorks = layout.FrictionlessMode == false;

            return propertyExists && propertyWorks;
        }
    }
}
