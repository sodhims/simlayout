using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Renderers;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 7B Tests: Pedestrian Renderer
    /// Tests rendering of walkways, crossings, and safety zones
    /// </summary>
    public static class Stage7BTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 7B Tests: Pedestrian Renderer ===\n");

            var tests = new Func<bool>[]
            {
                Test1_PedestrianRendererProperties,
                Test2_WalkwaysRenderCorrectly,
                Test3_CrossingsRenderCorrectly,
                Test4_SafetyZonesRenderCorrectly
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

            Console.WriteLine($"\nStage 7B Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: PedestrianRenderer has correct Layer and ZOrderBase
        /// </summary>
        private static bool Test1_PedestrianRendererProperties()
        {
            var selectionService = new SelectionService();
            var renderer = new PedestrianRenderer(selectionService);

            bool layerCorrect = renderer.Layer == LayerType.Pedestrian;
            bool zOrderCorrect = renderer.ZOrderBase == 700;

            return layerCorrect && zOrderCorrect;
        }

        /// <summary>
        /// Test 2: Walkways render with filled corridors and dashed edges
        /// </summary>
        private static bool Test2_WalkwaysRenderCorrectly()
        {
            var selectionService = new SelectionService();
            var renderer = new PedestrianRenderer(selectionService);
            var canvas = new Canvas();
            var layout = new LayoutData();

            // Create a walkway with two segments
            var walkway = new WalkwayData
            {
                Id = "w1",
                Name = "Main Walkway",
                Width = 30,
                WalkwayType = WalkwayTypes.Primary,
                Color = "#4A90E2"
            };
            walkway.Centerline.Add(new PointData(100, 100));
            walkway.Centerline.Add(new PointData(200, 100));
            walkway.Centerline.Add(new PointData(200, 200));

            layout.Walkways.Add(walkway);

            // Render
            renderer.Render(canvas, layout, (id, elem) => { });

            // Should have corridor segments (polygons) and a label (textblock)
            var polygons = canvas.Children.OfType<Polygon>().ToList();
            var labels = canvas.Children.OfType<TextBlock>().ToList();

            bool hasCorridorSegments = polygons.Count >= 2; // At least 2 segments
            bool hasLabel = labels.Any(l => l.Text == "Main Walkway");
            bool hasDashedEdges = polygons.Any(p => p.StrokeDashArray != null && p.StrokeDashArray.Count > 0);

            return hasCorridorSegments && hasLabel && hasDashedEdges;
        }

        /// <summary>
        /// Test 3: Crossings render with zebra stripes, signal icons, and dashed outlines
        /// </summary>
        private static bool Test3_CrossingsRenderCorrectly()
        {
            var selectionService = new SelectionService();
            var renderer = new PedestrianRenderer(selectionService);
            var canvas = new Canvas();
            var layout = new LayoutData();

            // Create zebra crossing
            var zebraCrossing = new PedestrianCrossingData
            {
                Id = "c1",
                Name = "Zebra",
                CrossingType = PedestrianCrossingTypes.Zebra,
                Color = "#FFFFFF"
            };
            zebraCrossing.Location.Add(new PointData(100, 100));
            zebraCrossing.Location.Add(new PointData(150, 100));
            layout.PedestrianCrossings.Add(zebraCrossing);

            // Create signal crossing
            var signalCrossing = new PedestrianCrossingData
            {
                Id = "c2",
                Name = "Signal",
                CrossingType = PedestrianCrossingTypes.Signal,
                Color = "#FFFFFF"
            };
            signalCrossing.Location.Add(new PointData(200, 100));
            signalCrossing.Location.Add(new PointData(250, 100));
            signalCrossing.Location.Add(new PointData(250, 150));
            layout.PedestrianCrossings.Add(signalCrossing);

            // Create unmarked crossing
            var unmarkedCrossing = new PedestrianCrossingData
            {
                Id = "c3",
                Name = "Unmarked",
                CrossingType = PedestrianCrossingTypes.Unmarked,
                Color = "#CCCCCC"
            };
            unmarkedCrossing.Location.Add(new PointData(300, 100));
            unmarkedCrossing.Location.Add(new PointData(350, 100));
            layout.PedestrianCrossings.Add(unmarkedCrossing);

            // Render
            renderer.Render(canvas, layout, (id, elem) => { });

            // Check zebra stripes (multiple polygons for stripes)
            var polygons = canvas.Children.OfType<Polygon>().ToList();
            bool hasZebraStripes = polygons.Count >= 3; // Should have multiple stripes

            // Check signal icons (rectangles and ellipses)
            var rectangles = canvas.Children.OfType<Rectangle>().ToList();
            var ellipses = canvas.Children.OfType<Ellipse>().ToList();
            bool hasSignalIcon = rectangles.Any() && ellipses.Count >= 2; // Traffic light box + red/green lights

            // Check unmarked crossing (dashed outline)
            bool hasDashedOutline = polygons.Any(p => p.StrokeDashArray != null && p.StrokeDashArray.Count > 0);

            return hasZebraStripes && hasSignalIcon && hasDashedOutline;
        }

        /// <summary>
        /// Test 4: Safety zones render with hatched pattern and bold boundary
        /// </summary>
        private static bool Test4_SafetyZonesRenderCorrectly()
        {
            var selectionService = new SelectionService();
            var renderer = new PedestrianRenderer(selectionService);
            var canvas = new Canvas();
            var layout = new LayoutData();

            // Create safety zone
            var zone = new SafetyZoneData
            {
                Id = "z1",
                Name = "Hard Hat Area",
                ZoneType = SafetyZoneTypes.HardHat,
                Color = "#FFAA00"
            };
            zone.Boundary.Add(new PointData(100, 100));
            zone.Boundary.Add(new PointData(200, 100));
            zone.Boundary.Add(new PointData(200, 200));
            zone.Boundary.Add(new PointData(100, 200));

            layout.SafetyZones.Add(zone);

            // Render
            renderer.Render(canvas, layout, (id, elem) => { });

            // Check polygon exists
            var polygons = canvas.Children.OfType<Polygon>().ToList();
            bool hasZonePolygon = polygons.Count > 0;

            // Check for bold boundary (stroke thickness >= 3)
            bool hasBoldBoundary = polygons.Any(p => p.StrokeThickness >= 3);

            // Check for hatched pattern (VisualBrush fill)
            bool hasHatchedPattern = polygons.Any(p => p.Fill is VisualBrush);

            // Check label
            var labels = canvas.Children.OfType<TextBlock>().ToList();
            bool hasLabel = labels.Any(l => l.Text.Contains("Hard Hat Area"));

            return hasZonePolygon && hasBoldBoundary && hasHatchedPattern && hasLabel;
        }
    }
}
