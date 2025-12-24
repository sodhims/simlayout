using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for PartType operations
/// </summary>
public interface IPartTypeRepository
{
    Task<IEnumerable<PartType>> GetAllAsync(int? scenarioId = null);
    Task<PartType?> GetByIdAsync(int id);
    Task<PartType?> GetByPartNumberAsync(string partNumber);
    Task<int> CreateAsync(PartType partType);
    Task<bool> UpdateAsync(PartType partType);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<PartType>> GetByCategory(PartCategory category);
    Task<IEnumerable<PartType>> SearchAsync(string searchTerm);
}
