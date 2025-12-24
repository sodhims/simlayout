# Factory Configurator - Manual Test Checklists

## How to Use This Document

After completing each prompt, run through the corresponding checklist below. 
Mark each item as ✓ (pass) or ✗ (fail). If any test fails, fix before proceeding.

---

## Prompt 1: Solution Foundation & Scenarios

### Setup Verification
- [ ] Solution opens in Visual Studio without errors
- [ ] All 4 projects present: Core, Data, Services, Configurator
- [ ] NuGet packages restored successfully
- [ ] Project references correct (Configurator → Services → Data → Core)

### Database
- [ ] factory.db file created in application directory
- [ ] Scenarios table exists with correct schema
- [ ] Base scenario (Id=1, Name='Base', IsBase=1) exists

### Application Launch
- [ ] Application starts without exceptions
- [ ] Main window appears
- [ ] Scenario dropdown visible in toolbar
- [ ] Base scenario appears in dropdown

### Scenario CRUD
- [ ] Click "New Scenario" → dialog appears
- [ ] Enter name "Test Config", click OK → scenario created
- [ ] New scenario appears in dropdown
- [ ] Select "Test Config" in dropdown → selection persists
- [ ] Click "Clone Scenario" with "Test Config" selected → creates "Test Config (Copy)"
- [ ] Clone appears in dropdown
- [ ] Select Base scenario, click "Delete" → error/warning (cannot delete Base)
- [ ] Select "Test Config (Copy)", click "Delete" → scenario deleted
- [ ] Deleted scenario no longer in dropdown

### Edge Cases
- [ ] Create scenario with empty name → validation error
- [ ] Create scenario with duplicate name → error message
- [ ] Close and reopen app → scenarios persist

---

## Prompt 2: Testing Infrastructure + Part Families Schema

### Test Project
- [ ] FactorySimulation.Tests project exists
- [ ] Project references Core, Data, Services
- [ ] Run `dotnet test` → 8 tests discovered
- [ ] All 8 tests pass (green)

### Database Schema (verify via DB browser or test)
- [ ] part_Categories table exists
- [ ] part_Families table exists  
- [ ] part_Variants table exists
- [ ] 6 categories seeded (RawMaterial, Component, Subassembly, FinishedGood, Packaging, Consumable)

### Test Isolation
- [ ] Run tests twice consecutively → both runs pass
- [ ] Tests don't affect production database

---

## Prompt 3: Part Families Service

### Tests
- [ ] Run `dotnet test` → 19 tests discovered
- [ ] All 19 tests pass

### Service Logic (verify via debugger or integration test)
- [ ] GetAllWithVariantsAsync returns families with nested variants
- [ ] CreateFamilyAsync with duplicate code throws exception
- [ ] SearchAsync finds families by code and name (case-insensitive)

---

## Prompt 4: Part Families ViewModel

### Tests
- [ ] Run `dotnet test` → 24 tests discovered
- [ ] All 24 tests pass
- [ ] NSubstitute (or Moq) package installed in test project

### ViewModel Behavior (verify via debugger)
- [ ] LoadFamiliesCommand populates Families collection
- [ ] Setting SelectedFamily updates SelectedFamilyVariants
- [ ] AddVariantCommand.CanExecute returns false when no family selected

---

## Prompt 5: Part Families View (UI)

### Tests
- [ ] Existing 24 tests still pass

### UI Layout
- [ ] Part Families tab visible in main window
- [ ] Three-column layout: Families | Variants | Detail
- [ ] Families list on left with search box above
- [ ] Variants list in middle
- [ ] Detail panel on right

### Family Operations
- [ ] Click "Add Family" → dialog opens
- [ ] Enter FamilyCode "MOT", Name "Motors", Category "FinishedGood" → click OK
- [ ] "MOT - Motors" appears in families list
- [ ] Select "MOT" → detail panel shows family info
- [ ] Edit Name to "Motor Family" → save → name updates in list
- [ ] Add second family "BRK" (Brackets)
- [ ] Search "MOT" → only Motors family visible
- [ ] Clear search → both families visible

