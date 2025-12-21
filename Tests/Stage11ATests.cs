using System;
using System.Collections.Generic;
using System.Windows;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 11A Tests: Constraint Framework
    /// Tests all constraint types (Linear, Arc, Path, Polygon)
    /// </summary>
    public static class Stage11ATests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 11A Tests: Constraint Framework ===\n");

            var tests = new Func<bool>[]
            {
                Test1_LinearConstraintProjection,
                Test2_LinearConstraintEvaluation,
                Test3_ArcConstraintProjection,
                Test4_ArcConstraintEvaluation,
                Test5_PathConstraintProjection,
                Test6_PathConstraintEvaluation,
                Test7_PolygonConstraintContains,
                Test8_PolygonConstraintClamp
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

            Console.WriteLine($"\nStage 11A Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Linear constraint projects points correctly
        /// </summary>
        private static bool Test1_LinearConstraintProjection()
        {
            var constraint = new LinearConstraint(new Point(0, 0), new Point(100, 0));

            // Point directly on line should project to itself
            var param1 = constraint.ProjectPoint(new Point(50, 0));
            bool test1 = Math.Abs(param1 - 0.5) < 0.01;

            // Point above line should project perpendicularly
            var param2 = constraint.ProjectPoint(new Point(50, 20));
            bool test2 = Math.Abs(param2 - 0.5) < 0.01;

            // Point before start should clamp to 0
            var param3 = constraint.ProjectPoint(new Point(-10, 0));
            bool test3 = Math.Abs(param3 - 0.0) < 0.01;

            // Point after end should clamp to 1
            var param4 = constraint.ProjectPoint(new Point(110, 0));
            bool test4 = Math.Abs(param4 - 1.0) < 0.01;

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 2: Linear constraint evaluates parameters correctly
        /// </summary>
        private static bool Test2_LinearConstraintEvaluation()
        {
            var constraint = new LinearConstraint(new Point(0, 0), new Point(100, 100));

            // Evaluate at start (0)
            var point1 = constraint.Evaluate(0.0);
            bool test1 = Math.Abs(point1.X - 0) < 0.01 && Math.Abs(point1.Y - 0) < 0.01;

            // Evaluate at middle (0.5)
            var point2 = constraint.Evaluate(0.5);
            bool test2 = Math.Abs(point2.X - 50) < 0.01 && Math.Abs(point2.Y - 50) < 0.01;

            // Evaluate at end (1)
            var point3 = constraint.Evaluate(1.0);
            bool test3 = Math.Abs(point3.X - 100) < 0.01 && Math.Abs(point3.Y - 100) < 0.01;

            // Range should be [0, 1]
            var range = constraint.GetParameterRange();
            bool test4 = range.min == 0 && range.max == 1;

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 3: Arc constraint projects points correctly
        /// </summary>
        private static bool Test3_ArcConstraintProjection()
        {
            // Arc from 0° to 90° (0 to π/2 radians)
            var constraint = new ArcConstraint(new Point(0, 0), 100, 0, Math.PI / 2);

            // Point at 45° should project to π/4
            var param1 = constraint.ProjectPoint(new Point(70.7, 70.7));
            bool test1 = Math.Abs(param1 - Math.PI / 4) < 0.1;

            // Point at 0° should project to 0
            var param2 = constraint.ProjectPoint(new Point(100, 0));
            bool test2 = Math.Abs(param2 - 0) < 0.1;

            // Point at 90° should project to π/2
            var param3 = constraint.ProjectPoint(new Point(0, 100));
            bool test3 = Math.Abs(param3 - Math.PI / 2) < 0.1;

            return test1 && test2 && test3;
        }

        /// <summary>
        /// Test 4: Arc constraint evaluates angles correctly
        /// </summary>
        private static bool Test4_ArcConstraintEvaluation()
        {
            // Arc from 0° to 180° (0 to π radians) with radius 100
            var constraint = new ArcConstraint(new Point(0, 0), 100, 0, Math.PI);

            // Evaluate at 0° (0 radians)
            var point1 = constraint.Evaluate(0);
            bool test1 = Math.Abs(point1.X - 100) < 0.01 && Math.Abs(point1.Y - 0) < 0.01;

            // Evaluate at 90° (π/2 radians)
            var point2 = constraint.Evaluate(Math.PI / 2);
            bool test2 = Math.Abs(point2.X - 0) < 0.01 && Math.Abs(point2.Y - 100) < 0.01;

            // Evaluate at 180° (π radians)
            var point3 = constraint.Evaluate(Math.PI);
            bool test3 = Math.Abs(point3.X - (-100)) < 0.01 && Math.Abs(point3.Y - 0) < 0.01;

            // Range should be [0, π]
            var range = constraint.GetParameterRange();
            bool test4 = Math.Abs(range.min - 0) < 0.01 && Math.Abs(range.max - Math.PI) < 0.01;

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 5: Path constraint projects points to nearest segment
        /// </summary>
        private static bool Test5_PathConstraintProjection()
        {
            var waypoints = new List<Point>
            {
                new Point(0, 0),
                new Point(100, 0),
                new Point(100, 100)
            };

            var constraint = new PathConstraint(waypoints);

            // Point near first segment should project to ~0.25
            var param1 = constraint.ProjectPoint(new Point(50, 10));
            bool test1 = param1 >= 0.2 && param1 <= 0.3;

            // Point near corner should project to ~0.5
            var param2 = constraint.ProjectPoint(new Point(100, 0));
            bool test2 = Math.Abs(param2 - 0.5) < 0.1;

            // Point near last segment should project to ~0.75
            var param3 = constraint.ProjectPoint(new Point(100, 50));
            bool test3 = param3 >= 0.7 && param3 <= 0.8;

            // Point at end should project to ~1.0
            var param4 = constraint.ProjectPoint(new Point(100, 100));
            bool test4 = Math.Abs(param4 - 1.0) < 0.1;

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 6: Path constraint evaluates parameters correctly
        /// </summary>
        private static bool Test6_PathConstraintEvaluation()
        {
            var waypoints = new List<Point>
            {
                new Point(0, 0),
                new Point(100, 0),
                new Point(100, 100)
            };

            var constraint = new PathConstraint(waypoints);

            // Evaluate at start (0)
            var point1 = constraint.Evaluate(0.0);
            bool test1 = Math.Abs(point1.X - 0) < 0.01 && Math.Abs(point1.Y - 0) < 0.01;

            // Evaluate at middle of path (~0.5 should be at corner)
            var point2 = constraint.Evaluate(0.5);
            bool test2 = Math.Abs(point2.X - 100) < 0.01 && Math.Abs(point2.Y - 0) < 0.01;

            // Evaluate at end (1.0)
            var point3 = constraint.Evaluate(1.0);
            bool test3 = Math.Abs(point3.X - 100) < 0.01 && Math.Abs(point3.Y - 100) < 0.01;

            // Total length should be 200
            bool test4 = Math.Abs(constraint.TotalLength - 200) < 0.01;

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 7: Polygon constraint correctly detects inside/outside
        /// </summary>
        private static bool Test7_PolygonConstraintContains()
        {
            // Square from (0,0) to (100,100)
            var vertices = new List<Point>
            {
                new Point(0, 0),
                new Point(100, 0),
                new Point(100, 100),
                new Point(0, 100)
            };

            var constraint = new PolygonConstraint(vertices);

            // Point inside
            bool test1 = constraint.Contains(new Point(50, 50));

            // Point outside
            bool test2 = !constraint.Contains(new Point(150, 50));

            // Point on edge
            bool test3 = constraint.Contains(new Point(50, 0));

            // Point at corner
            bool test4 = constraint.Contains(new Point(0, 0));

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 8: Polygon constraint clamps points to boundary
        /// </summary>
        private static bool Test8_PolygonConstraintClamp()
        {
            // Square from (0,0) to (100,100)
            var vertices = new List<Point>
            {
                new Point(0, 0),
                new Point(100, 0),
                new Point(100, 100),
                new Point(0, 100)
            };

            var constraint = new PolygonConstraint(vertices);

            // Point inside should stay the same
            var clamped1 = constraint.ClampToPolygon(new Point(50, 50));
            bool test1 = Math.Abs(clamped1.X - 50) < 0.01 && Math.Abs(clamped1.Y - 50) < 0.01;

            // Point outside to the right should clamp to right edge
            var clamped2 = constraint.ClampToPolygon(new Point(150, 50));
            bool test2 = Math.Abs(clamped2.X - 100) < 0.01 && Math.Abs(clamped2.Y - 50) < 0.01;

            // Point outside above should clamp to top edge
            var clamped3 = constraint.ClampToPolygon(new Point(50, 150));
            bool test3 = Math.Abs(clamped3.X - 50) < 0.01 && Math.Abs(clamped3.Y - 100) < 0.01;

            return test1 && test2 && test3;
        }
    }
}
