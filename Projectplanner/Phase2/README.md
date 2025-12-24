# Factory Configurator - Stage 2 Prompts

## Overview

This is a complete sequence of 32 prompts for building a Factory Configurator using WPF/C#/.NET 8. 
The configurator allows defining parts, families, variants, workstations, process times, operators, 
transport equipment, and visual routing - everything needed to configure a factory for simulation.

## Prerequisites

- Stage 1 Layout Editor complete (layout.db with Elements, Connections, Zones)
- Visual Studio 2022 or later
- .NET 8 SDK

## Prompt Sequence

| # | Prompt File | Focus | Tests Added | Total Tests |
|---|-------------|-------|-------------|-------------|
| 1 | Prompt-01-Solution-Foundation.md | Solution structure, Scenarios | 0 | 0 |
| 2 | Prompt-02-Testing-PartFamilies-Schema.md | Test infrastructure, Part Families/Variants schema | 8 | 8 |
| 3 | Prompt-03-PartFamilies-Service.md | Family and Variant services | 11 | 19 |
| 4 | Prompt-04-PartFamilies-ViewModel.md | ViewModel with mocking | 5 | 24 |
| 5 | Prompt-05-PartFamilies-View.md | UI implementation | 0 | 24 |
| 5a | Prompt-05a-DragDrop-Variants.md | Drag-drop variant movement | 5 | 29 |
| 6 | Prompt-06-VariantProperties-Schema.md | Physical properties schema | 6 | 35 |
| 7 | Prompt-07-VariantProperties-Service.md | Property inheritance | 5 | 40 |
| 8 | Prompt-08-VariantProperties-UI.md | Properties UI | 0 | 40 |
| 9 | Prompt-09-BOM-Schema.md | Bill of Materials schema | 2 | 42 |
| 10 | Prompt-10-BOM-Repository.md | BOM repository | 7 | 49 |
| 11 | Prompt-11-BOM-Service.md | BOM explosion, circular detection | 9 | 58 |
| 12 | Prompt-12-BOM-UI.md | BOM editing UI | 0 | 58 |
| 13 | Prompt-13-Workstation-Schema.md | Workstation schema | 5 | 63 |
| 14 | Prompt-14-Workstation-Service.md | Layout sync service | 4 | 67 |
| 15 | Prompt-15-Workstation-Variants.md | Workstation-variant assignments | 6 | 73 |
| 16 | Prompt-16-ProcessTimes-Schema.md | Process times schema | 5 | 78 |
| 17 | Prompt-17-ProcessTimes-Service.md | Fallback logic | 8 | 86 |
| 18 | Prompt-18-ProcessTimes-UI.md | Process times UI | 0 | 86 |
| 19 | Prompt-19-SetupTimes.md | Setup/changeover times | 5 | 91 |
| 20 | Prompt-20-Operators.md | Operators and types | 5 | 96 |
| 21 | Prompt-21-Skills.md | Skills and certifications | 7 | 103 |
| 22 | Prompt-22-WorkstationSkills.md | Workstation skill requirements | 7 | 110 |
| 23 | Prompt-23-TransportEquipment.md | Transport equipment | 6 | 116 |
| 24 | Prompt-24-Routing-Schema.md | Routing nodes/connections schema | 6 | 122 |
| 25 | Prompt-25-Routing-Service.md | Routing service with validation | 11 | 133 |
| 26 | Prompt-26-RoutingCanvas-Setup.md | NodeNetwork setup | 0 | 133 |
| 27 | Prompt-27-RoutingCanvas-Palette.md | Node palette, drag-drop | 0 | 133 |
| 28 | Prompt-28-RoutingCanvas-Times.md | Times display on canvas | 0 | 133 |
| 29 | Prompt-29-RoutingCanvas-Polish.md | Save/load, visual polish | 0 | 133 |
| 30 | Prompt-30-Validation-Service.md | Factory validation | 7 | 140 |
| 31 | Prompt-31-Dashboard.md | Dashboard and summary | 3 | 143 |
| 32 | Prompt-32-Export.md | Export for Stage 3 | 7 | 150 |

## Key NuGet Packages

- **CommunityToolkit.Mvvm** - MVVM infrastructure
- **Microsoft.Data.Sqlite** - SQLite database access
- **Dapper** - Micro ORM
- **xUnit** - Testing framework
- **FluentAssertions** - Test assertions
- **NSubstitute** - Mocking
- **GongSolutions.WPF.DragDrop** - Drag-drop functionality
- **NodeNetwork** - Visual node graph editor

## Architecture

```
FactorySimulation.Core        - Models, Interfaces, Enums
FactorySimulation.Data        - Repositories, Database access
FactorySimulation.Services    - Business logic
FactorySimulation.Configurator - WPF Application
FactorySimulation.Tests       - xUnit tests
```

## Database Tables

### Parts Domain (part_*)
- part_Categories
- part_Families
- part_Variants
- part_VariantProperties
- part_FamilyDefaults
- part_BOMs
- part_BOMItems

### Configuration Domain (cfg_*)
- cfg_Workstations
- cfg_WorkstationVariants
- cfg_WorkstationSkills
- cfg_ProcessTimes
- cfg_SetupTimes
- cfg_Routings
- cfg_RoutingNodes
- cfg_RoutingConnections

### Resources Domain (res_*)
- res_OperatorTypes
- res_Operators
- res_Skills
- res_OperatorSkills
- res_TransportTypes
- res_TransportEquipment
- res_TransportParams

## Output

After completing all prompts, you will have:
1. A fully functional Factory Configurator application
2. 150 passing unit tests
3. Export capability to JSON for Stage 3 simulation

## Stage 3 Preview

Stage 3 (Simulator) will:
- Read the exported JSON configuration
- Accept production orders
- Run deterministic or stochastic simulation
- Provide animation and performance metrics