### Variant Operations
- [ ] Select "MOT" family
- [ ] "Add Variant" button enabled
- [ ] Click "Add Variant" → dialog opens
- [ ] Enter PartNumber "MOT-SM", Name "Small Motor" → click OK
- [ ] Variant appears in middle panel
- [ ] Add "MOT-MD" (Medium) and "MOT-LG" (Large)
- [ ] All 3 variants visible under MOT family
- [ ] Select variant → detail panel shows variant info
- [ ] Category shown as inherited from family (read-only)
- [ ] Select "BRK" family → variants list clears (shows BRK variants, which is none)
- [ ] Select "MOT" family → variants reappear

### Delete Operations
- [ ] Select variant "MOT-LG" → click Delete → variant removed
- [ ] Select family "BRK" → click Delete → family removed
- [ ] Try to delete family with variants → warning or cascades

### Validation
- [ ] Try to add family with existing FamilyCode → error
- [ ] Try to add variant with existing PartNumber → error
- [ ] Empty FamilyCode → validation error

---

## Prompt 5a: Drag-Drop Family/Variant Management

### Tests
- [ ] Run `dotnet test` → 29 tests discovered
- [ ] All 29 tests pass

### Drag-Drop Setup
- [ ] GongSolutions.WPF.DragDrop package installed

### Drag Variant to Different Family
- [ ] Create family "FAM-A" with variant "VAR-1"
- [ ] Create family "FAM-B" (no variants)
- [ ] Drag "VAR-1" from FAM-A's variant list
- [ ] Drop on "FAM-B" in families list
- [ ] "VAR-1" disappears from FAM-A's variants
- [ ] Select FAM-B → "VAR-1" now appears under FAM-B
- [ ] Variant's category matches FAM-B's category

### Invalid Drops
- [ ] Drag variant over same family → red highlight/denied cursor
- [ ] Drop on same family → nothing happens
- [ ] Try to drag a family → not draggable

### Visual Feedback
- [ ] Dragging shows adorner with part number
- [ ] Valid drop target (different family) → blue highlight
- [ ] Invalid drop target → red or denied cursor

---

## Prompt 6: Variant Properties Schema & Repository

### Tests
- [ ] Run `dotnet test` → 35 tests discovered
- [ ] All 35 tests pass

### Database Schema
- [ ] part_VariantProperties table exists
- [ ] part_FamilyDefaults table exists
- [ ] Both have correct columns (LengthMm, WidthMm, etc.)

---

## Prompt 7: Variant Properties Service (Inheritance)

### Tests
- [ ] Run `dotnet test` → 40 tests discovered
- [ ] All 40 tests pass

### Inheritance Logic (verify via debugger or test)
- [ ] Variant with no properties → returns family defaults
- [ ] Variant with Weight only → Weight from variant, others from family
- [ ] Family with no defaults, variant with no properties → empty/null values

---

## Prompt 8: Variant Properties UI

### Tests
- [ ] Existing 40 tests still pass

### UI Tabs
- [ ] Variant detail panel has tabs: General, Physical Properties, Handling
- [ ] Family detail panel has tab: Default Properties

### Family Defaults
- [ ] Select family (no variant selected)
- [ ] Go to "Default Properties" tab
- [ ] Set: LengthMm=100, WeightKg=5.0, ContainerType=Box
- [ ] Save

### Variant Inheritance
- [ ] Create new variant under that family
- [ ] Select variant, go to "Physical Properties" tab
- [ ] Length shows 100 (gray/italic = inherited)
- [ ] Weight shows 5.0 (gray/italic = inherited)

### Variant Override
- [ ] Edit variant's WeightKg to 7.5 → save
- [ ] Weight shows 7.5 (normal text = local override)
- [ ] Length still shows 100 (inherited)
- [ ] Clear Weight field (or click "Clear" button if available)
- [ ] Weight reverts to 5.0 (inherited)

