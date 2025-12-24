# Prompt 5a: Drag-Drop Family/Variant Management

Continuing Factory Configurator. Prompt 5 complete, 24 tests passing, Part Families UI working.

Objective: Add drag-drop variant movement between families. All tests must pass.

## 1. NuGet package

Add to FactorySimulation.Configurator:
- GongSolutions.WPF.DragDrop

## 2. Service additions

### IPartVariantService additions:
- Task MoveToFamilyAsync(int variantId, int newFamilyId)
- Task<bool> CanMoveToFamilyAsync(int variantId, int newFamilyId)

### PartVariantService implementation:
- CanMoveToFamilyAsync: Returns true if newFamilyId exists and is different from current
- MoveToFamilyAsync: Updates FamilyId, variant inherits new category

## 3. Tests in Tests/Services/PartVariantServiceTests.cs (add)

- CanMoveToFamily_DifferentFamily_ReturnsTrue
- CanMoveToFamily_SameFamily_ReturnsFalse
- CanMoveToFamily_InvalidFamily_ReturnsFalse
- MoveToFamily_UpdatesFamilyId
- MoveToFamily_InheritsNewCategory

## 4. Drag-drop handlers

### Create ViewModels/Handlers/VariantDropHandler.cs:

Implement IDropTarget:
- DragOver: Allow drop on Family (not on Variant or self's family)
- Drop: Call MoveToFamilyAsync, refresh lists

### Create ViewModels/Handlers/VariantDragHandler.cs:

Implement IDragSource:
- StartDrag: Set data to PartVariant
- Only variants are draggable, not families

## 5. View updates

Enable drag-drop on Families ListBox (as drop target):
```xml
<ListBox ItemsSource="{Binding Families}"
         dd:DragDrop.IsDropTarget="True"
         dd:DragDrop.DropHandler="{Binding FamilyDropHandler}">
```

Enable drag-drop on Variants ListBox (as drag source):
```xml
<ListBox ItemsSource="{Binding SelectedFamilyVariants}"
         dd:DragDrop.IsDragSource="True"
         dd:DragDrop.DragHandler="{Binding VariantDragHandler}">
```

## 6. Visual feedback

- Dragging variant: Shows "📦 [PartNumber]" adorner
- Valid drop (different family): Blue highlight
- Invalid drop (same family): Red highlight / denied cursor
- After drop: Variant list refreshes, shows in new family

## 7. Manual verification

- Create 2 families with variants
- Drag variant from Family A to Family B
- Verify variant now appears under Family B
- Verify variant's category updated to Family B's category
- Drag variant to same family - verify rejected
- Drag family (not variant) - verify not draggable

## Run Tests

Run dotnet test. All 29 tests should pass (24 + 5).
