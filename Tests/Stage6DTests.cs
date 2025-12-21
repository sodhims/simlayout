using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Data;
using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Mapping;
using LayoutEditor.Data.Repositories;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 6D Tests: Layout Repository & Domain Mapping
    /// Tests layout CRUD operations and domain-to-DTO mapping
    /// </summary>
    public static class Stage6DTests
    {
        private static string _testDbPath = "";

        public static async Task<bool> RunAllTests()
        {
            Console.WriteLine("\n=== Stage 6D Tests: Layout Repository & Domain Mapping ===\n");

            var tests = new Func<Task<bool>>[]
            {
                Test1_InsertAndRetrieveLayout,
                Test2_UpdateLayout,
                Test3_DeleteLayout,
                Test4_LayoutExistsAndCount,
                Test5_ConveyorDomainToDto,
                Test6_ConveyorDtoToDomain,
                Test7_EOTCraneDomainToDto,
                Test8_MapperRegistryMultipleTypes
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

            Console.WriteLine($"\nStage 6D Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Insert and retrieve a layout
        /// </summary>
        private static async Task<bool> Test1_InsertAndRetrieveLayout()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var repo = new SQLiteLayoutRepository(dbManager);

                var layout = new LayoutDto("test-layout-1", "Test Layout", 200.0, 150.0, "meters");

                bool inserted = await repo.InsertAsync(layout);
                if (!inserted) return false;

                var retrieved = await repo.GetByIdAsync("test-layout-1");
                if (retrieved == null) return false;

                return retrieved.Id == "test-layout-1" &&
                       retrieved.Name == "Test Layout" &&
                       retrieved.Width == 200.0 &&
                       retrieved.Height == 150.0 &&
                       retrieved.Unit == "meters";
            }
            finally
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

        /// <summary>
        /// Test 2: Update an existing layout
        /// </summary>
        private static async Task<bool> Test2_UpdateLayout()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var repo = new SQLiteLayoutRepository(dbManager);

                var layout = new LayoutDto("test-layout-2", "Original Name", 100.0, 100.0);
                await repo.InsertAsync(layout);

                layout.Name = "Updated Name";
                layout.Width = 250.0;
                layout.Version = 2;

                bool updated = await repo.UpdateAsync(layout);
                if (!updated) return false;

                var retrieved = await repo.GetByIdAsync("test-layout-2");
                if (retrieved == null) return false;

                return retrieved.Name == "Updated Name" &&
                       retrieved.Width == 250.0 &&
                       retrieved.Version == 2;
            }
            finally
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

        /// <summary>
        /// Test 3: Delete a layout
        /// </summary>
        private static async Task<bool> Test3_DeleteLayout()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var repo = new SQLiteLayoutRepository(dbManager);

                var layout = new LayoutDto("test-layout-3", "To Delete", 100.0, 100.0);
                await repo.InsertAsync(layout);

                bool deleted = await repo.DeleteAsync("test-layout-3");
                if (!deleted) return false;

                var retrieved = await repo.GetByIdAsync("test-layout-3");
                return retrieved == null;
            }
            finally
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

        /// <summary>
        /// Test 4: Layout exists and count operations
        /// </summary>
        private static async Task<bool> Test4_LayoutExistsAndCount()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                var repo = new SQLiteLayoutRepository(dbManager);

                await repo.InsertAsync(new LayoutDto("layout-1", "Layout 1", 100.0, 100.0));
                await repo.InsertAsync(new LayoutDto("layout-2", "Layout 2", 100.0, 100.0));
                await repo.InsertAsync(new LayoutDto("layout-3", "Layout 3", 100.0, 100.0));

                bool exists = await repo.ExistsAsync("layout-2");
                if (!exists) return false;

                bool notExists = await repo.ExistsAsync("layout-99");
                if (notExists) return false;

                int count = await repo.GetCountAsync();
                return count == 3;
            }
            finally
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

        /// <summary>
        /// Test 5: Convert ConveyorData domain model to DTO
        /// </summary>
        private static async Task<bool> Test5_ConveyorDomainToDto()
        {
            var registry = new MapperRegistry();

            var conveyor = new ConveyorData
            {
                Id = "conveyor-1",
                Name = "Test Conveyor",
                Width = 1.5,
                Speed = 2.0,
                Direction = ConveyorDirections.Bidirectional,
                ConveyorType = ConveyorTypes.Belt
            };

            conveyor.Path.Add(new PointData(10, 20));
            conveyor.Path.Add(new PointData(30, 40));

            var dto = registry.ConveyorToDto(conveyor, "layout-1");

            if (dto.Id != "conveyor-1") return false;
            if (dto.LayoutId != "layout-1") return false;
            if (dto.ElementType != DbElementType.Conveyor) return false;
            if (dto.Layer != (int)LayerType.Equipment) return false;
            if (dto.Name != "Test Conveyor") return false;
            if (string.IsNullOrEmpty(dto.PropertiesJson)) return false;

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Test 6: Convert ElementDto to ConveyorData domain model
        /// </summary>
        private static async Task<bool> Test6_ConveyorDtoToDomain()
        {
            var registry = new MapperRegistry();

            var originalConveyor = new ConveyorData
            {
                Id = "conveyor-2",
                Name = "Test Conveyor 2",
                Width = 2.0,
                Speed = 1.5,
                Direction = ConveyorDirections.Forward,
                ConveyorType = ConveyorTypes.Roller
            };

            originalConveyor.Path.Add(new PointData(15, 25));
            originalConveyor.Path.Add(new PointData(35, 45));

            var dto = registry.ConveyorToDto(originalConveyor, "layout-1");
            var convertedConveyor = registry.ConveyorFromDto(dto);

            if (convertedConveyor.Id != "conveyor-2") return false;
            if (convertedConveyor.Name != "Test Conveyor 2") return false;
            if (convertedConveyor.Width != 2.0) return false;
            if (convertedConveyor.Speed != 1.5) return false;
            if (convertedConveyor.Direction != ConveyorDirections.Forward) return false;
            if (convertedConveyor.Path.Count != 2) return false;
            if (convertedConveyor.Path[0].X != 15) return false;
            if (convertedConveyor.Path[0].Y != 25) return false;

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Test 7: Convert EOTCraneData domain model to DTO
        /// </summary>
        private static async Task<bool> Test7_EOTCraneDomainToDto()
        {
            var registry = new MapperRegistry();

            var crane = new EOTCraneData
            {
                Id = "crane-1",
                Name = "Test Crane",
                RunwayId = "runway-1",
                ReachLeft = 10.0,
                ReachRight = 10.0,
                SpeedBridge = 1.0,
                SpeedTrolley = 0.5,
                SpeedHoist = 0.3
            };

            var dto = registry.EOTCraneToDto(crane, "layout-2");

            if (dto.Id != "crane-1") return false;
            if (dto.LayoutId != "layout-2") return false;
            if (dto.ElementType != DbElementType.EOTCrane) return false;
            if (dto.Layer != (int)LayerType.LocalFlow) return false;
            if (dto.Name != "Test Crane") return false;
            if (string.IsNullOrEmpty(dto.PropertiesJson)) return false;

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Test 8: MapperRegistry with multiple element types
        /// </summary>
        private static async Task<bool> Test8_MapperRegistryMultipleTypes()
        {
            var registry = new MapperRegistry();

            // Test that all mappers are registered
            if (!registry.HasMapper(DbElementType.Conveyor)) return false;
            if (!registry.HasMapper(DbElementType.EOTCrane)) return false;
            if (!registry.HasMapper(DbElementType.AGVPath)) return false;

            // Test GetRegisteredTypes
            var types = registry.GetRegisteredTypes().ToList();
            if (types.Count != 3) return false;
            if (!types.Contains(DbElementType.Conveyor)) return false;
            if (!types.Contains(DbElementType.EOTCrane)) return false;
            if (!types.Contains(DbElementType.AGVPath)) return false;

            // Test that we can get mappers for different types
            var conveyorMapper = registry.GetMapper<ConveyorData>(DbElementType.Conveyor);
            if (conveyorMapper == null) return false;

            var craneMapper = registry.GetMapper<EOTCraneData>(DbElementType.EOTCrane);
            if (craneMapper == null) return false;

            var pathMapper = registry.GetMapper<AGVPathData>(DbElementType.AGVPath);
            if (pathMapper == null) return false;

            return await Task.FromResult(true);
        }
    }
}
