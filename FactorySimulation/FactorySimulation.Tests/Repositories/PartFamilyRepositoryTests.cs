using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Repositories;

public class PartFamilyRepositoryTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public PartFamilyRepositoryTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateFamily_ValidData_ReturnsId()
    {
        // Arrange
        var repository = new PartFamilyRepository(() => _fixture.Connection);
        var family = new PartFamily
        {
            CategoryId = 1, // RawMaterial
            FamilyCode = "FAM-001",
            Name = "Test Family",
            Description = "A test family",
            IsActive = true
        };

        // Act
        var id = await repository.CreateAsync(family);

        // Assert
        id.Should().BeGreaterThan(0);

        // Verify it was saved
        var saved = await repository.GetByIdAsync(id);
        saved.Should().NotBeNull();
        saved!.FamilyCode.Should().Be("FAM-001");
        saved.Name.Should().Be("Test Family");
        saved.CategoryName.Should().Be("RawMaterial");
    }

    [Fact]
    public async Task CreateFamily_DuplicateFamilyCode_ThrowsException()
    {
        // Arrange
        var repository = new PartFamilyRepository(() => _fixture.Connection);
        var family1 = new PartFamily
        {
            CategoryId = 1,
            FamilyCode = "DUP-FAM",
            Name = "First Family"
        };
        var family2 = new PartFamily
        {
            CategoryId = 2,
            FamilyCode = "DUP-FAM", // Same code
            Name = "Second Family"
        };

        await repository.CreateAsync(family1);

        // Act & Assert
        var action = async () => await repository.CreateAsync(family2);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public async Task GetWithVariants_ReturnsPopulatedList()
    {
        // Arrange
        var familyRepository = new PartFamilyRepository(() => _fixture.Connection);
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);

        var family = new PartFamily
        {
            CategoryId = 2, // Component
            FamilyCode = "FAM-VARS",
            Name = "Family With Variants"
        };
        var familyId = await familyRepository.CreateAsync(family);

        // Create variants
        await variantRepository.CreateAsync(new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "VAR-001",
            Name = "Variant One"
        });
        await variantRepository.CreateAsync(new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "VAR-002",
            Name = "Variant Two"
        });

        // Act
        var result = await familyRepository.GetWithVariantsAsync(familyId);

        // Assert
        result.Should().NotBeNull();
        result!.Variants.Should().HaveCount(2);
        result.Variants.Should().Contain(v => v.PartNumber == "VAR-001");
        result.Variants.Should().Contain(v => v.PartNumber == "VAR-002");
    }

    [Fact]
    public async Task DeleteFamily_CascadesVariants()
    {
        // Arrange
        var familyRepository = new PartFamilyRepository(() => _fixture.Connection);
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);

        var family = new PartFamily
        {
            CategoryId = 3, // Subassembly
            FamilyCode = "FAM-DEL",
            Name = "Family To Delete"
        };
        var familyId = await familyRepository.CreateAsync(family);

        await variantRepository.CreateAsync(new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "DEL-001",
            Name = "Variant To Be Deleted"
        });

        // Verify variant exists
        var variantsBefore = await variantRepository.GetByFamilyAsync(familyId);
        variantsBefore.Should().HaveCount(1);

        // Act
        await familyRepository.DeleteAsync(familyId);

        // Assert - variants should be cascade deleted
        var variantsAfter = await variantRepository.GetByFamilyAsync(familyId);
        variantsAfter.Should().BeEmpty();
    }
}
