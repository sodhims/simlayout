# Prompt 32: Export for Stage 3

Continuing Factory Configurator. Prompt 31 complete, 143 tests passing.

Objective: Implement factory configuration export for simulation. All tests must pass.

## 1. Export Models

### FactoryExport.cs (root object):
- ExportVersion (string) = "1.0"
- ExportedAt (DateTime)
- ScenarioId (int)
- ScenarioName (string)
- Families (List<FamilyExport>)
- Variants (List<VariantExport>)
- Workstations (List<WorkstationExport>)
- ProcessTimes (List<ProcessTimeExport>)
- SetupTimes (List<SetupTimeExport>)
- Operators (List<OperatorExport>)
- Transport (List<TransportExport>)
- Routings (List<RoutingExport>)

### Export DTOs (simplified flat structures for JSON):

#### FamilyExport:
- Id, FamilyCode, Name, CategoryName

#### VariantExport:
- Id, FamilyId, PartNumber, Name
- CategoryName
- Properties: Length, Width, Height, Weight, ContainerType, etc. (effective/merged)

#### WorkstationExport:
- Id, ElementId, Name, Type, Capacity
- AssignedVariantIds (List<int>)
- RequiredSkillIds (List<int>)

#### ProcessTimeExport:
- WorkstationId, VariantId (nullable), ProcessSec, LoadSec, UnloadSec

#### SetupTimeExport:
- WorkstationId, FromVariantId, ToVariantId, SetupSec

#### OperatorExport:
- Id, Name, TypeName
- Skills: List of { SkillName, Level }

#### TransportExport:
- Id, Name, Category
- SpeedMps, LoadedSpeedMps, CapacityKg, LoadTimeSec, UnloadTimeSec
- HomeElementId

#### RoutingExport:
- VariantId, VariantPartNumber
- Nodes: List of { Id, NodeType, WorkstationId, PositionX, PositionY }
- Connections: List of { FromNodeId, ToNodeId, TransportCategory, TransportTimeSec }

## 2. Service

### IExportService:
- Task<FactoryExport> BuildExportAsync(int scenarioId)
- Task SaveToFileAsync(FactoryExport export, string filePath)
- Task<ValidationResult> ValidateBeforeExportAsync(int scenarioId)

### ExportService:

#### BuildExportAsync:
- Assembles all data into FactoryExport object
- Uses effective/merged properties (inheritance applied)
- Includes only active entities

#### SaveToFileAsync:
- Serializes to JSON using System.Text.Json
- Formatted with indentation for readability
- UTF-8 encoding

#### ValidateBeforeExportAsync:
- Calls ValidationService.ValidateFactoryAsync
- If any Errors exist, throw InvalidOperationException with message listing errors
- Warnings are OK - export proceeds

## 3. Tests in Tests/Services/ExportServiceTests.cs

- BuildExport_IncludesAllFamilies
- BuildExport_IncludesAllVariants
- BuildExport_IncludesRoutingsWithNodesAndConnections
- BuildExport_AppliesEffectiveProperties
  - Template with Weight=10, variant with no override
  - Assert export shows variant with Weight=10
- SaveToFile_CreatesValidJson
  - Build export, save to temp file
  - Read file, deserialize, assert no errors
  - Assert required fields present
- ValidateBeforeExport_WithErrors_ThrowsException
- ValidateBeforeExport_WithWarningsOnly_Succeeds

## 4. View

### ExportDialog.xaml:
- Scenario dropdown (defaults to current scenario)
- Validation section:
  - "Validate" button
  - Status: shows validation result inline
  - Issues list if any
- Export section:
  - File path picker (Save dialog, default: factory-{scenarioName}-{date}.json)
  - "Export" button (disabled if validation has errors)
- Progress indicator during export

### Add "Export" button to Dashboard toolbar:
- Opens ExportDialog
- Pre-selects current scenario

## 5. JSON Output Structure Example

```json
{
  "exportVersion": "1.0",
  "exportedAt": "2024-01-15T10:30:00Z",
  "scenarioId": 1,
  "scenarioName": "Base",
  "families": [
    { "id": 1, "familyCode": "MOT", "name": "Motors", "categoryName": "FinishedGood" }
  ],
  "variants": [
    { 
      "id": 1, "familyId": 1, "partNumber": "MOT-001", "name": "Small Motor",
      "categoryName": "FinishedGood",
      "properties": { "lengthMm": 100, "widthMm": 50, "heightMm": 30, "weightKg": 2.5 }
    }
  ],
  "workstations": [...],
  "processTimes": [...],
  "setupTimes": [...],
  "operators": [...],
  "transport": [...],
  "routings": [
    {
      "variantId": 1,
      "variantPartNumber": "MOT-001",
      "nodes": [
        { "id": 1, "nodeType": "Start", "workstationId": null, "positionX": 100, "positionY": 300 },
        { "id": 2, "nodeType": "Process", "workstationId": 5, "positionX": 300, "positionY": 300 },
        { "id": 3, "nodeType": "End", "workstationId": null, "positionX": 500, "positionY": 300 }
      ],
      "connections": [
        { "fromNodeId": 1, "toNodeId": 2, "transportCategory": "AGV", "transportTimeSec": 30 },
        { "fromNodeId": 2, "toNodeId": 3, "transportCategory": "Conveyor", "transportTimeSec": 15 }
      ]
    }
  ]
}
```

## 6. Manual verification

- Create complete factory configuration
- Open Export dialog (from Dashboard)
- Click Validate, verify it passes (or shows warnings)
- Export to JSON file
- Open JSON file in text editor, verify structure is correct
- Verify all variants, workstations, routings are present
- Introduce a validation error (e.g., routing without Start node)
- Re-open Export dialog
- Click Validate, see error
- Verify Export button is disabled
- Fix the error
- Export successfully

## Run Tests

Run dotnet test. All 150 tests should pass.

---

## Final Summary

Application is ready for Stage 3 (Simulator) development.

Stage 3 will:
- Read the exported JSON file
- Accept orders (separate input)
- Run deterministic simulation
- Optionally add distributions for stochastic simulation
- Provide animation and metrics
