# Prompt 12: BOM UI

Continuing Factory Configurator. Prompt 11 complete, 58 tests passing.

Objective: Implement BOM editing UI. Manual verification.

## 1. ViewModel

### BomViewModel : ObservableObject

Dependencies: IBomService, IBomRepository, IPartVariantService

#### Properties:
- SelectedVariant (bound from families view)
- CurrentBOM
- ObservableCollection<BOMItem> Items
- ObservableCollection<BOMExplosionLine> ExplosionLines

#### Commands:
- AddItemCommand
- RemoveItemCommand
- ExplodeCommand

## 2. Views

### BomEditorView.xaml:
- Header: BOM for [PartNumber]
- DataGrid: Child Part | Quantity | Unit | Remove button
- Add Component button
- Explode button

### VariantPickerDialog.xaml:
- Searchable list of variants
- Excludes: current variant, ancestors (to prevent cycles)
- Returns selected variant + quantity

### BomExplosionDialog.xaml:
- DataGrid: Level | Part Number | Name | Qty | Unit
- Indentation or level number to show hierarchy
- Read-only view

## 3. Integration

- Add BOM tab to variant detail panel
- Or separate BOM panel when variant selected

## 4. Manual verification

- Select FinishedGood variant
- Create BOM with 3 components
- Add subassembly with its own BOM
- Explode, verify quantities multiply correctly
- Test circular reference prevention (try to add parent as child)

No new automated tests. Existing 58 tests should pass.
