# Prompt 14: Workstation Service & Sync

Continuing Factory Configurator. Prompt 13 complete, 63 tests passing.

Objective: Implement workstation sync with layout. All tests must pass.

## 1. Service

### IWorkstationService:
- Task SyncWithLayoutAsync()
- Task<IReadOnlyList<Workstation>> GetAllAsync()

### WorkstationService.SyncWithLayoutAsync:
- Query Elements table for elements that should be workstations
  (ElementType in 'Machine', 'Station', 'Inspection', 'Assembly', 'Packaging')
- For each Element without a Workstation entry, create one with default WorkstationType
- For each Workstation whose Element no longer exists, mark IsActive = false
- Do not delete workstations (preserve configuration history)

### ElementType to WorkstationType mapping:
- "Machine" → Machining
- "Station" → Manual
- "Inspection" → Inspection
- "Assembly" → Assembly
- "Packaging" → Packaging
- Default → Manual

## 2. Tests in Tests/Services/WorkstationServiceTests.cs

- SyncWithLayout_NewElements_CreatesWorkstations
- SyncWithLayout_ExistingWorkstation_NotDuplicated
- SyncWithLayout_DeletedElement_MarksInactive
- SyncWithLayout_MapsElementTypeCorrectly

## 3. View

### WorkstationsView.xaml (new main tab):
- Toolbar: "Sync with Layout" button, filter by WorkstationType
- DataGrid: Element Name | Type | Capacity | Active
- Detail panel when row selected:
  - WorkstationType dropdown
  - Capacity input
  - Notes textbox
  - Save button

## Run Tests

Run dotnet test. All 67 tests should pass.
