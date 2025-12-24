using System.Reactive.Linq;
using System.Windows.Media;
using DynamicData;
using FactorySimulation.Core.Models;
using NodeNetwork.ViewModels;
using ReactiveUI;

namespace FactorySimulation.Configurator.Graph;

/// <summary>
/// Node types for BOM visualization
/// </summary>
public enum BomNodeType
{
    Product,
    SubAssembly,
    Component,
    RawMaterial
}

/// <summary>
/// View model for a node in the BOM graph representing a part
/// </summary>
public class BomNodeViewModel : NodeViewModel
{
    private PartType? _partType;
    private BomNodeType _nodeType;
    private decimal _quantity;
    private bool _isSelected;

    /// <summary>
    /// The underlying part type data
    /// </summary>
    public PartType? PartType
    {
        get => _partType;
        set => this.RaiseAndSetIfChanged(ref _partType, value);
    }

    /// <summary>
    /// The type of this BOM node
    /// </summary>
    public BomNodeType NodeType
    {
        get => _nodeType;
        set => this.RaiseAndSetIfChanged(ref _nodeType, value);
    }

    /// <summary>
    /// Quantity of this part in the parent assembly
    /// </summary>
    public decimal Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    /// <summary>
    /// Whether this node is currently selected
    /// </summary>
    public bool IsNodeSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// Input port for connections from parent assemblies
    /// </summary>
    public BomInputViewModel? ParentInput { get; private set; }

    /// <summary>
    /// Output port for connections to child components
    /// </summary>
    public BomOutputViewModel? ChildrenOutput { get; private set; }

    /// <summary>
    /// Display header for the node
    /// </summary>
    public string PartNumber => PartType?.PartNumber ?? "Unknown";

    /// <summary>
    /// Part name display
    /// </summary>
    public string PartName => PartType?.Name ?? "";

    /// <summary>
    /// Quantity display with unit
    /// </summary>
    public string QuantityDisplay => Quantity > 0 ? $"Qty: {Quantity}" : "";

    /// <summary>
    /// Node background color based on type
    /// </summary>
    public Brush NodeColor => NodeType switch
    {
        BomNodeType.Product => new SolidColorBrush(Color.FromRgb(76, 175, 80)),       // Green
        BomNodeType.SubAssembly => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // Blue
        BomNodeType.Component => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Amber
        BomNodeType.RawMaterial => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
    };

    public BomNodeViewModel(PartType partType, BomNodeType nodeType, decimal quantity = 1)
    {
        PartType = partType;
        NodeType = nodeType;
        Quantity = quantity;

        Name = partType.DisplayName;

        // Create input port (for parent connections) - not for root products
        if (nodeType != BomNodeType.Product)
        {
            ParentInput = new BomInputViewModel();
            Inputs.Add(ParentInput);
        }

        // Create output port (for child connections) - not for raw materials
        if (nodeType != BomNodeType.RawMaterial && nodeType != BomNodeType.Component)
        {
            ChildrenOutput = new BomOutputViewModel();
            Outputs.Add(ChildrenOutput);
        }
    }

    public static BomNodeType FromPartCategory(PartCategory category, bool isRoot = false)
    {
        if (isRoot) return BomNodeType.Product;

        return category switch
        {
            PartCategory.FinishedGood => BomNodeType.Product,
            PartCategory.SubAssembly => BomNodeType.SubAssembly,
            PartCategory.Component => BomNodeType.Component,
            PartCategory.RawMaterial => BomNodeType.RawMaterial,
            _ => BomNodeType.Component
        };
    }
}

/// <summary>
/// Input port for receiving parent connections
/// </summary>
public class BomInputViewModel : NodeInputViewModel
{
    public BomInputViewModel()
    {
        Name = "From Parent";
        PortPosition = PortPosition.Left;
    }
}

/// <summary>
/// Output port for child connections
/// </summary>
public class BomOutputViewModel : NodeOutputViewModel
{
    public BomOutputViewModel()
    {
        Name = "To Children";
        PortPosition = PortPosition.Right;
    }
}

/// <summary>
/// Connection between BOM nodes representing parent-child relationship
/// </summary>
public class BomConnectionViewModel : ConnectionViewModel
{
    private decimal _quantity;

    /// <summary>
    /// Quantity of child in parent assembly
    /// </summary>
    public decimal Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    public BomConnectionViewModel(NetworkViewModel parent, BomInputViewModel input, BomOutputViewModel output, decimal quantity = 1)
        : base(parent, input, output)
    {
        Quantity = quantity;
    }
}
