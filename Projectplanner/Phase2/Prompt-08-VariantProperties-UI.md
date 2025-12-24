# Prompt 8: Variant Properties UI

Continuing Factory Configurator. Prompt 7 complete, 40 tests passing.

Objective: Add properties editing to variant detail panel. Manual verification.

## 1. ViewModel additions

### PartFamiliesViewModel additions:
- VariantProperties? CurrentProperties (effective)
- VariantProperties? LocalProperties (variant's own)
- FamilyDefaults? CurrentFamilyDefaults
- SavePropertiesCommand
- SaveFamilyDefaultsCommand

- When SelectedVariant changes: Load properties
- When SelectedFamily changes (no variant): Load family defaults

## 2. View updates

### Variant detail panel - add tabs:

#### Tab: General (existing fields)

#### Tab: Physical Properties
- LengthMm, WidthMm, HeightMm, WeightKg
- Inherited values shown in gray italic
- Local values shown in normal text

#### Tab: Handling
- ContainerType dropdown (Box, Pallet, Bin, Tote, Loose)
- UnitsPerContainer
- RequiresForklift checkbox
- Notes

### Family detail panel - add tab:

#### Tab: Default Properties
- Same fields as variant
- "These defaults apply to all variants unless overridden"

## 3. Inheritance indicator

- Field with inherited value: gray background, italic, tooltip "From family defaults"
- Field with local value: white background, normal text
- "Clear" button per field to revert to inherited

## 4. Manual verification

- Set family defaults (Weight=10, Length=200)
- Create variant with no properties
- Verify variant shows inherited values (gray)
- Edit variant Weight to 15
- Verify Weight=15 (local), Length=200 (inherited)
- Clear variant Weight
- Verify reverts to Weight=10 (inherited)

No new automated tests. Existing 40 tests should pass.
