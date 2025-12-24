using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Repositories;

public class PartVariantRepositoryTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public PartVariantRepositoryTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<int> CreateTestFamilyAsync(string familyCode, int categoryId = 1)
    {
        var familyRepository = new PartFamilyRepository(() => _fixture.Connection);
        var family = new PartFamily
        {
            CategoryId = categoryId,
            FamilyCode = familyCode,
            Name = $"Test Family {familyCode}"
        };
        return await familyRepository.CreateAsync(family);
    }

    [Fact]
    public async Task CreateVariant_ValidData_ReturnsId()
    {
        // Arrange
        var repository = new PartVariantRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("VAR-TEST-1");

        var variant = new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "PN-001",
            Name = "Test Variant",
            Description = "A test variant",
            IsActive = true
        };

        // Act
        var id = await repository.CreateAsync(variant);

        // Assert
        id.Should().BeGreaterThan(0);

        // Verify it was saved
        var saved = await repository.GetByIdAsync(id);
        saved.Should().NotBeNull();
        saved!.PartNumber.Should().Be("PN-001");
        saved.Name.Should().Be("Test Variant");
    }

    [Fact]
    public async Task CreateVariant_DuplicatePartNumber_ThrowsException()
    {
        // Arrange
        var repository = new PartVariantRepository(() => _fixture.Connection);
        var familyId1 = await CreateTestFamilyAsync("VAR-DUP-1");
        var familyId2 = await CreateTestFamilyAsync("VAR-DUP-2");

        var variant1 = new PartVariant
        {
            FamilyId = familyId1,
            PartNumber = "DUP-PN",
            Name = "First Variant"
        };
        var variant2 = new PartVariant
        {
            FamilyId = familyId2,
            PartNumber = "DUP-PN", // Same part number
            Name = "Second Variant"
        };

        await repository.CreateAsync(variant1);

        // Act & Assert
        var action = async () => await repository.CreateAsync(variant2);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public async Task GetByFamily_ReturnsOnlyFamilyVariants()
    {
        // Arrange
        var repository = new PartVariantRepository(() => _fixture.Connection);
        var familyId1 = await CreateTestFamilyAsync("FAM-FILTER-1");
        var familyId2 = await CreateTestFamilyAsync("FAM-FILTER-2");

        // Create variants in family 1
        await repository.CreateAsync(new PartVariant
        {
            FamilyId = familyId1,
            PartNumber = "F1-V1",
            Name = "Family 1 Variant 1"
        });
        await repository.CreateAsync(new PartVariant
        {
            FamilyId = familyId1,
            PartNumber = "F1-V2",
            Name = "Family 1 Variant 2"
        });

        // Create variant in family 2
        await repository.CreateAsync(new PartVariant
        {
            FamilyId = familyId2,
            PartNumber = "F2-V1",
            Name = "Family 2 Variant 1"
        });

        // Act
        var family1Variants = await repository.GetByFamilyAsync(familyId1);
        var family2Variants = await repository.GetByFamilyAsync(familyId2);

        // Assert
        family1Variants.Should().HaveCount(2);
        family1Variants.Should().OnlyContain(v => v.FamilyId == familyId1);

        family2Variants.Should().HaveCount(1);
        family2Variants.Should().OnlyContain(v => v.FamilyId == familyId2);
    }

    [Fact]
    public async Task Variant_InheritsCategoryFromFamily()
    {
        // Arrange
        var repository = new PartVariantRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("FAM-CAT-INHERIT", categoryId: 4); // FinishedGood

        await repository.CreateAsync(new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "CAT-PN",
            Name = "Category Inherited Variant"
        });

        // Act
        var variants = await repository.GetByFamilyAsync(familyId);

        // Assert
        variants.Should().HaveCount(1);
        var variant = variants.First();
        variant.CategoryId.Should().Be(4);
        variant.CategoryName.Should().Be("FinishedGood");
        variant.FamilyCode.Should().Be("FAM-CAT-INHERIT");
    }
}
