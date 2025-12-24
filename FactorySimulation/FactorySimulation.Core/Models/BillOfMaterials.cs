using CommunityToolkit.Mvvm.ComponentModel;

namespace FactorySimulation.Core.Models;

/// <summary>
/// Represents a Bill of Materials for a part type.
/// Contains the list of components needed to make the parent part.
/// </summary>
public partial class BillOfMaterials : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _partTypeId;

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
    /// The parent part type this BOM belongs to
    /// </summary>
    public PartType? ParentPart { get; set; }

    /// <summary>
    /// Items in this BOM
    /// </summary>
    public List<BOMItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single item/component in a Bill of Materials
/// </summary>
public partial class BOMItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _bomId;

    [ObservableProperty]
    private int _componentPartTypeId;

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
    /// The component part type
    /// </summary>
    public PartType? ComponentPart { get; set; }

    /// <summary>
    /// Display text for the quantity with unit
    /// </summary>
    public string QuantityDisplay => $"{Quantity} {UnitOfMeasure}";
}

/// <summary>
/// Represents a line in a BOM explosion (flattened view)
/// </summary>
public class BOMExplosionLine
{
    public int Level { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public PartCategory Category { get; set; }

    /// <summary>
    /// Indentation string for tree display
    /// </summary>
    public string Indent => new string(' ', Level * 4);

    /// <summary>
    /// Display with indentation
    /// </summary>
    public string IndentedPartNumber => $"{Indent}{PartNumber}";
}
