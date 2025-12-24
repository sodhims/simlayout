using System.Windows;
using FactorySimulation.Core.Models;
using FactorySimulation.Data;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for creating a new scenario
/// </summary>
public partial class NewScenarioDialog : Window
{
    public string ScenarioName { get; private set; } = string.Empty;
    public string? ScenarioDescription { get; private set; }
    public int? ParentScenarioId { get; private set; }

    public NewScenarioDialog()
    {
        InitializeComponent();
        LoadParentScenarios();
        NameTextBox.Focus();
    }

    private async void LoadParentScenarios()
    {
        try
        {
            var repository = new ScenarioRepository();
            var scenarios = await repository.GetAllAsync();

            // Add "None" option
            ParentComboBox.Items.Add(new Scenario { Id = 0, Name = "(None - Standalone)" });

            foreach (var scenario in scenarios)
            {
                ParentComboBox.Items.Add(scenario);
            }

            ParentComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading scenarios: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter a scenario name.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        ScenarioName = NameTextBox.Text.Trim();
        ScenarioDescription = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
            ? null
            : DescriptionTextBox.Text.Trim();

        if (ParentComboBox.SelectedItem is Scenario parent && parent.Id > 0)
        {
            ParentScenarioId = parent.Id;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
