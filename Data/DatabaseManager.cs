using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace LayoutEditor.Data
{
    /// <summary>
    /// Manages database connections and initialization for SQLite storage
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        /// <summary>
        /// Creates a new DatabaseManager with default database location
        /// </summary>
        public DatabaseManager() : this(GetDefaultDatabasePath())
        {
        }

        /// <summary>
        /// Creates a new DatabaseManager with specified database path
        /// </summary>
        /// <param name="databasePath">Full path to the SQLite database file</param>
        public DatabaseManager(string databasePath)
        {
            _databasePath = databasePath;
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        /// <summary>
        /// Gets the path to the database file
        /// </summary>
        public string GetDatabasePath() => _databasePath;

        /// <summary>
        /// Creates a new connection to the database
        /// </summary>
        /// <returns>An open SqliteConnection</returns>
        public SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Begins a transaction for atomic operations
        /// </summary>
        /// <param name="connection">The connection to begin the transaction on</param>
        /// <returns>A SqliteTransaction</returns>
        public SqliteTransaction BeginTransaction(SqliteConnection connection)
        {
            return connection.BeginTransaction();
        }

        /// <summary>
        /// Ensures the database file and schema exist, creating them if necessary
        /// </summary>
        public void EnsureCreated()
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create database file and schema if it doesn't exist
            using var connection = GetConnection();
            CreateSchema(connection);
        }

        /// <summary>
        /// Creates the database schema (tables, indexes)
        /// </summary>
        private void CreateSchema(SqliteConnection connection)
        {
            var schema = @"
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
    ElementType TEXT NOT NULL,  -- 'EOTCrane', 'Conveyor', 'AGVPath', 'ForkliftAisle', etc.
    Layer INTEGER NOT NULL,     -- LayerType enum value
    Name TEXT,
    PropertiesJson TEXT NOT NULL,  -- Full JSON serialization of the element
    CreatedDate TEXT NOT NULL,
    ModifiedDate TEXT NOT NULL,
    FOREIGN KEY (LayoutId) REFERENCES Layouts(Id) ON DELETE CASCADE
);

-- Connections table (relationships between elements)
CREATE TABLE IF NOT EXISTS Connections (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL,
    SourceElementId TEXT NOT NULL,
    TargetElementId TEXT NOT NULL,
    ConnectionType TEXT NOT NULL,  -- 'Handoff', 'DropZone', 'Crossing', etc.
    PropertiesJson TEXT,  -- Additional connection metadata
    CreatedDate TEXT NOT NULL,
    FOREIGN KEY (LayoutId) REFERENCES Layouts(Id) ON DELETE CASCADE,
    FOREIGN KEY (SourceElementId) REFERENCES Elements(Id) ON DELETE CASCADE,
    FOREIGN KEY (TargetElementId) REFERENCES Elements(Id) ON DELETE CASCADE
);

-- Zones table (named regions for grouping elements)
CREATE TABLE IF NOT EXISTS Zones (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL,
    Name TEXT NOT NULL,
    ZoneType TEXT NOT NULL,  -- 'Functional', 'Safety', 'Custom', etc.
    BoundaryJson TEXT,  -- List of points defining zone boundary
    PropertiesJson TEXT,  -- Additional zone metadata
    CreatedDate TEXT NOT NULL,
    ModifiedDate TEXT NOT NULL,
    FOREIGN KEY (LayoutId) REFERENCES Layouts(Id) ON DELETE CASCADE
);

-- ElementZones junction table (many-to-many)
CREATE TABLE IF NOT EXISTS ElementZones (
    ElementId TEXT NOT NULL,
    ZoneId TEXT NOT NULL,
    PRIMARY KEY (ElementId, ZoneId),
    FOREIGN KEY (ElementId) REFERENCES Elements(Id) ON DELETE CASCADE,
    FOREIGN KEY (ZoneId) REFERENCES Zones(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_elements_layout ON Elements(LayoutId);
CREATE INDEX IF NOT EXISTS idx_elements_layer ON Elements(Layer);
CREATE INDEX IF NOT EXISTS idx_elements_type ON Elements(ElementType);
CREATE INDEX IF NOT EXISTS idx_connections_layout ON Connections(LayoutId);
CREATE INDEX IF NOT EXISTS idx_connections_source ON Connections(SourceElementId);
CREATE INDEX IF NOT EXISTS idx_connections_target ON Connections(TargetElementId);
CREATE INDEX IF NOT EXISTS idx_zones_layout ON Zones(LayoutId);
";

            using var command = connection.CreateCommand();
            command.CommandText = schema;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the default database path in the user's AppData folder
        /// </summary>
        private static string GetDefaultDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "LayoutEditor");
            return Path.Combine(appFolder, "layouts.db");
        }

        /// <summary>
        /// Deletes the database file (for testing purposes)
        /// </summary>
        public void DeleteDatabase()
        {
            if (File.Exists(_databasePath))
            {
                // Close any open connections first
                SqliteConnection.ClearAllPools();

                // Give a moment for file handles to release
                System.Threading.Thread.Sleep(100);

                // Try to delete with retries
                int retries = 3;
                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        File.Delete(_databasePath);
                        break;
                    }
                    catch (IOException) when (i < retries - 1)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the database file exists
        /// </summary>
        public bool DatabaseExists() => File.Exists(_databasePath);
    }
}
