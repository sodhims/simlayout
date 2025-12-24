using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactorySimulation.Core.Models;
using FactorySimulation.Services;

namespace FactorySimulation.Configurator.ViewModels;

/// <summary>
/// Node in the BOM tree for visualization
/// </summary>
public class BomTreeNode
{
    public int PartTypeId { get; set; }
    public int? BomItemId { get; set; }
    public int? ParentPartTypeId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public PartCategory Category { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal UnitCost { get; set; }
    public bool IsRoot { get; set; }

    public ObservableCollection<BomTreeNode> Children { get; } = new();

    public string QuantityDisplay => $"{Quantity:N2} {UnitOfMeasure}";
    public bool ShowQuantity => !IsRoot && Quantity > 0;

    public string CategoryTag => Category switch
    {
        PartCategory.FinishedGood => "[Product]",
        PartCategory.SubAssembly => "[Assembly]",
        PartCategory.Component => "[Component]",
        PartCategory.RawMaterial => "[Material]",
        _ => ""
    };

    public Brush NodeColor => Category switch
    {
        PartCategory.FinishedGood => new SolidColorBrush(Color.FromRgb(76, 175, 80)),     // Green
        PartCategory.SubAssembly => new SolidColorBrush(Color.FromRgb(33, 150, 243)),    // Blue
        PartCategory.Component => new SolidColorBrush(Color.FromRgb(255, 152, 0)),       // Orange
        PartCategory.RawMaterial => new SolidColorBrush(Color.FromRgb(158, 158, 158)),   // Gray
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
    };
}

/// <summary>
/// Item in the components-only flat list with visual properties
/// </summary>
public class ComponentListItem
{
    public int PartTypeId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public decimal TotalQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal UnitCost { get; set; }
    public PartCategory Category { get; set; } = PartCategory.Component;

    // For calculating relative bar widths
    public decimal MaxQuantityInList { get; set; } = 100;

    public string TotalQuantityDisplay => $"{TotalQuantity:N2}";
    public string UnitCostDisplay => UnitCost > 0 ? $"${UnitCost:N2}" : "-";
    public decimal ExtendedCost => TotalQuantity * UnitCost;
    public string ExtendedCostDisplay => UnitCost > 0 ? $"${ExtendedCost:N2}" : "-";

    // Visual properties for category
    public string CategoryIcon => Category switch
    {
        PartCategory.Component => "C",
        PartCategory.RawMaterial => "R",
        PartCategory.SubAssembly => "A",
        PartCategory.FinishedGood => "P",
        _ => "?"
    };

    public string CategoryName => Category switch
    {
        PartCategory.Component => "Component",
        PartCategory.RawMaterial => "Raw Material",
        PartCategory.SubAssembly => "Sub-Assembly",
        PartCategory.FinishedGood => "Finished Good",
        _ => "Unknown"
    };

    public Brush CategoryColor => Category switch
    {
        PartCategory.Component => new SolidColorBrush(Color.FromRgb(255, 152, 0)),      // Orange
        PartCategory.RawMaterial => new SolidColorBrush(Color.FromRgb(158, 158, 158)),  // Gray
        PartCategory.SubAssembly => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
        PartCategory.FinishedGood => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Green
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
    };

    // Quantity bar visualization
    public double QuantityBarWidth => MaxQuantityInList > 0
        ? Math.Min(100, (double)(TotalQuantity / MaxQuantityInList) * 100)
        : 0;

    public Brush QuantityBarColor => TotalQuantity switch
    {
        > 50 => new SolidColorBrush(Color.FromRgb(244, 67, 54)),    // Red - high quantity
        > 20 => new SolidColorBrush(Color.FromRgb(255, 152, 0)),    // Orange - medium
        > 5 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Amber - moderate
        _ => new SolidColorBrush(Color.FromRgb(76, 175, 80))        // Green - low
    };

    // Cost visualization
    public Brush CostForeground => UnitCost switch
    {
        0 => new SolidColorBrush(Color.FromRgb(158, 158, 158)),     // Gray for no cost
        > 10 => new SolidColorBrush(Color.FromRgb(211, 47, 47)),    // Dark red for expensive
        > 5 => new SolidColorBrush(Color.FromRgb(245, 124, 0)),     // Orange for moderate
        _ => new SolidColorBrush(Color.FromRgb(56, 142, 60))        // Green for cheap
    };

