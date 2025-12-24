# Prompt 9: Bill of Materials Schema

Continuing Factory Configurator. Prompt 8 complete, 40 tests passing.

Objective: Add BOM tables and models. All tests must pass.

## 1. Database schema

Add CreateBomSchemaAsync:

```sql
CREATE TABLE part_BOMs (
    Id INTEGER PRIMARY KEY,
    VariantId INTEGER NOT NULL REFERENCES part_Variants(Id) ON DELETE CASCADE,
    Version TEXT NOT NULL DEFAULT '1.0',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NULL,
    UNIQUE(VariantId, Version)
);

CREATE TABLE part_BOMItems (
    Id INTEGER PRIMARY KEY,
    BOMId INTEGER NOT NULL REFERENCES part_BOMs(Id) ON DELETE CASCADE,
    ChildVariantId INTEGER NOT NULL REFERENCES part_Variants(Id) ON DELETE RESTRICT,
    Quantity REAL NOT NULL CHECK(Quantity > 0),
    Unit TEXT NOT NULL,
    Position INTEGER NULL,
    UNIQUE(BOMId, ChildVariantId)
);
```

## 2. Models

### BillOfMaterials.cs:
- Id, VariantId, Version, IsActive, Notes
- Items (List<BOMItem>)
- VariantPartNumber, VariantName - for display

### BOMItem.cs:
- Id, BOMId, ChildVariantId, Quantity, Unit, Position
- ChildPartNumber, ChildName - for display

### BOMExplosionLine.cs:
- Level, VariantId, PartNumber, Name, Quantity, Unit, Path

## 3. Tests

- BOMModel_DefaultValues
- BOMItem_RequiresPositiveQuantity (document constraint in DB)

## Run Tests

Run dotnet test. All 42 tests should pass.
