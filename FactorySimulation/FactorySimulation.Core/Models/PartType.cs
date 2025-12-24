using CommunityToolkit.Mvvm.ComponentModel;

namespace FactorySimulation.Core.Models;

/// <summary>
/// Part category enumeration
/// </summary>
public enum PartCategory
{
    RawMaterial,
    Component,
    SubAssembly,
    FinishedGood
}

/// <summary>
/// Represents a part type in the factory simulation.
/// Parts can be raw materials, components, sub-assemblies, or finished goods.
/// </summary>
public partial class PartType : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _partNumber = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private PartCategory _category;

    [ObservableProperty]
    private string _unitOfMeasure = "EA";

    [ObservableProperty]
    private decimal _unitCost;

    [ObservableProperty]
    private int? _scenarioId;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _modifiedAt;

    /// <summary>
    /// Display name for UI binding
    /// </summary>
    public string DisplayName => $"{PartNumber} - {Name}";

    /// <summary>
    /// Indicates if this part can have a BOM (not raw materials)
    /// </summary>
    public bool CanHaveBOM => Category != PartCategory.RawMaterial;
}
