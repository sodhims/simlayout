using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactorySimulation.Core.Models;
using FactorySimulation.Services;

namespace FactorySimulation.Configurator.ViewModels;

/// <summary>
/// ViewModel for BOM editing functionality
/// </summary>
public partial class BomViewModel : ObservableObject
{
    private readonly IBomService _bomService;
    private readonly IPartTypeService _partTypeService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPart))]
    [NotifyPropertyChangedFor(nameof(CanCreateBom))]
    [NotifyCanExecuteChangedFor(nameof(LoadBomCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateBomCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePartCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditPartCommand))]
    private PartType? _selectedPartType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBom))]
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplodeCommand))]
    private BillOfMaterials? _currentBom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowWhereUsedCommand))]
    private BOMItem? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _hasChanges;

    public ObservableCollection<BOMItem> Items { get; } = new();
    public ObservableCollection<BOMExplosionLine> ExplosionLines { get; } = new();
    public ObservableCollection<PartType> AllParts { get; } = new();

    public bool HasSelectedPart => SelectedPartType != null;
    public bool HasBom => CurrentBom != null;
    public bool CanCreateBom => HasSelectedPart && !HasBom && SelectedPartType?.CanHaveBOM == true;

    public string BomHeader => CurrentBom != null && SelectedPartType != null
        ? $"BOM for {SelectedPartType.PartNumber}"
        : "No BOM Selected";

    public string VersionDisplay => CurrentBom != null
        ? $"Version {CurrentBom.Version}"
        : "";

    public BomViewModel(IBomService bomService, IPartTypeService partTypeService)
    {
        _bomService = bomService;
        _partTypeService = partTypeService;
    }

    public async Task InitializeAsync()
    {
        await LoadAllPartsAsync();
    }

    private async Task LoadAllPartsAsync()
    {
        var parts = await _partTypeService.GetAllAsync();
        AllParts.Clear();
        foreach (var part in parts)
        {
            AllParts.Add(part);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPart))]
    public async Task LoadBomAsync()
    {
        if (SelectedPartType == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Loading BOM...";

            CurrentBom = await _bomService.GetBomForPartAsync(SelectedPartType.Id);

            Items.Clear();
            if (CurrentBom?.Items != null)
            {
                foreach (var item in CurrentBom.Items)
                {
                    Items.Add(item);
                }
            }

            OnPropertyChanged(nameof(BomHeader));
            OnPropertyChanged(nameof(VersionDisplay));
            OnPropertyChanged(nameof(CanCreateBom));

            HasChanges = false;
            StatusMessage = CurrentBom != null
                ? $"Loaded BOM with {Items.Count} item(s)"
                : "No BOM exists for this part";
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

    [RelayCommand(CanExecute = nameof(CanCreateBom))]
    public async Task CreateBomAsync()
    {
        if (SelectedPartType == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Creating BOM...";

            var (success, error, bom) = await _bomService.CreateBomAsync(SelectedPartType.Id);

            if (success && bom != null)
            {
                CurrentBom = bom;
                Items.Clear();
                OnPropertyChanged(nameof(BomHeader));
                OnPropertyChanged(nameof(VersionDisplay));
                OnPropertyChanged(nameof(CanCreateBom));
                StatusMessage = "BOM created successfully";
            }
            else
            {
                MessageBox.Show(error ?? "Failed to create BOM", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Failed to create BOM";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating BOM: {ex.Message}";
            MessageBox.Show($"Failed to create BOM:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasBom))]
    public async Task AddItemAsync()
    {
        if (CurrentBom == null || SelectedPartType == null) return;

        // Get ancestors to exclude from selection
        var ancestors = await _bomService.GetWhereUsedAsync(SelectedPartType.Id);
        var excludeIds = new HashSet<int>(ancestors.Select(a => a.Id)) { SelectedPartType.Id };

        // Get available parts (excluding current assembly and ancestors)
        var availableParts = AllParts
            .Where(p => !excludeIds.Contains(p.Id) && !Items.Any(i => i.ComponentPartTypeId == p.Id))
            .ToList();

        var dialog = new Views.PartPickerDialog(availableParts);
        if (dialog.ShowDialog() == true && dialog.SelectedPart != null)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Adding component...";

                var (success, error) = await _bomService.AddItemAsync(
                    CurrentBom.Id,
                    dialog.SelectedPart.Id,
                    dialog.Quantity,
                    dialog.UnitOfMeasure);

                if (success)
                {
                    await LoadBomAsync();
                    HasChanges = false;
                    StatusMessage = $"Added {dialog.SelectedPart.PartNumber}";
                }
                else
                {
                    MessageBox.Show(error ?? "Failed to add component", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to add component";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding component: {ex.Message}";
                MessageBox.Show($"Failed to add component:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveItem))]
    public async Task RemoveItemAsync()
    {
        if (SelectedItem == null) return;

        var result = MessageBox.Show(
            $"Remove {SelectedItem.ComponentPart?.PartNumber} from BOM?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removing component...";

            var (success, error) = await _bomService.RemoveItemAsync(SelectedItem.Id);

            if (success)
            {
                Items.Remove(SelectedItem);
                SelectedItem = null;
                StatusMessage = "Component removed";
            }
            else
            {
                MessageBox.Show(error ?? "Failed to remove component", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Failed to remove component";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing component: {ex.Message}";
            MessageBox.Show($"Failed to remove component:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRemoveItem() => SelectedItem != null && HasBom;

    [RelayCommand(CanExecute = nameof(HasBom))]
    public async Task SaveAsync()
    {
        if (CurrentBom == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Saving BOM...";

            // Update quantities for all items
            foreach (var item in Items)
            {
                await _bomService.UpdateItemAsync(item);
            }

            HasChanges = false;
            StatusMessage = "BOM saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving BOM: {ex.Message}";
            MessageBox.Show($"Failed to save BOM:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasBom))]
    public async Task ExplodeAsync()
    {
        if (SelectedPartType == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Exploding BOM...";

            var lines = await _bomService.ExplodeBomAsync(SelectedPartType.Id, 1);

            ExplosionLines.Clear();
            foreach (var line in lines)
            {
                ExplosionLines.Add(line);
            }

            // Show explosion dialog
            var dialog = new Views.BomExplosionDialog(
                SelectedPartType.PartNumber,
                ExplosionLines.ToList());
            dialog.ShowDialog();

            StatusMessage = $"Explosion complete: {ExplosionLines.Count} line(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exploding BOM: {ex.Message}";
            MessageBox.Show($"Failed to explode BOM:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowWhereUsed))]
    public async Task ShowWhereUsedAsync()
    {
        var partId = SelectedItem?.ComponentPartTypeId ?? SelectedPartType?.Id;
        if (partId == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Finding where used...";

            var parents = await _bomService.GetWhereUsedAsync(partId.Value);
            var parentList = parents.ToList();

            if (parentList.Count == 0)
            {
                MessageBox.Show("This part is not used in any assemblies.",
                    "Where Used", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var message = "This part is used in:\n\n" +
                    string.Join("\n", parentList.Select(p => $"  - {p.PartNumber}: {p.Name}"));
                MessageBox.Show(message, "Where Used", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            StatusMessage = $"Found {parentList.Count} parent assembly(ies)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to get where-used:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanShowWhereUsed() => SelectedItem != null || SelectedPartType != null;

    public void OnQuantityChanged()
    {
        HasChanges = true;
    }

    partial void OnSelectedPartTypeChanged(PartType? value)
    {
        if (value != null)
        {
            _ = LoadBomAsync();
        }
        else
        {
            CurrentBom = null;
            Items.Clear();
            OnPropertyChanged(nameof(BomHeader));
            OnPropertyChanged(nameof(VersionDisplay));
        }
    }

    /// <summary>
    /// Creates a new part type
    /// </summary>
    public async Task<(bool Success, string? Error)> CreatePartAsync(PartType partType)
    {
        var (success, error, id) = await _partTypeService.CreateAsync(partType);
        if (success)
        {
            await LoadAllPartsAsync();
        }
        return (success, error);
    }

    /// <summary>
    /// Edits the selected part type
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedPart))]
    public async Task EditPartAsync()
    {
        if (SelectedPartType == null) return;

        var dialog = new Views.AddPartDialog(SelectedPartType);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Updating part...";

                // Update the part with new values
                SelectedPartType.Name = dialog.PartName;
                SelectedPartType.Description = dialog.Description;
                SelectedPartType.Category = dialog.Category;
                SelectedPartType.UnitOfMeasure = dialog.UnitOfMeasure;

                var (success, error) = await _partTypeService.UpdateAsync(SelectedPartType);

                if (success)
                {
                    // Refresh the parts list to show updated values
                    await LoadAllPartsAsync();
                    StatusMessage = "Part updated successfully";
                }
                else
                {
                    MessageBox.Show(error ?? "Failed to update part", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to update part";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating part: {ex.Message}";
                MessageBox.Show($"Failed to update part:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Deletes the selected part type
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedPart))]
    public async Task DeletePartAsync()
    {
        if (SelectedPartType == null) return;

        // Check if part is used in any BOMs
        var whereUsed = await _bomService.GetWhereUsedAsync(SelectedPartType.Id);
        var whereUsedList = whereUsed.ToList();

        if (whereUsedList.Count > 0)
        {
            var usedIn = string.Join(", ", whereUsedList.Take(5).Select(p => p.PartNumber));
            if (whereUsedList.Count > 5)
                usedIn += $" and {whereUsedList.Count - 5} more...";

            MessageBox.Show(
                $"Cannot delete '{SelectedPartType.PartNumber}' because it is used in BOMs:\n{usedIn}",
                "Cannot Delete",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Delete part '{SelectedPartType.PartNumber}: {SelectedPartType.Name}'?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting part...";

            var (success, error) = await _partTypeService.DeleteAsync(SelectedPartType.Id);

            if (success)
            {
                AllParts.Remove(SelectedPartType);
                SelectedPartType = null;
                CurrentBom = null;
                Items.Clear();
                OnPropertyChanged(nameof(BomHeader));
                OnPropertyChanged(nameof(VersionDisplay));
                StatusMessage = "Part deleted successfully";
            }
            else
            {
                MessageBox.Show(error ?? "Failed to delete part", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Failed to delete part";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting part: {ex.Message}";
            MessageBox.Show($"Failed to delete part:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
