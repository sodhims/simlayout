using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for Variant Properties business logic with family default inheritance
/// </summary>
public class VariantPropertiesService : IVariantPropertiesService
{
    private readonly IVariantPropertiesRepository _propertiesRepository;
    private readonly IPartVariantRepository _variantRepository;

    public VariantPropertiesService(
        IVariantPropertiesRepository propertiesRepository,
        IPartVariantRepository variantRepository)
    {
        _propertiesRepository = propertiesRepository;
        _variantRepository = variantRepository;
    }

    public async Task<VariantProperties?> GetPropertiesAsync(int variantId)
    {
        return await _propertiesRepository.GetByVariantIdAsync(variantId);
    }

    public async Task<VariantProperties> GetEffectivePropertiesAsync(int variantId)
    {
        // Get variant to find its family
        var variant = await _variantRepository.GetByIdAsync(variantId);
        if (variant == null)
        {
            throw new InvalidOperationException($"Variant with ID {variantId} does not exist");
        }

        // Get variant's own properties
        var variantProps = await _propertiesRepository.GetByVariantIdAsync(variantId);

        // Get family defaults
        var familyDefaults = await _propertiesRepository.GetFamilyDefaultsAsync(variant.FamilyId);

        // Create effective properties by merging
        return MergeProperties(variantId, variantProps, familyDefaults);
    }

    private static VariantProperties MergeProperties(int variantId, VariantProperties? variantProps, FamilyDefaults? familyDefaults)
    {
        // Start with empty properties
        var effective = new VariantProperties { VariantId = variantId };

        // If we have family defaults, use them as base
        if (familyDefaults != null)
        {
            effective.LengthMm = familyDefaults.LengthMm;
            effective.WidthMm = familyDefaults.WidthMm;
            effective.HeightMm = familyDefaults.HeightMm;
            effective.WeightKg = familyDefaults.WeightKg;
            effective.ContainerType = familyDefaults.ContainerType;
            effective.UnitsPerContainer = familyDefaults.UnitsPerContainer;
            effective.RequiresForklift = familyDefaults.RequiresForklift;
            effective.Notes = familyDefaults.Notes;
        }

        // Override with variant-specific values where set
        if (variantProps != null)
        {
            effective.Id = variantProps.Id;

            if (variantProps.LengthMm.HasValue)
                effective.LengthMm = variantProps.LengthMm;

            if (variantProps.WidthMm.HasValue)
                effective.WidthMm = variantProps.WidthMm;

            if (variantProps.HeightMm.HasValue)
                effective.HeightMm = variantProps.HeightMm;

            if (variantProps.WeightKg.HasValue)
                effective.WeightKg = variantProps.WeightKg;

            if (variantProps.ContainerType != null)
                effective.ContainerType = variantProps.ContainerType;

            if (variantProps.UnitsPerContainer.HasValue)
                effective.UnitsPerContainer = variantProps.UnitsPerContainer;

            // RequiresForklift: variant value takes precedence if variant properties exist
            effective.RequiresForklift = variantProps.RequiresForklift;

            if (variantProps.Notes != null)
                effective.Notes = variantProps.Notes;
        }

        return effective;
    }

    public async Task SavePropertiesAsync(int variantId, VariantProperties properties)
    {
        properties.VariantId = variantId;
        await _propertiesRepository.SaveAsync(properties);
    }

    public async Task<FamilyDefaults?> GetFamilyDefaultsAsync(int familyId)
    {
        return await _propertiesRepository.GetFamilyDefaultsAsync(familyId);
    }

    public async Task SaveFamilyDefaultsAsync(int familyId, FamilyDefaults defaults)
    {
        defaults.FamilyId = familyId;
        await _propertiesRepository.SaveFamilyDefaultsAsync(defaults);
    }
}
