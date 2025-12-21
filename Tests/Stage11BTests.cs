using System;
using System.Collections.Generic;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 11B Tests: Constrained Entity Interface
    /// Tests IConstrainedEntity implementation across all entity types
    /// </summary>
    public static class Stage11BTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 11B Tests: Constrained Entity Interface ===\n");

            var tests = new Func<bool>[]
            {
                Test1_EOTCraneImplementsIConstrainedEntity,
                Test2_JibCraneImplementsIConstrainedEntity,
                Test3_ConveyorImplementsIConstrainedEntity,
                Test4_AGVPathImplementsIConstrainedEntity,
                Test5_ZoneImplementsIConstrainedEntity,
                Test6_ConstraintFactoryResolvesCraneDependencies,
                Test7_ConstraintFactoryResolvesAGVDependencies
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

            Console.WriteLine($"\nStage 11B Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: EOT crane implements IConstrainedEntity and creates linear constraint
        /// </summary>
        private static bool Test1_EOTCraneImplementsIConstrainedEntity()
        {
            var runway = new RunwayData
            {
                Id = "runway1",
                StartX = 0,
                StartY = 0,
                EndX = 100,
                EndY = 0
            };

            var crane = new EOTCraneData
            {
                Id = "crane1",
                RunwayId = "runway1",
                ZoneMin = 0.2,
                ZoneMax = 0.8
            };

            // Check interface implementation
            bool implementsInterface = crane is IConstrainedEntity;
            bool supportsMovement = crane.SupportsConstrainedMovement;

            // Get constraint with runway
            var constraint = crane.GetConstraint(runway);

            bool hasConstraint = constraint != null;
            bool isLinear = constraint is LinearConstraint;

            // Verify constraint properties
            if (constraint is LinearConstraint linear)
            {
                var start = linear.Start;
                var end = linear.End;

                bool correctStart = Math.Abs(start.X - 20) < 0.01 && Math.Abs(start.Y - 0) < 0.01;
                bool correctEnd = Math.Abs(end.X - 80) < 0.01 && Math.Abs(end.Y - 0) < 0.01;

                return implementsInterface && supportsMovement && hasConstraint &&
                       isLinear && correctStart && correctEnd;
            }

            return false;
        }

        /// <summary>
        /// Test 2: Jib crane implements IConstrainedEntity and creates arc constraint
        /// </summary>
        private static bool Test2_JibCraneImplementsIConstrainedEntity()
        {
            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 50,
                CenterY = 50,
                Radius = 30,
                ArcStart = 0,
                ArcEnd = 180
            };

            // Check interface implementation
            bool implementsInterface = crane is IConstrainedEntity;
            bool supportsMovement = crane.SupportsConstrainedMovement;

            // Get constraint
            var constraint = crane.GetConstraint();

            bool hasConstraint = constraint != null;
            bool isArc = constraint is ArcConstraint;

            // Verify constraint properties
            if (constraint is ArcConstraint arc)
            {
                bool correctCenter = Math.Abs(arc.Center.X - 50) < 0.01 &&
                                     Math.Abs(arc.Center.Y - 50) < 0.01;
                bool correctRadius = Math.Abs(arc.Radius - 30) < 0.01;
                bool correctAngles = Math.Abs(arc.StartAngle - 0) < 0.01 &&
                                     Math.Abs(arc.EndAngle - Math.PI) < 0.01;

                return implementsInterface && supportsMovement && hasConstraint &&
                       isArc && correctCenter && correctRadius && correctAngles;
            }

            return false;
        }

        /// <summary>
        /// Test 3: Conveyor implements IConstrainedEntity and creates path constraint
        /// </summary>
        private static bool Test3_ConveyorImplementsIConstrainedEntity()
        {
            var conveyor = new ConveyorData
            {
                Id = "conv1"
            };

            conveyor.Path.Add(new PointData(0, 0));
            conveyor.Path.Add(new PointData(50, 0));
            conveyor.Path.Add(new PointData(50, 50));

            // Check interface implementation
            bool implementsInterface = conveyor is IConstrainedEntity;
            bool supportsMovement = conveyor.SupportsConstrainedMovement;

            // Get constraint
            var constraint = conveyor.GetConstraint();

            bool hasConstraint = constraint != null;
            bool isPath = constraint is PathConstraint;

            // Verify constraint properties
            if (constraint is PathConstraint path)
            {
                bool correctWaypointCount = path.Waypoints.Count == 3;
                bool correctLength = Math.Abs(path.TotalLength - 100) < 0.01;

                return implementsInterface && supportsMovement && hasConstraint &&
                       isPath && correctWaypointCount && correctLength;
            }

            return false;
        }

        /// <summary>
        /// Test 4: AGV path implements IConstrainedEntity
        /// </summary>
        private static bool Test4_AGVPathImplementsIConstrainedEntity()
        {
            var fromWaypoint = new AGVWaypointData
            {
                Id = "wp1",
                X = 0,
                Y = 0
            };

            var toWaypoint = new AGVWaypointData
            {
                Id = "wp2",
                X = 100,
                Y = 0
            };

            var agvPath = new AGVPathData
            {
                Id = "path1",
                FromWaypointId = "wp1",
                ToWaypointId = "wp2"
            };

            // Check interface implementation
            bool implementsInterface = agvPath is IConstrainedEntity;
            bool supportsMovement = agvPath.SupportsConstrainedMovement;

            // Get constraint with waypoints
            var constraint = agvPath.GetConstraint(fromWaypoint, toWaypoint);

            bool hasConstraint = constraint != null;
            bool isLinear = constraint is LinearConstraint;

            // Verify constraint properties
            if (constraint is LinearConstraint linear)
            {
                bool correctStart = Math.Abs(linear.Start.X - 0) < 0.01;
                bool correctEnd = Math.Abs(linear.End.X - 100) < 0.01;

                return implementsInterface && supportsMovement && hasConstraint &&
                       isLinear && correctStart && correctEnd;
            }

            return false;
        }

        /// <summary>
        /// Test 5: Zone implements IConstrainedEntity and creates polygon constraint
        /// </summary>
        private static bool Test5_ZoneImplementsIConstrainedEntity()
        {
            var zone = new ZoneData
            {
                Id = "zone1",
                Name = "Restricted Area"
            };

            zone.Points.Add(new PointData(0, 0));
            zone.Points.Add(new PointData(100, 0));
            zone.Points.Add(new PointData(100, 100));
            zone.Points.Add(new PointData(0, 100));

            // Check interface implementation
            bool implementsInterface = zone is IConstrainedEntity;
            bool supportsMovement = zone.SupportsConstrainedMovement;

            // Get constraint
            var constraint = zone.GetConstraint();

            bool hasConstraint = constraint != null;
            bool isPolygon = constraint is PolygonConstraint;

            // Verify constraint properties
            if (constraint is PolygonConstraint polygon)
            {
                bool correctVertexCount = polygon.Vertices.Count == 4;
                bool containsInsidePoint = polygon.Contains(new Point(50, 50));
                bool doesNotContainOutsidePoint = !polygon.Contains(new Point(150, 50));

                return implementsInterface && supportsMovement && hasConstraint &&
                       isPolygon && correctVertexCount && containsInsidePoint &&
                       doesNotContainOutsidePoint;
            }

            return false;
        }

        /// <summary>
        /// Test 6: ConstraintFactory resolves crane-runway dependencies
        /// </summary>
        private static bool Test6_ConstraintFactoryResolvesCraneDependencies()
        {
            var layout = new LayoutData();

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
                ZoneMin = 0,
                ZoneMax = 1
            };
            layout.EOTCranes.Add(crane);

            var factory = new ConstraintFactory(layout);

            // Test GetConstraintForEntity with automatic dependency resolution
            var constraint = factory.GetConstraintForEntity(crane);

            bool hasConstraint = constraint != null;
            bool isLinear = constraint is LinearConstraint;

            // Test specific method
            var craneConstraint = factory.GetEOTCraneConstraint(crane);
            bool hasSpecificConstraint = craneConstraint != null;

            // Test SupportsConstrainedMovement
            bool supports = factory.SupportsConstrainedMovement(crane);

            return hasConstraint && isLinear && hasSpecificConstraint && supports;
        }

        /// <summary>
        /// Test 7: ConstraintFactory resolves AGV path-waypoint dependencies
        /// </summary>
        private static bool Test7_ConstraintFactoryResolvesAGVDependencies()
        {
            var layout = new LayoutData();

            var wp1 = new AGVWaypointData
            {
                Id = "wp1",
                X = 0,
                Y = 0
            };
            layout.AGVWaypoints.Add(wp1);

            var wp2 = new AGVWaypointData
            {
                Id = "wp2",
                X = 100,
                Y = 100
            };
            layout.AGVWaypoints.Add(wp2);

            var agvPath = new AGVPathData
            {
                Id = "path1",
                FromWaypointId = "wp1",
                ToWaypointId = "wp2"
            };
            layout.AGVPaths.Add(agvPath);

            var factory = new ConstraintFactory(layout);

            // Test GetConstraintForEntity with automatic dependency resolution
            var constraint = factory.GetConstraintForEntity(agvPath);

            bool hasConstraint = constraint != null;
            bool isLinear = constraint is LinearConstraint;

            // Test specific method
            var pathConstraint = factory.GetAGVPathConstraint(agvPath);
            bool hasSpecificConstraint = pathConstraint != null;

            // Test SupportsConstrainedMovement
            bool supports = factory.SupportsConstrainedMovement(agvPath);

            return hasConstraint && isLinear && hasSpecificConstraint && supports;
        }
    }
}
