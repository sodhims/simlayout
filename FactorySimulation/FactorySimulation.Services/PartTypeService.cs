using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for PartType business logic
/// </summary>
public class PartTypeService : IPartTypeService
{
    private readonly IPartTypeRepository _repository;

    public PartTypeService(IPartTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PartType>> GetAllAsync(int? scenarioId = null)
    {
        return await _repository.GetAllAsync(scenarioId);
    }

    public async Task<PartType?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<PartType?> GetByPartNumberAsync(string partNumber)
    {
        return await _repository.GetByPartNumberAsync(partNumber);
    }

    public async Task<(bool Success, string? Error, int Id)> CreateAsync(PartType partType)
    {
        // Validate part number is unique
        if (await PartNumberExistsAsync(partType.PartNumber))
        {
            return (false, $"Part number '{partType.PartNumber}' already exists.", 0);
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(partType.PartNumber))
        {
            return (false, "Part number is required.", 0);
        }

        if (string.IsNullOrWhiteSpace(partType.Name))
        {
            return (false, "Part name is required.", 0);
        }

        var id = await _repository.CreateAsync(partType);
        return (true, null, id);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(PartType partType)
    {
        // Validate part number is unique (excluding current part)
        if (await PartNumberExistsAsync(partType.PartNumber, partType.Id))
        {
            return (false, $"Part number '{partType.PartNumber}' already exists.");
        }

        var success = await _repository.UpdateAsync(partType);
        return success ? (true, null) : (false, "Failed to update part type.");
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        // TODO: Add validation to check if part is used in any BOMs
        var success = await _repository.DeleteAsync(id);
        return success ? (true, null) : (false, "Failed to delete part type.");
    }

    public async Task<IEnumerable<PartType>> GetByCategory(PartCategory category)
    {
        return await _repository.GetByCategory(category);
    }

    public async Task<IEnumerable<PartType>> SearchAsync(string searchTerm)
    {
        return await _repository.SearchAsync(searchTerm);
    }

    public async Task<bool> PartNumberExistsAsync(string partNumber, int? excludeId = null)
    {
        var existing = await _repository.GetByPartNumberAsync(partNumber);
        if (existing == null) return false;
        if (excludeId.HasValue && existing.Id == excludeId.Value) return false;
        return true;
    }
}