### Tooltip
- [ ] Hover over inherited field → tooltip shows "From family defaults"

---

## Prompt 9: BOM Schema

### Tests
- [ ] Run `dotnet test` → 42 tests discovered
- [ ] All 42 tests pass

### Database Schema
- [ ] part_BOMs table exists
- [ ] part_BOMItems table exists
- [ ] BOMItems has quantity check constraint (> 0)

---

## Prompt 10: BOM Repository

### Tests
- [ ] Run `dotnet test` → 49 tests discovered
- [ ] All 49 tests pass

---

## Prompt 11: BOM Service

### Tests
- [ ] Run `dotnet test` → 58 tests discovered
- [ ] All 58 tests pass

### Explosion Logic (verify via test or debugger)
- [ ] Create: Assembly (2x Subassembly), Subassembly (3x Component)
- [ ] Explode Assembly → Component appears with Qty=6

### Circular Detection
- [ ] A contains B, B contains C
- [ ] Try to add A to C's BOM → DetectCircularReferenceAsync returns true

---

## Prompt 12: BOM UI

### Tests
- [ ] Existing 58 tests still pass

### BOM Tab
- [ ] Select a FinishedGood variant
- [ ] BOM tab visible in detail panel (or separate panel)
- [ ] Shows BOM items or "No BOM defined"

### Create BOM
- [ ] Click "Create BOM" or add first item
- [ ] Click "Add Component"
- [ ] Variant picker dialog opens
- [ ] Search for a Component variant
- [ ] Enter Quantity=2, Unit=Each → Add
- [ ] Item appears in BOM list
- [ ] Add 2 more components

### BOM Display
- [ ] DataGrid shows: Part Number | Name | Quantity | Unit
- [ ] Edit quantity inline → save → persists

### Remove Item
- [ ] Select item → click Remove → item deleted

### Explosion
- [ ] Create subassembly with its own BOM (2-level structure)
- [ ] Add subassembly to main assembly BOM
- [ ] Click "Explode"
- [ ] Explosion dialog shows flattened list
- [ ] Level column shows 1 for direct, 2 for nested
- [ ] Quantities multiplied correctly

### Circular Prevention
- [ ] Try to add parent assembly as child of its own component
- [ ] Error message: circular reference detected
- [ ] Item not added

### Where Used
- [ ] Select a component variant
- [ ] "Where Used" shows parent assemblies

---

## Prompt 13: Workstation Schema & Repository

### Tests
- [ ] Run `dotnet test` → 63 tests discovered
- [ ] All 63 tests pass

### Database Schema
- [ ] cfg_Workstations table exists
- [ ] Foreign key to Elements table
- [ ] Test Elements seeded

---

## Prompt 14: Workstation Service & Sync

### Tests
- [ ] Run `dotnet test` → 67 tests discovered
- [ ] All 67 tests pass

### Workstations Tab
- [ ] Workstations tab visible in main window
- [ ] DataGrid shows workstations

### Sync with Layout
- [ ] Click "Sync with Layout"
- [ ] Workstations created for Elements from Stage 1
- [ ] ElementType mapped to WorkstationType correctly
  - Machine → Machining
  - Station → Manual
  - etc.

### Edit Workstation
- [ ] Select workstation
- [ ] Detail panel shows: Element (read-only), Type dropdown, Capacity
- [ ] Change Type to "Inspection" → save
- [ ] Type updates in grid

### Deleted Element Handling
- [ ] (If possible) Delete an Element in layout
- [ ] Re-sync → workstation marked IsActive=false

---

## Prompt 15: Workstation-Variant Assignments

### Tests
- [ ] Run `dotnet test` → 73 tests discovered
- [ ] All 73 tests pass

### Assigned Variants Tab
- [ ] Select workstation
- [ ] "Assigned Variants" tab in detail panel
- [ ] Shows list of assigned variants (empty initially)

