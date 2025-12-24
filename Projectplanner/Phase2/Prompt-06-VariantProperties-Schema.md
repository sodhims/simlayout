# Prompt 6: Variant Properties Schema & Repository

Continuing Factory Configurator. Prompt 5a complete, 29 tests passing.

Objective: Add physical properties to variants. All tests must pass.

## 1. Database schema

Add to CreatePartsSchemaAsync:

```sql
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
```

## 2. Models

- VariantProperties.cs: Id, VariantId, LengthMm, WidthMm, HeightMm, WeightKg,
  ContainerType, UnitsPerContainer, RequiresForklift, Notes

- FamilyDefaults.cs: Same fields but FamilyId instead of VariantId

## 3. Repository

### IVariantPropertiesRepository:
- Task<VariantProperties?> GetByVariantIdAsync(int variantId)
- Task SaveAsync(VariantProperties properties) - upsert
- Task<FamilyDefaults?> GetFamilyDefaultsAsync(int familyId)
- Task SaveFamilyDefaultsAsync(FamilyDefaults defaults) - upsert

## 4. Tests in Tests/Repositories/VariantPropertiesRepositoryTests.cs

- Save_NewProperties_Inserts
- Save_ExistingProperties_Updates
- GetByVariantId_NoProperties_ReturnsNull
- DeleteVariant_CascadesProperties
- SaveFamilyDefaults_Works
- GetFamilyDefaults_ReturnsDefaults

## Run Tests

Run dotnet test. All 35 tests should pass.