    public Brush ExtendedCostBackground => ExtendedCost switch
    {
        0 => Brushes.Transparent,
        > 100 => new SolidColorBrush(Color.FromRgb(255, 235, 238)), // Light red
        > 50 => new SolidColorBrush(Color.FromRgb(255, 243, 224)),  // Light orange
        > 10 => new SolidColorBrush(Color.FromRgb(255, 249, 196)),  // Light yellow
        _ => new SolidColorBrush(Color.FromRgb(232, 245, 233))      // Light green
    };

    public Brush ExtendedCostForeground => ExtendedCost switch
    {
        0 => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
        > 100 => new SolidColorBrush(Color.FromRgb(198, 40, 40)),   // Dark red
        > 50 => new SolidColorBrush(Color.FromRgb(230, 81, 0)),     // Dark orange
        > 10 => new SolidColorBrush(Color.FromRgb(245, 127, 23)),   // Dark amber
        _ => new SolidColorBrush(Color.FromRgb(46, 125, 50))        // Dark green
    };
}

/// <summary>
/// ViewModel for the Visual BOM tree editor
/// </summary>
public partial class VisualBomViewModel : ObservableObject
{
    private readonly IBomService _bomService;
    private readonly IPartTypeService _partTypeService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoProduct))]
    private PartType? _selectedProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
    [NotifyPropertyChangedFor(nameof(SelectedItemInfo))]
    [NotifyPropertyChangedFor(nameof(SelectedItemType))]
    [NotifyPropertyChangedFor(nameof(SelectedItemQuantity))]
    [NotifyPropertyChangedFor(nameof(SelectedExtendedCost))]
    [NotifyPropertyChangedFor(nameof(SelectedExtendedCostBackground))]
    [NotifyPropertyChangedFor(nameof(SelectedExtendedCostForeground))]
    private BomTreeNode? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _showComponentsOnly;

    [ObservableProperty]
    private ComponentListItem? _selectedComponentItem;

    /// <summary>
    /// Available products (finished goods and subassemblies)
    /// </summary>
    public ObservableCollection<PartType> AvailableProducts { get; } = new();

    /// <summary>
    /// The BOM tree structure
    /// </summary>
    public ObservableCollection<BomTreeNode> BomTree { get; } = new();

    /// <summary>
    /// Flat list of components only (with aggregated quantities)
    /// </summary>
    public ObservableCollection<ComponentListItem> ComponentsOnlyList { get; } = new();

    /// <summary>
    /// Sortable view of the components list
    /// </summary>
    public ICollectionView ComponentsOnlyView => CollectionViewSource.GetDefaultView(ComponentsOnlyList);

    private string _currentSortColumn = "PartNumber";
    private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

    /// <summary>
    /// True when no product is selected
    /// </summary>
    public bool HasNoProduct => SelectedProduct == null && BomTree.Count == 0;

    /// <summary>
    /// True when an item is selected
    /// </summary>
    public bool HasSelectedItem => SelectedItem != null;

    /// <summary>
    /// Selected item info display
    /// </summary>
    public string SelectedItemInfo => SelectedItem != null
        ? $"{SelectedItem.PartNumber} - {SelectedItem.PartName}"
        : "";

    /// <summary>
    /// Selected item type display
    /// </summary>
    public string SelectedItemType => SelectedItem?.Category.ToString() ?? "";

    /// <summary>
    /// Selected item quantity display
    /// </summary>
    public string SelectedItemQuantity => SelectedItem?.QuantityDisplay ?? "";

    /// <summary>
    /// Extended cost for the selected item
    /// </summary>
    public decimal SelectedExtendedCost => SelectedItem != null
        ? SelectedItem.Quantity * SelectedItem.UnitCost
        : 0;

    /// <summary>
    /// Background color for extended cost display
    /// </summary>
    public Brush SelectedExtendedCostBackground => SelectedExtendedCost switch
    {
        0 => new SolidColorBrush(Color.FromRgb(245, 245, 245)),
        > 100 => new SolidColorBrush(Color.FromRgb(255, 235, 238)),
        > 50 => new SolidColorBrush(Color.FromRgb(255, 243, 224)),
        > 10 => new SolidColorBrush(Color.FromRgb(255, 249, 196)),
        _ => new SolidColorBrush(Color.FromRgb(232, 245, 233))
    };

    /// <summary>
    /// Foreground color for extended cost display
    /// </summary>
    public Brush SelectedExtendedCostForeground => SelectedExtendedCost switch
    {
        0 => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
        > 100 => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
        > 50 => new SolidColorBrush(Color.FromRgb(230, 81, 0)),
        > 10 => new SolidColorBrush(Color.FromRgb(245, 127, 23)),
        _ => new SolidColorBrush(Color.FromRgb(46, 125, 50))
    };

    public VisualBomViewModel(IBomService bomService, IPartTypeService partTypeService)
    {
        _bomService = bomService;
        _partTypeService = partTypeService;
    }

    /// <summary>
    /// Initialize the view model
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadAvailableProductsAsync();
    }

    /// <summary>
    /// Load all products that can have BOMs
    /// </summary>
    private async Task LoadAvailableProductsAsync()
    {
        try
        {
            IsLoading = true;
            var parts = await _partTypeService.GetAllAsync();

            AvailableProducts.Clear();
            foreach (var part in parts.Where(p => p.CanHaveBOM).OrderBy(p => p.PartNumber))
            {
                AvailableProducts.Add(part);
            }
            StatusMessage = $"Loaded {AvailableProducts.Count} products";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading products: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load BOM for selected product
    /// </summary>
    [RelayCommand]
    public async Task LoadBomAsync()
    {
        if (SelectedProduct == null)
        {
            BomTree.Clear();
            ComponentsOnlyList.Clear();
            SelectedItem = null;
            OnPropertyChanged(nameof(HasNoProduct));
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Loading BOM structure...";
            BomTree.Clear();
            SelectedItem = null;

            // Get the root BOM
            var bom = await _bomService.GetBomForPartAsync(SelectedProduct.Id);

            // Create root node
            var rootNode = new BomTreeNode
            {
                PartTypeId = SelectedProduct.Id,
                PartNumber = SelectedProduct.PartNumber,
                PartName = SelectedProduct.Name,
                Category = SelectedProduct.Category,
                IsRoot = true
            };

            if (bom?.Items != null && bom.Items.Count > 0)
            {
                // Build tree recursively with depth limit to avoid infinite loops
                var visited = new HashSet<int> { SelectedProduct.Id };
                await BuildTreeNodeAsync(rootNode, bom, visited, 0, 10);
            }

            BomTree.Add(rootNode);

            // Build the components-only list
            BuildComponentsOnlyList(rootNode);

            int totalNodes = CountNodes(rootNode);
            int componentCount = ComponentsOnlyList.Count;
            StatusMessage = $"Loaded {totalNodes} nodes, {componentCount} unique components";
            OnPropertyChanged(nameof(HasNoProduct));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading BOM: {ex.Message}";
            MessageBox.Show($"Failed to load BOM:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private int CountNodes(BomTreeNode node)
    {
        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }
        return count;
    }

    /// <summary>
    /// Builds a flat list of all components with aggregated quantities
    /// </summary>
    private void BuildComponentsOnlyList(BomTreeNode rootNode)
    {
        ComponentsOnlyList.Clear();
        var componentDict = new Dictionary<int, ComponentListItem>();

        // Recursively collect all components with their quantities
        CollectComponents(rootNode, 1m, componentDict);

        // Calculate max quantity for bar visualization
        var maxQty = componentDict.Values.Any()
            ? componentDict.Values.Max(c => c.TotalQuantity)
            : 100m;

        // Sort by part number and add to list
        foreach (var item in componentDict.Values.OrderBy(c => c.PartNumber))
        {
            item.MaxQuantityInList = maxQty;
            ComponentsOnlyList.Add(item);
        }

        // Notify that the view has changed
        OnPropertyChanged(nameof(ComponentsOnlyView));
    }

    /// <summary>
    /// Recursively collects components from the tree, multiplying quantities
    /// </summary>
    private void CollectComponents(BomTreeNode node, decimal parentMultiplier, Dictionary<int, ComponentListItem> componentDict)
    {
        foreach (var child in node.Children)
        {
            var effectiveQty = child.Quantity * parentMultiplier;

            if (child.Category == PartCategory.Component || child.Category == PartCategory.RawMaterial)
            {
                // It's a component - add to the dictionary
                if (componentDict.TryGetValue(child.PartTypeId, out var existing))
                {
                    existing.TotalQuantity += effectiveQty;
                }
                else
                {
                    componentDict[child.PartTypeId] = new ComponentListItem
                    {
                        PartTypeId = child.PartTypeId,
                        PartNumber = child.PartNumber,
                        PartName = child.PartName,
                        TotalQuantity = effectiveQty,
                        UnitOfMeasure = child.UnitOfMeasure,
                        UnitCost = child.UnitCost,
                        Category = child.Category
                    };
                }
            }

            // Recursively process children (for subassemblies)
            if (child.Children.Count > 0)
            {
                CollectComponents(child, effectiveQty, componentDict);
            }
        }
    }

    /// <summary>
    /// Build tree nodes recursively
    /// </summary>
    private async Task BuildTreeNodeAsync(BomTreeNode parentNode, BillOfMaterials bom, HashSet<int> visited, int depth, int maxDepth)
    {
        if (depth >= maxDepth) return;

        foreach (var item in bom.Items)
        {
            if (item.ComponentPart == null) continue;

            var childNode = new BomTreeNode
            {
                PartTypeId = item.ComponentPart.Id,
                BomItemId = item.Id,
                ParentPartTypeId = parentNode.PartTypeId,
                PartNumber = item.ComponentPart.PartNumber,
                PartName = item.ComponentPart.Name,
                Category = item.ComponentPart.Category,
                Quantity = item.Quantity,
                UnitOfMeasure = item.UnitOfMeasure,
                UnitCost = item.ComponentPart.UnitCost
            };

            parentNode.Children.Add(childNode);

            // Recursively load children for subassemblies (if not already visited to prevent cycles)
            if (item.ComponentPart.Category == PartCategory.SubAssembly && !visited.Contains(item.ComponentPart.Id))
            {
                visited.Add(item.ComponentPart.Id);
                var childBom = await _bomService.GetBomForPartAsync(item.ComponentPart.Id);
                if (childBom?.Items != null && childBom.Items.Count > 0)
                {
                    await BuildTreeNodeAsync(childNode, childBom, visited, depth + 1, maxDepth);
                }
            }
        }
    }

    /// <summary>
    /// Delete selected component from BOM
    /// </summary>
    [RelayCommand]
    public async Task DeleteSelectedAsync()
    {
        if (SelectedItem == null) return;

        if (SelectedItem.IsRoot)
        {
            MessageBox.Show("Cannot delete the root product.",
                "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedItem.BomItemId == null)
        {
            MessageBox.Show("Cannot determine BOM item to delete.",
                "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Remove '{SelectedItem.PartNumber}' from BOM?\n\nThis will remove it and all its children from the parent assembly.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removing from BOM...";

            var (success, error) = await _bomService.RemoveItemAsync(SelectedItem.BomItemId.Value);

            if (success)
            {
                await LoadBomAsync();
                StatusMessage = "Component removed";
            }
            else
            {
                MessageBox.Show(error ?? "Failed to remove from BOM", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to remove:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedProductChanged(PartType? value)
    {
        OnPropertyChanged(nameof(HasNoProduct));
    }

    /// <summary>
    /// Sort the components list by a column
    /// </summary>
    public void SortComponentsBy(string propertyName)
    {
        // Toggle direction if clicking the same column
        if (_currentSortColumn == propertyName)
        {
            _currentSortDirection = _currentSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            _currentSortColumn = propertyName;
            _currentSortDirection = ListSortDirection.Ascending;
        }

        ComponentsOnlyView.SortDescriptions.Clear();
        ComponentsOnlyView.SortDescriptions.Add(new SortDescription(propertyName, _currentSortDirection));
        ComponentsOnlyView.Refresh();
    }
}