### Assign Variant
- [ ] Click "Add Variant"
- [ ] Variant picker opens
- [ ] Select a variant → add
- [ ] Variant appears in assigned list
- [ ] Add 2 more variants

### Priority
- [ ] Edit priority for a variant (if editable)
- [ ] Save → priority persists

### Remove Assignment
- [ ] Select assigned variant → click Remove
- [ ] Variant removed from list

### Verify Reverse Query
- [ ] Select a variant in Part Families
- [ ] Can see which workstations it's assigned to (if UI shows this)

---

## Prompt 16: Process Times Schema & Repository

### Tests
- [ ] Run `dotnet test` → 78 tests discovered
- [ ] All 78 tests pass

### Database Schema
- [ ] cfg_ProcessTimes table exists
- [ ] Columns: ScenarioId, WorkstationId, VariantId (nullable), ProcessTimeSec, LoadTimeSec, UnloadTimeSec
- [ ] Unique constraint on (ScenarioId, WorkstationId, VariantId)

---

## Prompt 17: Process Times Service (Fallback)

### Tests
- [ ] Run `dotnet test` → 86 tests discovered
- [ ] All 86 tests pass

### Fallback Logic (verify via debugger or test)
- [ ] Create time in Base for (Workstation1, VariantA) = 30s
- [ ] Create child scenario, query (Workstation1, VariantA) → returns 30s (inherited)
- [ ] Create override in child = 20s → query returns 20s
- [ ] Query (Workstation1, VariantB) with no specific → returns default (if exists)

---

## Prompt 18: Process Times UI

### Tests
- [ ] Existing 86 tests still pass

### Process Times Tab
- [ ] Process Times tab visible in main window
- [ ] Workstation filter dropdown in toolbar
- [ ] DataGrid shows process times

### View Times
- [ ] Select a workstation in filter
- [ ] Times for that workstation displayed
- [ ] Columns: Variant (or Default) | Process | Load | Unload | Total

### Add Process Time
- [ ] Click "Add Time"
- [ ] Select variant (or leave null for default)
- [ ] Enter times: Process=30, Load=5, Unload=5
- [ ] Save → row appears in grid
- [ ] Total shows 40

### Edit Times
- [ ] Edit Process time inline → save
- [ ] Change persists on reload

### Scenario Inheritance Display
- [ ] Switch to child scenario
- [ ] Times from Base show in gray/italic (inherited)
- [ ] Edit inherited time → becomes local override (normal text)

### Copy from Base
- [ ] In child scenario, click "Copy from Base"
- [ ] Times copied to child scenario
- [ ] Times now editable as local values

---

## Prompt 19: Setup Times

### Tests
- [ ] Run `dotnet test` → 91 tests discovered
- [ ] All 91 tests pass

### Setup Times View
- [ ] Setup Times accessible (sub-tab or separate view)
- [ ] Workstation selector
- [ ] DataGrid: From Part | To Part | Setup Time

### Add Setup Time
- [ ] Click "Add"
- [ ] Select From = VariantA, To = VariantB
- [ ] Enter SetupTime = 120s
- [ ] Save → row appears

### Special Cases
- [ ] Add initial setup: From = NULL (or "Initial"), To = VariantA = 300s
- [ ] Add teardown: From = VariantA, To = NULL (or "Teardown") = 60s
- [ ] Both appear correctly in grid

### Edit/Delete
- [ ] Edit setup time inline → save
- [ ] Delete setup time → removed

---

## Prompt 20: Operators

### Tests
- [ ] Run `dotnet test` → 96 tests discovered
- [ ] All 96 tests pass

### Operators Tab
- [ ] Operators tab visible in main window
- [ ] ListView grouped by OperatorType
- [ ] Seeded types visible: Assembler, Machinist, Inspector, MaterialHandler, Supervisor

### Add Operator
- [ ] Click "Add Operator"
- [ ] Enter: Name="John Smith", EmployeeId="E001", Type=Machinist
- [ ] Save → operator appears in list under Machinist group

