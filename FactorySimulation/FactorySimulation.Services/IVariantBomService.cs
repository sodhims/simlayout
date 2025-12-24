using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service interface for Variant Bill of Materials business logic
/// </summary>
public interface IVariantBomService
{
    // BOM operations
    Task<VariantBillOfMaterials?> GetBomForVariantAsync(int variantId);
    Task<VariantBillOfMaterials?> GetBomWithItemsAsync(int variantId);
    Task<(bool Success, string? Error, VariantBillOfMaterials? Bom)> CreateBomAsync(int variantId);
    Task<(bool Success, string? Error)> SaveBomAsync(VariantBillOfMaterials bom);
    Task<(bool Success, string? Error)> DeleteBomAsync(int variantId);

    // BOM Item operations
    Task<(bool Success, string? Error, VariantBOMItem? Item)> AddItemAsync(int variantId, int componentVariantId, decimal quantity, string unitOfMeasure);
    Task<(bool Success, string? Error)> UpdateItemAsync(VariantBOMItem item);
    Task<(bool Success, string? Error)> RemoveItemAsync(int itemId);

    // BOM explosion (flattened view)
    Task<IEnumerable<VariantBOMExplosionLine>> ExplodeBomAsync(int variantId, decimal quantity = 1);

    // Where-used (find all variants that use this variant as a component)
    Task<IEnumerable<PartVariant>> GetWhereUsedAsync(int variantId);

    // Validation
    Task<(bool IsValid, string? Error)> ValidateAddComponentAsync(int parentVariantId, int childVariantId);
}
