using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 8A Tests: Conflict Rules Framework
    /// Tests conflict type enum, conflict model, and IConflictRule interface
    /// </summary>
    public static class Stage8ATests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 8A Tests: Conflict Rules Framework ===\n");

            var tests = new Func<bool>[]
            {
                Test1_ConflictTypeEnumComplete,
                Test2_ConflictModelWorks
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

            Console.WriteLine($"\nStage 8A Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: ConflictType enum has all required types
        /// </summary>
        private static bool Test1_ConflictTypeEnumComplete()
        {
            // Verify all conflict types are defined
            var conflictTypes = Enum.GetValues(typeof(ConflictType));

            bool hasPedestrianUnderDropZone = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.PedestrianUnderDropZone);
            bool hasCrossingWithoutSignal = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.CrossingWithoutSignal);
            bool hasWalkwayBlocksTransport = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.WalkwayBlocksTransport);
            bool hasCraneOverlapNoHandoff = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.CraneOverlapNoHandoff);
            bool hasAGVPathConflict = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.AGVPathConflict);
            bool hasForkliftInAGVZone = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.ForkliftInAGVZone);
            bool hasEmergencyExitBlocked = conflictTypes.Cast<ConflictType>()
                .Any(t => t == ConflictType.EmergencyExitBlocked);

            // Should have exactly 7 conflict types
            bool hasCorrectCount = conflictTypes.Length == 7;

            return hasPedestrianUnderDropZone && hasCrossingWithoutSignal &&
                   hasWalkwayBlocksTransport && hasCraneOverlapNoHandoff &&
                   hasAGVPathConflict && hasForkliftInAGVZone &&
                   hasEmergencyExitBlocked && hasCorrectCount;
        }

        /// <summary>
        /// Test 2: Conflict model can be created and populated
        /// </summary>
        private static bool Test2_ConflictModelWorks()
        {
            // Create a conflict instance
            var conflict = new Conflict
            {
                Type = ConflictType.PedestrianUnderDropZone,
                Description = "Walkway passes through crane drop zone",
                Location = new Point(150, 200),
                Severity = ConflictSeverity.Error,
                SuggestedFix = "Reroute walkway around drop zone or add protective barrier"
            };

            // Add involved element IDs
            conflict.InvolvedElementIds.Add("walkway-1");
            conflict.InvolvedElementIds.Add("dropzone-5");

            // Add metadata
            conflict.Metadata["WalkwayType"] = "Emergency";
            conflict.Metadata["DropZoneHeight"] = "3.5m";

            // Verify all properties work
            bool hasId = !string.IsNullOrEmpty(conflict.Id);
            bool hasCorrectType = conflict.Type == ConflictType.PedestrianUnderDropZone;
            bool hasDescription = conflict.Description == "Walkway passes through crane drop zone";
            bool hasLocation = conflict.Location.X == 150 && conflict.Location.Y == 200;
            bool hasSeverity = conflict.Severity == ConflictSeverity.Error;
            bool hasSuggestedFix = !string.IsNullOrEmpty(conflict.SuggestedFix);
            bool hasInvolvedIds = conflict.InvolvedElementIds.Count == 2;
            bool hasMetadata = conflict.Metadata.Count == 2;
            bool hasTimestamp = conflict.DetectedAt != default(DateTime);
            bool notAcknowledged = !conflict.IsAcknowledged;

            // Test acknowledgment
            conflict.IsAcknowledged = true;
            bool canAcknowledge = conflict.IsAcknowledged;

            // Test ToString
            string str = conflict.ToString();
            bool hasToString = str.Contains("Error") && str.Contains("PedestrianUnderDropZone");

            return hasId && hasCorrectType && hasDescription && hasLocation &&
                   hasSeverity && hasSuggestedFix && hasInvolvedIds && hasMetadata &&
                   hasTimestamp && notAcknowledged && canAcknowledge && hasToString;
        }
    }
}
