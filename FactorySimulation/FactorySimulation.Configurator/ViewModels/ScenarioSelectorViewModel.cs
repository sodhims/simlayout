using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactorySimulation.Core.Models;
using FactorySimulation.Services;

namespace FactorySimulation.Configurator.ViewModels;

/// <summary>
/// ViewModel for scenario selection and management in the toolbar
/// </summary>
public partial class ScenarioSelectorViewModel : ObservableObject
{
    private readonly ScenarioService _scenarioService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloneScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteScenarioCommand))]
    private Scenario? _selectedScenario;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<Scenario> Scenarios { get; } = new();

    public ScenarioSelectorViewModel(ScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    /// <summary>
    /// Loads all scenarios from the database
    /// </summary>
    [RelayCommand]
    public async Task LoadScenariosAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading scenarios...";

            var scenarios = await _scenarioService.GetAllAsync();

            Scenarios.Clear();
            foreach (var scenario in scenarios)
            {
                Scenarios.Add(scenario);
            }

            // Select the first scenario (Base) if available
            if (Scenarios.Count > 0 && SelectedScenario == null)
            {
                SelectedScenario = Scenarios[0];
            }

            StatusMessage = $"Loaded {Scenarios.Count} scenario(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading scenarios: {ex.Message}";
            MessageBox.Show($"Failed to load scenarios:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new standalone scenario
    /// </summary>
    [RelayCommand]
    public async Task NewScenarioAsync()
    {
        var dialog = new Views.NewScenarioDialog();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Creating scenario...";

                var (success, error, id) = await _scenarioService.CreateAsync(
                    dialog.ScenarioName,
                    dialog.ScenarioDescription,
                    dialog.ParentScenarioId);

                if (success)
                {
                    await LoadScenariosAsync();

                    // Select the newly created scenario
                    var newScenario = Scenarios.FirstOrDefault(s => s.Id == id);
                    if (newScenario != null)
                    {
                        SelectedScenario = newScenario;
                    }

                    StatusMessage = $"Created scenario '{dialog.ScenarioName}'";
                }
                else
                {
                    MessageBox.Show(error ?? "Failed to create scenario", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to create scenario";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating scenario:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error creating scenario";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Clones the selected scenario
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCloneScenario))]
    public async Task CloneScenarioAsync()
    {
        if (SelectedScenario == null) return;

        var dialog = new Views.CloneScenarioDialog(SelectedScenario.Name);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cloning scenario...";

                var (success, error, scenario) = await _scenarioService.CloneScenarioAsync(
                    SelectedScenario.Id,
                    dialog.NewScenarioName);

                if (success && scenario != null)
                {
                    await LoadScenariosAsync();

                    // Select the cloned scenario
                    var clonedScenario = Scenarios.FirstOrDefault(s => s.Id == scenario.Id);
                    if (clonedScenario != null)
                    {
                        SelectedScenario = clonedScenario;
                    }

                    StatusMessage = $"Cloned scenario as '{dialog.NewScenarioName}'";
                }
                else
                {
                    MessageBox.Show(error ?? "Failed to clone scenario", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to clone scenario";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cloning scenario:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error cloning scenario";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private bool CanCloneScenario() => SelectedScenario != null;

    /// <summary>
    /// Deletes the selected scenario
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteScenario))]
    public async Task DeleteScenarioAsync()
    {
        if (SelectedScenario == null) return;

        // Validate deletion first
        var (canDelete, validationError) = await _scenarioService.ValidateDeleteAsync(SelectedScenario.Id);
        if (!canDelete)
        {
            MessageBox.Show(validationError ?? "Cannot delete this scenario", "Cannot Delete",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Confirm deletion
        var result = MessageBox.Show(
            $"Are you sure you want to delete the scenario '{SelectedScenario.Name}'?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting scenario...";

            var scenarioName = SelectedScenario.Name;
            var (success, error) = await _scenarioService.DeleteAsync(SelectedScenario.Id);

            if (success)
            {
                await LoadScenariosAsync();
                StatusMessage = $"Deleted scenario '{scenarioName}'";
            }
            else
            {
                MessageBox.Show(error ?? "Failed to delete scenario", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Failed to delete scenario";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting scenario:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error deleting scenario";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeleteScenario() => SelectedScenario != null && SelectedScenario.CanDelete;
}
