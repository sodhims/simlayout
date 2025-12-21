using System;
using System.Collections.Generic;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 11D Tests: Constrained Dragging
    /// Tests constrained movement projection and entity position updates
    /// </summary>
    public static class Stage11DTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 11D Tests: Constrained Dragging ===\n");

            var tests = new Func<bool>[]
            {
                Test1_ConstrainedDragServiceCreation,
                Test2_ProjectToConstraintLinear,
                Test3_ProjectToConstraintArc,
                Test4_ProjectToConstraintPath,
                Test5_ProjectToConstraintPolygon,
                Test6_UpdateZonePosition,
                Test7_GetConstraintGuide,
                Test8_SupportsConstrainedMovement,
                Test9_ConstraintFactoryIntegration
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

            Console.WriteLine($"\nStage 11D Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: ConstrainedDragService can be created
        /// </summary>
        private static bool Test1_ConstrainedDragServiceCreation()
        {
            var layout = new LayoutData();
            var service = new ConstrainedDragService(layout);

            bool created = service != null;
            return created;
        }

        /// <summary>
        /// Test 2: ProjectToConstraint works for linear constraint (EOT crane)
        /// </summary>
        private static bool Test2_ProjectToConstraintLinear()
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
                ZoneMax = 1
            };
            layout.EOTCranes.Add(crane);

            var service = new ConstrainedDragService(layout);

            // Project a point above the runway (should snap to runway)
            var (position, parameter) = service.ProjectToConstraint(crane, new Point(50, 20));

            // Should be on the runway (y=0)
            bool onRunway = Math.Abs(position.Y - 0) < 0.01;
            // Should be at middle (x=50)
            bool atMiddle = Math.Abs(position.X - 50) < 1.0;
            // Parameter should be ~0.5
            bool parameterCorrect = Math.Abs(parameter - 0.5) < 0.1;

            return onRunway && atMiddle && parameterCorrect;
        }

        /// <summary>
        /// Test 3: ProjectToConstraint works for arc constraint (Jib crane)
        /// </summary>
        private static bool Test3_ProjectToConstraintArc()
        {
            var layout = new LayoutData();

            // Create jib crane
            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 0,
                CenterY = 0,
                Radius = 100,
                ArcStart = 0,
                ArcEnd = 90
            };
            layout.JibCranes.Add(crane);

            var service = new ConstrainedDragService(layout);

            // Project a point at 45 degrees
            var (position, parameter) = service.ProjectToConstraint(crane, new Point(70, 70));

            // Should be on the arc at radius 100
            var distFromCenter = Math.Sqrt(position.X * position.X + position.Y * position.Y);
            bool onArc = Math.Abs(distFromCenter - 100) < 1.0;

            // Should be in the first quadrant
            bool inQuadrant = position.X >= 0 && position.Y >= 0;

            return onArc && inQuadrant;
        }

        /// <summary>
        /// Test 4: ProjectToConstraint works for path constraint (Conveyor)
        /// </summary>
        private static bool Test4_ProjectToConstraintPath()
        {
            var layout = new LayoutData();

            // Create conveyor with waypoints
            var conveyor = new ConveyorData
            {
                Id = "conv1"
            };
            conveyor.Path.Add(new PointData(0, 0));
            conveyor.Path.Add(new PointData(100, 0));
            conveyor.Path.Add(new PointData(100, 100));
            layout.Conveyors.Add(conveyor);

            var service = new ConstrainedDragService(layout);

            // Project a point near the first segment
            var (position, parameter) = service.ProjectToConstraint(conveyor, new Point(50, 10));

            // Should be on the first segment (y=0)
            bool onPath = Math.Abs(position.Y - 0) < 1.0 || Math.Abs(position.X - 100) < 1.0;

            return onPath;
        }

        /// <summary>
        /// Test 5: ProjectToConstraint works for polygon constraint (Zone)
        /// </summary>
        private static bool Test5_ProjectToConstraintPolygon()
        {
            var layout = new LayoutData();

            // Create zone (square)
            var zone = new ZoneData
            {
                Id = "zone1",
                Name = "Test Zone"
            };
            zone.Points.Add(new PointData(0, 0));
            zone.Points.Add(new PointData(100, 0));
            zone.Points.Add(new PointData(100, 100));
            zone.Points.Add(new PointData(0, 100));
            layout.Zones.Add(zone);

            var service = new ConstrainedDragService(layout);

            // Project a point outside the zone
            var (position, parameter) = service.ProjectToConstraint(zone, new Point(150, 50));

            // Should be on or near the polygon boundary
            // For a square, right edge should be at x=100
            bool nearBoundary = position.X >= 0 && position.X <= 100 &&
                                position.Y >= 0 && position.Y <= 100;

            return nearBoundary;
        }

        /// <summary>
        /// Test 6: UpdateEntityPosition updates zone position
        /// </summary>
        private static bool Test6_UpdateZonePosition()
        {
            var layout = new LayoutData();

            // Create zone
            var zone = new ZoneData
            {
                Id = "zone1",
                X = 0,
                Y = 0
            };
            zone.Points.Add(new PointData(0, 0));
            zone.Points.Add(new PointData(50, 0));
            zone.Points.Add(new PointData(50, 50));
            zone.Points.Add(new PointData(0, 50));
            layout.Zones.Add(zone);

            var service = new ConstrainedDragService(layout);

            // Store original center
            var originalCenter = new Point(25, 25);

            // Update position (should move the zone)
            bool updated = service.UpdateEntityPosition(zone, new Point(75, 75));

            // Calculate new center
            double sumX = 0, sumY = 0;
            foreach (var point in zone.Points)
            {
                sumX += point.X;
                sumY += point.Y;
            }
            var newCenter = new Point(sumX / zone.Points.Count, sumY / zone.Points.Count);

            // Center should have moved
            bool moved = Math.Abs(newCenter.X - originalCenter.X) > 1.0 ||
                         Math.Abs(newCenter.Y - originalCenter.Y) > 1.0;

            return updated && moved;
        }

        /// <summary>
        /// Test 7: GetConstraintGuide returns geometry
        /// </summary>
        private static bool Test7_GetConstraintGuide()
        {
            var layout = new LayoutData();

            // Create jib crane
            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 0,
                CenterY = 0,
                Radius = 100,
                ArcStart = 0,
                ArcEnd = 180
            };
            layout.JibCranes.Add(crane);

            var service = new ConstrainedDragService(layout);

            // Get constraint guide
            var guide = service.GetConstraintGuide(crane);

            bool hasGuide = guide != null;

            return hasGuide;
        }

        /// <summary>
        /// Test 8: SupportsConstrainedMovement checks entity type
        /// </summary>
        private static bool Test8_SupportsConstrainedMovement()
        {
            var layout = new LayoutData();
            var service = new ConstrainedDragService(layout);

            // Create constrained entity (jib crane)
            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 0,
                CenterY = 0,
                Radius = 100
            };

            // Create non-constrained entity (regular node)
            var node = new NodeData
            {
                Id = "n1",
                Type = "Machine"
            };

            bool craneSupports = service.SupportsConstrainedMovement(crane);
            bool nodeDoesNotSupport = !service.SupportsConstrainedMovement(node);

            return craneSupports && nodeDoesNotSupport;
        }

        /// <summary>
        /// Test 9: ConstraintFactory integration with ConstrainedDragService
        /// </summary>
        private static bool Test9_ConstraintFactoryIntegration()
        {
            var layout = new LayoutData();

            // Create runway and crane
            var runway = new RunwayData
            {
                Id = "runway1",
                StartX = 0,
                StartY = 0,
                EndX = 200,
                EndY = 0
            };
            layout.Runways.Add(runway);

            var crane = new EOTCraneData
            {
                Id = "crane1",
                RunwayId = "runway1",
                ZoneMin = 0.2,
                ZoneMax = 0.8
            };
            layout.EOTCranes.Add(crane);

            var service = new ConstrainedDragService(layout);

            // Project should work through ConstraintFactory
            var (position, parameter) = service.ProjectToConstraint(crane, new Point(100, 50));

            // Should be on runway
            bool onRunway = Math.Abs(position.Y - 0) < 0.01;

            // Should be within zone limits (20 to 160)
            bool withinZone = position.X >= 40 - 1 && position.X <= 160 + 1;

            return onRunway && withinZone;
        }
    }
}
