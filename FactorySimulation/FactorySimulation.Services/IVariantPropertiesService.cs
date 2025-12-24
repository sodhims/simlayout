using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service interface for Variant Properties business logic with inheritance
/// </summary>
public interface IVariantPropertiesService
{
    Task<VariantProperties?> GetPropertiesAsync(int variantId);
    Task<VariantProperties> GetEffectivePropertiesAsync(int variantId);
    Task SavePropertiesAsync(int variantId, VariantProperties properties);
    Task<FamilyDefaults?> GetFamilyDefaultsAsync(int familyId);
    Task SaveFamilyDefaultsAsync(int familyId, FamilyDefaults defaults);
}
