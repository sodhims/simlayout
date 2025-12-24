using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Configurator.Dialogs;

/// <summary>
/// Result of the import preview dialog
/// </summary>
public enum ImportPreviewResult
{
    Accept,
    Skip,
    Cancel
}

/// <summary>
/// Node in the preview tree
/// </summary>
public class PreviewTreeNode : INotifyPropertyChanged
{
    private decimal _quantity = 1;

    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public PartCategory Category { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public bool IsRoot { get; set; }
    public bool IsNew { get; set; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<PreviewTreeNode> Children { get; } = new();

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

    public string StatusText => IsNew ? "NEW" : "EXISTS";

    public Brush StatusColor => IsNew
        ? new SolidColorBrush(Color.FromRgb(76, 175, 80))    // Green
        : new SolidColorBrush(Color.FromRgb(255, 152, 0));   // Orange

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// ViewModel for ImportPreviewDialog
/// </summary>
public class ImportPreviewViewModel : INotifyPropertyChanged
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";

    public ObservableCollection<PreviewTreeNode> PreviewTree { get; } = new();

    public int TotalParts { get; set; }
    public int NewParts { get; set; }
    public int ExistingParts { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Dialog for previewing import data before committing
/// </summary>
public partial class ImportPreviewDialog : Window
{
    public ImportPreviewResult Result { get; private set; } = ImportPreviewResult.Cancel;
    public ImportPreviewViewModel ViewModel { get; }

    public ImportPreviewDialog(ImportPreviewViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ImportPreviewResult.Accept;
        DialogResult = true;
        Close();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ImportPreviewResult.Skip;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ImportPreviewResult.Cancel;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Collects all the modified quantities from the tree
    /// </summary>
    public Dictionary<string, decimal> GetModifiedQuantities()
    {
        var result = new Dictionary<string, decimal>();
        CollectQuantities(ViewModel.PreviewTree, result);
        return result;
    }

    private void CollectQuantities(ObservableCollection<PreviewTreeNode> nodes, Dictionary<string, decimal> result)
    {
        foreach (var node in nodes)
        {
            if (!node.IsRoot)
            {
                result[node.PartNumber] = node.Quantity;
            }
            CollectQuantities(node.Children, result);
        }
    }
}
