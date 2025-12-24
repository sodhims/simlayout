using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for Variant Bill of Materials operations
/// </summary>
public interface IVariantBomRepository
{
    /// <summary>
    /// Get BOM for a variant (null if none exists)
    /// </summary>
    Task<VariantBillOfMaterials?> GetByVariantIdAsync(int variantId);

    /// <summary>
    /// Get BOM with all items loaded
    /// </summary>
    Task<VariantBillOfMaterials?> GetWithItemsAsync(int variantId);

    /// <summary>
    /// Create or update BOM for a variant
    /// </summary>
    Task<int> SaveBomAsync(VariantBillOfMaterials bom);

    /// <summary>
    /// Delete BOM for a variant
    /// </summary>
    Task DeleteBomAsync(int variantId);

    /// <summary>
    /// Add an item to a BOM
    /// </summary>
    Task<int> AddItemAsync(VariantBOMItem item);

    /// <summary>
    /// Update a BOM item
    /// </summary>
    Task UpdateItemAsync(VariantBOMItem item);

    /// <summary>
    /// Remove an item from a BOM
    /// </summary>
    Task DeleteItemAsync(int itemId);

    /// <summary>
    /// Get all items for a BOM
    /// </summary>
    Task<IReadOnlyList<VariantBOMItem>> GetItemsAsync(int bomId);

    /// <summary>
    /// Check if adding a component would create a circular reference
    /// </summary>
    Task<bool> WouldCreateCircularReferenceAsync(int parentVariantId, int componentVariantId);
}
