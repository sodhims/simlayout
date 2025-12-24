using FluentAssertions;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;
using FactorySimulation.Tests.Utilities;

namespace FactorySimulation.Tests.Repositories;

public class VariantBomRepositoryTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public VariantBomRepositoryTests(TestFixture fixture)
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
    public async Task SaveBom_NewBom_Inserts()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-NEW-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "BOM-NEW-PN");

        var bom = new VariantBillOfMaterials
        {
            VariantId = variantId,
            Version = 1,
            IsActive = true
        };

        // Act
        var bomId = await repository.SaveBomAsync(bom);

        // Assert
        bomId.Should().BeGreaterThan(0);

        var saved = await repository.GetByVariantIdAsync(variantId);
        saved.Should().NotBeNull();
        saved!.VariantId.Should().Be(variantId);
        saved.Version.Should().Be(1);
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SaveBom_ExistingBom_Updates()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-UPD-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "BOM-UPD-PN");

        var bom = new VariantBillOfMaterials
        {
            VariantId = variantId,
            Version = 1,
            IsActive = true
        };
        var originalId = await repository.SaveBomAsync(bom);

        // Act - update the BOM
        bom.Id = originalId;
        bom.Version = 2;
        bom.IsActive = false;
        var updatedId = await repository.SaveBomAsync(bom);

        // Assert
        updatedId.Should().Be(originalId);

        var saved = await repository.GetByVariantIdAsync(variantId);
        saved.Should().NotBeNull();
        saved!.Version.Should().Be(2);
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetByVariantId_NoBom_ReturnsNull()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-NULL-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "BOM-NULL-PN");

        // Act
        var result = await repository.GetByVariantIdAsync(variantId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddItem_Works()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-ITEM-FAM");
        var parentVariantId = await CreateTestVariantAsync(familyId, "BOM-ITEM-PARENT");
        var componentVariantId = await CreateTestVariantAsync(familyId, "BOM-ITEM-COMP");

        var bom = new VariantBillOfMaterials { VariantId = parentVariantId };
        var bomId = await repository.SaveBomAsync(bom);

        var item = new VariantBOMItem
        {
            BomId = bomId,
            ComponentVariantId = componentVariantId,
            Quantity = 5,
            UnitOfMeasure = "EA",
            Sequence = 1,
            Notes = "Test component"
        };

        // Act
        var itemId = await repository.AddItemAsync(item);

        // Assert
        itemId.Should().BeGreaterThan(0);

        var items = await repository.GetItemsAsync(bomId);
        items.Should().HaveCount(1);
        items[0].ComponentVariantId.Should().Be(componentVariantId);
        items[0].Quantity.Should().Be(5);
        items[0].UnitOfMeasure.Should().Be("EA");
        items[0].Sequence.Should().Be(1);
        items[0].Notes.Should().Be("Test component");
    }

    [Fact]
    public async Task GetWithItems_LoadsItemsWithVariants()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-LOAD-FAM");
        var parentVariantId = await CreateTestVariantAsync(familyId, "BOM-LOAD-PARENT");
        var comp1Id = await CreateTestVariantAsync(familyId, "BOM-LOAD-C1");
        var comp2Id = await CreateTestVariantAsync(familyId, "BOM-LOAD-C2");

        var bom = new VariantBillOfMaterials { VariantId = parentVariantId };
        var bomId = await repository.SaveBomAsync(bom);

        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomId,
            ComponentVariantId = comp1Id,
            Quantity = 2,
            UnitOfMeasure = "EA",
            Sequence = 1
        });
        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomId,
            ComponentVariantId = comp2Id,
            Quantity = 3,
            UnitOfMeasure = "KG",
            Sequence = 2
        });

        // Act
        var result = await repository.GetWithItemsAsync(parentVariantId);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items[0].ComponentVariant.Should().NotBeNull();
        result.Items[0].ComponentVariant!.PartNumber.Should().Be("BOM-LOAD-C1");
        result.Items[1].ComponentVariant.Should().NotBeNull();
        result.Items[1].ComponentVariant!.PartNumber.Should().Be("BOM-LOAD-C2");
    }

    [Fact]
    public async Task UpdateItem_Works()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-UPDITM-FAM");
        var parentVariantId = await CreateTestVariantAsync(familyId, "BOM-UPDITM-P");
        var componentVariantId = await CreateTestVariantAsync(familyId, "BOM-UPDITM-C");

        var bom = new VariantBillOfMaterials { VariantId = parentVariantId };
        var bomId = await repository.SaveBomAsync(bom);

        var item = new VariantBOMItem
        {
            BomId = bomId,
            ComponentVariantId = componentVariantId,
            Quantity = 5,
            UnitOfMeasure = "EA"
        };
        var itemId = await repository.AddItemAsync(item);

        // Act
        item.Id = itemId;
        item.Quantity = 10;
        item.UnitOfMeasure = "KG";
        await repository.UpdateItemAsync(item);

        // Assert
        var items = await repository.GetItemsAsync(bomId);
        items.Should().HaveCount(1);
        items[0].Quantity.Should().Be(10);
        items[0].UnitOfMeasure.Should().Be("KG");
    }

    [Fact]
    public async Task DeleteItem_Works()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-DELITM-FAM");
        var parentVariantId = await CreateTestVariantAsync(familyId, "BOM-DELITM-P");
        var componentVariantId = await CreateTestVariantAsync(familyId, "BOM-DELITM-C");

        var bom = new VariantBillOfMaterials { VariantId = parentVariantId };
        var bomId = await repository.SaveBomAsync(bom);

        var item = new VariantBOMItem
        {
            BomId = bomId,
            ComponentVariantId = componentVariantId,
            Quantity = 5
        };
        var itemId = await repository.AddItemAsync(item);

        // Act
        await repository.DeleteItemAsync(itemId);

        // Assert
        var items = await repository.GetItemsAsync(bomId);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBom_CascadesItems()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-CASCADE-FAM");
        var parentVariantId = await CreateTestVariantAsync(familyId, "BOM-CASCADE-P");
        var componentVariantId = await CreateTestVariantAsync(familyId, "BOM-CASCADE-C");

        var bom = new VariantBillOfMaterials { VariantId = parentVariantId };
        var bomId = await repository.SaveBomAsync(bom);

        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomId,
            ComponentVariantId = componentVariantId,
            Quantity = 5
        });

        // Verify items exist
        var itemsBefore = await repository.GetItemsAsync(bomId);
        itemsBefore.Should().HaveCount(1);

        // Act
        await repository.DeleteBomAsync(parentVariantId);

        // Assert
        var bomAfter = await repository.GetByVariantIdAsync(parentVariantId);
        bomAfter.Should().BeNull();

        // Items should be deleted via cascade
        var itemsAfter = await repository.GetItemsAsync(bomId);
        itemsAfter.Should().BeEmpty();
    }

    [Fact]
    public async Task WouldCreateCircularReference_SelfReference_ReturnsTrue()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-CIRC-SELF-FAM");
        var variantId = await CreateTestVariantAsync(familyId, "BOM-CIRC-SELF");

        // Act
        var result = await repository.WouldCreateCircularReferenceAsync(variantId, variantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WouldCreateCircularReference_DirectCircle_ReturnsTrue()
    {
        // Arrange: A -> B, trying to add B -> A
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-CIRC-DIR-FAM");
        var variantA = await CreateTestVariantAsync(familyId, "BOM-CIRC-A");
        var variantB = await CreateTestVariantAsync(familyId, "BOM-CIRC-B");

        // Create BOM: A uses B
        var bomA = new VariantBillOfMaterials { VariantId = variantA };
        var bomAId = await repository.SaveBomAsync(bomA);
        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomAId,
            ComponentVariantId = variantB,
            Quantity = 1
        });

        // Act - would adding A as component of B create a circle?
        var result = await repository.WouldCreateCircularReferenceAsync(variantB, variantA);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WouldCreateCircularReference_IndirectCircle_ReturnsTrue()
    {
        // Arrange: A -> B -> C, trying to add C -> A
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-CIRC-IND-FAM");
        var variantA = await CreateTestVariantAsync(familyId, "BOM-CIRC-IND-A");
        var variantB = await CreateTestVariantAsync(familyId, "BOM-CIRC-IND-B");
        var variantC = await CreateTestVariantAsync(familyId, "BOM-CIRC-IND-C");

        // Create BOM: A uses B
        var bomA = new VariantBillOfMaterials { VariantId = variantA };
        var bomAId = await repository.SaveBomAsync(bomA);
        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomAId,
            ComponentVariantId = variantB,
            Quantity = 1
        });

        // Create BOM: B uses C
        var bomB = new VariantBillOfMaterials { VariantId = variantB };
        var bomBId = await repository.SaveBomAsync(bomB);
        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomBId,
            ComponentVariantId = variantC,
            Quantity = 1
        });

        // Act - would adding A as component of C create a circle?
        var result = await repository.WouldCreateCircularReferenceAsync(variantC, variantA);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WouldCreateCircularReference_NoCircle_ReturnsFalse()
    {
        // Arrange
        var repository = new VariantBomRepository(() => _fixture.Connection);
        var familyId = await CreateTestFamilyAsync("BOM-NOCIRC-FAM");
        var variantA = await CreateTestVariantAsync(familyId, "BOM-NOCIRC-A");
        var variantB = await CreateTestVariantAsync(familyId, "BOM-NOCIRC-B");
        var variantC = await CreateTestVariantAsync(familyId, "BOM-NOCIRC-C");

        // Create BOM: A uses B
        var bomA = new VariantBillOfMaterials { VariantId = variantA };
        var bomAId = await repository.SaveBomAsync(bomA);
        await repository.AddItemAsync(new VariantBOMItem
        {
            BomId = bomAId,
            ComponentVariantId = variantB,
            Quantity = 1
        });

        // Act - adding C as component of A should be fine (no circle)
        var result = await repository.WouldCreateCircularReferenceAsync(variantA, variantC);

        // Assert
        result.Should().BeFalse();
    }
}
