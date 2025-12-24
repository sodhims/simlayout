using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Services;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Services;

public class PartFamilyServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public PartFamilyServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    private IPartFamilyService CreateService()
    {
        var repository = new PartFamilyRepository(() => _fixture.Connection);
        return new PartFamilyService(repository);
    }

    private async Task<int> CreateTestFamilyAsync(string familyCode, int categoryId = 1)
    {
        var repository = new PartFamilyRepository(() => _fixture.Connection);
        var family = new PartFamily
        {
            CategoryId = categoryId,
            FamilyCode = familyCode,
            Name = $"Test Family {familyCode}"
        };
        return await repository.CreateAsync(family);
    }

    [Fact]
    public async Task GetAllWithVariants_ReturnsFamiliesWithVariants()
    {
        // Arrange
        var service = CreateService();
        var variantRepository = new PartVariantRepository(() => _fixture.Connection);

        var familyId = await CreateTestFamilyAsync("SVC-FAM-VARS");
        await variantRepository.CreateAsync(new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "SVC-VAR-001",
            Name = "Service Variant 1"
        });
        await variantRepository.CreateAsync(new PartVariant
        {
            FamilyId = familyId,
            PartNumber = "SVC-VAR-002",
            Name = "Service Variant 2"
        });

        // Act
        var families = await service.GetAllWithVariantsAsync();

        // Assert
        families.Should().NotBeEmpty();
        var testFamily = families.FirstOrDefault(f => f.FamilyCode == "SVC-FAM-VARS");
        testFamily.Should().NotBeNull();
        testFamily!.Variants.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateFamily_ValidData_ReturnsFamily()
    {
        // Arrange
        var service = CreateService();

        // Act
        var family = await service.CreateFamilyAsync("SVC-CREATE-1", "Created Family", 1);

        // Assert
        family.Should().NotBeNull();
        family.Id.Should().BeGreaterThan(0);
        family.FamilyCode.Should().Be("SVC-CREATE-1");
        family.Name.Should().Be("Created Family");
        family.CategoryId.Should().Be(1);
        family.CategoryName.Should().Be("RawMaterial");
    }

    [Fact]
    public async Task CreateFamily_DuplicateCode_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        await CreateTestFamilyAsync("SVC-DUP-FAM");

        // Act & Assert
        var action = async () => await service.CreateFamilyAsync("SVC-DUP-FAM", "Duplicate Family", 1);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task ValidateFamilyCode_Exists_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        await CreateTestFamilyAsync("SVC-VAL-EXISTS");

        // Act
        var isValid = await service.ValidateFamilyCodeAsync("SVC-VAL-EXISTS");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateFamilyCode_NotExists_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var isValid = await service.ValidateFamilyCodeAsync("SVC-VAL-NOTEXIST");

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task Search_MatchesCodeAndName()
    {
        // Arrange
        var service = CreateService();
        await CreateTestFamilyAsync("SEARCH-CODE-123");
        var repository = new PartFamilyRepository(() => _fixture.Connection);
        await repository.CreateAsync(new PartFamily
        {
            CategoryId = 2,
            FamilyCode = "OTHER-FAM",
            Name = "Searchable Name Here"
        });

        // Act - search by code
        var resultsByCode = await service.SearchAsync("SEARCH-CODE");

        // Act - search by name
        var resultsByName = await service.SearchAsync("Searchable");

        // Assert
        resultsByCode.Should().Contain(f => f.FamilyCode == "SEARCH-CODE-123");
        resultsByName.Should().Contain(f => f.Name == "Searchable Name Here");
    }
}
