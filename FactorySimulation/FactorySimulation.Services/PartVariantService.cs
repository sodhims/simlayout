using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for Part Variant business logic
/// </summary>
public class PartVariantService : IPartVariantService
{
    private readonly IPartVariantRepository _variantRepository;
    private readonly IPartFamilyRepository _familyRepository;

    public PartVariantService(IPartVariantRepository variantRepository, IPartFamilyRepository familyRepository)
    {
        _variantRepository = variantRepository;
        _familyRepository = familyRepository;
    }

    public async Task<IReadOnlyList<PartVariant>> GetAllAsync()
    {
        return await _variantRepository.GetAllAsync();
    }

    public async Task<PartVariant> CreateVariantAsync(int familyId, string partNumber, string name)
    {
        // Validate family exists
        var family = await _familyRepository.GetByIdAsync(familyId);
        if (family == null)
        {
            throw new InvalidOperationException($"Family with ID {familyId} does not exist");
        }

        // Validate part number is unique
        var existingVariant = await _variantRepository.GetByPartNumberAsync(partNumber);
        if (existingVariant != null)
        {
            throw new InvalidOperationException($"Part number '{partNumber}' already exists");
        }

        var variant = new PartVariant
        {
            FamilyId = familyId,
            PartNumber = partNumber,
            Name = name,
            IsActive = true
        };

        var id = await _variantRepository.CreateAsync(variant);
        return (await _variantRepository.GetByIdAsync(id))!;
    }

    public async Task<bool> ValidatePartNumberAsync(string partNumber)
    {
        var existing = await _variantRepository.GetByPartNumberAsync(partNumber);
        return existing == null;
    }

    public async Task<IReadOnlyList<PartVariant>> GetByFamilyAsync(int familyId)
    {
        return await _variantRepository.GetByFamilyAsync(familyId);
    }

    public async Task<bool> CanMoveToFamilyAsync(int variantId, int newFamilyId)
    {
        // Get variant
        var variant = await _variantRepository.GetByIdAsync(variantId);
        if (variant == null)
            return false;

        // Check if moving to same family
        if (variant.FamilyId == newFamilyId)
            return false;

        // Check if new family exists
        var newFamily = await _familyRepository.GetByIdAsync(newFamilyId);
        return newFamily != null;
    }

    public async Task MoveToFamilyAsync(int variantId, int newFamilyId)
    {
        // Validate the move
        if (!await CanMoveToFamilyAsync(variantId, newFamilyId))
        {
            throw new InvalidOperationException($"Cannot move variant {variantId} to family {newFamilyId}");
        }

        // Get the variant and update its family
        var variant = await _variantRepository.GetByIdAsync(variantId);
        variant!.FamilyId = newFamilyId;

        await _variantRepository.UpdateAsync(variant);
    }
}
