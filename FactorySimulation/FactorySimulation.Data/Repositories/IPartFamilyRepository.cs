using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for Part Family operations
/// </summary>
public interface IPartFamilyRepository
{
    Task<IReadOnlyList<PartFamily>> GetAllAsync();
    Task<PartFamily?> GetByIdAsync(int id);
    Task<PartFamily?> GetByFamilyCodeAsync(string familyCode);
    Task<PartFamily?> GetWithVariantsAsync(int id);
    Task<IReadOnlyList<PartFamily>> GetAllWithVariantsAsync();
    Task<IReadOnlyList<PartFamily>> SearchAsync(string searchTerm);
    Task<int> CreateAsync(PartFamily family);
    Task UpdateAsync(PartFamily family);
    Task DeleteAsync(int id);
}
