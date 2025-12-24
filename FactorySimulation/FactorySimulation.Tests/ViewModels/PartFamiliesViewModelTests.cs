using FluentAssertions;
using NSubstitute;
using FactorySimulation.Core.Models;
using FactorySimulation.Services;
using FactorySimulation.Configurator.ViewModels;

namespace FactorySimulation.Tests.ViewModels;

public class PartFamiliesViewModelTests
{
    private static List<PartFamily> CreateTestFamilies()
    {
        return new List<PartFamily>
        {
            new PartFamily
            {
                Id = 1,
                FamilyCode = "MOT-001",
                Name = "Motor Family 1",
                CategoryId = 1,
                CategoryName = "RawMaterial",
                Variants = new List<PartVariant>
                {
                    new PartVariant { Id = 1, FamilyId = 1, PartNumber = "MOT-001-A", Name = "Motor A" },
                    new PartVariant { Id = 2, FamilyId = 1, PartNumber = "MOT-001-B", Name = "Motor B" }
                }
            },
            new PartFamily
            {
                Id = 2,
                FamilyCode = "BRK-001",
                Name = "Brake Family",
                CategoryId = 2,
                CategoryName = "Component",
                Variants = new List<PartVariant>()
            },
            new PartFamily
            {
                Id = 3,
                FamilyCode = "MOT-002",
                Name = "Motor Family 2",
                CategoryId = 1,
                CategoryName = "RawMaterial",
                Variants = new List<PartVariant>()
            }
        };
    }

    [Fact]
    public async Task LoadFamilies_PopulatesFamilies()
    {
        // Arrange
        var familyService = Substitute.For<IPartFamilyService>();
        var variantService = Substitute.For<IPartVariantService>();

        var testFamilies = CreateTestFamilies();
        familyService.GetAllWithVariantsAsync().Returns(testFamilies);

        var viewModel = new PartFamiliesViewModel(familyService, variantService);

        // Act
        await viewModel.LoadFamiliesCommand.ExecuteAsync(null);

        // Assert
        viewModel.Families.Should().HaveCount(3);
    }

    [Fact]
    public async Task SelectFamily_UpdatesSelectedFamilyVariants()
    {
        // Arrange
        var familyService = Substitute.For<IPartFamilyService>();
        var variantService = Substitute.For<IPartVariantService>();

        var testFamilies = CreateTestFamilies();
        familyService.GetAllWithVariantsAsync().Returns(testFamilies);

        var viewModel = new PartFamiliesViewModel(familyService, variantService);
        await viewModel.LoadFamiliesCommand.ExecuteAsync(null);

        // Act - select family with 2 variants
        viewModel.SelectedFamily = viewModel.Families.First(f => f.FamilyCode == "MOT-001");

        // Assert
        viewModel.SelectedFamilyVariants.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddVariantCommand_WhenFamilySelected_IsEnabled()
    {
        // Arrange
        var familyService = Substitute.For<IPartFamilyService>();
        var variantService = Substitute.For<IPartVariantService>();

        var testFamilies = CreateTestFamilies();
        familyService.GetAllWithVariantsAsync().Returns(testFamilies);

        var viewModel = new PartFamiliesViewModel(familyService, variantService);
        await viewModel.LoadFamiliesCommand.ExecuteAsync(null);

        // Act
        viewModel.SelectedFamily = viewModel.Families.First();

        // Assert
        viewModel.AddVariantCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddVariantCommand_WhenNoFamilySelected_IsDisabled()
    {
        // Arrange
        var familyService = Substitute.For<IPartFamilyService>();
        var variantService = Substitute.For<IPartVariantService>();

        var viewModel = new PartFamiliesViewModel(familyService, variantService);

        // Act - ensure no family is selected
        viewModel.SelectedFamily = null;

        // Assert
        viewModel.AddVariantCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Search_FiltersVisibleFamilies()
    {
        // Arrange
        var familyService = Substitute.For<IPartFamilyService>();
        var variantService = Substitute.For<IPartVariantService>();

        var testFamilies = CreateTestFamilies(); // MOT-001, BRK-001, MOT-002
        familyService.GetAllWithVariantsAsync().Returns(testFamilies);

        var viewModel = new PartFamiliesViewModel(familyService, variantService);
        await viewModel.LoadFamiliesCommand.ExecuteAsync(null);

        // Act
        viewModel.SearchText = "MOT";

        // Assert - should filter to MOT-001 and MOT-002
        viewModel.Families.Should().HaveCount(2);
        viewModel.Families.Should().OnlyContain(f => f.FamilyCode.Contains("MOT"));
    }
}
