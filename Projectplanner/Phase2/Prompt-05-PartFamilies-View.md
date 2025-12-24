# Prompt 5: Part Families View (UI)

Continuing Factory Configurator. Prompt 4 complete, 24 tests passing.

Objective: Implement PartFamiliesView with master-detail layout. Manual verification.

## 1. View (Configurator project, Views folder)

### PartFamiliesView.xaml (UserControl):

DataContext: PartFamiliesViewModel

#### Layout: Three-column grid
- Left (250px): Family list
- Middle (250px): Variant list for selected family
- Right (flex): Detail editor

#### Left panel - Families:
- Toolbar: Search box, Add Family button
- ListBox bound to Families
- ItemTemplate: FamilyCode - Name (Category badge)
- SelectedItem bound to SelectedFamily
- Grouped by Category (optional)

#### Middle panel - Variants:
- Header: "Variants of [FamilyCode]"
- Toolbar: Add Variant button (enabled when family selected)
- ListBox bound to SelectedFamilyVariants
- ItemTemplate: PartNumber - Name
- SelectedItem bound to SelectedVariant

#### Right panel - Detail editor:
- Shows Family details when SelectedFamily != null and SelectedVariant == null
- Shows Variant details when SelectedVariant != null

##### Family fields:
- FamilyCode (TextBox)
- Name (TextBox)
- Category (ComboBox)
- Description (TextBox multiline)
- IsActive (CheckBox)

##### Variant fields:
- PartNumber (TextBox)
- Name (TextBox)
- Description (TextBox multiline)
- IsActive (CheckBox)
- Category display (read-only, inherited from family)

## 2. Dialogs

### AddFamilyDialog.xaml:
- FamilyCode input with validation
- Name input
- Category dropdown
- OK/Cancel buttons
- Validates code is unique before enabling OK

### AddVariantDialog.xaml:
- PartNumber input with validation
- Name input
- Shows parent family info (read-only)
- OK/Cancel buttons
- Validates part number is unique

## 3. Wire up in MainWindow

- Add "Part Families" tab to main TabControl
- Tab content: PartFamiliesView

## 4. Manual verification

- Run application
- Create family "MOT" (Motors) in FinishedGood category
- Create 3 variants: MOT-SM, MOT-MD, MOT-LG
- Verify variants appear under family
- Select variant, verify detail panel shows variant info
- Select family (no variant), verify detail panel shows family info
- Test search filters families
- Delete variant, verify removed from list
- Attempt duplicate family code, verify error shown

No new automated tests. Existing 24 tests should pass.
