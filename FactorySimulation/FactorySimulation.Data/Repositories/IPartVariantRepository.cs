using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for Part Variant operations
/// </summary>
public interface IPartVariantRepository
{
    Task<IReadOnlyList<PartVariant>> GetAllAsync();
    Task<PartVariant?> GetByIdAsync(int id);
    Task<PartVariant?> GetByPartNumberAsync(string partNumber);
    Task<IReadOnlyList<PartVariant>> GetByFamilyAsync(int familyId);
    Task<int> CreateAsync(PartVariant variant);
    Task UpdateAsync(PartVariant variant);
    Task DeleteAsync(int id);
}
