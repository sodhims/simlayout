using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository interface for Scenario data access
/// </summary>
public interface IScenarioRepository
{
    /// <summary>
    /// Gets all scenarios
    /// </summary>
    Task<IEnumerable<Scenario>> GetAllAsync();

    /// <summary>
    /// Gets a scenario by its ID
    /// </summary>
    Task<Scenario?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new scenario
    /// </summary>
    Task<int> CreateAsync(Scenario scenario);

    /// <summary>
    /// Updates an existing scenario
    /// </summary>
    Task<bool> UpdateAsync(Scenario scenario);

    /// <summary>
    /// Deletes a scenario by its ID
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Gets all child scenarios of a parent scenario
    /// </summary>
    Task<IEnumerable<Scenario>> GetChildrenAsync(int parentId);

    /// <summary>
    /// Checks if a scenario has any children
    /// </summary>
    Task<bool> HasChildrenAsync(int id);

    /// <summary>
    /// Checks if a scenario name already exists
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}
