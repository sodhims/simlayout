using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Data;
using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Repositories;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Tests for Stage 6C: Connection & Zone Repositories
    /// </summary>
    public static class Stage6CTests
    {
        private static string _testDbPath = "";

        /// <summary>
        /// Helper method to create a test layout in the database
        /// </summary>
        private static async Task CreateTestLayoutAsync(DatabaseManager dbManager, string layoutId = "layout1")
        {
            using var connection = dbManager.GetConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Layouts (Id, Name, Width, Height, Unit, CreatedDate, ModifiedDate, Version)
                VALUES (@id, @name, @width, @height, @unit, @createdDate, @modifiedDate, @version)";

            command.Parameters.AddWithValue("@id", layoutId);
            command.Parameters.AddWithValue("@name", "Test Layout");
            command.Parameters.AddWithValue("@width", 100.0);
            command.Parameters.AddWithValue("@height", 100.0);
            command.Parameters.AddWithValue("@unit", "meters");
            command.Parameters.AddWithValue("@createdDate", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@modifiedDate", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@version", 1);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Helper method to create test elements in the database
        /// </summary>
        private static async Task CreateTestElementsAsync(DatabaseManager dbManager)
        {
            using var connection = dbManager.GetConnection();
            for (int i = 1; i <= 3; i++)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Elements (Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate)
                    VALUES (@id, @layoutId, @elementType, @layer, @name, @propertiesJson, @createdDate, @modifiedDate)";

                command.Parameters.AddWithValue("@id", $"elem{i}");
                command.Parameters.AddWithValue("@layoutId", "layout1");
                command.Parameters.AddWithValue("@elementType", "Conveyor");
                command.Parameters.AddWithValue("@layer", 2);
                command.Parameters.AddWithValue("@name", $"Element {i}");
                command.Parameters.AddWithValue("@propertiesJson", "{}");
                command.Parameters.AddWithValue("@createdDate", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@modifiedDate", DateTime.UtcNow.ToString("o"));

                await command.ExecuteNonQueryAsync();
            }
        }

        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 6C: Connection & Zone Repositories Tests ===\n");
            int passed = 0, failed = 0;

            // T6C.1: Insert and retrieve connection
            if (Test_T6C_1_InsertAndRetrieveConnection().Result) passed++; else failed++;

            // T6C.2: Get connections by source/target
            if (Test_T6C_2_GetConnectionsBySourceTarget().Result) passed++; else failed++;

            // T6C.3: Delete connection
            if (Test_T6C_3_DeleteConnection().Result) passed++; else failed++;

            // T6C.4: Batch insert connections
            if (Test_T6C_4_BatchInsertConnections().Result) passed++; else failed++;

            // T6C.5: Insert and retrieve zone
            if (Test_T6C_5_InsertAndRetrieveZone().Result) passed++; else failed++;

            // T6C.6: Update zone
            if (Test_T6C_6_UpdateZone().Result) passed++; else failed++;

            // T6C.7: Get zones by type
            if (Test_T6C_7_GetZonesByType().Result) passed++; else failed++;

            // T6C.8: Element-Zone relationships
            if (Test_T6C_8_ElementZoneRelationships().Result) passed++; else failed++;

            // T6C.9: Zone count and exists
            if (Test_T6C_9_ZoneCountAndExists().Result) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/9");
            Console.WriteLine($"Failed: {failed}/9");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static async Task<bool> Test_T6C_1_InsertAndRetrieveConnection()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                await CreateTestElementsAsync(dbManager);
                var repo = new SQLiteConnectionRepository(dbManager);

                // Create test connection
                var connection = new ConnectionDto
                {
                    Id = "conn1",
                    LayoutId = "layout1",
                    SourceElementId = "elem1",
                    TargetElementId = "elem2",
                    ConnectionType = DbConnectionType.CraneHandoff,
                    PropertiesJson = "{\"Priority\": 1}"
                };

                // Insert
                var inserted = await repo.InsertAsync(connection);

                // Retrieve
                var retrieved = await repo.GetByIdAsync("conn1");

                var result = inserted && retrieved != null &&
                            retrieved.SourceElementId == "elem1" &&
                            retrieved.TargetElementId == "elem2" &&
                            retrieved.ConnectionType == DbConnectionType.CraneHandoff;

                Console.WriteLine($"T6C.1 - Insert and retrieve connection: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Inserted: {inserted}, Retrieved: {retrieved != null})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.1 - Insert and retrieve connection: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_2_GetConnectionsBySourceTarget()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                await CreateTestElementsAsync(dbManager);
                var repo = new SQLiteConnectionRepository(dbManager);

                // Insert connections
                await repo.InsertAsync(new ConnectionDto { Id = "c1", LayoutId = "layout1", SourceElementId = "elem1", TargetElementId = "elem2", ConnectionType = DbConnectionType.CraneHandoff });
                await repo.InsertAsync(new ConnectionDto { Id = "c2", LayoutId = "layout1", SourceElementId = "elem1", TargetElementId = "elem3", ConnectionType = DbConnectionType.CraneHandoff });
                await repo.InsertAsync(new ConnectionDto { Id = "c3", LayoutId = "layout1", SourceElementId = "elem2", TargetElementId = "elem3", ConnectionType = DbConnectionType.CraneDropZone });

                // Get by source
                var fromElem1 = (await repo.GetBySourceElementAsync("elem1")).ToList();

                // Get by target
                var toElem3 = (await repo.GetByTargetElementAsync("elem3")).ToList();

                // Get between elements
                var between = (await repo.GetBetweenElementsAsync("elem1", "elem2")).ToList();

                var result = fromElem1.Count == 2 &&
                            toElem3.Count == 2 &&
                            between.Count == 1 &&
                            between[0].Id == "c1";

                Console.WriteLine($"T6C.2 - Get connections by source/target: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(From elem1: {fromElem1.Count}, To elem3: {toElem3.Count}, Between: {between.Count})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.2 - Get connections by source/target: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_3_DeleteConnection()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                await CreateTestElementsAsync(dbManager);
                var repo = new SQLiteConnectionRepository(dbManager);

                // Insert
                await repo.InsertAsync(new ConnectionDto { Id = "conn1", LayoutId = "layout1", SourceElementId = "elem1", TargetElementId = "elem2", ConnectionType = DbConnectionType.Generic });

                // Delete
                var deleted = await repo.DeleteAsync("conn1");

                // Verify deletion
                var exists = await repo.ExistsAsync("conn1");

                var result = deleted && !exists;

                Console.WriteLine($"T6C.3 - Delete connection: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Deleted: {deleted}, Still exists: {exists})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.3 - Delete connection: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_4_BatchInsertConnections()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                await CreateTestElementsAsync(dbManager);
                var repo = new SQLiteConnectionRepository(dbManager);

                // Create batch
                var connections = new[]
                {
                    new ConnectionDto { Id = "c1", LayoutId = "layout1", SourceElementId = "elem1", TargetElementId = "elem2", ConnectionType = DbConnectionType.Generic },
                    new ConnectionDto { Id = "c2", LayoutId = "layout1", SourceElementId = "elem2", TargetElementId = "elem3", ConnectionType = DbConnectionType.Generic }
                };

                // Batch insert
                var inserted = await repo.BatchInsertAsync(connections);

                // Verify
                var count = await repo.GetCountAsync("layout1");

                var result = inserted == 2 && count == 2;

                Console.WriteLine($"T6C.4 - Batch insert connections: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Inserted: {inserted}, Count: {count})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.4 - Batch insert connections: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_5_InsertAndRetrieveZone()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                var repo = new SQLiteZoneRepository(dbManager);

                // Create test zone
                var zone = new ZoneDto
                {
                    Id = "zone1",
                    LayoutId = "layout1",
                    Name = "Loading Zone",
                    ZoneType = DbZoneType.Functional,
                    BoundaryJson = "[{\"X\":0,\"Y\":0},{\"X\":10,\"Y\":0},{\"X\":10,\"Y\":10},{\"X\":0,\"Y\":10}]",
                    PropertiesJson = "{\"Capacity\": 50}"
                };

                // Insert
                var inserted = await repo.InsertAsync(zone);

                // Retrieve
                var retrieved = await repo.GetByIdAsync("zone1");

                var result = inserted && retrieved != null &&
                            retrieved.Name == "Loading Zone" &&
                            retrieved.ZoneType == DbZoneType.Functional;

                Console.WriteLine($"T6C.5 - Insert and retrieve zone: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Inserted: {inserted}, Retrieved: {retrieved != null})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.5 - Insert and retrieve zone: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_6_UpdateZone()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                var repo = new SQLiteZoneRepository(dbManager);

                // Insert
                var zone = new ZoneDto { Id = "zone1", LayoutId = "layout1", Name = "Original Name", ZoneType = DbZoneType.Functional };
                await repo.InsertAsync(zone);

                // Update
                zone.Name = "Updated Name";
                var updated = await repo.UpdateAsync(zone);

                // Retrieve and verify
                var retrieved = await repo.GetByIdAsync("zone1");

                var result = updated && retrieved != null && retrieved.Name == "Updated Name";

                Console.WriteLine($"T6C.6 - Update zone: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Updated: {updated}, Name changed: {retrieved?.Name == "Updated Name"})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.6 - Update zone: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_7_GetZonesByType()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                var repo = new SQLiteZoneRepository(dbManager);

                // Insert zones
                await repo.InsertAsync(new ZoneDto { Id = "z1", LayoutId = "layout1", Name = "Zone 1", ZoneType = DbZoneType.Functional });
                await repo.InsertAsync(new ZoneDto { Id = "z2", LayoutId = "layout1", Name = "Zone 2", ZoneType = DbZoneType.Functional });
                await repo.InsertAsync(new ZoneDto { Id = "z3", LayoutId = "layout1", Name = "Zone 3", ZoneType = DbZoneType.Safety });

                // Get by type
                var functionalZones = (await repo.GetByLayoutAndTypeAsync("layout1", DbZoneType.Functional)).ToList();

                var result = functionalZones.Count == 2 &&
                            functionalZones.All(z => z.ZoneType == DbZoneType.Functional);

                Console.WriteLine($"T6C.7 - Get zones by type: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Functional zones: {functionalZones.Count}, Expected: 2)");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.7 - Get zones by type: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_8_ElementZoneRelationships()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                await CreateTestElementsAsync(dbManager);
                var repo = new SQLiteZoneRepository(dbManager);

                // Create zone
                await repo.InsertAsync(new ZoneDto { Id = "zone1", LayoutId = "layout1", Name = "Zone 1", ZoneType = DbZoneType.Functional });

                // Add elements to zone
                await repo.AddElementToZoneAsync("zone1", "elem1");
                await repo.AddElementToZoneAsync("zone1", "elem2");

                // Get elements in zone
                var elementsInZone = (await repo.GetElementsInZoneAsync("zone1")).ToList();

                // Get zones for element
                var zonesForElem1 = (await repo.GetZonesForElementAsync("elem1")).ToList();

                // Remove element from zone
                await repo.RemoveElementFromZoneAsync("zone1", "elem1");
                var elementsAfterRemove = (await repo.GetElementsInZoneAsync("zone1")).ToList();

                var result = elementsInZone.Count == 2 &&
                            zonesForElem1.Count == 1 &&
                            zonesForElem1[0] == "zone1" &&
                            elementsAfterRemove.Count == 1 &&
                            elementsAfterRemove[0] == "elem2";

                Console.WriteLine($"T6C.8 - Element-Zone relationships: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Elements in zone: {elementsInZone.Count}, After remove: {elementsAfterRemove.Count})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.8 - Element-Zone relationships: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static async Task<bool> Test_T6C_9_ZoneCountAndExists()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();
                await CreateTestLayoutAsync(dbManager);
                var repo = new SQLiteZoneRepository(dbManager);

                // Insert zones
                await repo.InsertAsync(new ZoneDto { Id = "z1", LayoutId = "layout1", Name = "Zone 1", ZoneType = DbZoneType.Functional });
                await repo.InsertAsync(new ZoneDto { Id = "z2", LayoutId = "layout1", Name = "Zone 2", ZoneType = DbZoneType.Safety });

                // Test count
                var count = await repo.GetCountAsync("layout1");

                // Test exists
                var exists1 = await repo.ExistsAsync("z1");
                var existsNot = await repo.ExistsAsync("nonexistent");

                var result = count == 2 && exists1 && !existsNot;

                Console.WriteLine($"T6C.9 - Zone count and exists: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Count: {count}, Exists z1: {exists1}, Exists nonexistent: {existsNot})");

                dbManager.DeleteDatabase();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6C.9 - Zone count and exists: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }
    }
}
