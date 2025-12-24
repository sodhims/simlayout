# Prompt 11: BOM Service

Continuing Factory Configurator. Prompt 10 complete, 49 tests passing.

Objective: Implement BOM explosion and circular detection. All tests must pass.

## 1. Service

### IBomService:
- Task<IReadOnlyList<BOMExplosionLine>> ExplodeBOMAsync(int bomId, int maxLevels = 10)
- Task<bool> DetectCircularReferenceAsync(int bomId, int candidateChildVariantId)
- Task<IReadOnlyList<BillOfMaterials>> GetWhereUsedAsync(int variantId)
- Task<int> CloneBOMAsync(int bomId, string newVersion)

### BomService implementation:

#### ExplodeBOMAsync:
- Recursively expand BOM items
- For each item, if child variant has its own active BOM, expand it
- Multiply quantities through levels
- Track Level (1 = direct child, 2 = grandchild, etc.)
- Build Path string showing hierarchy
- Stop at maxLevels to prevent infinite loops
- Return flat list sorted by path/level

#### DetectCircularReferenceAsync:
- If candidateChildVariantId is added to this BOM, would it create a cycle?
- Get the BOM's parent VariantId
- Recursively check if candidateChildVariantId's BOM contains parent VariantId anywhere in its tree
- Return true if cycle would be created

#### GetWhereUsedAsync:
- Find all BOMs that contain this variantId as a child item
- Return list of parent assemblies

#### CloneBOMAsync:
- Copy BOM and all items with new version string
- Return new BOM Id

## 2. Tests in Tests/Services/BomServiceTests.cs

- ExplodeBOM_SingleLevel_ReturnsDirectChildren
- ExplodeBOM_TwoLevels_MultipliesQuantities
  - Assembly needs 2x Subassembly
  - Subassembly needs 3x Component
  - Assert explosion shows Component with Quantity = 6 (2 * 3)
- ExplodeBOM_ThreeLevels_CalculatesCorrectly
  - A (2x B), B (3x C), C (4x D)
  - Assert D appears with Quantity = 24
  - Assert Level = 3 for D
- ExplodeBOM_MaxLevelsRespected
- DetectCircular_DirectCycle_ReturnsTrue
- DetectCircular_IndirectCycle_ReturnsTrue
- DetectCircular_NoCycle_ReturnsFalse
- GetWhereUsed_ReturnsParentAssemblies
- CloneBOM_CopiesAllItems

## Run Tests

Run dotnet test. All 58 tests should pass.
