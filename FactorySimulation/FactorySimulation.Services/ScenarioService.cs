using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for managing scenarios with business logic
/// </summary>
public class ScenarioService
{
    private readonly IScenarioRepository _repository;

    public ScenarioService(IScenarioRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all scenarios
    /// </summary>
    public Task<IEnumerable<Scenario>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    /// <summary>
    /// Gets a scenario by ID
    /// </summary>
    public Task<Scenario?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// Creates a new scenario
    /// </summary>
    public async Task<(bool Success, string? Error, int? Id)> CreateAsync(string name, string? description = null, int? parentId = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Scenario name cannot be empty", null);
        }

        // Check for duplicate name
        if (await _repository.NameExistsAsync(name))
        {
            return (false, $"A scenario named '{name}' already exists", null);
        }

        // Validate parent exists if specified
        if (parentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(parentId.Value);
            if (parent == null)
            {
                return (false, "Parent scenario not found", null);
            }
        }

        var scenario = new Scenario
        {
            Name = name,
            Description = description,
            ParentScenarioId = parentId,
            IsBase = false,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        var id = await _repository.CreateAsync(scenario);
        return (true, null, id);
    }

    /// <summary>
    /// Clones an existing scenario to create a child scenario
    /// </summary>
    public async Task<(bool Success, string? Error, Scenario? Scenario)> CloneScenarioAsync(int sourceId, string newName)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(newName))
        {
            return (false, "New scenario name cannot be empty", null);
        }

        // Check for duplicate name
        if (await _repository.NameExistsAsync(newName))
        {
            return (false, $"A scenario named '{newName}' already exists", null);
        }

        // Get source scenario
        var source = await _repository.GetByIdAsync(sourceId);
        if (source == null)
        {
            return (false, "Source scenario not found", null);
        }

        // Create new scenario as child of source
        var scenario = new Scenario
        {
            Name = newName,
            Description = $"Clone of {source.Name}",
            ParentScenarioId = sourceId,
            IsBase = false,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        var id = await _repository.CreateAsync(scenario);
        scenario.Id = id;

        return (true, null, scenario);
    }

    /// <summary>
    /// Validates if a scenario can be deleted
    /// </summary>
    public async Task<(bool CanDelete, string? Error)> ValidateDeleteAsync(int id)
    {
        var scenario = await _repository.GetByIdAsync(id);

        if (scenario == null)
        {
            return (false, "Scenario not found");
        }

        if (scenario.IsBase)
        {
            return (false, "The Base scenario cannot be deleted");
        }

        if (await _repository.HasChildrenAsync(id))
        {
            return (false, "Cannot delete a scenario that has child scenarios");
        }

        return (true, null);
    }

    /// <summary>
    /// Deletes a scenario after validation
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var (canDelete, error) = await ValidateDeleteAsync(id);
        if (!canDelete)
        {
            return (false, error);
        }

        var deleted = await _repository.DeleteAsync(id);
        return deleted
            ? (true, null)
            : (false, "Failed to delete scenario");
    }

    /// <summary>
    /// Updates a scenario's properties
    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateAsync(Scenario scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            return (false, "Scenario name cannot be empty");
        }

        var existing = await _repository.GetByIdAsync(scenario.Id);
        if (existing == null)
        {
            return (false, "Scenario not found");
        }

        if (existing.IsBase)
        {
            return (false, "The Base scenario cannot be modified");
        }

        // Check for duplicate name (excluding current scenario)
        if (await _repository.NameExistsAsync(scenario.Name, scenario.Id))
        {
            return (false, $"A scenario named '{scenario.Name}' already exists");
        }

        var updated = await _repository.UpdateAsync(scenario);
        return updated
            ? (true, null)
            : (false, "Failed to update scenario");
    }

    /// <summary>
    /// Gets child scenarios of a parent
    /// </summary>
    public Task<IEnumerable<Scenario>> GetChildrenAsync(int parentId)
    {
        return _repository.GetChildrenAsync(parentId);
    }
}
