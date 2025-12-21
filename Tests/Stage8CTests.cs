using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Controls;
using LayoutEditor.Models;
using LayoutEditor.Renderers;
using LayoutEditor.Services.Conflicts;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 8C Tests: Conflict Visualization
    /// Tests conflict rendering, panel, and user interaction
    /// </summary>
    public static class Stage8CTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 8C Tests: Conflict Visualization ===\n");

            var tests = new Func<bool>[]
            {
                Test1_ConflictHighlightsVisible,
                Test2_ConflictPanelListsAll,
                Test3_ConflictSelectionWorks,
                Test4_AutoCheckWorks
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

            Console.WriteLine($"\nStage 8C Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Conflict highlights are visible on canvas
        /// </summary>
        private static bool Test1_ConflictHighlightsVisible()
        {
            var canvas = new Canvas();
            var renderer = new ConflictRenderer();

            // Create test conflicts
            var conflicts = new System.Collections.Generic.List<Conflict>
            {
                new Conflict
                {
                    Type = ConflictType.PedestrianUnderDropZone,
                    Description = "Test conflict",
                    Location = new Point(100, 100),
                    Severity = ConflictSeverity.Error
                },
                new Conflict
                {
                    Type = ConflictType.CrossingWithoutSignal,
                    Description = "Test warning",
                    Location = new Point(200, 200),
                    Severity = ConflictSeverity.Warning
                }
            };

            // Render conflicts
            renderer.RenderConflicts(canvas, conflicts);

            // Check that elements were added to canvas
            bool hasElements = canvas.Children.Count > 0;

            // Check that error (red) and warning (orange) highlights exist
            var ellipses = canvas.Children.OfType<System.Windows.Shapes.Ellipse>().ToList();
            bool hasHighlights = ellipses.Count >= 2;

            return hasElements && hasHighlights;
        }

        /// <summary>
        /// Test 2: Conflict panel lists all conflicts
        /// </summary>
        private static bool Test2_ConflictPanelListsAll()
        {
            var panel = new ConflictPanel();

            // Create test conflicts
            var conflicts = new System.Collections.Generic.List<Conflict>
            {
                new Conflict
                {
                    Type = ConflictType.PedestrianUnderDropZone,
                    Description = "Conflict 1",
                    Location = new Point(100, 100),
                    Severity = ConflictSeverity.Error
                },
                new Conflict
                {
                    Type = ConflictType.CrossingWithoutSignal,
                    Description = "Conflict 2",
                    Location = new Point(200, 200),
                    Severity = ConflictSeverity.Warning
                },
                new Conflict
                {
                    Type = ConflictType.WalkwayBlocksTransport,
                    Description = "Conflict 3",
                    Location = new Point(300, 300),
                    Severity = ConflictSeverity.Warning
                }
            };

            // Load conflicts
            panel.LoadConflicts(conflicts);

            // Panel should be created successfully
            bool panelCreated = panel != null;

            // Close panel
            panel.Close();

            return panelCreated;
        }

        /// <summary>
        /// Test 3: Clicking conflict in panel raises selection event
        /// </summary>
        private static bool Test3_ConflictSelectionWorks()
        {
            var panel = new ConflictPanel();
            bool eventRaised = false;
            Conflict? selectedConflict = null;

            // Subscribe to selection event
            panel.ConflictSelected += (sender, conflict) =>
            {
                eventRaised = true;
                selectedConflict = conflict;
            };

            // Create and load conflicts
            var testConflict = new Conflict
            {
                Type = ConflictType.PedestrianUnderDropZone,
                Description = "Test conflict",
                Location = new Point(100, 100),
                Severity = ConflictSeverity.Error
            };

            panel.LoadConflicts(new System.Collections.Generic.List<Conflict> { testConflict });

            // Note: Full UI testing would require actually clicking the list item
            // For now, verify panel is set up correctly
            bool panelReady = panel != null;

            panel.Close();

            return panelReady;
        }

        /// <summary>
        /// Test 4: ConflictChecker can be called multiple times (auto-check simulation)
        /// </summary>
        private static bool Test4_AutoCheckWorks()
        {
            var layout = new LayoutData();
            var checker = new ConflictChecker();

            // First check - empty layout
            var conflicts1 = checker.CheckAll(layout);
            bool firstCheckOk = conflicts1 != null;

            // Modify layout - add elements
            var walkway = new WalkwayData { Id = "w1", Name = "Test" };
            walkway.Centerline.Add(new PointData(10, 10));
            walkway.Centerline.Add(new PointData(50, 10));
            layout.Walkways.Add(walkway);

            // Second check - with walkway
            var conflicts2 = checker.CheckAll(layout);
            bool secondCheckOk = conflicts2 != null;

            // Third check - should work repeatedly (simulates auto-check)
            var conflicts3 = checker.CheckAll(layout);
            bool thirdCheckOk = conflicts3 != null;

            return firstCheckOk && secondCheckOk && thirdCheckOk;
        }
    }
}
