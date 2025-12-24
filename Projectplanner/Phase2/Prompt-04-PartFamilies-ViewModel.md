# Prompt 4: Part Families ViewModel

Continuing Factory Configurator. Prompt 3 complete, 19 tests passing.

Objective: Implement PartFamiliesViewModel for UI binding. All tests must pass.

## 1. NuGet package for testing

Add to FactorySimulation.Tests:
- NSubstitute (or Moq)

## 2. ViewModel (Configurator project, ViewModels folder)

### PartFamiliesViewModel : ObservableObject

Dependencies: IPartFamilyService, IPartVariantService (injected)

#### Properties:
- ObservableCollection<PartFamily> Families
- PartFamily? SelectedFamily
- PartVariant? SelectedVariant
- string SearchText (filters families)
- bool IsLoading

#### Commands:
- LoadFamiliesCommand - calls GetAllWithVariantsAsync
- AddFamilyCommand - opens dialog, creates family
- AddVariantCommand - enabled when SelectedFamily != null
- DeleteFamilyCommand - deletes SelectedFamily
- DeleteVariantCommand - deletes SelectedVariant

#### Computed:
- ObservableCollection<PartVariant> SelectedFamilyVariants - variants of SelectedFamily

## 3. Tests in Tests/ViewModels/PartFamiliesViewModelTests.cs

Use NSubstitute to mock services:

- LoadFamilies_PopulatesFamilies
  - Mock GetAllWithVariantsAsync to return 3 families.
  - Execute LoadFamiliesCommand.
  - Assert Families.Count == 3.

- SelectFamily_UpdatesSelectedFamilyVariants
  - Set SelectedFamily to family with 2 variants.
  - Assert SelectedFamilyVariants.Count == 2.

- AddVariantCommand_WhenFamilySelected_IsEnabled
  - Set SelectedFamily.
  - Assert AddVariantCommand.CanExecute() == true.

- AddVariantCommand_WhenNoFamilySelected_IsDisabled
  - SelectedFamily = null.
  - Assert AddVariantCommand.CanExecute() == false.

- Search_FiltersVisibleFamilies
  - Load 3 families: "MOT-001", "BRK-001", "MOT-002".
  - Set SearchText = "MOT".
  - Assert filtered result contains 2 families.

## Run Tests

Run dotnet test. All 24 tests should pass.
