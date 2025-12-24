using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Services;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Services;

public class VariantPropertiesServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public VariantPropertiesServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    private IVariantPropertiesService CreateService()
    {
        var propertiesRepository = new VariantPropertiesRepository(() => _fixture.Connection);
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);
        return new VariantPropertiesService(propertiesRepository, variantRepository);
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
    public async Task GetEffectiveProperties_VariantHasAll_ReturnsVariantValues()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("EFF-ALL-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "EFF-ALL-PN");

        // Set family defaults
        await service.SaveFamilyDefaultsAsync(familyId, new FamilyDefaults
        {
            LengthMm = 100.0,
            WidthMm = 50.0,
            HeightMm = 25.0,
            WeightKg = 1.0,
            ContainerType = "Box",
            UnitsPerContainer = 10,
            RequiresForklift = false,
            Notes = "Family notes"
        });

        // Set variant properties (overrides all)
        await service.SavePropertiesAsync(variantId, new VariantProperties
        {
            LengthMm = 200.0,
            WidthMm = 100.0,
            HeightMm = 50.0,
            WeightKg = 2.0,
            ContainerType = "Crate",
            UnitsPerContainer = 5,
            RequiresForklift = true,
            Notes = "Variant notes"
        });

        // Act
        var effective = await service.GetEffectivePropertiesAsync(variantId);

        // Assert - all should be variant values
        effective.LengthMm.Should().Be(200.0);
        effective.WidthMm.Should().Be(100.0);
        effective.HeightMm.Should().Be(50.0);
        effective.WeightKg.Should().Be(2.0);
        effective.ContainerType.Should().Be("Crate");
        effective.UnitsPerContainer.Should().Be(5);
        effective.RequiresForklift.Should().BeTrue();
        effective.Notes.Should().Be("Variant notes");
    }

    [Fact]
    public async Task GetEffectiveProperties_VariantHasNone_ReturnsFamilyDefaults()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("EFF-NONE-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "EFF-NONE-PN");

        // Set family defaults only
        await service.SaveFamilyDefaultsAsync(familyId, new FamilyDefaults
        {
            LengthMm = 300.0,
            WidthMm = 200.0,
            HeightMm = 100.0,
            WeightKg = 5.0,
            ContainerType = "Pallet",
            UnitsPerContainer = 50,
            RequiresForklift = true,
            Notes = "Default notes"
        });

        // No variant properties set

        // Act
        var effective = await service.GetEffectivePropertiesAsync(variantId);

        // Assert - all should be family defaults
        effective.LengthMm.Should().Be(300.0);
        effective.WidthMm.Should().Be(200.0);
        effective.HeightMm.Should().Be(100.0);
        effective.WeightKg.Should().Be(5.0);
        effective.ContainerType.Should().Be("Pallet");
        effective.UnitsPerContainer.Should().Be(50);
        effective.RequiresForklift.Should().BeTrue();
        effective.Notes.Should().Be("Default notes");
    }

    [Fact]
    public async Task GetEffectiveProperties_PartialOverride_MergesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("EFF-PART-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "EFF-PART-PN");

        // Family: Length=100, Weight=5
        await service.SaveFamilyDefaultsAsync(familyId, new FamilyDefaults
        {
            LengthMm = 100.0,
            WeightKg = 5.0
        });

        // Variant: Weight=3 (Length null)
        await service.SavePropertiesAsync(variantId, new VariantProperties
        {
            WeightKg = 3.0
            // LengthMm is null
        });

        // Act
        var effective = await service.GetEffectivePropertiesAsync(variantId);

        // Assert - Length from family, Weight from variant
        effective.LengthMm.Should().Be(100.0);
        effective.WeightKg.Should().Be(3.0);
    }

    [Fact]
    public async Task GetEffectiveProperties_NoFamilyDefaults_ReturnsVariantOnly()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("EFF-NODEF-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "EFF-NODEF-PN");

        // No family defaults set

        // Set variant properties
        await service.SavePropertiesAsync(variantId, new VariantProperties
        {
            LengthMm = 150.0,
            WeightKg = 2.5,
            ContainerType = "Tray"
        });

        // Act
        var effective = await service.GetEffectivePropertiesAsync(variantId);

        // Assert - only variant values
        effective.LengthMm.Should().Be(150.0);
        effective.WeightKg.Should().Be(2.5);
        effective.ContainerType.Should().Be("Tray");
        effective.WidthMm.Should().BeNull();
        effective.HeightMm.Should().BeNull();
    }

    [Fact]
    public async Task GetEffectiveProperties_NothingSet_ReturnsEmptyProperties()
    {
        // Arrange
        var service = CreateService();
        var familyId = await CreateTestFamilyAsync("EFF-EMPTY-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "EFF-EMPTY-PN");

        // No family defaults, no variant properties

        // Act
        var effective = await service.GetEffectivePropertiesAsync(variantId);

        // Assert - all null/default
        effective.VariantId.Should().Be(variantId);
        effective.LengthMm.Should().BeNull();
        effective.WidthMm.Should().BeNull();
        effective.HeightMm.Should().BeNull();
        effective.WeightKg.Should().BeNull();
        effective.ContainerType.Should().BeNull();
        effective.UnitsPerContainer.Should().BeNull();
        effective.RequiresForklift.Should().BeFalse();
        effective.Notes.Should().BeNull();
    }
}
