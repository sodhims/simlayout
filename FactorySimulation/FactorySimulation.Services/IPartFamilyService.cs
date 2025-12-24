using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service interface for Part Family business logic
/// </summary>
public interface IPartFamilyService
{
    Task<IReadOnlyList<PartFamily>> GetAllWithVariantsAsync();
    Task<PartFamily> CreateFamilyAsync(string familyCode, string name, int categoryId);
    Task<bool> ValidateFamilyCodeAsync(string familyCode);
    Task<IReadOnlyList<PartFamily>> SearchAsync(string searchTerm);
}
