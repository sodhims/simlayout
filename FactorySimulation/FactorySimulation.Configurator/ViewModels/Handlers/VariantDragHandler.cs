using System.Windows;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Configurator.ViewModels.Handlers;

/// <summary>
/// Handles drag operations for Part Variants
/// </summary>
public class VariantDragHandler
{
    public static readonly string VariantDataFormat = "PartVariant";

    /// <summary>
    /// Initiates a drag operation for a variant
    /// </summary>
    public static void StartDrag(PartVariant variant, UIElement source)
    {
        if (variant == null) return;

        var data = new DataObject(VariantDataFormat, variant);
        DragDrop.DoDragDrop(source, data, DragDropEffects.Move);
    }

    /// <summary>
    /// Checks if the data being dragged is a variant
    /// </summary>
    public static bool IsVariantDrag(IDataObject data)
    {
        return data.GetDataPresent(VariantDataFormat);
    }

    /// <summary>
    /// Gets the variant from drag data
    /// </summary>
    public static PartVariant? GetVariant(IDataObject data)
    {
        if (!IsVariantDrag(data)) return null;
        return data.GetData(VariantDataFormat) as PartVariant;
    }
}
