using System;
using System.Linq;
using System.Windows.Controls;
using LayoutEditor.Models;
using LayoutEditor.Renderers;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    public static class Stage5CTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 5C: Overhead Transport Renderer Tests ===\n");
            int passed = 0, failed = 0;

            // T5C.1: Crane coverage renders
            if (Test_T5C_1_CraneCoverageRenders()) passed++; else failed++;

            // T5C.2: Handoff points render
            if (Test_T5C_2_HandoffPointsRender()) passed++; else failed++;

            // T5C.3: Drop zones render
            if (Test_T5C_3_DropZonesRender()) passed++; else failed++;

            // T5C.4: Layer property correct
            if (Test_T5C_4_LayerPropertyCorrect()) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/4");
            Console.WriteLine($"Failed: {failed}/4");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T5C_1_CraneCoverageRenders()
        {
            // Create test layout with crane and runway
            var layout = new LayoutData();
            var canvas = new Canvas();
            var selectionService = new SelectionService();

            // Add runway
            var runway = new RunwayData
            {
                Id = "runway1",
                StartX = 0,
                StartY = 0,
                EndX = 100,
                EndY = 0
            };
            layout.Runways.Add(runway);

            // Add EOT crane
            var eotCrane = new EOTCraneData
            {
                Name = "EOT Test",
                RunwayId = runway.Id,
                ZoneMin = 0.0,
                ZoneMax = 1.0,
                ReachLeft = 10,
                ReachRight = 10
            };
            layout.EOTCranes.Add(eotCrane);

            // Add Jib crane
            var jibCrane = new JibCraneData
            {
                Name = "Jib Test",
                CenterX = 50,
                CenterY = 50,
                Radius = 20
            };
            layout.JibCranes.Add(jibCrane);

            // Render
            var renderer = new OverheadTransportRenderer(selectionService);
            int elementCount = 0;
            renderer.Render(canvas, layout, (id, element) => { elementCount++; });

            // Should have rendered coverage polygons and labels (EOT + Jib = at least 4 elements)
            var result = canvas.Children.Count >= 4;

            Console.WriteLine($"T5C.1 - Crane coverage renders: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Elements: {canvas.Children.Count}, Expected: ≥4)");
            return result;
        }

        private static bool Test_T5C_2_HandoffPointsRender()
        {
            // Create test layout with handoff points
            var layout = new LayoutData();
            var canvas = new Canvas();
            var selectionService = new SelectionService();

            // Add handoff points
            var handoff1 = new HandoffPointData
            {
                Name = "Handoff 1",
                X = 100,
                Y = 100,
                HandoffType = HandoffTypes.Direct
            };
            layout.HandoffPoints.Add(handoff1);

            var handoff2 = new HandoffPointData
            {
                Name = "Handoff 2",
                X = 200,
                Y = 200,
                HandoffType = HandoffTypes.GroundBuffer
            };
            layout.HandoffPoints.Add(handoff2);

            // Render
            var renderer = new OverheadTransportRenderer(selectionService);
            int elementCount = 0;
            renderer.Render(canvas, layout, (id, element) => { elementCount++; });

            // Should have rendered 2 stars and 2 labels (4 elements total)
            var result = canvas.Children.Count >= 4;

            Console.WriteLine($"T5C.2 - Handoff points render: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Elements: {canvas.Children.Count}, Expected: ≥4)");
            return result;
        }

        private static bool Test_T5C_3_DropZonesRender()
        {
            // Create test layout with drop zones
            var layout = new LayoutData();
            var canvas = new Canvas();
            var selectionService = new SelectionService();

            // Add drop zone
            var dropZone = new DropZoneData
            {
                Name = "Drop Zone 1",
                CraneId = "crane1",
                IsPedestrianExclusion = true,
                Boundary = new System.Collections.Generic.List<PointData>
                {
                    new PointData(0, 0),
                    new PointData(50, 0),
                    new PointData(50, 50),
                    new PointData(0, 50)
                }
            };
            layout.DropZones.Add(dropZone);

            // Render
            var renderer = new OverheadTransportRenderer(selectionService);
            int elementCount = 0;
            renderer.Render(canvas, layout, (id, element) => { elementCount++; });

            // Should have rendered polygon and label (2 elements)
            var result = canvas.Children.Count >= 2;

            Console.WriteLine($"T5C.3 - Drop zones render: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Elements: {canvas.Children.Count}, Expected: ≥2)");
            return result;
        }

        private static bool Test_T5C_4_LayerPropertyCorrect()
        {
            var selectionService = new SelectionService();
            var renderer = new OverheadTransportRenderer(selectionService);

            var layerCorrect = renderer.Layer == LayerType.OverheadTransport;
            var zOrderCorrect = renderer.ZOrderBase == 500;

            var result = layerCorrect && zOrderCorrect;

            Console.WriteLine($"T5C.4 - Layer property correct: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Layer: {renderer.Layer}, ZOrder: {renderer.ZOrderBase})");
            return result;
        }
    }
}
