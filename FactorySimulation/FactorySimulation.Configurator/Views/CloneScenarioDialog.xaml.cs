using System.Windows;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for cloning a scenario
/// </summary>
public partial class CloneScenarioDialog : Window
{
    public string NewScenarioName { get; private set; } = string.Empty;

    public CloneScenarioDialog(string sourceName)
    {
        InitializeComponent();
        SourceNameText.Text = sourceName;
        NewNameTextBox.Text = $"{sourceName} - Copy";
        NewNameTextBox.Focus();
        NewNameTextBox.SelectAll();
    }

    private void CloneButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewNameTextBox.Text))
        {
            MessageBox.Show("Please enter a name for the new scenario.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NewNameTextBox.Focus();
            return;
        }

        NewScenarioName = NewNameTextBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
