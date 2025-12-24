# Prompt 18: Process Times UI

Continuing Factory Configurator. Prompt 17 complete, 86 tests passing.

Objective: Implement process times UI. Manual verification.

## 1. ViewModel

### ProcessTimesViewModel : ObservableObject

Dependencies: IProcessTimeService, IWorkstationRepository, IPartVariantRepository

#### Properties:
- int SelectedScenarioId (from main window scenario selector)
- Workstation? SelectedWorkstation
- ObservableCollection<Workstation> Workstations
- ObservableCollection<ProcessTimeRow> TimeRows
- ProcessTimeRow? SelectedTimeRow

#### ProcessTimeRow (display model):
- ProcessTime Time
- string VariantDisplay (PartNumber or "[Default]" if VariantId null)
- bool IsInherited (true if from base scenario)
- decimal TotalCycleTime

#### Commands:
- LoadCommand - loads workstations
- SelectWorkstationCommand - loads times for selected workstation
- AddTimeCommand - add new time entry
- DeleteTimeCommand - remove selected
- SaveCommand - save changes
- CopyFromBaseCommand - copies base times to current scenario

## 2. View

### ProcessTimesView.xaml (new main tab):

#### Toolbar:
- Workstation dropdown (filter times by workstation)
- "Copy from Base" button (enabled when not Base scenario)

#### DataGrid:
- Columns: Variant (or Default) | Process (sec) | Load (sec) | Unload (sec) | Total | Notes
- Inherited rows shown in italic or gray background
- Editable cells for times

#### Buttons:
- Add/Delete buttons
- Save button

## 3. Inheritance indicator

- Times inherited from Base scenario: show visually distinct (gray/italic)
- Tooltip: "Inherited from Base scenario"
- When user edits inherited row, create new entry in current scenario (override)

## 4. Manual verification

- Select Base scenario, add process times for a workstation
- Create child scenario, select it
- Verify times show as inherited
- Edit one time, verify it becomes a local override
- Add new time in child, verify it's not in Base
- Test Copy from Base button

No new automated tests. Existing 86 tests should pass.
