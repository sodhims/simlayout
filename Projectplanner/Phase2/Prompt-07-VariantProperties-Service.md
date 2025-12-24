# Prompt 7: Variant Properties Service (Inheritance)

Continuing Factory Configurator. Prompt 6 complete, 35 tests passing.

Objective: Implement property inheritance from family defaults. All tests must pass.

## 1. Service

### IVariantPropertiesService:
- Task<VariantProperties?> GetPropertiesAsync(int variantId)
- Task<VariantProperties> GetEffectivePropertiesAsync(int variantId)
- Task SavePropertiesAsync(int variantId, VariantProperties properties)
- Task<FamilyDefaults?> GetFamilyDefaultsAsync(int familyId)
- Task SaveFamilyDefaultsAsync(int familyId, FamilyDefaults defaults)

### VariantPropertiesService:

#### GetEffectivePropertiesAsync:
- Get variant's own properties
- Get family defaults
- Merge: Use variant value if not null, else family default
- Return merged result

## 2. Tests in Tests/Services/VariantPropertiesServiceTests.cs

- GetEffectiveProperties_VariantHasAll_ReturnsVariantValues
- GetEffectiveProperties_VariantHasNone_ReturnsFamilyDefaults
- GetEffectiveProperties_PartialOverride_MergesCorrectly
  - Family: Length=100, Weight=5
  - Variant: Weight=3 (Length null)
  - Result: Length=100, Weight=3
- GetEffectiveProperties_NoFamilyDefaults_ReturnsVariantOnly
- GetEffectiveProperties_NothingSet_ReturnsEmptyProperties

## Run Tests

Run dotnet test. All 40 tests should pass.
