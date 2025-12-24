using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Services;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Services;

public class PartVariantServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public PartVariantServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    private IPartVariantService CreateService()
    {
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);
        var familyRepository = new PartFamilyRepository(() => _fixture.Connection);
        return new PartVariantService(variantRepository, familyRepository);
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
    public async Task CreateVariant_ValidData_ReturnsVariant()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("VSVC-CREATE-FAM");

        // Act
        var variant = await service.CreateVariantAsync(familyId, "VSVC-PN-001", "New Variant");

        // Assert
        variant.Should().NotBeNull();
        variant.Id.Should().BeGreaterThan(0);
        variant.PartNumber.Should().Be("VSVC-PN-001");
        variant.Name.Should().Be("New Variant");
        variant.FamilyId.Should().Be(familyId);
    }

    [Fact]
    public async Task CreateVariant_InvalidFamilyId_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var invalidFamilyId = 99999;

        // Act & Assert
        var action = async () => await service.CreateVariantAsync(invalidFamilyId, "VSVC-INV-PN", "Invalid Variant");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task CreateVariant_DuplicatePartNumber_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var familyId1 = await CreateTestFamilyAsync("VSVC-DUP-FAM1");
        var familyId2 = await CreateTestFamilyAsync("VSVC-DUP-FAM2");

        await service.CreateVariantAsync(familyId1, "VSVC-DUP-PN", "First Variant");

        // Act & Assert - try to create with same part number in different family
        var action = async () => await service.CreateVariantAsync(familyId2, "VSVC-DUP-PN", "Duplicate Variant");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateVariant_InheritsCategoryFromFamily()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("VSVC-CAT-FAM", categoryId: 4); // FinishedGood

        // Act
        var variant = await service.CreateVariantAsync(familyId, "VSVC-CAT-PN", "Category Test Variant");

        // Assert
        variant.CategoryId.Should().Be(4);
        variant.CategoryName.Should().Be("FinishedGood");
        variant.FamilyCode.Should().Be("VSVC-CAT-FAM");
    }

    [Fact]
    public async Task ValidatePartNumber_Exists_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("VSVC-VAL-FAM");
        await service.CreateVariantAsync(familyId, "VSVC-VAL-EXISTS", "Existing Variant");

        // Act
        var isValid = await service.ValidatePartNumberAsync("VSVC-VAL-EXISTS");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task CanMoveToFamily_DifferentFamily_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var familyId1 = await CreateTestFamilyAsync("MOVE-FAM-1");
        var familyId2 = await CreateTestFamilyAsync("MOVE-FAM-2");
        var variant = await service.CreateVariantAsync(familyId1, "MOVE-PN-1", "Moveable Variant");

        // Act
        var canMove = await service.CanMoveToFamilyAsync(variant.Id, familyId2);

        // Assert
        canMove.Should().BeTrue();
    }

    [Fact]
    public async Task CanMoveToFamily_SameFamily_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("MOVE-SAME-FAM");
        var variant = await service.CreateVariantAsync(familyId, "MOVE-SAME-PN", "Same Family Variant");

        // Act
        var canMove = await service.CanMoveToFamilyAsync(variant.Id, familyId);

        // Assert
        canMove.Should().BeFalse();
    }

    [Fact]
    public async Task CanMoveToFamily_InvalidFamily_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("MOVE-INV-FAM");
        var variant = await service.CreateVariantAsync(familyId, "MOVE-INV-PN", "Invalid Target Variant");
        var invalidFamilyId = 99999;

        // Act
        var canMove = await service.CanMoveToFamilyAsync(variant.Id, invalidFamilyId);

        // Assert
        canMove.Should().BeFalse();
    }

    [Fact]
    public async Task MoveToFamily_UpdatesFamilyId()
    {
        // Arrange
        var service = CreateService();
        var familyId1 = await CreateTestFamilyAsync("MOVE-UPD-FAM1");
        var familyId2 = await CreateTestFamilyAsync("MOVE-UPD-FAM2");
        var variant = await service.CreateVariantAsync(familyId1, "MOVE-UPD-PN", "Update Family Variant");

        // Act
        await service.MoveToFamilyAsync(variant.Id, familyId2);

        // Assert - get variants from new family
        var variantsInNewFamily = await service.GetByFamilyAsync(familyId2);
        variantsInNewFamily.Should().Contain(v => v.PartNumber == "MOVE-UPD-PN");

        var variantsInOldFamily = await service.GetByFamilyAsync(familyId1);
        variantsInOldFamily.Should().NotContain(v => v.PartNumber == "MOVE-UPD-PN");
    }

    [Fact]
    public async Task MoveToFamily_InheritsNewCategory()
    {
        // Arrange
        var service = CreateService();
        var familyId1 = await CreateTestFamilyAsync("MOVE-CAT-FAM1", categoryId: 1); // RawMaterial
        var familyId2 = await CreateTestFamilyAsync("MOVE-CAT-FAM2", categoryId: 4); // FinishedGood
        var variant = await service.CreateVariantAsync(familyId1, "MOVE-CAT-PN", "Category Move Variant");

        // Verify initial category
        variant.CategoryId.Should().Be(1);
        variant.CategoryName.Should().Be("RawMaterial");

        // Act
        await service.MoveToFamilyAsync(variant.Id, familyId2);

        // Assert - variant should now have new family's category
        var variantsInNewFamily = await service.GetByFamilyAsync(familyId2);
        var movedVariant = variantsInNewFamily.First(v => v.PartNumber == "MOVE-CAT-PN");
        movedVariant.CategoryId.Should().Be(4);
        movedVariant.CategoryName.Should().Be("FinishedGood");
    }
}
