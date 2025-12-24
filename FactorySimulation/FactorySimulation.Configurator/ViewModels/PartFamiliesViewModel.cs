using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactorySimulation.Core.Models;
using FactorySimulation.Services;

namespace FactorySimulation.Configurator.ViewModels;

/// <summary>
/// ViewModel for managing Part Families and Variants
/// </summary>
public partial class PartFamiliesViewModel : ObservableObject
{
    private readonly IPartFamilyService _familyService;
    private readonly IPartVariantService _variantService;
    private readonly IVariantPropertiesService? _propertiesService;
    private readonly IVariantBomService? _bomService;

    private List<PartFamily> _allFamilies = new();

    [ObservableProperty]
    private ObservableCollection<PartFamily> _families = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFamilyVariants))]
    [NotifyCanExecuteChangedFor(nameof(AddVariantCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteFamilyCommand))]
    private PartFamily? _selectedFamily;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteVariantCommand))]
    [NotifyCanExecuteChangedFor(nameof(SavePropertiesCommand))]
    private PartVariant? _selectedVariant;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    // Properties support
    [ObservableProperty]
    private VariantProperties? _currentProperties;

    [ObservableProperty]
    private VariantProperties? _localProperties;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFamilyDefaultsCommand))]
    private FamilyDefaults? _currentFamilyDefaults;

    // Editable properties for variant
    [ObservableProperty]
    private double? _editLengthMm;

    [ObservableProperty]
    private double? _editWidthMm;

    [ObservableProperty]
    private double? _editHeightMm;

    [ObservableProperty]
    private double? _editWeightKg;

    [ObservableProperty]
    private string? _editContainerType;

    [ObservableProperty]
    private int? _editUnitsPerContainer;

    [ObservableProperty]
    private bool _editRequiresForklift;

    [ObservableProperty]
    private string? _editNotes;

    // Editable properties for family defaults
    [ObservableProperty]
    private double? _editDefaultLengthMm;

    [ObservableProperty]
    private double? _editDefaultWidthMm;

    [ObservableProperty]
    private double? _editDefaultHeightMm;

    [ObservableProperty]
    private double? _editDefaultWeightKg;

    [ObservableProperty]
    private string? _editDefaultContainerType;

    [ObservableProperty]
    private int? _editDefaultUnitsPerContainer;

    [ObservableProperty]
    private bool _editDefaultRequiresForklift;

    [ObservableProperty]
    private string? _editDefaultNotes;

    // BOM support
    [ObservableProperty]
    private VariantBillOfMaterials? _currentBom;

    [ObservableProperty]
    private ObservableCollection<VariantBOMItem> _bomItems = new();

    [ObservableProperty]
    private ObservableCollection<PartVariant> _availableComponents = new();

    [ObservableProperty]
    private PartVariant? _selectedComponentToAdd;

    [ObservableProperty]
    private decimal _newComponentQuantity = 1;

    [ObservableProperty]
    private string _newComponentUom = "EA";

    [ObservableProperty]
    private VariantBOMItem? _selectedBomItem;

    [ObservableProperty]
    private string? _bomErrorMessage;

    public static List<string> UnitsOfMeasure => new() { "EA", "KG", "M", "L", "PCS" };

    // Inheritance indicators (true = inherited from family)
    public bool IsLengthInherited => LocalProperties?.LengthMm == null && CurrentFamilyDefaults?.LengthMm != null;
    public bool IsWidthInherited => LocalProperties?.WidthMm == null && CurrentFamilyDefaults?.WidthMm != null;
    public bool IsHeightInherited => LocalProperties?.HeightMm == null && CurrentFamilyDefaults?.HeightMm != null;
    public bool IsWeightInherited => LocalProperties?.WeightKg == null && CurrentFamilyDefaults?.WeightKg != null;
    public bool IsContainerTypeInherited => LocalProperties?.ContainerType == null && CurrentFamilyDefaults?.ContainerType != null;
    public bool IsUnitsPerContainerInherited => LocalProperties?.UnitsPerContainer == null && CurrentFamilyDefaults?.UnitsPerContainer != null;
    public bool IsRequiresForkliftInherited => LocalProperties == null && CurrentFamilyDefaults != null;
    public bool IsNotesInherited => LocalProperties?.Notes == null && CurrentFamilyDefaults?.Notes != null;

    public static List<string> ContainerTypes => new() { "Box", "Pallet", "Bin", "Tote", "Loose" };

    public ObservableCollection<PartVariant> SelectedFamilyVariants
    {
        get
        {
            if (SelectedFamily?.Variants == null)
                return new ObservableCollection<PartVariant>();
            return new ObservableCollection<PartVariant>(SelectedFamily.Variants);
        }
    }

    public PartFamiliesViewModel(IPartFamilyService familyService, IPartVariantService variantService)
    {
        _familyService = familyService;
        _variantService = variantService;
    }

    public PartFamiliesViewModel(
        IPartFamilyService familyService,
        IPartVariantService variantService,
        IVariantPropertiesService propertiesService)
    {
        _familyService = familyService;
        _variantService = variantService;
        _propertiesService = propertiesService;
    }

    public PartFamiliesViewModel(
        IPartFamilyService familyService,
        IPartVariantService variantService,
        IVariantPropertiesService propertiesService,
        IVariantBomService bomService)
    {
        _familyService = familyService;
        _variantService = variantService;
        _propertiesService = propertiesService;
        _bomService = bomService;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedFamilyChanged(PartFamily? value)
    {
        // Clear variant selection when family changes
        SelectedVariant = null;

        // Load family defaults when family selected (no variant)
        if (value != null && _propertiesService != null)
        {
            _ = LoadFamilyDefaultsAsync(value.Id);
        }
        else
        {
            CurrentFamilyDefaults = null;
            ClearDefaultEditFields();
        }
    }

    partial void OnSelectedVariantChanged(PartVariant? value)
    {
        if (value != null && _propertiesService != null)
        {
            _ = LoadVariantPropertiesAsync(value.Id);
        }
        else
        {
            CurrentProperties = null;
            LocalProperties = null;
            ClearEditFields();
        }

        // Load BOM for selected variant
        if (value != null && _bomService != null)
        {
            _ = LoadVariantBomAsync(value.Id);
        }
        else
        {
            CurrentBom = null;
            BomItems.Clear();
        }

        NotifyInheritanceChanged();
    }

    private async Task LoadVariantPropertiesAsync(int variantId)
    {
        if (_propertiesService == null) return;

        try
        {
            // Load both effective and local properties
            CurrentProperties = await _propertiesService.GetEffectivePropertiesAsync(variantId);
            LocalProperties = await _propertiesService.GetPropertiesAsync(variantId);

            // Load family defaults for inheritance display
            if (SelectedFamily != null)
            {
                CurrentFamilyDefaults = await _propertiesService.GetFamilyDefaultsAsync(SelectedFamily.Id);
            }

            // Populate edit fields with effective values
            PopulateEditFields();
            NotifyInheritanceChanged();
        }
        catch
        {
            // Handle error silently for now
        }
    }

    private async Task LoadFamilyDefaultsAsync(int familyId)
    {
        if (_propertiesService == null) return;

        try
        {
            CurrentFamilyDefaults = await _propertiesService.GetFamilyDefaultsAsync(familyId);
            PopulateDefaultEditFields();
        }
        catch
        {
            // Handle error silently for now
        }
    }

    private void PopulateEditFields()
    {
        if (CurrentProperties == null)
        {
            ClearEditFields();
            return;
        }

        EditLengthMm = CurrentProperties.LengthMm;
        EditWidthMm = CurrentProperties.WidthMm;
        EditHeightMm = CurrentProperties.HeightMm;
        EditWeightKg = CurrentProperties.WeightKg;
        EditContainerType = CurrentProperties.ContainerType;
        EditUnitsPerContainer = CurrentProperties.UnitsPerContainer;
        EditRequiresForklift = CurrentProperties.RequiresForklift;
        EditNotes = CurrentProperties.Notes;
    }

    private void ClearEditFields()
    {
        EditLengthMm = null;
        EditWidthMm = null;
        EditHeightMm = null;
        EditWeightKg = null;
        EditContainerType = null;
        EditUnitsPerContainer = null;
        EditRequiresForklift = false;
        EditNotes = null;
    }

    private void PopulateDefaultEditFields()
    {
        if (CurrentFamilyDefaults == null)
        {
            ClearDefaultEditFields();
            return;
        }

        EditDefaultLengthMm = CurrentFamilyDefaults.LengthMm;
        EditDefaultWidthMm = CurrentFamilyDefaults.WidthMm;
        EditDefaultHeightMm = CurrentFamilyDefaults.HeightMm;
        EditDefaultWeightKg = CurrentFamilyDefaults.WeightKg;
        EditDefaultContainerType = CurrentFamilyDefaults.ContainerType;
        EditDefaultUnitsPerContainer = CurrentFamilyDefaults.UnitsPerContainer;
        EditDefaultRequiresForklift = CurrentFamilyDefaults.RequiresForklift;
        EditDefaultNotes = CurrentFamilyDefaults.Notes;
    }

    private void ClearDefaultEditFields()
    {
        EditDefaultLengthMm = null;
        EditDefaultWidthMm = null;
        EditDefaultHeightMm = null;
        EditDefaultWeightKg = null;
        EditDefaultContainerType = null;
        EditDefaultUnitsPerContainer = null;
        EditDefaultRequiresForklift = false;
        EditDefaultNotes = null;
    }

    private void NotifyInheritanceChanged()
    {
        OnPropertyChanged(nameof(IsLengthInherited));
        OnPropertyChanged(nameof(IsWidthInherited));
        OnPropertyChanged(nameof(IsHeightInherited));
        OnPropertyChanged(nameof(IsWeightInherited));
        OnPropertyChanged(nameof(IsContainerTypeInherited));
        OnPropertyChanged(nameof(IsUnitsPerContainerInherited));
        OnPropertyChanged(nameof(IsRequiresForkliftInherited));
        OnPropertyChanged(nameof(IsNotesInherited));
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Families = new ObservableCollection<PartFamily>(_allFamilies);
        }
        else
        {
            var filtered = _allFamilies
                .Where(f => f.FamilyCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Families = new ObservableCollection<PartFamily>(filtered);
        }
    }

    [RelayCommand]
    private async Task LoadFamiliesAsync()
    {
        IsLoading = true;
        try
        {
            var families = await _familyService.GetAllWithVariantsAsync();
            _allFamilies = families.ToList();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddFamilyAsync()
    {
        // In a real implementation, this would open a dialog
        // For now, we'll create a placeholder family
        var family = await _familyService.CreateFamilyAsync(
            $"FAM-{DateTime.Now.Ticks}",
            "New Family",
            1);

        _allFamilies.Add(family);
        ApplyFilter();
        SelectedFamily = family;
    }

    [RelayCommand(CanExecute = nameof(CanAddVariant))]
    private async Task AddVariantAsync()
    {
        if (SelectedFamily == null) return;

        var variant = await _variantService.CreateVariantAsync(
            SelectedFamily.Id,
            $"PN-{DateTime.Now.Ticks}",
            "New Variant");

        SelectedFamily.Variants.Add(variant);
        OnPropertyChanged(nameof(SelectedFamilyVariants));
        SelectedVariant = variant;
    }

    private bool CanAddVariant() => SelectedFamily != null;

    [RelayCommand(CanExecute = nameof(CanDeleteFamily))]
    private async Task DeleteFamilyAsync()
    {
        if (SelectedFamily == null) return;

        // In a real implementation, this would call repository.DeleteAsync
        _allFamilies.Remove(SelectedFamily);
        ApplyFilter();
        SelectedFamily = null;
        await Task.CompletedTask;
    }

    private bool CanDeleteFamily() => SelectedFamily != null;

    [RelayCommand(CanExecute = nameof(CanDeleteVariant))]
    private async Task DeleteVariantAsync()
    {
        if (SelectedFamily == null || SelectedVariant == null) return;

        // In a real implementation, this would call repository.DeleteAsync
        SelectedFamily.Variants.Remove(SelectedVariant);
        OnPropertyChanged(nameof(SelectedFamilyVariants));
        SelectedVariant = null;
        await Task.CompletedTask;
    }

    private bool CanDeleteVariant() => SelectedVariant != null;

    [RelayCommand(CanExecute = nameof(CanSaveProperties))]
    private async Task SavePropertiesAsync()
    {
        if (SelectedVariant == null || _propertiesService == null) return;

        var properties = new VariantProperties
        {
            VariantId = SelectedVariant.Id,
            LengthMm = EditLengthMm,
            WidthMm = EditWidthMm,
            HeightMm = EditHeightMm,
            WeightKg = EditWeightKg,
            ContainerType = EditContainerType,
            UnitsPerContainer = EditUnitsPerContainer,
            RequiresForklift = EditRequiresForklift,
            Notes = EditNotes
        };

        await _propertiesService.SavePropertiesAsync(SelectedVariant.Id, properties);

        // Reload to update inheritance indicators
        await LoadVariantPropertiesAsync(SelectedVariant.Id);
    }

    private bool CanSaveProperties() => SelectedVariant != null;

    [RelayCommand(CanExecute = nameof(CanSaveFamilyDefaults))]
    private async Task SaveFamilyDefaultsAsync()
    {
        if (SelectedFamily == null || _propertiesService == null) return;

        var defaults = new FamilyDefaults
        {
            FamilyId = SelectedFamily.Id,
            LengthMm = EditDefaultLengthMm,
            WidthMm = EditDefaultWidthMm,
            HeightMm = EditDefaultHeightMm,
            WeightKg = EditDefaultWeightKg,
            ContainerType = EditDefaultContainerType,
            UnitsPerContainer = EditDefaultUnitsPerContainer,
            RequiresForklift = EditDefaultRequiresForklift,
            Notes = EditDefaultNotes
        };

        await _propertiesService.SaveFamilyDefaultsAsync(SelectedFamily.Id, defaults);

        // Reload
        await LoadFamilyDefaultsAsync(SelectedFamily.Id);
    }

    private bool CanSaveFamilyDefaults() => SelectedFamily != null && SelectedVariant == null;

    // Commands to clear individual fields (revert to inherited)
    [RelayCommand]
    private void ClearLength() { EditLengthMm = null; }

    [RelayCommand]
    private void ClearWidth() { EditWidthMm = null; }

    [RelayCommand]
    private void ClearHeight() { EditHeightMm = null; }

    [RelayCommand]
    private void ClearWeight() { EditWeightKg = null; }

    [RelayCommand]
    private void ClearContainerType() { EditContainerType = null; }

    [RelayCommand]
    private void ClearUnitsPerContainer() { EditUnitsPerContainer = null; }

    [RelayCommand]
    private void ClearNotes() { EditNotes = null; }

    // BOM Methods
    private async Task LoadVariantBomAsync(int variantId)
    {
        if (_bomService == null) return;

        try
        {
            BomErrorMessage = null;
            CurrentBom = await _bomService.GetBomWithItemsAsync(variantId);
            BomItems = CurrentBom?.Items != null
                ? new ObservableCollection<VariantBOMItem>(CurrentBom.Items)
                : new ObservableCollection<VariantBOMItem>();

            // Load available components (all variants except current)
            await LoadAvailableComponentsAsync(variantId);
        }
        catch (Exception ex)
        {
            BomErrorMessage = $"Failed to load BOM: {ex.Message}";
        }
    }

    private async Task LoadAvailableComponentsAsync(int excludeVariantId)
    {
        try
        {
            var allVariants = await _variantService.GetAllAsync();
            var available = allVariants
                .Where(v => v.Id != excludeVariantId)
                .OrderBy(v => v.PartNumber)
                .ToList();
            AvailableComponents = new ObservableCollection<PartVariant>(available);
        }
        catch
        {
            AvailableComponents = new ObservableCollection<PartVariant>();
        }
    }

    [RelayCommand]
    private async Task AddBomComponentAsync()
    {
        if (SelectedVariant == null || _bomService == null || SelectedComponentToAdd == null)
            return;

        BomErrorMessage = null;

        var result = await _bomService.AddItemAsync(
            SelectedVariant.Id,
            SelectedComponentToAdd.Id,
            NewComponentQuantity,
            NewComponentUom);

        if (result.Success && result.Item != null)
        {
            BomItems.Add(result.Item);
            SelectedComponentToAdd = null;
            NewComponentQuantity = 1;
            NewComponentUom = "EA";

            // Reload to get updated BOM
            await LoadVariantBomAsync(SelectedVariant.Id);
        }
        else
        {
            BomErrorMessage = result.Error ?? "Failed to add component";
        }
    }

    [RelayCommand]
    private async Task RemoveBomItemAsync()
    {
        if (SelectedBomItem == null || _bomService == null || SelectedVariant == null)
            return;

        BomErrorMessage = null;

        var result = await _bomService.RemoveItemAsync(SelectedBomItem.Id);
        if (result.Success)
        {
            BomItems.Remove(SelectedBomItem);
            SelectedBomItem = null;
        }
        else
        {
            BomErrorMessage = result.Error ?? "Failed to remove item";
        }
    }

    [RelayCommand]
    private async Task UpdateBomItemAsync()
    {
        if (SelectedBomItem == null || _bomService == null)
            return;

        BomErrorMessage = null;

        var result = await _bomService.UpdateItemAsync(SelectedBomItem);
        if (!result.Success)
        {
            BomErrorMessage = result.Error ?? "Failed to update item";
        }
    }
}