### Duplicate EmployeeId
- [ ] Add operator with EmployeeId="E001" (same as above)
- [ ] Error: duplicate EmployeeId

### Null EmployeeId
- [ ] Add operator with no EmployeeId → succeeds
- [ ] Add another with no EmployeeId → succeeds (NULL is allowed multiple times)

### Edit Operator
- [ ] Select operator
- [ ] Detail panel shows fields
- [ ] Change name → save → updates in list

### Delete Operator
- [ ] Select operator → delete → removed from list

---

## Prompt 21: Skills

### Tests
- [ ] Run `dotnet test` → 103 tests discovered
- [ ] All 103 tests pass

### Seeded Skills
- [ ] Skills seeded: Assembly, Machining, Inspection, ForkliftOperation, CraneOperation, SafetyCertified

### Operator Skills Tab
- [ ] Select an operator
- [ ] "Skills" tab in detail panel
- [ ] DataGrid: Skill | Level | Certified | Expires

### Add Skill
- [ ] Click "Add Skill"
- [ ] Skill picker opens
- [ ] Select "Machining", Level=3
- [ ] Optionally set certification dates
- [ ] Save → skill appears in list

### Duplicate Skill
- [ ] Try to add same skill again → error

### Edit Skill Level
- [ ] Change level from 3 to 4 → save → persists

### Remove Skill
- [ ] Select skill → remove → deleted

### Expiring Certifications
- [ ] Add skill with expiration date = 15 days from now
- [ ] (If UI shows warnings) Expiring certification warning appears
- [ ] Or verify via GetExpiringCertificationsAsync

---

## Prompt 22: Workstation Skill Requirements

### Tests
- [ ] Run `dotnet test` → 110 tests discovered
- [ ] All 110 tests pass

### Required Skills Tab
- [ ] Select a workstation
- [ ] "Required Skills" tab in detail panel
- [ ] DataGrid: Skill | Min Level

### Add Required Skill
- [ ] Click "Add"
- [ ] Select skill "Machining", MinLevel=3
- [ ] Save → requirement appears

### Qualified Operators Display
- [ ] Info shows "X operators qualified"
- [ ] Create operator with Machining level 4 → count increases
- [ ] Create operator with Machining level 2 → count doesn't increase (below minimum)

### Remove Requirement
- [ ] Remove skill requirement → qualified count updates

### Validation
- [ ] (Via service) ValidateOperatorForWorkstation returns gaps if operator missing skill

---

## Prompt 23: Transport Equipment

### Tests
- [ ] Run `dotnet test` → 116 tests discovered
- [ ] All 116 tests pass

### Transport Tab
- [ ] Transport tab visible in main window
- [ ] ListView grouped by TransportType
- [ ] Seeded types: Forklift, AGV, OverheadCrane, PalletJack

### Add Equipment
- [ ] Click "Add Equipment"
- [ ] Enter: Name="Forklift 1", AssetId="FL-001", Type=Forklift
- [ ] Select Home Location (from Elements)
- [ ] Save → equipment appears in list

### Duplicate AssetId
- [ ] Add equipment with AssetId="FL-001" → error

### Status
- [ ] Change status to "Maintenance" → save → persists
- [ ] Available equipment query excludes this one

### Parameters Tab
- [ ] Select equipment
- [ ] "Parameters" tab shows: Speed, Loaded Speed, Capacity, Load Time, Unload Time
- [ ] Enter values → save → persist

### Delete Equipment
- [ ] Delete equipment → removed
- [ ] Parameters cascade deleted

---

## Prompt 24: Routing Schema

### Tests
- [ ] Run `dotnet test` → 122 tests discovered
- [ ] All 122 tests pass

### Database Schema
- [ ] cfg_Routings table exists
- [ ] cfg_RoutingNodes table exists (with PositionX, PositionY)
- [ ] cfg_RoutingConnections table exists

---

## Prompt 25: Routing Service

### Tests
- [ ] Run `dotnet test` → 133 tests discovered
- [ ] All 133 tests pass

