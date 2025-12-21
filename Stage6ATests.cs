using System;
using System.IO;
using LayoutEditor.Data;
using Microsoft.Data.Sqlite;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Tests for Stage 6A: Schema Creation
    /// </summary>
    public static class Stage6ATests
    {
        private static string _testDbPath = "";

        public static void RunAllTests()
        {
            Console.WriteLine("=== Stage 6A: Schema Creation Tests ===\n");
            int passed = 0, failed = 0;

            // T6A.1: DatabaseManager creates database file
            if (Test_T6A_1_DatabaseManagerCreatesFile()) passed++; else failed++;

            // T6A.2: Schema tables created correctly
            if (Test_T6A_2_SchemaTablesCreated()) passed++; else failed++;

            // T6A.3: Schema indexes created correctly
            if (Test_T6A_3_SchemaIndexesCreated()) passed++; else failed++;

            // T6A.4: Migration system tracks version
            if (Test_T6A_4_MigrationTracksVersion()) passed++; else failed++;

            // T6A.5: Connection pooling works
            if (Test_T6A_5_ConnectionPoolingWorks()) passed++; else failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/5");
            Console.WriteLine($"Failed: {failed}/5");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T6A_1_DatabaseManagerCreatesFile()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);

                // Initially should not exist
                var existsBefore = dbManager.DatabaseExists();

                // Create database
                dbManager.EnsureCreated();

                // Now should exist
                var existsAfter = dbManager.DatabaseExists();

                // Verify file was created
                var fileExists = File.Exists(_testDbPath);

                var result = !existsBefore && existsAfter && fileExists;

                Console.WriteLine($"T6A.1 - DatabaseManager creates database file: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Before: {existsBefore}, After: {existsAfter}, File: {fileExists})");

                // Cleanup
                dbManager.DeleteDatabase();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6A.1 - DatabaseManager creates database file: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static bool Test_T6A_2_SchemaTablesCreated()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();

                // Check that all tables exist
                bool hasLayouts, hasElements, hasConnections, hasZones, hasElementZones;
                using (var connection = dbManager.GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT name FROM sqlite_master
                        WHERE type='table'
                        ORDER BY name";

                    var tables = new System.Collections.Generic.List<string>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }

                    // Expected tables (excluding internal sqlite tables)
                    hasLayouts = tables.Contains("Layouts");
                    hasElements = tables.Contains("Elements");
                    hasConnections = tables.Contains("Connections");
                    hasZones = tables.Contains("Zones");
                    hasElementZones = tables.Contains("ElementZones");
                }

                var result = hasLayouts && hasElements && hasConnections && hasZones && hasElementZones;

                Console.WriteLine($"T6A.2 - Schema tables created correctly: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Layouts: {hasLayouts}, Elements: {hasElements}, Connections: {hasConnections}, " +
                                 $"Zones: {hasZones}, ElementZones: {hasElementZones})");

                // Cleanup
                dbManager.DeleteDatabase();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6A.2 - Schema tables created correctly: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static bool Test_T6A_3_SchemaIndexesCreated()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();

                bool hasElementsLayout, hasElementsLayer, hasElementsType, hasConnectionsLayout, hasZonesLayout;
                int indexCount;
                using (var connection = dbManager.GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT name FROM sqlite_master
                        WHERE type='index' AND name LIKE 'idx_%'
                        ORDER BY name";

                    var indexes = new System.Collections.Generic.List<string>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            indexes.Add(reader.GetString(0));
                        }
                    }

                    indexCount = indexes.Count;

                    // Check for key indexes
                    hasElementsLayout = indexes.Contains("idx_elements_layout");
                    hasElementsLayer = indexes.Contains("idx_elements_layer");
                    hasElementsType = indexes.Contains("idx_elements_type");
                    hasConnectionsLayout = indexes.Contains("idx_connections_layout");
                    hasZonesLayout = indexes.Contains("idx_zones_layout");
                }

                var result = hasElementsLayout && hasElementsLayer && hasElementsType &&
                            hasConnectionsLayout && hasZonesLayout;

                Console.WriteLine($"T6A.3 - Schema indexes created correctly: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Found {indexCount} indexes, Key indexes present: {result})");

                // Cleanup
                dbManager.DeleteDatabase();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6A.3 - Schema indexes created correctly: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static bool Test_T6A_4_MigrationTracksVersion()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                var migration = new SchemaMigration(dbManager);

                // Initially version should be 0
                var versionBefore = migration.GetCurrentVersion();

                // Ensure database is created
                dbManager.EnsureCreated();

                // Migrate to latest
                migration.MigrateToLatest();

                // Version should now be 1
                var versionAfter = migration.GetCurrentVersion();

                // Verify schema_version table exists
                bool versionRecordExists;
                using (var connection = dbManager.GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(*) FROM schema_version WHERE Version = 1";
                    versionRecordExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                }

                var result = versionBefore == 0 && versionAfter == 1 && versionRecordExists;

                Console.WriteLine($"T6A.4 - Migration system tracks version: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(Before: {versionBefore}, After: {versionAfter}, Record exists: {versionRecordExists})");

                // Cleanup
                dbManager.DeleteDatabase();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6A.4 - Migration system tracks version: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static bool Test_T6A_5_ConnectionPoolingWorks()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid()}.db");

            try
            {
                var dbManager = new DatabaseManager(_testDbPath);
                dbManager.EnsureCreated();

                // Create multiple connections
                var conn1 = dbManager.GetConnection();
                var conn2 = dbManager.GetConnection();
                var conn3 = dbManager.GetConnection();

                // All connections should be open
                var allOpen = conn1.State == System.Data.ConnectionState.Open &&
                             conn2.State == System.Data.ConnectionState.Open &&
                             conn3.State == System.Data.ConnectionState.Open;

                // All connections should be able to query
                var canQuery1 = CanExecuteQuery(conn1);
                var canQuery2 = CanExecuteQuery(conn2);
                var canQuery3 = CanExecuteQuery(conn3);

                // Close connections
                conn1.Dispose();
                conn2.Dispose();
                conn3.Dispose();

                var result = allOpen && canQuery1 && canQuery2 && canQuery3;

                Console.WriteLine($"T6A.5 - Connection pooling works: {(result ? "✓ PASS" : "✗ FAIL")} " +
                                 $"(All open: {allOpen}, All query: {canQuery1 && canQuery2 && canQuery3})");

                // Cleanup
                dbManager.DeleteDatabase();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T6A.5 - Connection pooling works: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static bool CanExecuteQuery(SqliteConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                var result = command.ExecuteScalar();
                return result != null && Convert.ToInt32(result) == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
