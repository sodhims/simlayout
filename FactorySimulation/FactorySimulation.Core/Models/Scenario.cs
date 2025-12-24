using CommunityToolkit.Mvvm.ComponentModel;

namespace FactorySimulation.Core.Models;

/// <summary>
/// Represents a configuration scenario for the factory simulation.
/// Scenarios can inherit from parent scenarios to create variants.
/// </summary>
public partial class Scenario : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int? _parentScenarioId;

    [ObservableProperty]
    private bool _isBase;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _modifiedAt;

    /// <summary>
    /// Display name including parent relationship indicator
    /// </summary>
    public string DisplayName => ParentScenarioId.HasValue
        ? $"  └─ {Name}"
        : Name;

    /// <summary>
    /// Indicates if this scenario can be deleted (Base cannot be deleted)
    /// </summary>
    public bool CanDelete => !IsBase;
}