### Service Logic (verify via test or debugger)
- [ ] CreateRoutingAsync creates routing with Start and End nodes
- [ ] AddNodeAsync adds node at specified position
- [ ] ConnectNodesAsync creates connection
- [ ] ValidateRoutingAsync returns false if no Start node
- [ ] CalculateTotalTimeAsync sums process + transport times

---

## Prompt 26: Visual Routing Canvas - Setup

### Tests
- [ ] Existing 133 tests still pass

### NuGet Packages
- [ ] NodeNetwork package installed
- [ ] DynamicData package installed

### Routing Tab
- [ ] Routing tab visible in main window
- [ ] Canvas area visible
- [ ] Variant selector dropdown present

### Initial Routing
- [ ] Select a variant that has no routing
- [ ] Default routing created with Start and End nodes
- [ ] Start node (green) visible on left
- [ ] End node (red) visible on right

### Canvas Navigation
- [ ] Mouse wheel zooms in/out
- [ ] Middle mouse drag pans canvas
- [ ] Nodes are selectable (click)

---

## Prompt 27: Visual Routing Canvas - Node Palette & Drag-Drop

### Tests
- [ ] Existing 133 tests still pass

### Node Palette
- [ ] Palette panel on left of canvas
- [ ] Node types visible: Start, End, Process, Inspect, Assemble, Package, Decision
- [ ] Start disabled if routing already has Start node

### Drag from Palette
- [ ] Drag Process node from palette
- [ ] Drop on canvas
- [ ] Process node appears at drop location
- [ ] Node shows "[Click to assign]" for workstation

### Connect Nodes
- [ ] Drag from Start's output port
- [ ] Rubber band line follows cursor
- [ ] Drop on Process node's input port
- [ ] Connection line created
- [ ] Transport dialog opens

### Transport Configuration
- [ ] Select transport type: AGV
- [ ] Enter time: 30 seconds
- [ ] OK → connection shows transport info

### Node Context Menu
- [ ] Right-click on Process node
- [ ] Menu shows: Assign Workstation, Delete Node
- [ ] Click "Assign Workstation" → picker opens
- [ ] Select workstation → node updates to show name

### Delete Operations
- [ ] Select connection → press Delete → connection removed
- [ ] Select node → press Delete → node and its connections removed

---

## Prompt 28: Visual Routing Canvas - Workstation & Time Display

### Tests
- [ ] Existing 133 tests still pass

### Workstation Picker
- [ ] Right-click Process node → Assign Workstation
- [ ] Dialog shows workstation list
- [ ] Can search/filter
- [ ] Select and confirm

### Node Display with Workstation
- [ ] After assignment, node shows:
  - Workstation name
  - Process time (from cfg_ProcessTimes)
  - Load/Unload time (optional)
- [ ] If no process time defined, shows "--" or warning

### Connection Display
- [ ] Connection shows transport icon and time
- [ ] Or tooltip on hover shows details

### Time Summary
- [ ] Footer/panel shows:
  - Total process time
  - Total transport time
  - Total cycle time
- [ ] Values update as nodes/connections added

### Validation Warnings
- [ ] Node without workstation: orange border
- [ ] Connection without transport time: dashed line
- [ ] Unreachable node: red border

### Scenario Change
- [ ] Change scenario in main toolbar
- [ ] Times on nodes update to reflect scenario's process times
- [ ] Inherited times shown differently (gray/italic)

---

## Prompt 29: Visual Routing Canvas - Save & Polish

### Tests
- [ ] Existing 133 tests still pass

### Auto-Save
- [ ] Move a node
- [ ] Close app
- [ ] Reopen → node in new position

### Manual Operations
- [ ] Toolbar has: Zoom slider, Fit to View, Validate, Clone, Delete
- [ ] Click "Fit to View" → canvas zooms to show all nodes

### Validate
- [ ] Click "Validate"
- [ ] If valid: ✓ Valid message
- [ ] If invalid: shows errors/warnings

