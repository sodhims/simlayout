# Prompt 20: Operators

Continuing Factory Configurator. Prompt 19 complete, 91 tests passing.

Objective: Implement operators. All tests must pass.

## 1. Database schema

Add CreateOperatorsSchemaAsync:

```sql
CREATE TABLE res_OperatorTypes (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    HourlyRate REAL NULL,
    Color TEXT NULL
);

CREATE TABLE res_Operators (
    Id INTEGER PRIMARY KEY,
    OperatorTypeId INTEGER NOT NULL REFERENCES res_OperatorTypes(Id) ON DELETE RESTRICT,
    EmployeeId TEXT NULL UNIQUE,
    Name TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NULL
);
```

## 2. Seed data (SeedOperatorsDataAsync)

OperatorTypes: Assembler, Machinist, Inspector, MaterialHandler, Supervisor

## 3. Models

### OperatorType.cs:
- Id, Name, HourlyRate (decimal?), Color (string?)

### Operator.cs:
- Id, OperatorTypeId, EmployeeId (string?), Name, IsActive, Notes
- OperatorTypeName (string?) - for display

## 4. Repository

### IOperatorRepository:
- Task<IReadOnlyList<Operator>> GetAllAsync()
- Task<Operator?> GetByIdAsync(int id)
- Task<IReadOnlyList<Operator>> GetByTypeAsync(int operatorTypeId)
- Task<int> CreateAsync(Operator op)
- Task UpdateAsync(Operator op)
- Task DeleteAsync(int id)

### IOperatorTypeRepository:
- Task<IReadOnlyList<OperatorType>> GetAllAsync()
- Task<OperatorType?> GetByIdAsync(int id)
- Task<int> CreateAsync(OperatorType type)

## 5. Tests in Tests/Repositories/OperatorRepositoryTests.cs

- CreateOperator_ValidData_ReturnsId
- CreateOperator_DuplicateEmployeeId_ThrowsException
- CreateOperator_NullEmployeeId_AllowsMultiple
- GetByType_ReturnsFiltered
- GetAll_IncludesTypeName

## 6. View

### OperatorsView.xaml (new main tab):
- Left: ListView grouped by OperatorType
- Right: Detail panel
  - Name, EmployeeId, Type dropdown, Active checkbox, Notes
- Add/Delete buttons

## Run Tests

Run dotnet test. All 96 tests should pass.
