using System;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    public static class Stage5ATests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 5A: Crane Coverage Models Tests ===\n");
            int passed = 0, failed = 0;

            // T5A.1: EOT coverage returns rectangle
            if (Test_T5A_1_EOTCoverageReturnsRectangle()) passed++; else failed++;

            // T5A.2: Jib coverage returns arc polygon
            if (Test_T5A_2_JibCoverageReturnsArc()) passed++; else failed++;

            // T5A.3: Layer property correct
            if (Test_T5A_3_LayerPropertyCorrect()) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/3");
            Console.WriteLine($"Failed: {failed}/3");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T5A_1_EOTCoverageReturnsRectangle()
        {
            // Create a runway from (0,0) to (100,0) - horizontal line
            var runway = new RunwayData
            {
                StartX = 0,
                StartY = 0,
                EndX = 100,
                EndY = 0
            };

            // Create EOT crane covering full runway with 10m reach on each side
            var crane = new EOTCraneData
            {
                RunwayId = runway.Id,
                ZoneMin = 0.0,
                ZoneMax = 1.0,
                ReachLeft = 10,
                ReachRight = 10
            };

            var coverage = crane.GetCoveragePolygon(runway);

            // Should return 4 corners forming a rectangle
            var result = coverage.Count == 4;

            // Verify it's a rectangle with correct dimensions
            if (result)
            {
                // Check that we have points at the extremes
                var minX = coverage.Min(p => p.X);
                var maxX = coverage.Max(p => p.X);
                var minY = coverage.Min(p => p.Y);
                var maxY = coverage.Max(p => p.Y);

                // Rectangle should span 0-100 in X, -10 to 10 in Y
                result = Math.Abs(minX - 0) < 0.1 &&
                        Math.Abs(maxX - 100) < 0.1 &&
                        Math.Abs(minY - (-10)) < 0.1 &&
                        Math.Abs(maxY - 10) < 0.1;
            }

            Console.WriteLine($"T5A.1 - EOT coverage returns rectangle: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Points: {coverage.Count}, Expected: 4)");
            return result;
        }

        private static bool Test_T5A_2_JibCoverageReturnsArc()
        {
            // Create a jib crane at (50, 50) with 20m radius, 90-degree arc
            var jibCrane = new JibCraneData
            {
                CenterX = 50,
                CenterY = 50,
                Radius = 20,
                ArcStart = 0,    // 0 degrees (right)
                ArcEnd = 90      // 90 degrees (up)
            };

            var coverage = jibCrane.GetCoveragePolygon(segments: 16);

            // Should return arc approximation (center + arc points + close back to center)
            // For a 90-degree arc with 16 segments: center + 17 arc points + center = 19 points
            var expectedPoints = 1 + 16 + 1 + 1; // center + segments + 1 (end point) + center (close)
            var result = coverage.Count == expectedPoints;

            // Verify the shape is an arc sector
            if (result)
            {
                // First point should be center
                result = Math.Abs(coverage[0].X - 50) < 0.1 &&
                        Math.Abs(coverage[0].Y - 50) < 0.1;

                // Check that arc points are at radius distance from center
                if (result)
                {
                    for (int i = 1; i < coverage.Count - 1; i++)
                    {
                        var dx = coverage[i].X - 50;
                        var dy = coverage[i].Y - 50;
                        var dist = Math.Sqrt(dx * dx + dy * dy);

                        if (Math.Abs(dist - 20) > 0.1)
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }

            Console.WriteLine($"T5A.2 - Jib coverage returns arc polygon: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Points: {coverage.Count}, Arc shape: {(result ? "valid" : "invalid")})");
            return result;
        }

        private static bool Test_T5A_3_LayerPropertyCorrect()
        {
            var eotCrane = new EOTCraneData();
            var jibCrane = new JibCraneData();

            var eotLayerCorrect = eotCrane.ArchitectureLayer == LayerType.OverheadTransport;
            var jibLayerCorrect = jibCrane.ArchitectureLayer == LayerType.OverheadTransport;

            var result = eotLayerCorrect && jibLayerCorrect;

            Console.WriteLine($"T5A.3 - Layer property correct: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(EOT: {eotCrane.ArchitectureLayer}, Jib: {jibCrane.ArchitectureLayer})");
            return result;
        }
    }
}
