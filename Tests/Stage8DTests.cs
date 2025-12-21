using System;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Data;
using LayoutEditor.Data.Services;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 8D Tests: Database Integration for Conflict Management
    /// Tests conflict resolution persistence and layer connections
    /// </summary>
    public static class Stage8DTests
    {
        public static async Task<bool> RunAllTests()
        {
            Console.WriteLine("\n=== Stage 8D Tests: Database Integration ===\n");

            var tests = new Func<Task<bool>>[]
            {
                Test1_ConflictResolutionSaveLoad,
                Test2_LayerConnectionSaveLoad,
                Test3_MultipleConflictsPersist
            };

            int passed = 0;
            int failed = 0;

            for (int i = 0; i < tests.Length; i++)
            {
                try
                {
                    bool result = await tests[i]();
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

            Console.WriteLine($"\nStage 8D Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: ConflictResolution saves and loads correctly
        /// </summary>
        private static async Task<bool> Test1_ConflictResolutionSaveLoad()
        {
            var dbPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"test_conflict_res_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(dbPath);
                dbManager.EnsureCreated();

                var service = new LayoutService(dbManager);

                // Create layout with conflict resolution
                var layout = new LayoutData();
                layout.Metadata.Name = "Test Layout";

                var resolution = new ConflictResolutionData
                {
                    Id = "res-1",
                    ConflictType = "PedestrianUnderDropZone",
                    Description = "Walkway under crane resolved",
                    AcknowledgedBy = "TestUser",
                    ResolutionNotes = "Added protective barrier",
                    IsResolved = true
                };
                resolution.InvolvedElementIds.Add("walkway-1");
                resolution.InvolvedElementIds.Add("dropzone-1");

                layout.ConflictResolutions.Add(resolution);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved)
                    return false;

                // Load
                var loaded = await service.LoadLayoutAsync(layout.Id);
                if (loaded == null)
                    return false;

                // Verify
                bool hasResolution = loaded.ConflictResolutions.Count == 1;
                bool correctType = loaded.ConflictResolutions[0].ConflictType == "PedestrianUnderDropZone";
                bool correctDescription = loaded.ConflictResolutions[0].Description == "Walkway under crane resolved";
                bool hasElements = loaded.ConflictResolutions[0].InvolvedElementIds.Count == 2;

                return hasResolution && correctType && correctDescription && hasElements;
            }
            finally
            {
                // Note: Database files are left in temp folder for cleanup by OS
                // Deleting immediately causes "file in use" issues
            }
        }

        /// <summary>
        /// Test 2: LayerConnection saves and loads correctly
        /// </summary>
        private static async Task<bool> Test2_LayerConnectionSaveLoad()
        {
            var dbPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"test_layer_conn_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(dbPath);
                dbManager.EnsureCreated();

                var service = new LayoutService(dbManager);

                // Create layout with layer connection
                var layout = new LayoutData();
                layout.Metadata.Name = "Test Layout";

                var connection = new LayerConnectionData
                {
                    Id = "conn-1",
                    Name = "AGV-Forklift Crossing",
                    FromElementId = "agv-path-1",
                    ToElementId = "forklift-aisle-1",
                    ConnectionType = LayerConnectionTypes.Crossing,
                    X = 100,
                    Y = 200,
                    Notes = "Signaled crossing zone"
                };

                layout.LayerConnections.Add(connection);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved)
                    return false;

                // Load
                var loaded = await service.LoadLayoutAsync(layout.Id);
                if (loaded == null)
                    return false;

                // Verify
                bool hasConnection = loaded.LayerConnections.Count == 1;
                bool correctName = loaded.LayerConnections[0].Name == "AGV-Forklift Crossing";
                bool correctType = loaded.LayerConnections[0].ConnectionType == LayerConnectionTypes.Crossing;
                bool correctLocation = loaded.LayerConnections[0].X == 100 && loaded.LayerConnections[0].Y == 200;

                return hasConnection && correctName && correctType && correctLocation;
            }
            finally
            {
                // Note: Database files are left in temp folder for cleanup by OS
                // Deleting immediately causes "file in use" issues
            }
        }

        /// <summary>
        /// Test 3: Multiple conflict resolutions and connections persist correctly
        /// </summary>
        private static async Task<bool> Test3_MultipleConflictsPersist()
        {
            var dbPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"test_multi_conflicts_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(dbPath);
                dbManager.EnsureCreated();

                var service = new LayoutService(dbManager);

                // Create layout with multiple resolutions and connections
                var layout = new LayoutData();
                layout.Metadata.Name = "Complex Layout";

                // Add 3 conflict resolutions
                for (int i = 1; i <= 3; i++)
                {
                    var resolution = new ConflictResolutionData
                    {
                        Id = $"res-{i}",
                        ConflictType = $"ConflictType{i}",
                        Description = $"Conflict {i} resolved",
                        IsResolved = true
                    };
                    layout.ConflictResolutions.Add(resolution);
                }

                // Add 2 layer connections
                for (int i = 1; i <= 2; i++)
                {
                    var connection = new LayerConnectionData
                    {
                        Id = $"conn-{i}",
                        Name = $"Connection {i}",
                        ConnectionType = LayerConnectionTypes.Handoff,
                        X = i * 100,
                        Y = i * 100
                    };
                    layout.LayerConnections.Add(connection);
                }

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved)
                    return false;

                // Load
                var loaded = await service.LoadLayoutAsync(layout.Id);
                if (loaded == null)
                    return false;

                // Verify counts
                bool correctResolutionCount = loaded.ConflictResolutions.Count == 3;
                bool correctConnectionCount = loaded.LayerConnections.Count == 2;

                // Verify specific items
                bool hasResolution2 = loaded.ConflictResolutions.Any(r => r.ConflictType == "ConflictType2");
                bool hasConnection1 = loaded.LayerConnections.Any(c => c.Name == "Connection 1");

                return correctResolutionCount && correctConnectionCount && hasResolution2 && hasConnection1;
            }
            finally
            {
                // Note: Database files are left in temp folder for cleanup by OS
                // Deleting immediately causes "file in use" issues
            }
        }
    }
}
