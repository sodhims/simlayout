using Microsoft.Data.Sqlite;

namespace FactorySimulation.Data;

/// <summary>
/// Manages database connection configuration and initialization
/// </summary>
public static class DatabaseConfiguration
{
    private static string? _connectionString;
    private static bool _isInitialized;

    /// <summary>
    /// Gets the connection string for the factory database
    /// </summary>
    public static string ConnectionString
    {
        get
        {
            if (_connectionString == null)
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "factory.db");
                _connectionString = $"Data Source={dbPath}";
            }
            return _connectionString;
        }
    }

    /// <summary>
    /// Gets the path to the database file
    /// </summary>
    public static string DatabasePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "factory.db");

    /// <summary>
    /// Creates a new database connection
    /// </summary>
    public static SqliteConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    /// <summary>
    /// Initializes the database, creating tables if they don't exist
    /// </summary>
    public static async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var initSql = GetInitializationScript();
        await using var command = new SqliteCommand(initSql, connection);
        await command.ExecuteNonQueryAsync();

        _isInitialized = true;
    }

    /// <summary>
    /// Initializes the database synchronously
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        using var connection = CreateConnection();
        connection.Open();

        var initSql = GetInitializationScript();
        using var command = new SqliteCommand(initSql, connection);
        command.ExecuteNonQuery();

        _isInitialized = true;
    }

    private static string GetInitializationScript()
    {
        return """
            -- Scenarios table
            CREATE TABLE IF NOT EXISTS Scenarios (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL,
                Description TEXT,
                ParentScenarioId INTEGER REFERENCES Scenarios(Id),
                IsBase INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );

            -- Insert base scenario if not exists
            INSERT OR IGNORE INTO Scenarios (Id, Name, Description, IsBase, CreatedAt, ModifiedAt)
            VALUES (1, 'Base', 'Base configuration scenario', 1, datetime('now'), datetime('now'));

            -- Layouts table
            CREATE TABLE IF NOT EXISTS Layouts (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                CanvasWidth REAL DEFAULT 1200,
                CanvasHeight REAL DEFAULT 800,
                GridSize INTEGER DEFAULT 20,
                SnapToGrid INTEGER DEFAULT 1,
                ShowGrid INTEGER DEFAULT 1,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now'))
            );

            -- Elements table
            CREATE TABLE IF NOT EXISTS Elements (
                Id TEXT PRIMARY KEY,
                LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
                ElementType TEXT NOT NULL,
                Name TEXT NOT NULL,
                X REAL DEFAULT 0,
                Y REAL DEFAULT 0,
                Width REAL DEFAULT 80,
                Height REAL DEFAULT 60,
                Rotation REAL DEFAULT 0,
                Properties TEXT,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_elements_layout ON Elements(LayoutId);

            -- Connections table
            CREATE TABLE IF NOT EXISTS Connections (
                Id TEXT PRIMARY KEY,
                LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
                FromElementId TEXT NOT NULL REFERENCES Elements(Id) ON DELETE CASCADE,
                ToElementId TEXT NOT NULL REFERENCES Elements(Id) ON DELETE CASCADE,
                ConnectionType TEXT DEFAULT 'flow',
                Properties TEXT,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_connections_layout ON Connections(LayoutId);

            -- Zones table
            CREATE TABLE IF NOT EXISTS Zones (
                Id TEXT PRIMARY KEY,
                LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
                Name TEXT NOT NULL,
                ZoneType TEXT DEFAULT 'storage',
                X REAL DEFAULT 0,
                Y REAL DEFAULT 0,
                Width REAL DEFAULT 100,
                Height REAL DEFAULT 100,
                Capacity INTEGER DEFAULT 100,
                FillColor TEXT DEFAULT '#3498DB',
                BorderColor TEXT DEFAULT '#2980B9',
                Opacity REAL DEFAULT 0.3,
                Properties TEXT,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_zones_layout ON Zones(LayoutId);

            -- ElementZones table
            CREATE TABLE IF NOT EXISTS ElementZones (
                ElementId TEXT NOT NULL REFERENCES Elements(Id) ON DELETE CASCADE,
                ZoneId TEXT NOT NULL REFERENCES Zones(Id) ON DELETE CASCADE,
                PRIMARY KEY (ElementId, ZoneId)
            );

            -- PartTypes table
            CREATE TABLE IF NOT EXISTS PartTypes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PartNumber TEXT UNIQUE NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                Category INTEGER DEFAULT 1,
                UnitOfMeasure TEXT DEFAULT 'EA',
                UnitCost REAL DEFAULT 0,
                ScenarioId INTEGER REFERENCES Scenarios(Id),
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_parttypes_partnumber ON PartTypes(PartNumber);
            CREATE INDEX IF NOT EXISTS idx_parttypes_category ON PartTypes(Category);

            -- BillOfMaterials table
            CREATE TABLE IF NOT EXISTS BillOfMaterials (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PartTypeId INTEGER NOT NULL REFERENCES PartTypes(Id) ON DELETE CASCADE,
                Version INTEGER DEFAULT 1,
                IsActive INTEGER DEFAULT 1,
                EffectiveDate TEXT DEFAULT (datetime('now')),
                ExpirationDate TEXT,
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_bom_parttype ON BillOfMaterials(PartTypeId);

            -- BOMItems table
            CREATE TABLE IF NOT EXISTS BOMItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BomId INTEGER NOT NULL REFERENCES BillOfMaterials(Id) ON DELETE CASCADE,
                ComponentPartTypeId INTEGER NOT NULL REFERENCES PartTypes(Id),
                Quantity REAL DEFAULT 1,
                UnitOfMeasure TEXT DEFAULT 'EA',
                Sequence INTEGER DEFAULT 0,
                Notes TEXT,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_bomitems_bom ON BOMItems(BomId);
            CREATE INDEX IF NOT EXISTS idx_bomitems_component ON BOMItems(ComponentPartTypeId);

            -- Part Categories table
            CREATE TABLE IF NOT EXISTS part_Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL,
                Description TEXT,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );

            -- Insert default categories if not exists
            INSERT OR IGNORE INTO part_Categories (Id, Name, Description) VALUES (1, 'Assemblies', 'Assembled products');
            INSERT OR IGNORE INTO part_Categories (Id, Name, Description) VALUES (2, 'Components', 'Individual components');
            INSERT OR IGNORE INTO part_Categories (Id, Name, Description) VALUES (3, 'Raw Materials', 'Raw materials and supplies');

            -- Part Families table
            CREATE TABLE IF NOT EXISTS part_Families (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId INTEGER NOT NULL REFERENCES part_Categories(Id),
                FamilyCode TEXT UNIQUE NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                IsActive INTEGER DEFAULT 1,
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_families_category ON part_Families(CategoryId);

            -- Part Variants table
            CREATE TABLE IF NOT EXISTS part_Variants (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FamilyId INTEGER NOT NULL REFERENCES part_Families(Id) ON DELETE CASCADE,
                PartNumber TEXT UNIQUE NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                IsActive INTEGER DEFAULT 1,
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_variants_family ON part_Variants(FamilyId);
            CREATE INDEX IF NOT EXISTS idx_variants_partnumber ON part_Variants(PartNumber);

            -- Variant Properties table
            CREATE TABLE IF NOT EXISTS part_VariantProperties (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VariantId INTEGER NOT NULL UNIQUE REFERENCES part_Variants(Id) ON DELETE CASCADE,
                LengthMm REAL NULL,
                WidthMm REAL NULL,
                HeightMm REAL NULL,
                WeightKg REAL NULL,
                ContainerType TEXT NULL,
                UnitsPerContainer INTEGER NULL,
                RequiresForklift INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NULL,
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );

            -- Family Defaults table
            CREATE TABLE IF NOT EXISTS part_FamilyDefaults (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FamilyId INTEGER NOT NULL UNIQUE REFERENCES part_Families(Id) ON DELETE CASCADE,
                LengthMm REAL NULL,
                WidthMm REAL NULL,
                HeightMm REAL NULL,
                WeightKg REAL NULL,
                ContainerType TEXT NULL,
                UnitsPerContainer INTEGER NULL,
                RequiresForklift INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NULL,
                CreatedAt TEXT DEFAULT (datetime('now')),
                ModifiedAt TEXT DEFAULT (datetime('now'))
            );

            -- Variant Bill of Materials table
            CREATE TABLE IF NOT EXISTS variant_BillOfMaterials (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VariantId INTEGER NOT NULL UNIQUE REFERENCES part_Variants(Id) ON DELETE CASCADE,
                Version INTEGER NOT NULL DEFAULT 1,
                IsActive INTEGER NOT NULL DEFAULT 1,
                EffectiveDate TEXT NOT NULL DEFAULT (datetime('now')),
                ExpirationDate TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                ModifiedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );

            -- Variant BOM Items table
            CREATE TABLE IF NOT EXISTS variant_BOMItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BomId INTEGER NOT NULL REFERENCES variant_BillOfMaterials(Id) ON DELETE CASCADE,
                ComponentVariantId INTEGER NOT NULL REFERENCES part_Variants(Id) ON DELETE RESTRICT,
                Quantity REAL NOT NULL DEFAULT 1,
                UnitOfMeasure TEXT NOT NULL DEFAULT 'EA',
                Sequence INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS idx_vbomitems_bom ON variant_BOMItems(BomId);
            CREATE INDEX IF NOT EXISTS idx_vbomitems_component ON variant_BOMItems(ComponentVariantId);
            """;
    }
}
