using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for Variant Bill of Materials business logic
/// </summary>
public class VariantBomService : IVariantBomService
{
    private readonly IVariantBomRepository _bomRepository;
    private readonly IPartVariantRepository _variantRepository;

    public VariantBomService(IVariantBomRepository bomRepository, IPartVariantRepository variantRepository)
    {
        _bomRepository = bomRepository;
        _variantRepository = variantRepository;
    }

    public async Task<VariantBillOfMaterials?> GetBomForVariantAsync(int variantId)
    {
        return await _bomRepository.GetByVariantIdAsync(variantId);
    }

    public async Task<VariantBillOfMaterials?> GetBomWithItemsAsync(int variantId)
    {
        return await _bomRepository.GetWithItemsAsync(variantId);
    }

    public async Task<(bool Success, string? Error, VariantBillOfMaterials? Bom)> CreateBomAsync(int variantId)
    {
        // Check if variant exists
        var variant = await _variantRepository.GetByIdAsync(variantId);
        if (variant == null)
        {
            return (false, "Variant not found.", null);
        }

        // Check if BOM already exists
        var existingBom = await _bomRepository.GetByVariantIdAsync(variantId);
        if (existingBom != null)
        {
            return (false, "A Bill of Materials already exists for this variant.", null);
        }

        var bom = new VariantBillOfMaterials
        {
            VariantId = variantId,
            Version = 1,
            IsActive = true,
            EffectiveDate = DateTime.Now
        };

        var bomId = await _bomRepository.SaveBomAsync(bom);
        bom.Id = bomId;
        bom.ParentVariant = variant;

        return (true, null, bom);
    }

    public async Task<(bool Success, string? Error)> SaveBomAsync(VariantBillOfMaterials bom)
    {
        try
        {
            await _bomRepository.SaveBomAsync(bom);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save BOM: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteBomAsync(int variantId)
    {
        try
        {
            await _bomRepository.DeleteBomAsync(variantId);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete BOM: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error, VariantBOMItem? Item)> AddItemAsync(int variantId, int componentVariantId, decimal quantity, string unitOfMeasure)
    {
        // Validate the component can be added
        var validation = await ValidateAddComponentAsync(variantId, componentVariantId);
        if (!validation.IsValid)
        {
            return (false, validation.Error, null);
        }

        // Get or create BOM
        var bom = await _bomRepository.GetWithItemsAsync(variantId);
        if (bom == null)
        {
            var createResult = await CreateBomAsync(variantId);
            if (!createResult.Success)
            {
                return (false, createResult.Error, null);
            }
            bom = createResult.Bom!;
        }

        // Check if component is already in BOM
        if (bom.Items.Any(i => i.ComponentVariantId == componentVariantId))
        {
            return (false, "Component is already in this BOM. Update the quantity instead.", null);
        }

        var item = new VariantBOMItem
        {
            BomId = bom.Id,
            ComponentVariantId = componentVariantId,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            Sequence = bom.Items.Count
        };

        var itemId = await _bomRepository.AddItemAsync(item);
        item.Id = itemId;

        // Load component variant info
        item.ComponentVariant = await _variantRepository.GetByIdAsync(componentVariantId);

        return (true, null, item);
    }

    public async Task<(bool Success, string? Error)> UpdateItemAsync(VariantBOMItem item)
    {
        if (item.Quantity <= 0)
        {
            return (false, "Quantity must be greater than zero.");
        }

        try
        {
            await _bomRepository.UpdateItemAsync(item);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to update item: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> RemoveItemAsync(int itemId)
    {
        try
        {
            await _bomRepository.DeleteItemAsync(itemId);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to remove item: {ex.Message}");
        }
    }

    public async Task<IEnumerable<VariantBOMExplosionLine>> ExplodeBomAsync(int variantId, decimal quantity = 1)
    {
        var lines = new List<VariantBOMExplosionLine>();
        await ExplodeRecursive(variantId, quantity, 0, lines, new HashSet<int>());
        return lines;
    }

    private async Task ExplodeRecursive(int variantId, decimal quantity, int level, List<VariantBOMExplosionLine> lines, HashSet<int> visited)
    {
        // Prevent infinite loops
        if (visited.Contains(variantId))
            return;

        visited.Add(variantId);

        var bom = await _bomRepository.GetWithItemsAsync(variantId);
        if (bom?.Items == null || !bom.Items.Any())
        {
            visited.Remove(variantId);
            return;
        }

        foreach (var item in bom.Items)
        {
            var calculatedQty = item.Quantity * quantity;

            lines.Add(new VariantBOMExplosionLine
            {
                Level = level + 1,
                VariantId = item.ComponentVariantId,
                PartNumber = item.ComponentVariant?.PartNumber ?? "Unknown",
                VariantName = item.ComponentVariant?.Name ?? "Unknown",
                FamilyCode = item.ComponentVariant?.FamilyCode ?? "",
                Quantity = calculatedQty,
                UnitOfMeasure = item.UnitOfMeasure
            });

            // Recurse into sub-assemblies
            await ExplodeRecursive(item.ComponentVariantId, calculatedQty, level + 1, lines, visited);
        }

        visited.Remove(variantId);
    }

    public async Task<IEnumerable<PartVariant>> GetWhereUsedAsync(int variantId)
    {
        // Find all variants that use this variant as a component
        var allVariants = await _variantRepository.GetAllAsync();
        var whereUsed = new List<PartVariant>();

        foreach (var variant in allVariants)
        {
            var bom = await _bomRepository.GetWithItemsAsync(variant.Id);
            if (bom?.Items.Any(i => i.ComponentVariantId == variantId) == true)
            {
                whereUsed.Add(variant);
            }
        }

        return whereUsed;
    }

    public async Task<(bool IsValid, string? Error)> ValidateAddComponentAsync(int parentVariantId, int childVariantId)
    {
        // Check child exists
        var childVariant = await _variantRepository.GetByIdAsync(childVariantId);
        if (childVariant == null)
        {
            return (false, "Component variant not found.");
        }

        // Check self-reference
        if (parentVariantId == childVariantId)
        {
            return (false, "A variant cannot be a component of itself.");
        }

        // Check for cycles
        if (await _bomRepository.WouldCreateCircularReferenceAsync(parentVariantId, childVariantId))
        {
            return (false, "Adding this component would create a circular reference.");
        }

        return (true, null);
    }
}
