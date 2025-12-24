# Prompt 2: Testing Infrastructure + Part Families Schema

Continuing Factory Configurator (WPF/C#/.NET 8). Existing scenarios are working.

Objective: Add testing infrastructure and Part Families/Variants schema. All tests must pass with dotnet test.

## 1. Add test project to solution

- Project: FactorySimulation.Tests (.NET 8, xUnit)
- Project references: Core, Data, Services
- NuGet packages:
  - xunit
  - xunit.runner.visualstudio
  - FluentAssertions
  - Microsoft.Data.Sqlite

## 2. Testing approach (SQLite in-memory)

- Use SQLite in-memory with single open connection per test run.
- Isolation via transactions per test (begin in setup, rollback in teardown).

## 3. Test utilities in Tests/Utilities/

### TestDbFactory.cs
- CreateOpenInMemoryConnection(): returns open SqliteConnection
- CreateSchemaAsync(IDbConnection): calls domain-specific schema methods
- SeedBasicDataAsync(IDbConnection): calls domain-specific seed methods

### TestFixture.cs
- Implements IAsyncLifetime
- Opens and holds connection
- Calls CreateSchemaAsync and SeedBasicDataAsync
- Exposes Connection property

## 4. Database schema for Part Families

Create in CreatePartsSchemaAsync:

```sql
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
```

## 5. Seed categories in SeedPartsDataAsync

RawMaterial, Component, Subassembly, FinishedGood, Packaging, Consumable

## 6. Core models (Core project)

- PartCategory.cs: Id, Name, Color, SortOrder

- PartFamily.cs: Id, CategoryId, FamilyCode, Name, Description, IsActive, CreatedAt, ModifiedAt
  - CategoryName (string?) - for display
  - Variants (List<PartVariant>) - navigation

- PartVariant.cs: Id, FamilyId, PartNumber, Name, Description, IsActive, CreatedAt, ModifiedAt
  - FamilyCode (string?) - for display
  - FamilyName (string?) - for display
  - CategoryId (int) - inherited from family
  - CategoryName (string?) - inherited from family

## 7. Repository (Data project)

### IPartFamilyRepository:
- Task<IReadOnlyList<PartFamily>> GetAllAsync()
- Task<PartFamily?> GetByIdAsync(int id)
- Task<PartFamily?> GetWithVariantsAsync(int id)
- Task<int> CreateAsync(PartFamily family)
- Task UpdateAsync(PartFamily family)
- Task DeleteAsync(int id)

### IPartVariantRepository:
- Task<IReadOnlyList<PartVariant>> GetAllAsync()
- Task<PartVariant?> GetByIdAsync(int id)
- Task<IReadOnlyList<PartVariant>> GetByFamilyAsync(int familyId)
- Task<int> CreateAsync(PartVariant variant)
- Task UpdateAsync(PartVariant variant)
- Task DeleteAsync(int id)

### Behavior rules:
- Duplicate FamilyCode or PartNumber throws InvalidOperationException with "duplicate"
- GetWithVariantsAsync populates Variants list
- Variant inherits CategoryId from Family (computed, not stored)

## 8. Tests in Tests/Repositories/

### PartFamilyRepositoryTests.cs:
- CreateFamily_ValidData_ReturnsId
- CreateFamily_DuplicateFamilyCode_ThrowsException
- GetWithVariants_ReturnsPopulatedList
- DeleteFamily_CascadesVariants

### PartVariantRepositoryTests.cs:
- CreateVariant_ValidData_ReturnsId
- CreateVariant_DuplicatePartNumber_ThrowsException
- GetByFamily_ReturnsOnlyFamilyVariants
- Variant_InheritsCategoryFromFamily

## 9. Schema extension pattern

Structure CreateSchemaAsync for incremental growth:

```csharp
public static async Task CreateSchemaAsync(IDbConnection db)
{
    await CreateCoreSchemaAsync(db);      // Scenarios
    await CreatePartsSchemaAsync(db);     // Categories, Families, Variants
    // Future: CreateWorkstationsSchemaAsync, etc.
}
```

Test class pattern for all future tests:

```csharp
public class XxxTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    public XxxTests(TestFixture fixture) => _fixture = fixture;
}
```

## Run Tests

Run dotnet test. All 8 tests should pass.
