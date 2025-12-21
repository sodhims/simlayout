using System;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services.Conflicts;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 8B Tests: Conflict Rule Implementations
    /// Tests conflict detection rules and ConflictChecker service
    /// </summary>
    public static class Stage8BTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 8B Tests: Conflict Rule Implementations ===\n");

            var tests = new Func<bool>[]
            {
                Test1_PedestrianUnderDropZoneDetected,
                Test2_MissingCrossingDetected,
                Test3_MissingHandoffDetected,
                Test4_NoFalsePositives
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

            Console.WriteLine($"\nStage 8B Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Pedestrian walkway under drop zone is detected
        /// </summary>
        private static bool Test1_PedestrianUnderDropZoneDetected()
        {
            var layout = new LayoutData();
            var checker = new ConflictChecker();

            // Create a drop zone
            var dropZone = new DropZoneData
            {
                Id = "drop-1",
                Name = "Drop Zone 1"
            };
            dropZone.Boundary.Add(new PointData(100, 100));
            dropZone.Boundary.Add(new PointData(200, 100));
            dropZone.Boundary.Add(new PointData(200, 200));
            dropZone.Boundary.Add(new PointData(100, 200));
            layout.DropZones.Add(dropZone);

            // Create a walkway that passes through the drop zone
            var walkway = new WalkwayData
            {
                Id = "walk-1",
                Name = "Main Walkway",
                WalkwayType = WalkwayTypes.Primary
            };
            walkway.Centerline.Add(new PointData(50, 150));
            walkway.Centerline.Add(new PointData(250, 150)); // Passes through drop zone
            layout.Walkways.Add(walkway);

            // Check for conflicts
            var conflicts = checker.CheckAll(layout);

            // Should detect the pedestrian under drop zone conflict
            bool hasConflict = conflicts.Any(c => c.Type == ConflictType.PedestrianUnderDropZone);
            bool hasCorrectElements = conflicts.Any(c =>
                c.InvolvedElementIds.Contains("walk-1") &&
                c.InvolvedElementIds.Contains("drop-1"));

            return hasConflict && hasCorrectElements;
        }

        /// <summary>
        /// Test 2: CrossingWithoutSignalRule is registered (placeholder test until model exists)
        /// </summary>
        private static bool Test2_MissingCrossingDetected()
        {
            var checker = new ConflictChecker();

            // Verify the rule is registered
            var registeredTypes = checker.GetRegisteredTypes();
            bool hasRule = registeredTypes.Contains(ConflictType.CrossingWithoutSignal);

            // Check with empty layout returns no conflicts (placeholder implementation)
            var layout = new LayoutData();
            var conflicts = checker.CheckByType(layout, ConflictType.CrossingWithoutSignal);
            bool noConflicts = conflicts.Count == 0;

            return hasRule && noConflicts;
        }

        /// <summary>
        /// Test 3: CraneOverlapNoHandoff rule is registered (placeholder test until model exists)
        /// </summary>
        private static bool Test3_MissingHandoffDetected()
        {
            var checker = new ConflictChecker();

            // Verify the rule is registered
            var registeredTypes = checker.GetRegisteredTypes();
            bool hasRule = registeredTypes.Contains(ConflictType.CraneOverlapNoHandoff);

            // Check with empty layout returns no conflicts (placeholder implementation)
            var layout = new LayoutData();
            var conflicts = checker.CheckByType(layout, ConflictType.CraneOverlapNoHandoff);
            bool noConflicts = conflicts.Count == 0;

            return hasRule && noConflicts;
        }

        /// <summary>
        /// Test 4: Clean layout has no false positives
        /// </summary>
        private static bool Test4_NoFalsePositives()
        {
            var layout = new LayoutData();
            var checker = new ConflictChecker();

            // Create elements that don't conflict

            // Walkway that doesn't pass through any drop zone
            var walkway = new WalkwayData { Id = "walk-1", Name = "Safe Walkway" };
            walkway.Centerline.Add(new PointData(10, 10));
            walkway.Centerline.Add(new PointData(50, 10));
            layout.Walkways.Add(walkway);

            // Drop zone far away from walkway
            var dropZone = new DropZoneData { Id = "drop-1", Name = "Drop Zone 1" };
            dropZone.Boundary.Add(new PointData(200, 200));
            dropZone.Boundary.Add(new PointData(250, 200));
            dropZone.Boundary.Add(new PointData(250, 250));
            dropZone.Boundary.Add(new PointData(200, 250));
            layout.DropZones.Add(dropZone);

            // Check for conflicts
            var conflicts = checker.CheckAll(layout);

            // Should have NO conflicts
            return conflicts.Count == 0;
        }
    }
}
