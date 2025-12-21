using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Data;
using LayoutEditor.Data.Services;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 6E Tests: Layout Persistence Service
    /// Tests high-level service for saving and loading complete layouts
    /// </summary>
    public static class Stage6ETests
    {
        private static string _testDbPath = "";

        public static async Task<bool> RunAllTests()
        {
            Console.WriteLine("\n=== Stage 6E Tests: Layout Persistence Service ===\n");

            var tests = new Func<Task<bool>>[]
            {
                Test1_SaveAndLoadEmptyLayout,
                Test2_SaveAndLoadLayoutWithConveyors,
                Test3_SaveAndLoadLayoutWithMultipleElementTypes,
                Test4_UpdateExistingLayout,
                Test5_DeleteLayout,
                Test6_GetAllLayoutMetadata,
                Test7_GetElementCount,
                Test8_LayoutExists
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

            Console.WriteLine($"\nStage 6E Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Save and load an empty layout
        /// </summary>
        private static async Task<bool> Test1_SaveAndLoadEmptyLayout()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create empty layout
                var layout = new LayoutData();
                layout.Id = "layout-1";
                layout.Metadata.Name = "Test Layout";
                layout.Canvas.Width = 200.0;
                layout.Canvas.Height = 150.0;
                layout.Metadata.Units = "meters";

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("layout-1");
                if (loaded == null) return false;

                return loaded.Id == "layout-1" &&
                       loaded.Metadata.Name == "Test Layout" &&
                       loaded.Canvas.Width == 200.0 &&
                       loaded.Canvas.Height == 150.0;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 2: Save and load a layout with conveyors
        /// </summary>
        private static async Task<bool> Test2_SaveAndLoadLayoutWithConveyors()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with conveyors
                var layout = new LayoutData();
                layout.Id = "layout-2";
                layout.Metadata.Name = "Layout with Conveyors";
                layout.Canvas.Width = 300.0;
                layout.Canvas.Height = 200.0;

                // Add conveyors
                var conveyor1 = new ConveyorData
                {
                    Id = "conv-1",
                    Name = "Conveyor 1",
                    Width = 1.5,
                    Speed = 2.0,
                    Direction = ConveyorDirections.Forward
                };
                conveyor1.Path.Add(new PointData(10, 20));
                conveyor1.Path.Add(new PointData(50, 20));

                var conveyor2 = new ConveyorData
                {
                    Id = "conv-2",
                    Name = "Conveyor 2",
                    Width = 2.0,
                    Speed = 1.5,
                    Direction = ConveyorDirections.Bidirectional
                };
                conveyor2.Path.Add(new PointData(60, 30));
                conveyor2.Path.Add(new PointData(100, 30));

                layout.Conveyors.Add(conveyor1);
                layout.Conveyors.Add(conveyor2);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("layout-2");
                if (loaded == null) return false;

                // Verify
                return loaded.Conveyors.Count == 2 &&
                       loaded.Conveyors[0].Name == "Conveyor 1" &&
                       loaded.Conveyors[1].Name == "Conveyor 2" &&
                       loaded.Conveyors[0].Path.Count == 2 &&
                       loaded.Conveyors[1].Path.Count == 2;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 3: Save and load layout with multiple element types
        /// </summary>
        private static async Task<bool> Test3_SaveAndLoadLayoutWithMultipleElementTypes()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with multiple element types
                var layout = new LayoutData();
                layout.Id = "layout-3";
                layout.Metadata.Name = "Multi-Element Layout";
                layout.Canvas.Width = 500.0;
                layout.Canvas.Height = 400.0;

                // Add conveyor
                var conveyor = new ConveyorData
                {
                    Id = "conv-3",
                    Name = "Test Conveyor",
                    Width = 1.0,
                    Speed = 1.0
                };
                conveyor.Path.Add(new PointData(10, 10));
                layout.Conveyors.Add(conveyor);

                // Add EOT crane
                var crane = new EOTCraneData
                {
                    Id = "crane-1",
                    Name = "Test Crane",
                    RunwayId = "runway-1",
                    ReachLeft = 10.0,
                    ReachRight = 10.0
                };
                layout.EOTCranes.Add(crane);

                // Add AGV path
                var agvPath = new AGVPathData
                {
                    Id = "agv-1",
                    Name = "Test AGV Path"
                };
                layout.AGVPaths.Add(agvPath);

                // Save
                bool saved = await service.SaveLayoutAsync(layout);
                if (!saved) return false;

                // Load
                var loaded = await service.LoadLayoutAsync("layout-3");
                if (loaded == null) return false;

                // Verify all element types loaded
                return loaded.Conveyors.Count == 1 &&
                       loaded.EOTCranes.Count == 1 &&
                       loaded.AGVPaths.Count == 1;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 4: Update an existing layout
        /// </summary>
        private static async Task<bool> Test4_UpdateExistingLayout()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create and save initial layout
                var layout = new LayoutData();
                layout.Id = "layout-4";
                layout.Metadata.Name = "Original Name";
                layout.Canvas.Width = 100.0;
                layout.Canvas.Height = 100.0;

                var conveyor = new ConveyorData { Id = "conv-4", Name = "Conveyor 4" };
                conveyor.Path.Add(new PointData(5, 5));
                layout.Conveyors.Add(conveyor);

                await service.SaveLayoutAsync(layout);

                // Update layout
                layout.Metadata.Name = "Updated Name";
                layout.Canvas.Width = 250.0;

                // Add another conveyor
                var conveyor2 = new ConveyorData { Id = "conv-5", Name = "Conveyor 5" };
                conveyor2.Path.Add(new PointData(10, 10));
                layout.Conveyors.Add(conveyor2);

                await service.SaveLayoutAsync(layout);

                // Load and verify
                var loaded = await service.LoadLayoutAsync("layout-4");
                if (loaded == null) return false;

                return loaded.Metadata.Name == "Updated Name" &&
                       loaded.Canvas.Width == 250.0 &&
                       loaded.Conveyors.Count == 2;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 5: Delete a layout
        /// </summary>
        private static async Task<bool> Test5_DeleteLayout()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create and save layout
                var layout = new LayoutData();
                layout.Id = "layout-5";
                layout.Metadata.Name = "To Delete";
                layout.Canvas.Width = 100.0;
                layout.Canvas.Height = 100.0;

                await service.SaveLayoutAsync(layout);

                // Verify exists
                bool existsBefore = await service.LayoutExistsAsync("layout-5");
                if (!existsBefore) return false;

                // Delete
                bool deleted = await service.DeleteLayoutAsync("layout-5");
                if (!deleted) return false;

                // Verify deleted
                bool existsAfter = await service.LayoutExistsAsync("layout-5");
                return !existsAfter;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 6: Get all layout metadata
        /// </summary>
        private static async Task<bool> Test6_GetAllLayoutMetadata()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create multiple layouts
                for (int i = 1; i <= 3; i++)
                {
                    var layout = new LayoutData();
                    layout.Id = $"layout-{i}";
                    layout.Metadata.Name = $"Layout {i}";
                    layout.Canvas.Width = 100.0 * i;
                    layout.Canvas.Height = 100.0 * i;

                    // Add some conveyors to layout 2
                    if (i == 2)
                    {
                        layout.Conveyors.Add(new ConveyorData { Id = $"conv-{i}-1", Name = $"Conveyor {i}-1" });
                        layout.Conveyors.Add(new ConveyorData { Id = $"conv-{i}-2", Name = $"Conveyor {i}-2" });
                    }

                    await service.SaveLayoutAsync(layout);
                }

                // Get metadata
                var metadata = (await service.GetAllLayoutMetadataAsync()).ToList();

                // Verify
                if (metadata.Count != 3) return false;

                var layout2Meta = metadata.FirstOrDefault(m => m.Id == "layout-2");
                if (layout2Meta == null) return false;

                return layout2Meta.ElementCount == 2 &&
                       layout2Meta.Name == "Layout 2" &&
                       layout2Meta.Width == 200.0;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 7: Get element count for a layout
        /// </summary>
        private static async Task<bool> Test7_GetElementCount()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Create layout with elements
                var layout = new LayoutData();
                layout.Id = "layout-7";
                layout.Metadata.Name = "Element Count Test";
                layout.Canvas.Width = 100.0;
                layout.Canvas.Height = 100.0;

                // Add 5 conveyors
                for (int i = 1; i <= 5; i++)
                {
                    var conv = new ConveyorData { Id = $"conv-{i}", Name = $"Conveyor {i}" };
                    conv.Path.Add(new PointData(i * 10, i * 10));
                    layout.Conveyors.Add(conv);
                }

                // Add 2 cranes
                for (int i = 1; i <= 2; i++)
                {
                    layout.EOTCranes.Add(new EOTCraneData { Id = $"crane-{i}", Name = $"Crane {i}" });
                }

                await service.SaveLayoutAsync(layout);

                // Get element count
                int count = await service.GetElementCountAsync("layout-7");

                return count == 7; // 5 conveyors + 2 cranes
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Test 8: Check if layout exists
        /// </summary>
        private static async Task<bool> Test8_LayoutExists()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var service = new LayoutService(dbManager);

                // Check non-existent layout
                bool existsBefore = await service.LayoutExistsAsync("nonexistent");
                if (existsBefore) return false;

                // Create layout
                var layout = new LayoutData();
                layout.Id = "layout-8";
                layout.Metadata.Name = "Exists Test";
                layout.Canvas.Width = 100.0;
                layout.Canvas.Height = 100.0;

                await service.SaveLayoutAsync(layout);

                // Check existing layout
                bool existsAfter = await service.LayoutExistsAsync("layout-8");
                return existsAfter;
            }
            finally
            {
                CleanupDatabase();
            }
        }

        /// <summary>
        /// Cleanup helper to delete test database
        /// </summary>
        private static void CleanupDatabase()
        {
            try
            {
                if (File.Exists(_testDbPath))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Delete(_testDbPath);
                }
            }
            catch { /* Ignore cleanup errors */ }
        }
    }
}
