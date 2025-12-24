using System.Windows;
using System.Windows.Controls;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for selecting a part to add to a BOM
/// </summary>
public partial class PartPickerDialog : Window
{
    private readonly List<PartType> _allParts;
    private List<PartType> _filteredParts;

    public PartType? SelectedPart { get; private set; }
    public decimal Quantity { get; private set; } = 1;
    public string UnitOfMeasure { get; private set; } = "EA";

    public PartPickerDialog(IEnumerable<PartType> availableParts)
    {
        InitializeComponent();

        _allParts = availableParts.ToList();
        _filteredParts = _allParts;
        PartsListBox.ItemsSource = _filteredParts;

        SearchTextBox.Focus();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = SearchTextBox.Text.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(searchTerm))
        {
            _filteredParts = _allParts;
        }
        else
        {
            _filteredParts = _allParts
                .Where(p => p.PartNumber.ToLowerInvariant().Contains(searchTerm) ||
                           p.Name.ToLowerInvariant().Contains(searchTerm))
                .ToList();
        }

        PartsListBox.ItemsSource = _filteredParts;
    }

    private void PartsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = PartsListBox.SelectedItem != null;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (PartsListBox.SelectedItem is not PartType selectedPart)
        {
            MessageBox.Show("Please select a part.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Please enter a valid quantity greater than zero.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            QuantityTextBox.Focus();
            QuantityTextBox.SelectAll();
            return;
        }

        SelectedPart = selectedPart;
        Quantity = quantity;
        UnitOfMeasure = (UnitComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "EA";

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
