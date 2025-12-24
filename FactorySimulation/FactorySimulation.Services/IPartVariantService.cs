using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service interface for Part Variant business logic
/// </summary>
public interface IPartVariantService
{
    Task<IReadOnlyList<PartVariant>> GetAllAsync();
    Task<PartVariant> CreateVariantAsync(int familyId, string partNumber, string name);
    Task<bool> ValidatePartNumberAsync(string partNumber);
    Task<IReadOnlyList<PartVariant>> GetByFamilyAsync(int familyId);
    Task<bool> CanMoveToFamilyAsync(int variantId, int newFamilyId);
    Task MoveToFamilyAsync(int variantId, int newFamilyId);
}
