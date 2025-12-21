using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace LayoutEditor.Data
{
    /// <summary>
    /// Manages database schema migrations and versioning
    /// </summary>
    public class SchemaMigration
    {
        private readonly DatabaseManager _databaseManager;
        private const int CurrentSchemaVersion = 1;

        public SchemaMigration(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
        }

        /// <summary>
        /// Gets the current schema version from the database
        /// </summary>
        public int GetCurrentVersion()
        {
            if (!_databaseManager.DatabaseExists())
            {
                return 0;
            }

            try
            {
                using var connection = _databaseManager.GetConnection();
                using var command = connection.CreateCommand();

                // Check if schema_version table exists
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE type='table' AND name='schema_version'";

                var tableExists = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (!tableExists)
                {
                    return 0;
                }

                // Get current version
                command.CommandText = "SELECT MAX(Version) FROM schema_version";
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Migrates the database schema to the latest version
        /// </summary>
        public void MigrateToLatest()
        {
            var currentVersion = GetCurrentVersion();

            if (currentVersion == CurrentSchemaVersion)
            {
                return; // Already at latest version
            }

            using var connection = _databaseManager.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Create schema_version table if it doesn't exist
                if (currentVersion == 0)
                {
                    CreateSchemaVersionTable(connection);
                }

                // Apply migrations in order
                var migrations = GetMigrations();
                foreach (var migration in migrations)
                {
                    if (migration.Version > currentVersion)
                    {
                        ApplyMigration(connection, migration);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Creates the schema_version tracking table
        /// </summary>
        private void CreateSchemaVersionTable(SqliteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS schema_version (
                    Version INTEGER PRIMARY KEY,
                    AppliedDate TEXT NOT NULL,
                    Description TEXT
                )";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Applies a single migration
        /// </summary>
        private void ApplyMigration(SqliteConnection connection, Migration migration)
        {
            // Execute migration SQL
            using (var command = connection.CreateCommand())
            {
                command.CommandText = migration.Sql;
                command.ExecuteNonQuery();
            }

            // Record migration in schema_version table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO schema_version (Version, AppliedDate, Description)
                    VALUES (@version, @date, @description)";

                command.Parameters.AddWithValue("@version", migration.Version);
                command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@description", migration.Description);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets the list of all migrations in order
        /// </summary>
        private List<Migration> GetMigrations()
        {
            return new List<Migration>
            {
                new Migration
                {
                    Version = 1,
                    Description = "Initial schema - Layouts, Elements, Connections, Zones",
                    Sql = @"
-- Layouts table
CREATE TABLE IF NOT EXISTS Layouts (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Width REAL NOT NULL,
    Height REAL NOT NULL,
    Unit TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    ModifiedDate TEXT NOT NULL,
    Version INTEGER NOT NULL DEFAULT 1
);

-- Elements table (unified storage for all layer types)
CREATE TABLE IF NOT EXISTS Elements (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL,
    ElementType TEXT NOT NULL,
    Layer INTEGER NOT NULL,
    Name TEXT,
    PropertiesJson TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    ModifiedDate TEXT NOT NULL,
    FOREIGN KEY (LayoutId) REFERENCES Layouts(Id) ON DELETE CASCADE
);

-- Connections table
CREATE TABLE IF NOT EXISTS Connections (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL,
    SourceElementId TEXT NOT NULL,
    TargetElementId TEXT NOT NULL,
    ConnectionType TEXT NOT NULL,
    PropertiesJson TEXT,
    CreatedDate TEXT NOT NULL,
    FOREIGN KEY (LayoutId) REFERENCES Layouts(Id) ON DELETE CASCADE,
    FOREIGN KEY (SourceElementId) REFERENCES Elements(Id) ON DELETE CASCADE,
    FOREIGN KEY (TargetElementId) REFERENCES Elements(Id) ON DELETE CASCADE
);

-- Zones table
CREATE TABLE IF NOT EXISTS Zones (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL,
    Name TEXT NOT NULL,
    ZoneType TEXT NOT NULL,
    BoundaryJson TEXT,
    PropertiesJson TEXT,
    CreatedDate TEXT NOT NULL,
    ModifiedDate TEXT NOT NULL,
    FOREIGN KEY (LayoutId) REFERENCES Layouts(Id) ON DELETE CASCADE
);

-- ElementZones junction table
CREATE TABLE IF NOT EXISTS ElementZones (
    ElementId TEXT NOT NULL,
    ZoneId TEXT NOT NULL,
    PRIMARY KEY (ElementId, ZoneId),
    FOREIGN KEY (ElementId) REFERENCES Elements(Id) ON DELETE CASCADE,
    FOREIGN KEY (ZoneId) REFERENCES Zones(Id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_elements_layout ON Elements(LayoutId);
CREATE INDEX IF NOT EXISTS idx_elements_layer ON Elements(Layer);
CREATE INDEX IF NOT EXISTS idx_elements_type ON Elements(ElementType);
CREATE INDEX IF NOT EXISTS idx_connections_layout ON Connections(LayoutId);
CREATE INDEX IF NOT EXISTS idx_connections_source ON Connections(SourceElementId);
CREATE INDEX IF NOT EXISTS idx_connections_target ON Connections(TargetElementId);
CREATE INDEX IF NOT EXISTS idx_zones_layout ON Zones(LayoutId);
"
                }
                // Future migrations will be added here as new versions
            };
        }

        /// <summary>
        /// Represents a single database migration
        /// </summary>
        private class Migration
        {
            public int Version { get; set; }
            public string Description { get; set; } = "";
            public string Sql { get; set; } = "";
        }
    }
}
