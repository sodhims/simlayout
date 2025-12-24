# Prompt 3: Part Families Service

Continuing Factory Configurator. Prompt 2 complete, 8 tests passing.

Objective: Implement PartFamilyService and PartVariantService with business logic. All tests must pass.

## 1. Service interfaces (Core or Services project)

### IPartFamilyService:
- Task<IReadOnlyList<PartFamily>> GetAllWithVariantsAsync()
- Task<PartFamily> CreateFamilyAsync(string familyCode, string name, int categoryId)
- Task<bool> ValidateFamilyCodeAsync(string familyCode)
- Task<IReadOnlyList<PartFamily>> SearchAsync(string searchTerm)

### IPartVariantService:
- Task<PartVariant> CreateVariantAsync(int familyId, string partNumber, string name)
- Task<bool> ValidatePartNumberAsync(string partNumber)
- Task<IReadOnlyList<PartVariant>> GetByFamilyAsync(int familyId)

## 2. Service implementation

### PartFamilyService:
- GetAllWithVariantsAsync: Returns all families with Variants populated
- CreateFamilyAsync: Validates code unique, creates family
- ValidateFamilyCodeAsync: Returns true if code not in use
- SearchAsync: Searches FamilyCode and Name (case-insensitive)

### PartVariantService:
- CreateVariantAsync: Validates familyId exists, partNumber unique, creates variant
- ValidatePartNumberAsync: Returns true if part number not in use
- GetByFamilyAsync: Returns variants for family

## 3. Tests in Tests/Services/

### PartFamilyServiceTests.cs:
- GetAllWithVariants_ReturnsFamiliesWithVariants
- CreateFamily_ValidData_ReturnsFamily
- CreateFamily_DuplicateCode_ThrowsException
- ValidateFamilyCode_Exists_ReturnsFalse
- ValidateFamilyCode_NotExists_ReturnsTrue
- Search_MatchesCodeAndName

### PartVariantServiceTests.cs:
- CreateVariant_ValidData_ReturnsVariant
- CreateVariant_InvalidFamilyId_ThrowsException
- CreateVariant_DuplicatePartNumber_ThrowsException
- CreateVariant_InheritsCategoryFromFamily
- ValidatePartNumber_Exists_ReturnsFalse

## Run Tests

Run dotnet test. All 19 tests should pass (8 + 11).
