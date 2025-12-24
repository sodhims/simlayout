using System.Windows;
using FactorySimulation.Core.Models;
using FactorySimulation.Services;

namespace FactorySimulation.Configurator.ViewModels.Handlers;

/// <summary>
/// Handles drop operations for Part Variants onto Part Families
/// </summary>
public class VariantDropHandler
{
    private readonly IPartVariantService _variantService;
    private readonly Action _onDropComplete;

    public VariantDropHandler(IPartVariantService variantService, Action onDropComplete)
    {
        _variantService = variantService;
        _onDropComplete = onDropComplete;
    }

    /// <summary>
    /// Handles DragOver event to determine if drop is allowed
    /// </summary>
    public async Task<bool> CanDropAsync(PartVariant? variant, PartFamily? targetFamily)
    {
        if (variant == null || targetFamily == null)
            return false;

        return await _variantService.CanMoveToFamilyAsync(variant.Id, targetFamily.Id);
    }

    /// <summary>
    /// Handles the drop operation
    /// </summary>
    public async Task DropAsync(PartVariant variant, PartFamily targetFamily)
    {
        await _variantService.MoveToFamilyAsync(variant.Id, targetFamily.Id);
        _onDropComplete?.Invoke();
    }

    /// <summary>
    /// Gets the appropriate drag effect based on whether drop is allowed
    /// </summary>
    public static DragDropEffects GetDragEffect(bool canDrop)
    {
        return canDrop ? DragDropEffects.Move : DragDropEffects.None;
    }
}
