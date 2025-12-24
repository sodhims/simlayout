using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Tests.Utilities;
using Dapper;

namespace FactorySimulation.Tests.Repositories;

public class VariantPropertiesRepositoryTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public VariantPropertiesRepositoryTests(TestFixture fixture)
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
    public async Task Save_NewProperties_Inserts()
    {
        // Arrange
        var repository = new VariantPropertiesRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("PROP-NEW-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "PROP-NEW-PN");

        var properties = new VariantProperties
        {
            VariantId = variantId,
            LengthMm = 100.5,
            WidthMm = 50.25,
            HeightMm = 25.0,
            WeightKg = 1.5,
            ContainerType = "Box",
            UnitsPerContainer = 10,
            RequiresForklift = false,
            Notes = "Test notes"
        };

        // Act
        await repository.SaveAsync(properties);

        // Assert
        properties.Id.Should().BeGreaterThan(0);

        var saved = await repository.GetByVariantIdAsync(variantId);
        saved.Should().NotBeNull();
        saved!.LengthMm.Should().Be(100.5);
        saved.WidthMm.Should().Be(50.25);
        saved.HeightMm.Should().Be(25.0);
        saved.WeightKg.Should().Be(1.5);
        saved.ContainerType.Should().Be("Box");
        saved.UnitsPerContainer.Should().Be(10);
        saved.RequiresForklift.Should().BeFalse();
        saved.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task Save_ExistingProperties_Updates()
    {
        // Arrange
        var repository = new VariantPropertiesRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("PROP-UPD-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "PROP-UPD-PN");

        var properties = new VariantProperties
        {
            VariantId = variantId,
            LengthMm = 100.0,
            WeightKg = 1.0,
            ContainerType = "Box"
        };
        await repository.SaveAsync(properties);
        var originalId = properties.Id;

        // Act - update the properties
        properties.LengthMm = 200.0;
        properties.WeightKg = 2.5;
        properties.ContainerType = "Pallet";
        properties.RequiresForklift = true;
        await repository.SaveAsync(properties);

        // Assert
        properties.Id.Should().Be(originalId); // Same record updated

        var saved = await repository.GetByVariantIdAsync(variantId);
        saved.Should().NotBeNull();
        saved!.LengthMm.Should().Be(200.0);
        saved.WeightKg.Should().Be(2.5);
        saved.ContainerType.Should().Be("Pallet");
        saved.RequiresForklift.Should().BeTrue();
    }

    [Fact]
    public async Task GetByVariantId_NoProperties_ReturnsNull()
    {
        // Arrange
        var repository = new VariantPropertiesRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("PROP-NULL-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "PROP-NULL-PN");

        // Act
        var result = await repository.GetByVariantIdAsync(variantId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteVariant_CascadesProperties()
    {
        // Arrange
        var repository = new VariantPropertiesRepository(() => _fixture.Connection);
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("PROP-CASCADE-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "PROP-CASCADE-PN");

        var properties = new VariantProperties
        {
            VariantId = variantId,
            LengthMm = 100.0,
            WeightKg = 1.0
        };
        await repository.SaveAsync(properties);

        // Verify properties exist
        var beforeDelete = await repository.GetByVariantIdAsync(variantId);
        beforeDelete.Should().NotBeNull();

        // Act - delete the variant
        await variantRepository.DeleteAsync(variantId);

        // Assert - properties should be deleted via cascade
        var afterDelete = await repository.GetByVariantIdAsync(variantId);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task SaveFamilyDefaults_Works()
    {
        // Arrange
        var repository = new VariantPropertiesRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("DEF-FAM");

        var defaults = new FamilyDefaults
        {
            FamilyId = familyId,
            LengthMm = 500.0,
            WidthMm = 400.0,
            HeightMm = 300.0,
            WeightKg = 25.0,
            ContainerType = "Pallet",
            UnitsPerContainer = 100,
            RequiresForklift = true,
            Notes = "Family defaults"
        };

        // Act
        await repository.SaveFamilyDefaultsAsync(defaults);

        // Assert
        defaults.Id.Should().BeGreaterThan(0);

        var saved = await repository.GetFamilyDefaultsAsync(familyId);
        saved.Should().NotBeNull();
        saved!.LengthMm.Should().Be(500.0);
        saved.WidthMm.Should().Be(400.0);
        saved.HeightMm.Should().Be(300.0);
        saved.WeightKg.Should().Be(25.0);
        saved.ContainerType.Should().Be("Pallet");
        saved.UnitsPerContainer.Should().Be(100);
        saved.RequiresForklift.Should().BeTrue();
        saved.Notes.Should().Be("Family defaults");
    }

    [Fact]
    public async Task GetFamilyDefaults_ReturnsDefaults()
    {
        // Arrange
        var repository = new VariantPropertiesRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("DEF-GET-FAM");

        var defaults = new FamilyDefaults
        {
            FamilyId = familyId,
            LengthMm = 200.0,
            ContainerType = "Crate"
        };
        await repository.SaveFamilyDefaultsAsync(defaults);

        // Act
        var result = await repository.GetFamilyDefaultsAsync(familyId);

        // Assert
        result.Should().NotBeNull();
        result!.FamilyId.Should().Be(familyId);
        result.LengthMm.Should().Be(200.0);
        result.ContainerType.Should().Be("Crate");
        result.WidthMm.Should().BeNull();
        result.HeightMm.Should().BeNull();
    }
}
