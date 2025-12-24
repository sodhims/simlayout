using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Services;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Services;

public class VariantBomServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public VariantBomServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    private IVariantBomService CreateService()
    {
        var bomRepository = new VariantBomRepository(() => _fixture.Connection);
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);
        return new VariantBomService(bomRepository, variantRepository);
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

    private async Task<int> CreateTestVariantAsync(int familyId, string partNumber)
    {
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);
        var variant = new PartVariant
        {
            FamilyId = familyId,
            PartNumber = partNumber,
            Name = $"Variant {partNumber}"
        };
        return await variantRepository.CreateAsync(variant);
    }

    [Fact]
    public async Task CreateBom_ValidVariant_Success()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-BOM-NEW-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "SVC-BOM-NEW-PN");

        // Act
        var result = await service.CreateBomAsync(variantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Bom.Should().NotBeNull();
        result.Bom!.VariantId.Should().Be(variantId);
    }

    [Fact]
    public async Task CreateBom_InvalidVariant_Fails()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateBomAsync(999999);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
        result.Bom.Should().BeNull();
    }

    [Fact]
    public async Task CreateBom_AlreadyExists_Fails()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-BOM-DUP-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "SVC-BOM-DUP-PN");

        await service.CreateBomAsync(variantId);

        // Act - try to create another BOM for the same variant
        var result = await service.CreateBomAsync(variantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task AddItem_Valid_Success()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-ADD-ITEM-FAM");
        var parentId = await CreateTestVariantAsync(familyId, "SVC-ADD-ITEM-P");
        var componentId = await CreateTestVariantAsync(familyId, "SVC-ADD-ITEM-C");

        // Act
        var result = await service.AddItemAsync(parentId, componentId, 5, "EA");

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Item.Should().NotBeNull();
        result.Item!.ComponentVariantId.Should().Be(componentId);
        result.Item.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task AddItem_SelfReference_Fails()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-SELF-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "SVC-SELF-PN");

        // Act
        var result = await service.AddItemAsync(variantId, variantId, 1, "EA");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("cannot be a component of itself");
    }

    [Fact]
    public async Task AddItem_CircularReference_Fails()
    {
        // Arrange: A -> B, trying to add B -> A
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-CIRC-FAM");
        var variantA = await CreateTestVariantAsync(familyId, "SVC-CIRC-A");
        var variantB = await CreateTestVariantAsync(familyId, "SVC-CIRC-B");

        // A uses B
        await service.AddItemAsync(variantA, variantB, 1, "EA");

        // Act - try to make B use A
        var result = await service.AddItemAsync(variantB, variantA, 1, "EA");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("circular reference");
    }

    [Fact]
    public async Task AddItem_InvalidComponent_Fails()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-BADCOMP-FAM");
        var parentId = await CreateTestVariantAsync(familyId, "SVC-BADCOMP-P");

        // Act
        var result = await service.AddItemAsync(parentId, 999999, 1, "EA");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task AddItem_Duplicate_Fails()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-DUP-ITEM-FAM");
        var parentId = await CreateTestVariantAsync(familyId, "SVC-DUP-ITEM-P");
        var componentId = await CreateTestVariantAsync(familyId, "SVC-DUP-ITEM-C");

        await service.AddItemAsync(parentId, componentId, 1, "EA");

        // Act - try to add the same component again
        var result = await service.AddItemAsync(parentId, componentId, 2, "EA");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already in this BOM");
    }

    [Fact]
    public async Task UpdateItem_InvalidQuantity_Fails()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-BADQTY-FAM");
        var parentId = await CreateTestVariantAsync(familyId, "SVC-BADQTY-P");
        var componentId = await CreateTestVariantAsync(familyId, "SVC-BADQTY-C");

        var addResult = await service.AddItemAsync(parentId, componentId, 1, "EA");
        var item = addResult.Item!;

        // Act
        item.Quantity = 0;
        var result = await service.UpdateItemAsync(item);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task ExplodeBom_SingleLevel_Works()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-EXPLODE-FAM");
        var parentId = await CreateTestVariantAsync(familyId, "SVC-EXPLODE-P");
        var comp1Id = await CreateTestVariantAsync(familyId, "SVC-EXPLODE-C1");
        var comp2Id = await CreateTestVariantAsync(familyId, "SVC-EXPLODE-C2");

        await service.AddItemAsync(parentId, comp1Id, 2, "EA");
        await service.AddItemAsync(parentId, comp2Id, 3, "KG");

        // Act
        var lines = (await service.ExplodeBomAsync(parentId)).ToList();

        // Assert
        lines.Should().HaveCount(2);
        lines[0].Level.Should().Be(1);
        lines[0].PartNumber.Should().Be("SVC-EXPLODE-C1");
        lines[0].Quantity.Should().Be(2);
        lines[1].PartNumber.Should().Be("SVC-EXPLODE-C2");
        lines[1].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task ExplodeBom_MultiLevel_Works()
    {
        // Arrange: A -> B -> C
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-EXPLODE-ML-FAM");
        var variantA = await CreateTestVariantAsync(familyId, "SVC-EXPLODE-ML-A");
        var variantB = await CreateTestVariantAsync(familyId, "SVC-EXPLODE-ML-B");
        var variantC = await CreateTestVariantAsync(familyId, "SVC-EXPLODE-ML-C");

        // A uses 2 of B
        await service.AddItemAsync(variantA, variantB, 2, "EA");
        // B uses 3 of C
        await service.AddItemAsync(variantB, variantC, 3, "EA");

        // Act
        var lines = (await service.ExplodeBomAsync(variantA)).ToList();

        // Assert
        lines.Should().HaveCount(2);

        // Level 1: B (2 EA)
        lines[0].Level.Should().Be(1);
        lines[0].PartNumber.Should().Be("SVC-EXPLODE-ML-B");
        lines[0].Quantity.Should().Be(2);

        // Level 2: C (6 EA = 2 * 3)
        lines[1].Level.Should().Be(2);
        lines[1].PartNumber.Should().Be("SVC-EXPLODE-ML-C");
        lines[1].Quantity.Should().Be(6);
    }

    [Fact]
    public async Task ValidateAddComponent_Valid_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("SVC-VAL-OK-FAM");
        var parentId = await CreateTestVariantAsync(familyId, "SVC-VAL-OK-P");
        var componentId = await CreateTestVariantAsync(familyId, "SVC-VAL-OK-C");

        // Act
        var result = await service.ValidateAddComponentAsync(parentId, componentId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}
