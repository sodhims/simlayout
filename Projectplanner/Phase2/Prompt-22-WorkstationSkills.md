# Prompt 22: Workstation Skill Requirements

Continuing Factory Configurator. Prompt 21 complete, 103 tests passing.

Objective: Define skill requirements for workstations. All tests must pass.

## 1. Database schema

Add to CreateWorkstationsSchemaAsync:

```sql
CREATE TABLE cfg_WorkstationSkills (
    Id INTEGER PRIMARY KEY,
    WorkstationId INTEGER NOT NULL REFERENCES cfg_Workstations(Id) ON DELETE CASCADE,
    SkillId INTEGER NOT NULL REFERENCES res_Skills(Id) ON DELETE CASCADE,
    MinProficiencyLevel INTEGER NOT NULL DEFAULT 1 CHECK(MinProficiencyLevel BETWEEN 1 AND 5),
    UNIQUE(WorkstationId, SkillId)
);
```

## 2. Model

### WorkstationSkill.cs:
- Id, WorkstationId, SkillId, MinProficiencyLevel
- SkillName (string?) - for display

### SkillGap.cs (for validation results):
- SkillId, SkillName
- RequiredLevel, ActualLevel (nullable if missing)

## 3. Repository additions to IWorkstationRepository

- Task<IReadOnlyList<WorkstationSkill>> GetRequiredSkillsAsync(int workstationId)
- Task AddRequiredSkillAsync(int workstationId, int skillId, int minLevel)
- Task UpdateRequiredSkillAsync(int workstationSkillId, int newMinLevel)
- Task RemoveRequiredSkillAsync(int workstationSkillId)

## 4. Service additions to IWorkstationService

- Task<IReadOnlyList<Operator>> GetQualifiedOperatorsAsync(int workstationId)
  - Returns operators who have ALL required skills at or above required levels

- Task<IReadOnlyList<SkillGap>> ValidateOperatorForWorkstationAsync(int operatorId, int workstationId)
  - Returns list of missing skills or insufficient levels
  - Empty list means operator is qualified

## 5. Tests in Tests/Services/WorkstationServiceTests.cs

- GetQualifiedOperators_AllMet_ReturnsOperator
- GetQualifiedOperators_MissingSkill_NotReturned
- GetQualifiedOperators_LevelTooLow_NotReturned
- GetQualifiedOperators_NoRequirements_ReturnsAllActive
- ValidateOperator_MissingSkill_ReturnsGap
- ValidateOperator_LevelInsufficient_ReturnsGap
- ValidateOperator_AllMet_ReturnsEmpty

## 6. View update

### Workstation detail - new tab: Required Skills
- DataGrid: Skill | Min Level
- Add/Remove buttons
- Info panel: "X operators qualified"

## Run Tests

Run dotnet test. All 110 tests should pass.
