using System;
using System.Linq;
using LayoutEditor.Models;
using System.Text.Json;

namespace LayoutEditor.Tests
{
    public static class Stage5BTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 5B: Handoff & Drop Zone Models Tests ===\n");
            int passed = 0, failed = 0;

            // T5B.1: HandoffPoint serializes/deserializes
            if (Test_T5B_1_HandoffPointSerializes()) passed++; else failed++;

            // T5B.2: DropZone serializes/deserializes
            if (Test_T5B_2_DropZoneSerializes()) passed++; else failed++;

            // T5B.3: Collections added to LayoutData
            if (Test_T5B_3_CollectionsInLayoutData()) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/3");
            Console.WriteLine($"Failed: {failed}/3");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T5B_1_HandoffPointSerializes()
        {
            // Create a handoff point
            var handoff = new HandoffPointData
            {
                Id = "handoff1",
                Name = "Handoff Test",
                X = 100,
                Y = 50,
                RunwayId = "runway1",
                Crane1Id = "crane1",
                Crane2Id = "crane2",
                Position = 0.5,
                HandoffType = HandoffTypes.Direct,
                HandoffRule = "transfer"
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(handoff);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<HandoffPointData>(json);

            // Verify properties
            var result = deserialized != null &&
                        deserialized.Id == "handoff1" &&
                        deserialized.Name == "Handoff Test" &&
                        Math.Abs(deserialized.X - 100) < 0.1 &&
                        Math.Abs(deserialized.Y - 50) < 0.1 &&
                        deserialized.RunwayId == "runway1" &&
                        deserialized.Crane1Id == "crane1" &&
                        deserialized.Crane2Id == "crane2" &&
                        Math.Abs(deserialized.Position - 0.5) < 0.01 &&
                        deserialized.HandoffType == HandoffTypes.Direct &&
                        deserialized.HandoffRule == "transfer";

            // Verify layer property
            if (result && deserialized != null)
            {
                result = deserialized.ArchitectureLayer == LayerType.OverheadTransport;
            }

            Console.WriteLine($"T5B.1 - HandoffPoint serializes/deserializes: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Round-trip: {(result ? "success" : "failed")})");
            return result;
        }

        private static bool Test_T5B_2_DropZoneSerializes()
        {
            // Create a drop zone
            var dropZone = new DropZoneData
            {
                Id = "dropzone1",
                Name = "Drop Zone Test",
                CraneId = "crane1",
                IsPedestrianExclusion = true,
                Boundary = new System.Collections.Generic.List<PointData>
                {
                    new PointData(0, 0),
                    new PointData(10, 0),
                    new PointData(10, 10),
                    new PointData(0, 10)
                }
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(dropZone);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<DropZoneData>(json);

            // Verify properties
            var result = deserialized != null &&
                        deserialized.Id == "dropzone1" &&
                        deserialized.Name == "Drop Zone Test" &&
                        deserialized.CraneId == "crane1" &&
                        deserialized.IsPedestrianExclusion == true &&
                        deserialized.Boundary != null &&
                        deserialized.Boundary.Count == 4;

            // Verify layer property
            if (result && deserialized != null)
            {
                result = deserialized.ArchitectureLayer == LayerType.OverheadTransport;
            }

            Console.WriteLine($"T5B.2 - DropZone serializes/deserializes: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(Round-trip: {(result ? "success" : "failed")}, Boundary points: {deserialized?.Boundary?.Count ?? 0})");
            return result;
        }

        private static bool Test_T5B_3_CollectionsInLayoutData()
        {
            var layout = new LayoutData();

            // Test HandoffPoints collection
            var handoff = new HandoffPointData
            {
                Id = "handoff1",
                Name = "Test Handoff",
                X = 50,
                Y = 50
            };
            layout.HandoffPoints.Add(handoff);

            var handoffAdded = layout.HandoffPoints.Count == 1 &&
                              layout.HandoffPoints[0].Id == "handoff1";

            // Test DropZones collection
            var dropZone = new DropZoneData
            {
                Id = "dropzone1",
                Name = "Test Drop Zone",
                CraneId = "crane1"
            };
            layout.DropZones.Add(dropZone);

            var dropZoneAdded = layout.DropZones.Count == 1 &&
                               layout.DropZones[0].Id == "dropzone1";

            // Test removal
            layout.HandoffPoints.Remove(handoff);
            layout.DropZones.Remove(dropZone);

            var canRemove = layout.HandoffPoints.Count == 0 &&
                           layout.DropZones.Count == 0;

            var result = handoffAdded && dropZoneAdded && canRemove;

            Console.WriteLine($"T5B.3 - Collections added to LayoutData: {(result ? "✓ PASS" : "✗ FAIL")} " +
                             $"(HandoffPoints: {(handoffAdded ? "✓" : "✗")}, DropZones: {(dropZoneAdded ? "✓" : "✗")}, Remove: {(canRemove ? "✓" : "✗")})");
            return result;
        }
    }
}
