using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for Variant Properties operations
/// </summary>
public interface IVariantPropertiesRepository
{
    Task<VariantProperties?> GetByVariantIdAsync(int variantId);
    Task SaveAsync(VariantProperties properties);
    Task<FamilyDefaults?> GetFamilyDefaultsAsync(int familyId);
    Task SaveFamilyDefaultsAsync(FamilyDefaults defaults);
}
