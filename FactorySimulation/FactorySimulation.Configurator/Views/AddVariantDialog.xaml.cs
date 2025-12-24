using System.Windows;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for creating a new part variant
/// </summary>
public partial class AddVariantDialog : Window
{
    public string PartNumber { get; private set; } = string.Empty;
    public string VariantName { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public AddVariantDialog()
    {
        InitializeComponent();
        PartNumberTextBox.Focus();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PartNumberTextBox.Text))
        {
            MessageBox.Show("Please enter a part number.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PartNumberTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter a variant name.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        PartNumber = PartNumberTextBox.Text.Trim();
        VariantName = NameTextBox.Text.Trim();
        Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
            ? null
            : DescriptionTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
