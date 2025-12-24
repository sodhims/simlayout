# Prompt 16: Process Times Schema & Repository

Continuing Factory Configurator. Prompt 15 complete, 73 tests passing.

Objective: Add process times with scenario support. All tests must pass.

## 1. Database schema

Add CreateProcessTimesSchemaAsync:

```sql
CREATE TABLE cfg_ProcessTimes (
    Id INTEGER PRIMARY KEY,
    ScenarioId INTEGER NOT NULL REFERENCES Scenarios(Id) ON DELETE CASCADE,
    WorkstationId INTEGER NOT NULL REFERENCES cfg_Workstations(Id) ON DELETE CASCADE,
    VariantId INTEGER NULL REFERENCES part_Variants(Id) ON DELETE CASCADE,
    ProcessTimeSec REAL NOT NULL CHECK(ProcessTimeSec >= 0),
    LoadTimeSec REAL NOT NULL DEFAULT 0 CHECK(LoadTimeSec >= 0),
    UnloadTimeSec REAL NOT NULL DEFAULT 0 CHECK(UnloadTimeSec >= 0),
    Notes TEXT NULL,
    UNIQUE(ScenarioId, WorkstationId, VariantId)
);
```

Note: VariantId NULL = default time for any variant at this workstation

## 2. Model

### ProcessTime.cs:
- Id, ScenarioId, WorkstationId, VariantId (nullable)
- ProcessTimeSec, LoadTimeSec, UnloadTimeSec
- Notes
- TotalCycleTime (computed) => LoadTimeSec + ProcessTimeSec + UnloadTimeSec

## 3. Repository

### IProcessTimeRepository:
- Task<ProcessTime?> GetAsync(int scenarioId, int workstationId, int? variantId)
- Task<IReadOnlyList<ProcessTime>> GetAllForWorkstationAsync(int scenarioId, int workstationId)
- Task<IReadOnlyList<ProcessTime>> GetAllForScenarioAsync(int scenarioId)
- Task<int> CreateAsync(ProcessTime time)
- Task UpdateAsync(ProcessTime time)
- Task DeleteAsync(int id)

### Implementation notes:
- GetAsync: exact match on all three keys (scenarioId, workstationId, variantId)
- Duplicate key combination throws InvalidOperationException

## 4. Tests in Tests/Repositories/ProcessTimeRepositoryTests.cs

- Create_ValidData_ReturnsId
- Create_DuplicateKey_ThrowsException
- Create_NullVariantId_AllowedOnce
- GetAllForWorkstation_ReturnsCorrect
- DeleteScenario_CascadesTimes

## Run Tests

Run dotnet test. All 78 tests should pass.
