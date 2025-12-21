using System;
using System.Linq;
using LayoutEditor.Models;
using System.Text.Json;

namespace LayoutEditor.Tests
{
    public static class Stage5DTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 5D: Forklift Models Tests ===\n");
            int passed = 0, failed = 0;

            // T5D.1: ForkliftAisle serializes/deserializes
            if (Test_T5D_1_ForkliftAisleSerializes()) passed++; else failed++;

            // T5D.2: StagingArea serializes/deserializes
            if (Test_T5D_2_StagingAreaSerializes()) passed++; else failed++;

            // T5D.3: CrossingZone serializes/deserializes
            if (Test_T5D_3_CrossingZoneSerializes()) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/3");
            Console.WriteLine($"Failed: {failed}/3");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T5D_1_ForkliftAisleSerializes()
        {
            // Create a forklift aisle
            var aisle = new ForkliftAisleData
            {
                Id = "aisle1",
                Name = "Main Aisle",
                Width = 3.5,
                MinTurningRadius = 2.0,
                AisleType = ForkliftAisleTypes.Main,
                Centerline = new System.Collections.Generic.List<PointData>
                {
                    new PointData(0, 0),
                    new PointData(100, 0),
                    new PointData(100, 50)
                }
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(aisle);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<ForkliftAisleData>(json);

            // Verify properties
            var result = deserialized != null &&
                        deserialized.Id == "aisle1" &&
                        deserialized.Name == "Main Aisle" &&
                        Math.Abs(deserialized.Width - 3.5) < 0.1 &&
                        Math.Abs(deserialized.MinTurningRadius - 2.0) < 0.1 &&
                        deserialized.AisleType == ForkliftAisleTypes.Main &&
                        deserialized.Centerline != null &&
                        deserialized.Centerline.Count == 3;

            // Verify layer property
            if (result && deserialized != null)
            {
                result = deserialized.ArchitectureLayer == LayerType.FlexibleTransport;
            }

            Console.WriteLine($"T5D.1 - ForkliftAisle serializes/deserializes: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Round-trip: {(result ? "success" : "failed")}, Centerline points: {deserialized?.Centerline?.Count ?? 0})");
            return result;
        }

        private static bool Test_T5D_2_StagingAreaSerializes()
        {
            // Create a staging area
            var staging = new StagingAreaData
            {
                Id = "staging1",
                Name = "Loading Dock A",
                Capacity = 20,
                StagingType = StagingTypes.Dock,
                Boundary = new System.Collections.Generic.List<PointData>
                {
                    new PointData(0, 0),
                    new PointData(20, 0),
                    new PointData(20, 10),
                    new PointData(0, 10)
                }
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(staging);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<StagingAreaData>(json);

            // Verify properties
            var result = deserialized != null &&
                        deserialized.Id == "staging1" &&
                        deserialized.Name == "Loading Dock A" &&
                        deserialized.Capacity == 20 &&
                        deserialized.StagingType == StagingTypes.Dock &&
                        deserialized.Boundary != null &&
                        deserialized.Boundary.Count == 4;

            // Verify layer property
            if (result && deserialized != null)
            {
                result = deserialized.ArchitectureLayer == LayerType.FlexibleTransport;
            }

            Console.WriteLine($"T5D.2 - StagingArea serializes/deserializes: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Round-trip: {(result ? "success" : "failed")}, Capacity: {deserialized?.Capacity ?? 0})");
            return result;
        }

        private static bool Test_T5D_3_CrossingZoneSerializes()
        {
            // Create a crossing zone
            var crossing = new CrossingZoneData
            {
                Id = "crossing1",
                Name = "AGV/Forklift Crossing A",
                X = 100,
                Y = 200,
                AisleId = "aisle1",
                AGVPathId = "path1",
                CrossingType = CrossingTypes.SignalControlled,
                Boundary = new System.Collections.Generic.List<PointData>
                {
                    new PointData(95, 195),
                    new PointData(105, 195),
                    new PointData(105, 205),
                    new PointData(95, 205)
                }
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(crossing);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<CrossingZoneData>(json);

            // Verify properties
            var result = deserialized != null &&
                        deserialized.Id == "crossing1" &&
                        deserialized.Name == "AGV/Forklift Crossing A" &&
                        Math.Abs(deserialized.X - 100) < 0.1 &&
                        Math.Abs(deserialized.Y - 200) < 0.1 &&
                        deserialized.AisleId == "aisle1" &&
                        deserialized.AGVPathId == "path1" &&
                        deserialized.CrossingType == CrossingTypes.SignalControlled &&
                        deserialized.Boundary != null &&
                        deserialized.Boundary.Count == 4;

            // Verify layer property
            if (result && deserialized != null)
            {
                result = deserialized.ArchitectureLayer == LayerType.FlexibleTransport;
            }

            Console.WriteLine($"T5D.3 - CrossingZone serializes/deserializes: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Round-trip: {(result ? "success" : "failed")}, Type: {deserialized?.CrossingType ?? "null"})");
            return result;
        }
    }
}
