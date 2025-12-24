using System.Windows;
using System.Windows.Controls;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for creating a new part family
/// </summary>
public partial class AddFamilyDialog : Window
{
    public string FamilyCode { get; private set; } = string.Empty;
    public string FamilyName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int CategoryId { get; private set; } = 2; // Default to Component

    public AddFamilyDialog()
    {
        InitializeComponent();
        FamilyCodeTextBox.Focus();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FamilyCodeTextBox.Text))
        {
            MessageBox.Show("Please enter a family code.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            FamilyCodeTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter a family name.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        FamilyCode = FamilyCodeTextBox.Text.Trim();
        FamilyName = NameTextBox.Text.Trim();
        Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
            ? null
            : DescriptionTextBox.Text.Trim();

        // Parse category ID from Tag
        var categoryTag = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "2";
        CategoryId = int.TryParse(categoryTag, out var id) ? id : 2;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
