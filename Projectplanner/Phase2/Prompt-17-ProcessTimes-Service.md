# Prompt 17: Process Times Service (Fallback)

Continuing Factory Configurator. Prompt 16 complete, 78 tests passing.

Objective: Implement process time fallback logic. All tests must pass.

## 1. Service

### IProcessTimeService:
- Task<ProcessTime?> GetEffectiveTimeAsync(int scenarioId, int workstationId, int variantId)
- Task<decimal?> GetCycleTimeAsync(int scenarioId, int workstationId, int variantId)
- Task CopyFromScenarioAsync(int sourceScenarioId, int targetScenarioId, int workstationId)
- Task SaveAsync(ProcessTime time)

### ProcessTimeService:

Dependencies: IProcessTimeRepository, IScenarioRepository

#### GetEffectiveTimeAsync fallback order:
1. (scenarioId, workstationId, variantId) - exact match
2. (scenarioId, workstationId, NULL) - scenario default
3. (baseScenarioId, workstationId, variantId) - base scenario specific
4. (baseScenarioId, workstationId, NULL) - base scenario default
5. Return null if nothing found

Note: Get baseScenarioId from Scenarios table (ParentScenarioId chain or IsBase = 1)

#### GetCycleTimeAsync:
- Get effective time
- Return TotalCycleTime (Load + Process + Unload) or null if no time found

#### CopyFromScenarioAsync:
- Copy all times for workstation from source to target scenario
- Skip if target already has entry (don't overwrite)

## 2. Tests in Tests/Services/ProcessTimeServiceTests.cs

- GetEffectiveTime_ExactMatch_ReturnsIt
- GetEffectiveTime_NoExact_ReturnsDefault
- GetEffectiveTime_NoScenarioEntry_FallsBackToBase
- GetEffectiveTime_NothingExists_ReturnsNull
- GetEffectiveTime_ChildOverridesBase
  - Create time in Base (ProcessTimeSec = 10)
  - Create same key in Child (ProcessTimeSec = 5)
  - Query Child
  - Assert returns 5, not 10
- GetCycleTime_SumsCorrectly
- CopyFromScenario_CopiesAll
- CopyFromScenario_DoesNotOverwrite

## Run Tests

Run dotnet test. All 86 tests should pass.
