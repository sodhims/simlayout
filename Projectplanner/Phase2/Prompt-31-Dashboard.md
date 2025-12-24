# Prompt 31: Dashboard & Summary

Continuing Factory Configurator. Prompt 30 complete, 140 tests passing.

Objective: Implement dashboard. All tests must pass.

## 1. Model

### FactorySummary.cs:
- FamilyCount (int)
- VariantCount (int)
- VariantsByCategory (Dictionary<string, int>)
- WorkstationCount (int)
- WorkstationsByType (Dictionary<string, int>)
- OperatorCount (int)
- TransportCount (int)
- RoutingCount (int) - variants with routings
- RoutingCoverage (decimal) - percent of FinishedGoods with routings
- LastValidation (ValidationResult?)
- LastValidatedAt (DateTime?)

## 2. Service

### ISummaryService:
- Task<FactorySummary> GetSummaryAsync(int scenarioId)

### SummaryService:
- Queries counts from all tables
- RoutingCoverage = (FinishedGoods with routing / Total FinishedGoods) * 100
- If no FinishedGoods, RoutingCoverage = 100

## 3. Tests in Tests/Services/SummaryServiceTests.cs

- GetSummary_ReturnsCorrectFamilyCount
- GetSummary_GroupsVariantsByCategory
- GetSummary_CalculatesRoutingCoverage

## 4. ViewModel

### DashboardViewModel : ObservableObject

Properties:
- FactorySummary Summary
- ValidationResult? LastValidation
- bool IsValidating
- ObservableCollection<ValidationIssue> FilteredIssues
- string IssueFilter ("All", "Errors", "Warnings")

Commands:
- RefreshCommand - reloads summary
- ValidateCommand - runs ValidateFactoryAsync
- NavigateToCommand(string tabName) - switches to tab

## 5. View

### DashboardView.xaml (make this the HOME tab, first position):

#### Summary cards row:
```
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ 📁 Families │ │ 📦 Variants │ │ 🏭 Stations │ │ 👷 Operators│ │ 🚜 Transport│
│     12      │ │     45      │ │     23      │ │     18      │ │      8      │
└─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘
```
Each card clickable - navigates to corresponding tab.

#### Routing coverage bar:
```
Routing Coverage: 89% [████████████░░░░] 45/51 finished goods have routings
```

#### Validation section:
- "Validate Factory" button
- Status indicator: ✓ Valid / ⚠ 3 Warnings / ✗ 2 Errors
- Last validated: "2 hours ago" or "Never"

#### Issues list:
- Filter buttons: [All] [Errors] [Warnings]
- DataGrid:
  | Severity | Category | Entity | Message |
  | ⚠️ | Parts | MOT-001 | No workstation assignment |
  | ✗ | Routing | BRK-002 | No Start node |
- Double-click row navigates to the entity

## 6. Navigation

- Each summary card is clickable
- Uses NavigateToCommand to switch tabs
- Tab names: "PartFamilies", "Workstations", "Operators", "Transport", "Routing"

- Issues list double-click:
  - Navigate to relevant tab
  - Select/highlight the problematic entity

## 7. Manual verification

- Load app, Dashboard is first visible tab
- Verify counts match actual data
- Click "Part Families" card, verify navigates to that tab
- Go back to Dashboard
- Click "Validate Factory"
- See validation results appear
- Double-click an issue, verify navigates to entity
- Create a configuration problem (delete workstation assignment)
- Re-validate, see new warning appear

## Run Tests

Run dotnet test. All 143 tests should pass.
