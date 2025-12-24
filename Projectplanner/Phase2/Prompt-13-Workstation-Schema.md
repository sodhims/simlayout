# Prompt 13: Workstation Schema & Repository

Continuing Factory Configurator. Prompt 12 complete, 58 tests passing.

Objective: Add workstation configuration. All tests must pass.

## 1. Database schema

Add CreateWorkstationsSchemaAsync:

```sql
CREATE TABLE cfg_Workstations (
    Id INTEGER PRIMARY KEY,
    ElementId INTEGER NOT NULL UNIQUE,
    Name TEXT NULL,
    WorkstationType TEXT NOT NULL,
    Capacity INTEGER NOT NULL DEFAULT 1,
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (ElementId) REFERENCES Elements(Id) ON DELETE CASCADE
);
```

Add minimal Elements table to test schema (from Stage 1):
```sql
CREATE TABLE Elements (
    Id INTEGER PRIMARY KEY,
    Name TEXT,
    ElementType TEXT,
    LayoutId INTEGER
);
```

## 2. Seed test Elements in SeedWorkstationsDataAsync

Create 3-5 test Elements with ElementType values like "Machine", "Station", "Inspection"

## 3. Enum

```csharp
public enum WorkstationType
{
    Assembly,
    Machining,
    Inspection,
    Packaging,
    Storage,
    LoadUnload,
    Manual
}
```

## 4. Model

### Workstation.cs:
- Id, ElementId, Name, WorkstationType, Capacity, IsActive
- ElementName (string?) - for display

## 5. Repository

### IWorkstationRepository:
- Task<IReadOnlyList<Workstation>> GetAllAsync()
- Task<Workstation?> GetByIdAsync(int id)
- Task<Workstation?> GetByElementIdAsync(int elementId)
- Task<IReadOnlyList<Workstation>> GetByTypeAsync(WorkstationType type)
- Task<int> CreateAsync(Workstation workstation)
- Task UpdateAsync(Workstation workstation)
- Task DeleteAsync(int id)

### Implementation notes:
- GetAllAsync joins with Elements to populate ElementName
- CreateAsync with duplicate ElementId throws InvalidOperationException

## 6. Tests in Tests/Repositories/WorkstationRepositoryTests.cs

- CreateWorkstation_ValidData_ReturnsId
- CreateWorkstation_DuplicateElementId_ThrowsException
- GetAll_IncludesElementName
- GetByType_FiltersCorrectly
- DeleteElement_CascadesWorkstation

## Run Tests

Run dotnet test. All 63 tests should pass.