### Clone Routing
- [ ] Click "Clone to Scenario"
- [ ] Select target scenario
- [ ] Clone created
- [ ] Switch to target scenario → routing appears
- [ ] Modify clone → original unchanged

### Delete Routing
- [ ] Click "Delete Routing"
- [ ] Confirmation dialog
- [ ] Confirm → routing deleted
- [ ] Canvas shows empty or default Start/End

### Visual Polish
- [ ] Grid background visible
- [ ] Node hover effect (subtle scale)
- [ ] Selected node has highlight/glow
- [ ] Connections are smooth curves

### Keyboard Shortcuts
- [ ] Delete key removes selected
- [ ] Escape deselects
- [ ] Ctrl+A selects all (if implemented)

### Complete Routing Flow Test
- [ ] Create routing: Start → Process → Inspect → Package → End
- [ ] Assign workstations to all process nodes
- [ ] Add transport between all nodes
- [ ] Validate → should pass
- [ ] Total time calculated correctly
- [ ] Save, close, reopen → routing preserved

---

## Prompt 30: Validation Service

### Tests
- [ ] Run `dotnet test` → 140 tests discovered
- [ ] All 140 tests pass

### Validation Checks (verify by creating conditions)

#### Parts Validation
- [ ] Create variant with no workstation assignment → warning
- [ ] Create FinishedGood variant with no BOM → warning
- [ ] Create FinishedGood variant with no routing → warning

#### Workstation Validation
- [ ] Create workstation with no process times → warning
- [ ] Create workstation with required skills but no qualified operators → warning

#### Routing Validation
- [ ] Create routing without Start node → error
- [ ] Create routing with unreachable node → error
- [ ] Create routing step without workstation → warning
- [ ] Create routing connection without transport time → warning

#### Operator Validation
- [ ] Create operator with skill expiring in 15 days → warning

#### Transport Validation
- [ ] Delete all transport equipment → warning (no available transport)

---

## Prompt 31: Dashboard & Summary

### Tests
- [ ] Run `dotnet test` → 143 tests discovered
- [ ] All 143 tests pass

### Dashboard Tab
- [ ] Dashboard is first tab (home)
- [ ] Opens by default when app starts

### Summary Cards
- [ ] Cards show counts: Families, Variants, Workstations, Operators, Transport
- [ ] Counts are accurate

### Card Navigation
- [ ] Click "Families" card → navigates to Part Families tab
- [ ] Click "Workstations" card → navigates to Workstations tab
- [ ] etc. for all cards

### Routing Coverage
- [ ] Shows percentage: "X% of finished goods have routings"
- [ ] Bar visualization (if implemented)

### Validation Section
- [ ] "Validate Factory" button visible
- [ ] Click button → validation runs
- [ ] Status shows: ✓ Valid, ⚠ N Warnings, or ✗ N Errors
- [ ] Last validated timestamp shown

### Issues List
- [ ] After validation, issues displayed in list
- [ ] Columns: Severity, Category, Entity, Message
- [ ] Filter buttons: All, Errors, Warnings

### Issue Navigation
- [ ] Double-click an issue → navigates to that entity
- [ ] Entity selected/highlighted in target tab

---

## Prompt 32: Export for Stage 3

### Tests
- [ ] Run `dotnet test` → 150 tests discovered
- [ ] All 150 tests pass

### Export Button
- [ ] Export button on Dashboard toolbar
- [ ] Click → Export dialog opens

### Export Dialog
- [ ] Scenario dropdown (defaults to current)
- [ ] Validate button
- [ ] Export button
- [ ] File path picker

### Pre-Export Validation
- [ ] Click Validate
- [ ] Shows validation results inline
- [ ] If errors: Export button disabled
- [ ] If warnings only: Export button enabled

### Fix and Re-Validate
- [ ] Note the errors
- [ ] Close dialog
- [ ] Fix the issues
- [ ] Reopen export dialog
- [ ] Validate again → errors gone

### Export
- [ ] Choose file path
- [ ] Click Export
- [ ] Progress indicator (if implemented)
- [ ] Success message

