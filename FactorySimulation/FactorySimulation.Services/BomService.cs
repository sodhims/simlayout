using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for Bill of Materials business logic
/// </summary>
public class BomService : IBomService
{
    private readonly IBomRepository _bomRepository;
    private readonly IPartTypeRepository _partTypeRepository;

    public BomService(IBomRepository bomRepository, IPartTypeRepository partTypeRepository)
    {
        _bomRepository = bomRepository;
        _partTypeRepository = partTypeRepository;
    }

    public async Task<BillOfMaterials?> GetBomForPartAsync(int partTypeId)
    {
        return await _bomRepository.GetByPartTypeIdAsync(partTypeId);
    }

    public async Task<(bool Success, string? Error, BillOfMaterials? Bom)> CreateBomAsync(int partTypeId)
    {
        // Check if part exists
        var partType = await _partTypeRepository.GetByIdAsync(partTypeId);
        if (partType == null)
        {
            return (false, "Part type not found.", null);
        }

        // Check if part can have BOM
        if (!partType.CanHaveBOM)
        {
            return (false, "Raw materials cannot have a Bill of Materials.", null);
        }

        // Check if BOM already exists
        var existingBom = await _bomRepository.GetByPartTypeIdAsync(partTypeId);
        if (existingBom != null)
        {
            return (false, "A Bill of Materials already exists for this part.", null);
        }

        var bom = new BillOfMaterials
        {
            PartTypeId = partTypeId,
            Version = 1,
            IsActive = true,
            EffectiveDate = DateTime.Now
        };

        await _bomRepository.CreateAsync(bom);
        bom.ParentPart = partType;

        return (true, null, bom);
    }

    public async Task<(bool Success, string? Error)> SaveBomAsync(BillOfMaterials bom)
    {
        var success = await _bomRepository.UpdateAsync(bom);
        return success ? (true, null) : (false, "Failed to save BOM.");
    }

    public async Task<(bool Success, string? Error)> DeleteBomAsync(int bomId)
    {
        var success = await _bomRepository.DeleteAsync(bomId);
        return success ? (true, null) : (false, "Failed to delete BOM.");
    }

    public async Task<(bool Success, string? Error)> AddItemAsync(int bomId, int componentPartTypeId, decimal quantity, string unitOfMeasure)
    {
        // Get the BOM to find parent part type
        var bom = await _bomRepository.GetByIdAsync(bomId);
        if (bom == null)
        {
            return (false, "BOM not found.");
        }

        // Validate the component can be added
        var validation = await ValidateAddComponentAsync(bom.PartTypeId, componentPartTypeId);
        if (!validation.IsValid)
        {
            return (false, validation.Error);
        }

        // Check if component is already in BOM
        if (bom.Items.Any(i => i.ComponentPartTypeId == componentPartTypeId))
        {
            return (false, "Component is already in this BOM. Update the quantity instead.");
        }

        var item = new BOMItem
        {
            BomId = bomId,
            ComponentPartTypeId = componentPartTypeId,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure
        };

        await _bomRepository.AddItemAsync(item);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateItemAsync(BOMItem item)
    {
        if (item.Quantity <= 0)
        {
            return (false, "Quantity must be greater than zero.");
        }

        var success = await _bomRepository.UpdateItemAsync(item);
        return success ? (true, null) : (false, "Failed to update item.");
    }

    public async Task<(bool Success, string? Error)> RemoveItemAsync(int itemId)
    {
        var success = await _bomRepository.RemoveItemAsync(itemId);
        return success ? (true, null) : (false, "Failed to remove item.");
    }

    public async Task<IEnumerable<BOMExplosionLine>> ExplodeBomAsync(int partTypeId, decimal quantity = 1)
    {
        var lines = new List<BOMExplosionLine>();
        await ExplodeRecursive(partTypeId, quantity, 0, lines, new HashSet<int>());
        return lines;
    }

    private async Task ExplodeRecursive(int partTypeId, decimal quantity, int level, List<BOMExplosionLine> lines, HashSet<int> visited)
    {
        // Prevent infinite loops
        if (visited.Contains(partTypeId))
            return;

        visited.Add(partTypeId);

        var bom = await _bomRepository.GetByPartTypeIdAsync(partTypeId);
        if (bom?.Items == null || !bom.Items.Any())
        {
            visited.Remove(partTypeId);
            return;
        }

        foreach (var item in bom.Items)
        {
            var calculatedQty = item.Quantity * quantity;

            lines.Add(new BOMExplosionLine
            {
                Level = level + 1,
                PartNumber = item.ComponentPart?.PartNumber ?? "Unknown",
                PartName = item.ComponentPart?.Name ?? "Unknown",
                Quantity = calculatedQty,
                UnitOfMeasure = item.UnitOfMeasure,
                Category = item.ComponentPart?.Category ?? PartCategory.Component
            });

            // Recurse into sub-assemblies
            if (item.ComponentPart?.CanHaveBOM == true)
            {
                await ExplodeRecursive(item.ComponentPartTypeId, calculatedQty, level + 1, lines, visited);
            }
        }

        visited.Remove(partTypeId);
    }

    public async Task<IEnumerable<PartType>> GetWhereUsedAsync(int partTypeId)
    {
        return await _bomRepository.GetWhereUsedAsync(partTypeId);
    }

    public async Task<(bool IsValid, string? Error)> ValidateAddComponentAsync(int parentPartTypeId, int childPartTypeId)
    {
        // Check child exists
        var childPart = await _partTypeRepository.GetByIdAsync(childPartTypeId);
        if (childPart == null)
        {
            return (false, "Component part not found.");
        }

        // Check for cycles
        if (await _bomRepository.WouldCreateCycleAsync(parentPartTypeId, childPartTypeId))
        {
            return (false, "Adding this component would create a circular reference.");
        }

        return (true, null);
    }
}
