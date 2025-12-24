# Prompt 19: Setup Times

Continuing Factory Configurator. Prompt 18 complete, 86 tests passing.

Objective: Add setup/changeover times. All tests must pass.

## 1. Database schema

Add to CreateProcessTimesSchemaAsync:

```sql
CREATE TABLE cfg_SetupTimes (
    Id INTEGER PRIMARY KEY,
    ScenarioId INTEGER NOT NULL REFERENCES Scenarios(Id) ON DELETE CASCADE,
    WorkstationId INTEGER NOT NULL REFERENCES cfg_Workstations(Id) ON DELETE CASCADE,
    FromVariantId INTEGER NULL REFERENCES part_Variants(Id) ON DELETE CASCADE,
    ToVariantId INTEGER NULL REFERENCES part_Variants(Id) ON DELETE CASCADE,
    SetupTimeSec REAL NOT NULL CHECK(SetupTimeSec >= 0),
    Notes TEXT NULL,
    UNIQUE(ScenarioId, WorkstationId, FromVariantId, ToVariantId)
);
```

Note: 
- FromVariantId NULL = initial setup (cold start)
- ToVariantId NULL = teardown

## 2. Model

### SetupTime.cs:
- Id, ScenarioId, WorkstationId
- FromVariantId (nullable), ToVariantId (nullable)
- SetupTimeSec
- Notes

## 3. Repository

### ISetupTimeRepository:
- Task<SetupTime?> GetAsync(int scenarioId, int workstationId, int? fromVariantId, int? toVariantId)
- Task<IReadOnlyList<SetupTime>> GetAllForWorkstationAsync(int scenarioId, int workstationId)
- Task<int> CreateAsync(SetupTime time)
- Task UpdateAsync(SetupTime time)
- Task DeleteAsync(int id)

## 4. Service

### ISetupTimeService:
- Task<decimal?> GetSetupTimeAsync(int scenarioId, int workstationId, int? fromVariantId, int toVariantId)
- Task<IReadOnlyList<SetupTime>> GetMatrixAsync(int scenarioId, int workstationId)

## 5. Tests in Tests/Services/SetupTimeServiceTests.cs

- GetSetupTime_ExactMatch_ReturnsTime
- GetSetupTime_InitialSetup_FromNull
- GetSetupTime_Teardown_ToNull
- GetSetupTime_NoEntry_ReturnsNull
- GetMatrix_ReturnsAll

## 6. View

### SetupTimesView.xaml (sub-view or separate tab):
- Workstation selector
- DataGrid (simple list, not pivot matrix):
  - Columns: From Part | To Part | Setup Time (sec) | Notes
  - "Initial" shown for From = NULL
  - "Teardown" shown for To = NULL
- Add/Edit/Delete buttons

## Run Tests

Run dotnet test. All 91 tests should pass.
