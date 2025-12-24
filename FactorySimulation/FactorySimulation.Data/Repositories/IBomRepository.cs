using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for Bill of Materials operations
/// </summary>
public interface IBomRepository
{
    // BOM operations
    Task<BillOfMaterials?> GetByIdAsync(int id);
    Task<BillOfMaterials?> GetByPartTypeIdAsync(int partTypeId, bool activeOnly = true);
    Task<int> CreateAsync(BillOfMaterials bom);
    Task<bool> UpdateAsync(BillOfMaterials bom);
    Task<bool> DeleteAsync(int id);

    // BOM Item operations
    Task<IEnumerable<BOMItem>> GetItemsByBomIdAsync(int bomId);
    Task<int> AddItemAsync(BOMItem item);
    Task<bool> UpdateItemAsync(BOMItem item);
    Task<bool> RemoveItemAsync(int itemId);
    Task<bool> ClearItemsAsync(int bomId);

    // Where-used queries
    Task<IEnumerable<PartType>> GetWhereUsedAsync(int partTypeId);
    Task<IEnumerable<int>> GetAncestorIdsAsync(int partTypeId);

    // Cycle detection
    Task<bool> WouldCreateCycleAsync(int parentPartTypeId, int childPartTypeId);
}
