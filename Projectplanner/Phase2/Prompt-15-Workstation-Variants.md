# Prompt 15: Workstation-Variant Assignments

Continuing Factory Configurator. Prompt 14 complete, 67 tests passing.

Objective: Assign variants to workstations. All tests must pass.

## 1. Database schema

Add to CreateWorkstationsSchemaAsync:

```sql
CREATE TABLE cfg_WorkstationVariants (
    Id INTEGER PRIMARY KEY,
    WorkstationId INTEGER NOT NULL REFERENCES cfg_Workstations(Id) ON DELETE CASCADE,
    VariantId INTEGER NOT NULL REFERENCES part_Variants(Id) ON DELETE CASCADE,
    Priority INTEGER NOT NULL DEFAULT 0,
    UNIQUE(WorkstationId, VariantId)
);
```

## 2. Model

### WorkstationVariant.cs:
- Id, WorkstationId, VariantId, Priority
- PartNumber (string?) - for display
- VariantName (string?) - for display

## 3. Repository additions to IWorkstationRepository

- Task<IReadOnlyList<WorkstationVariant>> GetVariantsForWorkstationAsync(int workstationId)
- Task<IReadOnlyList<Workstation>> GetWorkstationsForVariantAsync(int variantId)
- Task AssignVariantAsync(int workstationId, int variantId)
- Task UnassignVariantAsync(int workstationId, int variantId)

## 4. Tests in Tests/Repositories/WorkstationVariantRepositoryTests.cs

- AssignVariant_CreatesRecord
- AssignVariant_Duplicate_ThrowsException
- GetWorkstationsForVariant_ReturnsAll
- UnassignVariant_RemovesRecord
- DeleteWorkstation_CascadesAssignments
- DeleteVariant_CascadesAssignments

## 5. View update

### Workstation detail panel - new tab: "Assigned Variants"
- DataGrid: Part Number | Variant Name | Priority | Remove button
- "Add Variant" button opens variant picker
- Priority editable in grid

## Run Tests

Run dotnet test. All 73 tests should pass.
