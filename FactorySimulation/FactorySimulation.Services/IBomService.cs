using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service interface for Bill of Materials business logic
/// </summary>
public interface IBomService
{
    // BOM operations
    Task<BillOfMaterials?> GetBomForPartAsync(int partTypeId);
    Task<(bool Success, string? Error, BillOfMaterials? Bom)> CreateBomAsync(int partTypeId);
    Task<(bool Success, string? Error)> SaveBomAsync(BillOfMaterials bom);
    Task<(bool Success, string? Error)> DeleteBomAsync(int bomId);

    // BOM Item operations
    Task<(bool Success, string? Error)> AddItemAsync(int bomId, int componentPartTypeId, decimal quantity, string unitOfMeasure);
    Task<(bool Success, string? Error)> UpdateItemAsync(BOMItem item);
    Task<(bool Success, string? Error)> RemoveItemAsync(int itemId);

    // BOM explosion
    Task<IEnumerable<BOMExplosionLine>> ExplodeBomAsync(int partTypeId, decimal quantity = 1);

    // Where-used
    Task<IEnumerable<PartType>> GetWhereUsedAsync(int partTypeId);

    // Validation
    Task<(bool IsValid, string? Error)> ValidateAddComponentAsync(int parentPartTypeId, int childPartTypeId);
}
