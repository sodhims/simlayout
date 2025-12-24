using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service interface for PartType business logic
/// </summary>
public interface IPartTypeService
{
    Task<IEnumerable<PartType>> GetAllAsync(int? scenarioId = null);
    Task<PartType?> GetByIdAsync(int id);
    Task<PartType?> GetByPartNumberAsync(string partNumber);
    Task<(bool Success, string? Error, int Id)> CreateAsync(PartType partType);
    Task<(bool Success, string? Error)> UpdateAsync(PartType partType);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<IEnumerable<PartType>> GetByCategory(PartCategory category);
    Task<IEnumerable<PartType>> SearchAsync(string searchTerm);
    Task<bool> PartNumberExistsAsync(string partNumber, int? excludeId = null);
}
