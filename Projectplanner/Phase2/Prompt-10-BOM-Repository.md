# Prompt 10: BOM Repository

Continuing Factory Configurator. Prompt 9 complete, 42 tests passing.

Objective: Implement BOM repository. All tests must pass.

## 1. Repository

### IBomRepository:
- Task<BillOfMaterials?> GetByVariantAsync(int variantId) - active BOM
- Task<BillOfMaterials?> GetWithItemsAsync(int bomId)
- Task<int> CreateAsync(BillOfMaterials bom)
- Task UpdateAsync(BillOfMaterials bom)
- Task DeleteAsync(int bomId)
- Task AddItemAsync(int bomId, BOMItem item)
- Task UpdateItemAsync(BOMItem item)
- Task RemoveItemAsync(int bomItemId)

### Implementation notes:
- GetByVariantAsync: Returns BOM where IsActive = 1 for given variant
- GetWithItemsAsync: Joins part_BOMItems with part_Variants for ChildPartNumber/Name
- AddItemAsync: Inserts item, returns with Id set
- Duplicate ChildVariantId in same BOM throws InvalidOperationException with "duplicate"

## 2. Tests in Tests/Repositories/BomRepositoryTests.cs

- CreateBom_ValidData_ReturnsId
- GetByVariant_ActiveBom_ReturnsBom
- GetByVariant_NoBom_ReturnsNull
- GetWithItems_PopulatesItems
- AddItem_DuplicateChild_ThrowsException
- RemoveItem_RemovesFromBom
- DeleteBom_CascadesItems

## Run Tests

Run dotnet test. All 49 tests should pass.
