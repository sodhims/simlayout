# Prompt 21: Skills

Continuing Factory Configurator. Prompt 20 complete, 96 tests passing.

Objective: Add skills and operator skills. All tests must pass.

## 1. Database schema additions

Add to CreateOperatorsSchemaAsync:

```sql
CREATE TABLE res_Skills (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    Category TEXT NOT NULL,
    Description TEXT NULL
);

CREATE TABLE res_OperatorSkills (
    Id INTEGER PRIMARY KEY,
    OperatorId INTEGER NOT NULL REFERENCES res_Operators(Id) ON DELETE CASCADE,
    SkillId INTEGER NOT NULL REFERENCES res_Skills(Id) ON DELETE CASCADE,
    ProficiencyLevel INTEGER NOT NULL CHECK(ProficiencyLevel BETWEEN 1 AND 5),
    CertifiedDate TEXT NULL,
    ExpirationDate TEXT NULL,
    UNIQUE(OperatorId, SkillId)
);
```

## 2. Seed data (SeedSkillsDataAsync)

Skills with categories:
- Assembly (Operation)
- Machining (Operation)
- Inspection (Quality)
- ForkliftOperation (Equipment)
- CraneOperation (Equipment)
- SafetyCertified (Safety)

## 3. Models

### Skill.cs:
- Id, Name, Category, Description

### OperatorSkill.cs:
- Id, OperatorId, SkillId
- ProficiencyLevel (1-5)
- CertifiedDate, ExpirationDate
- SkillName (string?) - for display

## 4. Repository additions

### ISkillRepository:
- Task<IReadOnlyList<Skill>> GetAllAsync()
- Task<Skill?> GetByIdAsync(int id)

### IOperatorRepository additions:
- Task<IReadOnlyList<OperatorSkill>> GetSkillsForOperatorAsync(int operatorId)
- Task AddSkillAsync(int operatorId, int skillId, int level, DateTime? certified, DateTime? expires)
- Task UpdateSkillLevelAsync(int operatorSkillId, int newLevel)
- Task RemoveSkillAsync(int operatorSkillId)
- Task<IReadOnlyList<Operator>> GetBySkillAsync(int skillId, int minLevel = 1)

## 5. Service

### ISkillService:
- Task<IReadOnlyList<OperatorSkill>> GetExpiringCertificationsAsync(int daysAhead)

## 6. Tests in Tests/Repositories/OperatorSkillRepositoryTests.cs

- AddSkill_CreatesRecord
- AddSkill_Duplicate_ThrowsException
- UpdateSkillLevel_Works
- GetBySkill_FiltersByMinLevel
- RemoveSkill_Deletes
- DeleteOperator_CascadesSkills

### Tests in Tests/Services/SkillServiceTests.cs

- GetExpiringCertifications_FindsCorrect

## 7. View update

### Operator detail - new tab: Skills
- DataGrid: Skill | Level (1-5) | Certified | Expires
- Add Skill button (skill picker + level input)
- Remove button

### Proficiency levels legend:
1 = Trainee (needs supervision)
2 = Basic (can perform with guidance)
3 = Competent (independent work)
4 = Proficient (handles exceptions)
5 = Expert (can train others)

## Run Tests

Run dotnet test. All 103 tests should pass.
