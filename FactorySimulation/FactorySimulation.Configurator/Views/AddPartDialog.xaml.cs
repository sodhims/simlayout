using System.Windows;
using System.Windows.Controls;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for creating or editing a part type
/// </summary>
public partial class AddPartDialog : Window
{
    private readonly bool _isEditMode;

    public string PartNumber { get; private set; } = string.Empty;
    public string PartName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PartCategory Category { get; private set; } = PartCategory.Component;
    public string UnitOfMeasure { get; private set; } = "EA";

    /// <summary>
    /// Indicates if this is Add mode (true) or Edit mode (false)
    /// </summary>
    public bool IsAddMode => !_isEditMode;

    /// <summary>
    /// Constructor for Add mode
    /// </summary>
    public AddPartDialog()
    {
        InitializeComponent();
        _isEditMode = false;
        Title = "Add New Part";
        OkButton.Content = "Create";
        PartNumberTextBox.Focus();
    }

    /// <summary>
    /// Constructor for Edit mode
    /// </summary>
    public AddPartDialog(PartType partToEdit) : this()
    {
        _isEditMode = true;
        Title = "Edit Part";
        OkButton.Content = "Save";

        // Pre-populate fields with existing values
        PartNumberTextBox.Text = partToEdit.PartNumber;
        NameTextBox.Text = partToEdit.Name;
        DescriptionTextBox.Text = partToEdit.Description ?? string.Empty;

        // Set category
        var categoryIndex = partToEdit.Category switch
        {
            PartCategory.RawMaterial => 0,
            PartCategory.Component => 1,
            PartCategory.SubAssembly => 2,
            PartCategory.FinishedGood => 3,
            _ => 1
        };
        CategoryComboBox.SelectedIndex = categoryIndex;

        // Set unit of measure
        for (int i = 0; i < UnitComboBox.Items.Count; i++)
        {
            if ((UnitComboBox.Items[i] as ComboBoxItem)?.Content?.ToString() == partToEdit.UnitOfMeasure)
            {
                UnitComboBox.SelectedIndex = i;
                break;
            }
        }

        NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
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
            MessageBox.Show("Please enter a part name.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        PartNumber = PartNumberTextBox.Text.Trim();
        PartName = NameTextBox.Text.Trim();
        Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
            ? null
            : DescriptionTextBox.Text.Trim();

        // Parse category
        var categoryTag = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Component";
        Category = categoryTag switch
        {
            "RawMaterial" => PartCategory.RawMaterial,
            "SubAssembly" => PartCategory.SubAssembly,
            "FinishedGood" => PartCategory.FinishedGood,
            _ => PartCategory.Component
        };

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
