using CommunityToolkit.Mvvm.ComponentModel;

namespace FactorySimulation.Core.Models;

/// <summary>
/// Represents a Bill of Materials for a part variant.
/// Contains the list of component variants needed to make the parent variant.
/// </summary>
public partial class VariantBillOfMaterials : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _variantId;

    [ObservableProperty]
    private int _version = 1;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private DateTime _effectiveDate = DateTime.Now;

    [ObservableProperty]
    private DateTime? _expirationDate;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _modifiedAt;

    /// <summary>
    /// The parent variant this BOM belongs to
    /// </summary>
    public PartVariant? ParentVariant { get; set; }

    /// <summary>
    /// Items in this BOM
    /// </summary>
    public List<VariantBOMItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single item/component in a Variant Bill of Materials
/// </summary>
public partial class VariantBOMItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _bomId;

    [ObservableProperty]
    private int _componentVariantId;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private string _unitOfMeasure = "EA";

    [ObservableProperty]
    private int _sequence;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private DateTime _createdAt;

    /// <summary>
    /// The component variant
    /// </summary>
    public PartVariant? ComponentVariant { get; set; }

    /// <summary>
    /// Display text for the quantity with unit
    /// </summary>
    public string QuantityDisplay => $"{Quantity} {UnitOfMeasure}";
}

/// <summary>
/// Represents a line in a Variant BOM explosion (flattened view)
/// </summary>
public class VariantBOMExplosionLine
{
    public int Level { get; set; }
    public int VariantId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string FamilyCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public string? CategoryName { get; set; }

    /// <summary>
    /// Indentation string for tree display
    /// </summary>
    public string Indent => new string(' ', Level * 4);

    /// <summary>
    /// Display with indentation
    /// </summary>
    public string IndentedPartNumber => $"{Indent}{PartNumber}";
}
