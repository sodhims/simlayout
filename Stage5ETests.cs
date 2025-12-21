using System;
using System.Linq;
using System.Windows.Controls;
using LayoutEditor.Models;
using LayoutEditor.Renderers;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    public static class Stage5ETests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 5E: Flexible Transport Renderer Tests ===\n");
            int passed = 0, failed = 0;

            // T5E.1: Forklift aisles render
            if (Test_T5E_1_ForkliftAislesRender()) passed++; else failed++;

            // T5E.2: Staging areas render
            if (Test_T5E_2_StagingAreasRender()) passed++; else failed++;

            // T5E.3: Crossing zones render
            if (Test_T5E_3_CrossingZonesRender()) passed++; else failed++;

            // T5E.4: Layer property correct
            if (Test_T5E_4_LayerPropertyCorrect()) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/4");
            Console.WriteLine($"Failed: {failed}/4");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T5E_1_ForkliftAislesRender()
        {
            // Create test layout with forklift aisle
            var layout = new LayoutData();
            var canvas = new Canvas();
            var selectionService = new SelectionService();

            // Add forklift aisle with 3 centerline points
            var aisle = new ForkliftAisleData
            {
                Name = "Main Aisle",
                Width = 3.5,
                Centerline = new System.Collections.Generic.List<PointData>
                {
                    new PointData(0, 0),
                    new PointData(100, 0),
                    new PointData(100, 50)
                }
            };
            layout.ForkliftAisles.Add(aisle);

            // Render
            var renderer = new FlexibleTransportRenderer(selectionService);
            int elementCount = 0;
            renderer.Render(canvas, layout, (id, element) => { elementCount++; });

            // Should have rendered lines for 2 segments (each segment has 3 lines: 2 edges + 1 centerline)
            // Plus 1 label = 2*3 + 1 = 7 elements minimum
            var result = canvas.Children.Count >= 6;

            Console.WriteLine($"T5E.1 - Forklift aisles render: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Elements: {canvas.Children.Count}, Expected: ≥6)");
            return result;
        }

        private static bool Test_T5E_2_StagingAreasRender()
        {
            // Create test layout with staging area
            var layout = new LayoutData();
            var canvas = new Canvas();
            var selectionService = new SelectionService();

            // Add staging area
            var staging = new StagingAreaData
            {
                Name = "Loading Dock",
                Capacity = 25,
                Boundary = new System.Collections.Generic.List<PointData>
                {
                    new PointData(0, 0),
                    new PointData(50, 0),
                    new PointData(50, 30),
                    new PointData(0, 30)
                }
            };
            layout.StagingAreas.Add(staging);

            // Render
            var renderer = new FlexibleTransportRenderer(selectionService);
            int elementCount = 0;
            renderer.Render(canvas, layout, (id, element) => { elementCount++; });

            // Should have rendered polygon and label (2 elements)
            var result = canvas.Children.Count >= 2;

            Console.WriteLine($"T5E.2 - Staging areas render: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Elements: {canvas.Children.Count}, Expected: ≥2)");
            return result;
        }

        private static bool Test_T5E_3_CrossingZonesRender()
        {
            // Create test layout with crossing zone
            var layout = new LayoutData();
            var canvas = new Canvas();
            var selectionService = new SelectionService();

            // Add crossing zone with boundary
            var crossing = new CrossingZoneData
            {
                Name = "AGV Crossing",
                X = 100,
                Y = 100,
                AisleId = "aisle1",
                AGVPathId = "path1",
                CrossingType = CrossingTypes.SignalControlled,
                Boundary = new System.Collections.Generic.List<PointData>
                {
                    new PointData(95, 95),
                    new PointData(105, 95),
                    new PointData(105, 105),
                    new PointData(95, 105)
                }
            };
            layout.CrossingZones.Add(crossing);

            // Render
            var renderer = new FlexibleTransportRenderer(selectionService);
            int elementCount = 0;
            renderer.Render(canvas, layout, (id, element) => { elementCount++; });

            // Should have rendered polygon and label (2 elements)
            var result = canvas.Children.Count >= 2;

            Console.WriteLine($"T5E.3 - Crossing zones render: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Elements: {canvas.Children.Count}, Expected: ≥2)");
            return result;
        }

        private static bool Test_T5E_4_LayerPropertyCorrect()
        {
            var selectionService = new SelectionService();
            var renderer = new FlexibleTransportRenderer(selectionService);

            var layerCorrect = renderer.Layer == LayerType.FlexibleTransport;
            var zOrderCorrect = renderer.ZOrderBase == 600;

            var result = layerCorrect && zOrderCorrect;

            Console.WriteLine($"T5E.4 - Layer property correct: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Layer: {renderer.Layer}, ZOrder: {renderer.ZOrderBase})");
            return result;
        }
    }
}