### Verify JSON
- [ ] Open exported file in text editor
- [ ] Valid JSON (no syntax errors)
- [ ] Contains sections: families, variants, workstations, processTimes, operators, transport, routings
- [ ] Routings include nodes and connections
- [ ] Effective properties applied (inheritance resolved)

### Export Different Scenarios
- [ ] Export Base scenario
- [ ] Export child scenario
- [ ] Verify process times differ based on scenario overrides

---

## Final Integration Test

After all prompts complete, run this end-to-end test:

### Create Complete Factory Configuration

1. **Part Families**
   - [ ] Create family "MOTOR" (FinishedGood)
   - [ ] Add variants: MOT-SM, MOT-MD, MOT-LG
   - [ ] Set family default properties
   - [ ] Override weight on MOT-LG

2. **Bill of Materials**
   - [ ] Create family "SHAFT" (Component) with variant SHAFT-001
   - [ ] Create family "HOUSING" (Component) with variant HSG-001
   - [ ] Add BOM to MOT-SM: 1x SHAFT-001, 1x HSG-001
   - [ ] Explode BOM → verify components listed

3. **Workstations**
   - [ ] Sync with layout (or create manually)
   - [ ] Have at least: Assembly, Machining, Inspection stations
   - [ ] Assign MOT-SM to Assembly and Inspection

4. **Process Times**
   - [ ] Add process time: Assembly + MOT-SM = 120s
   - [ ] Add process time: Inspection + MOT-SM = 60s
   - [ ] Add default for Assembly (null variant) = 90s

5. **Setup Times**
   - [ ] Add initial setup for Assembly = 300s
   - [ ] Add changeover SHAFT to HOUSING = 45s

6. **Operators**
   - [ ] Create "Alice" (Assembler) with Assembly skill level 4
   - [ ] Create "Bob" (Inspector) with Inspection skill level 3
   - [ ] Set Assembly station requires Assembly skill level 3

7. **Transport**
   - [ ] Create Forklift "FL-01" with speed 2 m/s
   - [ ] Create AGV "AGV-01" with speed 1 m/s

8. **Routing for MOT-SM**
   - [ ] Open routing canvas
   - [ ] Create: Start → Assembly → Inspection → End
   - [ ] Assign workstations
   - [ ] Add transport (Forklift between Start→Assembly, AGV between Assembly→Inspection)
   - [ ] Validate routing → should pass
   - [ ] Verify total time calculated

9. **Scenario**
   - [ ] Create child scenario "High Speed"
   - [ ] Override Assembly process time to 90s
   - [ ] Verify routing shows updated time

10. **Validation**
    - [ ] Run factory validation
    - [ ] Should pass with minimal warnings
    - [ ] Fix any errors

11. **Export**
    - [ ] Export Base scenario to JSON
    - [ ] Export "High Speed" scenario to JSON
    - [ ] Verify both files valid and different

### Performance
- [ ] App responsive with 50+ variants
- [ ] Routing canvas smooth with 10+ nodes
- [ ] Validation completes in reasonable time

---

## Test Sign-Off

| Prompt | Date Tested | Tester | Pass/Fail | Notes |
|--------|-------------|--------|-----------|-------|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |
| 5a | | | | |
| 6 | | | | |
| 7 | | | | |
| 8 | | | | |
| 9 | | | | |
| 10 | | | | |
| 11 | | | | |
| 12 | | | | |
| 13 | | | | |
| 14 | | | | |
| 15 | | | | |
| 16 | | | | |
| 17 | | | | |
| 18 | | | | |
| 19 | | | | |
| 20 | | | | |
| 21 | | | | |
| 22 | | | | |
| 23 | | | | |
| 24 | | | | |
| 25 | | | | |
| 26 | | | | |
| 27 | | | | |
| 28 | | | | |
| 29 | | | | |
| 30 | | | | |
| 31 | | | | |
| 32 | | | | |
| **Final Integration** | | | | |
