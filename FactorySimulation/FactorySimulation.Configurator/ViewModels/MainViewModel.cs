using CommunityToolkit.Mvvm.ComponentModel;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Services;

namespace FactorySimulation.Configurator.ViewModels;

/// <summary>
/// Main ViewModel for the application window
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Factory Configurator";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ScenarioSelectorViewModel ScenarioSelector { get; }
    public BomViewModel BomEditor { get; }
    public VisualBomViewModel VisualBom { get; }
    public PartFamiliesViewModel PartFamilies { get; }
    public IPartImportExportService ImportExportService { get; }

    public MainViewModel()
    {
        // Create repositories
        var scenarioRepository = new ScenarioRepository();
        var partTypeRepository = new PartTypeRepository();
        var bomRepository = new BomRepository(partTypeRepository);
        var familyRepository = new PartFamilyRepository();
        var variantRepository = new PartVariantRepository();

        // Create services
        var scenarioService = new ScenarioService(scenarioRepository);
        var partTypeService = new PartTypeService(partTypeRepository);
        var bomService = new BomService(bomRepository, partTypeRepository);
        var familyService = new PartFamilyService(familyRepository);
        var variantService = new PartVariantService(variantRepository, familyRepository);
        var propertiesRepository = new VariantPropertiesRepository();
        var propertiesService = new VariantPropertiesService(propertiesRepository, variantRepository);
        var variantBomRepository = new VariantBomRepository();
        var variantBomService = new VariantBomService(variantBomRepository, variantRepository);
        var importExportService = new PartImportExportService(partTypeRepository, bomRepository);

        // Expose import/export service
        ImportExportService = importExportService;

        // Create ViewModels
        ScenarioSelector = new ScenarioSelectorViewModel(scenarioService);
        BomEditor = new BomViewModel(bomService, partTypeService);
        VisualBom = new VisualBomViewModel(bomService, partTypeService);
        PartFamilies = new PartFamiliesViewModel(familyService, variantService, propertiesService, variantBomService);

        // Wire up status messages
        ScenarioSelector.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ScenarioSelectorViewModel.StatusMessage))
            {
                StatusMessage = ScenarioSelector.StatusMessage ?? "Ready";
            }
        };

        BomEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(BomViewModel.StatusMessage))
            {
                StatusMessage = BomEditor.StatusMessage ?? "Ready";
            }
        };

        VisualBom.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(VisualBomViewModel.StatusMessage))
            {
                StatusMessage = VisualBom.StatusMessage ?? "Ready";
            }
        };
    }

    /// <summary>
    /// Initializes the application data
    /// </summary>
    public async Task InitializeAsync()
    {
        await ScenarioSelector.LoadScenariosAsync();
        await BomEditor.InitializeAsync();
        await VisualBom.InitializeAsync();
        await PartFamilies.LoadFamiliesCommand.ExecuteAsync(null);
    }
}
