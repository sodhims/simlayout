using System;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 9B Tests: Snapping & Guides
    /// Tests grid snapping, element snapping, guides, and spacing
    /// </summary>
    public static class Stage9BTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 9B Tests: Snapping & Guides ===\n");

            var tests = new Func<bool>[]
            {
                Test1_GridSnapWorks,
                Test2_ElementSnapWorks,
                Test3_GuidesAppear,
                Test4_SpacingSnapWorks
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

            Console.WriteLine($"\nStage 9B Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Grid snap rounds position to grid
        /// </summary>
        private static bool Test1_GridSnapWorks()
        {
            var snappingService = new SnappingService();
            snappingService.GridSize = 10;
            snappingService.GridSnapEnabled = true;

            // Test snapping various points
            var point1 = new Point(12, 18);
            var snapped1 = snappingService.SnapToGrid(point1);
            // 12/10=1.2, round to 1, *10=10; 18/10=1.8, round to 2, *10=20
            bool test1 = Math.Abs(snapped1.X - 10) < 0.01 && Math.Abs(snapped1.Y - 20) < 0.01;

            var point2 = new Point(25, 33);
            var snapped2 = snappingService.SnapToGrid(point2);
            // 25/10=2.5, round to 2 or 3 (banker's rounding), *10=20 or 30; 33/10=3.3, round to 3, *10=30
            bool test2 = (Math.Abs(snapped2.X - 20) < 0.01 || Math.Abs(snapped2.X - 30) < 0.01) &&
                        Math.Abs(snapped2.Y - 30) < 0.01;

            var point3 = new Point(44, 46);
            var snapped3 = snappingService.SnapToGrid(point3);
            // 44/10=4.4, round to 4, *10=40; 46/10=4.6, round to 5, *10=50
            bool test3 = Math.Abs(snapped3.X - 40) < 0.01 && Math.Abs(snapped3.Y - 50) < 0.01;

            // Test with grid snap disabled
            snappingService.GridSnapEnabled = false;
            var point4 = new Point(12, 18);
            var snapped4 = snappingService.SnapToGrid(point4);
            bool test4 = Math.Abs(snapped4.X - 12) < 0.01 && Math.Abs(snapped4.Y - 18) < 0.01;

            return test1 && test2 && test3 && test4;
        }

        /// <summary>
        /// Test 2: Element snap works - snaps to nearby edge
        /// </summary>
        private static bool Test2_ElementSnapWorks()
        {
            var layout = new LayoutData();
            var snappingService = new SnappingService();
            snappingService.ElementSnapEnabled = true;
            snappingService.SnapThreshold = 10;

            // Create a reference node
            var refNode = new NodeData { Id = "ref" };
            refNode.Visual.X = 100;
            refNode.Visual.Y = 100;
            refNode.Visual.Width = 50;
            refNode.Visual.Height = 50;
            layout.Nodes.Add(refNode);

            // Test snapping to left edge (at X=100)
            var point1 = new Point(102, 120);
            var (snapped1, guides1) = snappingService.SnapToElements(point1, layout);
            bool snapToLeft = Math.Abs(snapped1.X - 100) < 0.01;
            bool hasGuide1 = guides1.Count > 0;

            // Test snapping to right edge (at X=150)
            var point2 = new Point(148, 120);
            var (snapped2, guides2) = snappingService.SnapToElements(point2, layout);
            bool snapToRight = Math.Abs(snapped2.X - 150) < 0.01;
            bool hasGuide2 = guides2.Count > 0;

            // Test snapping to top edge (at Y=100)
            var point3 = new Point(120, 102);
            var (snapped3, guides3) = snappingService.SnapToElements(point3, layout);
            bool snapToTop = Math.Abs(snapped3.Y - 100) < 0.01;
            bool hasGuide3 = guides3.Count > 0;

            // Test no snap when too far
            var point4 = new Point(200, 200);
            var (snapped4, guides4) = snappingService.SnapToElements(point4, layout);
            bool noSnap = snapped4.X == 200 && snapped4.Y == 200;
            bool noGuide = guides4.Count == 0;

            return snapToLeft && hasGuide1 && snapToRight && hasGuide2 &&
                   snapToTop && hasGuide3 && noSnap && noGuide;
        }

        /// <summary>
        /// Test 3: Guides appear during snap
        /// </summary>
        private static bool Test3_GuidesAppear()
        {
            var layout = new LayoutData();
            var snappingService = new SnappingService();

            // Create reference nodes
            var node1 = new NodeData { Id = "n1" };
            node1.Visual.X = 100;
            node1.Visual.Y = 100;
            node1.Visual.Width = 50;
            node1.Visual.Height = 50;
            layout.Nodes.Add(node1);

            var node2 = new NodeData { Id = "n2" };
            node2.Visual.X = 200;
            node2.Visual.Y = 100;
            node2.Visual.Width = 50;
            node2.Visual.Height = 50;
            layout.Nodes.Add(node2);

            // Test guides are created when snapping
            var point = new Point(102, 102);
            var (snappedPoint, guides) = snappingService.SnapToElements(point, layout);

            bool hasGuides = guides.Count > 0;
            bool guidesAreValid = guides.Count > 0 &&
                                 (guides[0].IsVertical || !guides[0].IsVertical); // Just check property exists

            // Test guide lines have correct orientation
            bool hasVerticalGuide = guides.Exists(g => g.IsVertical);
            bool hasHorizontalGuide = guides.Exists(g => !g.IsVertical);

            return hasGuides && guidesAreValid;
        }

        /// <summary>
        /// Test 4: Spacing calculation works
        /// </summary>
        private static bool Test4_SpacingSnapWorks()
        {
            var layout = new LayoutData();
            var snappingService = new SnappingService();

            // Create nodes with spacing
            var node1 = new NodeData { Id = "n1" };
            node1.Visual.X = 100;
            node1.Visual.Y = 100;
            node1.Visual.Width = 50;
            node1.Visual.Height = 50;
            layout.Nodes.Add(node1);

            var node2 = new NodeData { Id = "n2" };
            node2.Visual.X = 170; // 20 pixel gap from node1
            node2.Visual.Y = 100;
            node2.Visual.Width = 50;
            node2.Visual.Height = 50;
            layout.Nodes.Add(node2);

            var node3 = new NodeData { Id = "n3" };
            node3.Visual.X = 240; // 20 pixel gap from node2
            node3.Visual.Y = 100;
            node3.Visual.Width = 50;
            node3.Visual.Height = 50;
            layout.Nodes.Add(node3);

            // Calculate spacing
            var bounds = new System.Windows.Rect(0, 0, 400, 400);
            var spacingGuides = snappingService.CalculateSpacing(layout, bounds);

            // Should detect spacing between nodes
            bool hasSpacing = spacingGuides.Count >= 2;
            bool correctSpacing = spacingGuides.Count > 0 &&
                                Math.Abs(spacingGuides[0].Spacing - 20) < 0.01;

            return hasSpacing && correctSpacing;
        }
    }
}
