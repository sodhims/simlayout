# Prompt 23: Transport Equipment

Continuing Factory Configurator. Prompt 22 complete, 110 tests passing.

Objective: Implement transport equipment. All tests must pass.

## 1. Database schema

Add CreateTransportSchemaAsync:

```sql
CREATE TABLE res_TransportTypes (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    Category TEXT NOT NULL
);

CREATE TABLE res_TransportEquipment (
    Id INTEGER PRIMARY KEY,
    TransportTypeId INTEGER NOT NULL REFERENCES res_TransportTypes(Id) ON DELETE RESTRICT,
    AssetId TEXT NULL UNIQUE,
    Name TEXT NOT NULL,
    HomeElementId INTEGER NULL REFERENCES Elements(Id) ON DELETE SET NULL,
    Status TEXT NOT NULL DEFAULT 'Available',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NULL
);

CREATE TABLE res_TransportParams (
    Id INTEGER PRIMARY KEY,
    TransportEquipmentId INTEGER NOT NULL UNIQUE REFERENCES res_TransportEquipment(Id) ON DELETE CASCADE,
    SpeedMps REAL NULL,
    LoadedSpeedMps REAL NULL,
    CapacityKg REAL NULL,
    LoadTimeSec REAL NULL,
    UnloadTimeSec REAL NULL
);
```

## 2. Seed data (SeedTransportDataAsync)

TransportTypes with categories:
- Forklift (Forklift)
- AGV (AGV)
- OverheadCrane (Crane)
- PalletJack (Manual)

## 3. Enums

```csharp
public enum TransportCategory
{
    Forklift,
    AGV,
    Crane,
    Manual
}

public enum TransportStatus
{
    Available,
    InUse,
    Charging,
    Maintenance,
    OutOfService
}
```

## 4. Models

### TransportType.cs:
- Id, Name, Category

### TransportEquipment.cs:
- Id, TransportTypeId, AssetId, Name, HomeElementId, Status, IsActive, Notes
- TransportTypeName (string?) - for display
- HomeElementName (string?) - for display

### TransportParams.cs:
- Id, TransportEquipmentId
- SpeedMps, LoadedSpeedMps, CapacityKg, LoadTimeSec, UnloadTimeSec

## 5. Repository

### ITransportRepository:
- Task<IReadOnlyList<TransportEquipment>> GetAllAsync()
- Task<TransportEquipment?> GetByIdAsync(int id)
- Task<IReadOnlyList<TransportEquipment>> GetByCategoryAsync(TransportCategory category)
- Task<IReadOnlyList<TransportEquipment>> GetAvailableAsync()
- Task<int> CreateAsync(TransportEquipment equipment)
- Task UpdateAsync(TransportEquipment equipment)
- Task DeleteAsync(int id)
- Task<TransportParams?> GetParamsAsync(int equipmentId)
- Task SaveParamsAsync(TransportParams params)

## 6. Tests in Tests/Repositories/TransportRepositoryTests.cs

- CreateEquipment_ValidData_ReturnsId
- CreateEquipment_DuplicateAssetId_ThrowsException
- GetByCategory_FiltersCorrectly
- GetAvailable_ExcludesInactiveAndNonAvailable
- SaveParams_Works
- DeleteEquipment_CascadesParams

## 7. View

### TransportView.xaml (new main tab):
- Left: ListView grouped by TransportType
- Right: Detail panel
  - General tab: Name, AssetId, Type, Status dropdown, Home location picker, Active
  - Parameters tab: Speed, Loaded Speed, Capacity, Load Time, Unload Time
- Add/Delete buttons

## Run Tests

Run dotnet test. All 116 tests should pass.
