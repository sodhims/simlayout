using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;

namespace FactorySimulation.Tests.Utilities;

/// <summary>
/// Factory for creating in-memory SQLite connections for testing
/// </summary>
public static class TestDbFactory
{
    /// <summary>
    /// Creates and opens an in-memory SQLite connection
    /// </summary>
    public static SqliteConnection CreateOpenInMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates the complete database schema
    /// </summary>
    public static async Task CreateSchemaAsync(IDbConnection connection)
    {
        await CreateCoreSchemaAsync(connection);
        await CreatePartsSchemaAsync(connection);
        // Future: CreateWorkstationsSchemaAsync, etc.
    }

    /// <summary>
    /// Creates core/common tables (Scenarios)
    /// </summary>
    public static async Task CreateCoreSchemaAsync(IDbConnection connection)
    {
        const string sql = """
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

            -- Insert base scenario
            INSERT INTO Scenarios (Id, Name, Description, IsBase, CreatedAt, ModifiedAt)
            VALUES (1, 'Base', 'Base configuration scenario', 1, datetime('now'), datetime('now'));
            """;

        await connection.ExecuteAsync(sql);
    }

    /// <summary>
    /// Creates parts schema (Categories, Families, Variants)
    /// </summary>
    public static async Task CreatePartsSchemaAsync(IDbConnection connection)
    {
        const string sql = """
            -- part_Categories
            CREATE TABLE part_Categories (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE,
                Color TEXT NULL,
                SortOrder INTEGER NULL
            );

            -- part_Families
            CREATE TABLE part_Families (
                Id INTEGER PRIMARY KEY,
                CategoryId INTEGER NOT NULL REFERENCES part_Categories(Id) ON DELETE RESTRICT,
                FamilyCode TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NULL,
                ModifiedAt TEXT NULL
            );

            -- part_Variants
            CREATE TABLE part_Variants (
                Id INTEGER PRIMARY KEY,
                FamilyId INTEGER NOT NULL REFERENCES part_Families(Id) ON DELETE CASCADE,
                PartNumber TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NULL,
                ModifiedAt TEXT NULL
            );

            -- part_VariantProperties
            CREATE TABLE part_VariantProperties (
                Id INTEGER PRIMARY KEY,
                VariantId INTEGER NOT NULL UNIQUE REFERENCES part_Variants(Id) ON DELETE CASCADE,
                LengthMm REAL NULL,
                WidthMm REAL NULL,
                HeightMm REAL NULL,
                WeightKg REAL NULL,
                ContainerType TEXT NULL,
                UnitsPerContainer INTEGER NULL,
                RequiresForklift INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NULL
            );

            -- part_FamilyDefaults
            CREATE TABLE part_FamilyDefaults (
                Id INTEGER PRIMARY KEY,
                FamilyId INTEGER NOT NULL UNIQUE REFERENCES part_Families(Id) ON DELETE CASCADE,
                LengthMm REAL NULL,
                WidthMm REAL NULL,
                HeightMm REAL NULL,
                WeightKg REAL NULL,
                ContainerType TEXT NULL,
                UnitsPerContainer INTEGER NULL,
                RequiresForklift INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NULL
            );

            -- variant_BillOfMaterials
            CREATE TABLE variant_BillOfMaterials (
                Id INTEGER PRIMARY KEY,
                VariantId INTEGER NOT NULL UNIQUE REFERENCES part_Variants(Id) ON DELETE CASCADE,
                Version INTEGER NOT NULL DEFAULT 1,
                IsActive INTEGER NOT NULL DEFAULT 1,
                EffectiveDate TEXT NOT NULL DEFAULT (datetime('now')),
                ExpirationDate TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                ModifiedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );

            -- variant_BOMItems
            CREATE TABLE variant_BOMItems (
                Id INTEGER PRIMARY KEY,
                BomId INTEGER NOT NULL REFERENCES variant_BillOfMaterials(Id) ON DELETE CASCADE,
                ComponentVariantId INTEGER NOT NULL REFERENCES part_Variants(Id) ON DELETE RESTRICT,
                Quantity REAL NOT NULL DEFAULT 1,
                UnitOfMeasure TEXT NOT NULL DEFAULT 'EA',
                Sequence INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """;

        await connection.ExecuteAsync(sql);
    }

    /// <summary>
    /// Seeds basic reference data for testing
    /// </summary>
    public static async Task SeedBasicDataAsync(IDbConnection connection)
    {
        await SeedPartsDataAsync(connection);
    }

    /// <summary>
    /// Seeds the part categories
    /// </summary>
    public static async Task SeedPartsDataAsync(IDbConnection connection)
    {
        const string sql = """
            INSERT INTO part_Categories (Name, SortOrder)
            VALUES
                ('RawMaterial', 1),
                ('Component', 2),
                ('Subassembly', 3),
                ('FinishedGood', 4),
                ('Packaging', 5),
                ('Consumable', 6);
            """;

        await connection.ExecuteAsync(sql);
    }
}
