using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for Part Family business logic
/// </summary>
public class PartFamilyService : IPartFamilyService
{
    private readonly IPartFamilyRepository _repository;

    public PartFamilyService(IPartFamilyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PartFamily>> GetAllWithVariantsAsync()
    {
        return await _repository.GetAllWithVariantsAsync();
    }

    public async Task<PartFamily> CreateFamilyAsync(string familyCode, string name, int categoryId)
    {
        // Validate code is unique
        var exists = await _repository.GetByFamilyCodeAsync(familyCode);
        if (exists != null)
        {
            throw new InvalidOperationException($"Family code '{familyCode}' already exists");
        }

        var family = new PartFamily
        {
            FamilyCode = familyCode,
            Name = name,
            CategoryId = categoryId,
            IsActive = true
        };

        var id = await _repository.CreateAsync(family);
        return (await _repository.GetByIdAsync(id))!;
    }

    public async Task<bool> ValidateFamilyCodeAsync(string familyCode)
    {
        var existing = await _repository.GetByFamilyCodeAsync(familyCode);
        return existing == null;
    }

    public async Task<IReadOnlyList<PartFamily>> SearchAsync(string searchTerm)
    {
        return await _repository.SearchAsync(searchTerm);
    }
}
