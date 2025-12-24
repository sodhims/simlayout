# Prompt 1: Solution Foundation & Scenarios

Building Stage 2 of a Factory Simulation Suite using WPF/C#/.NET 8.
Stage 1 created a Layout Editor with SQLite database (layout.db) containing:
Layouts, Elements, Connections, Zones, ElementZones.

Create the foundation for the Factory Configurator:

## 1. Solution structure

- FactorySimulation.Core (class library) - Models, Interfaces, Enums
- FactorySimulation.Data (class library) - Repositories, DB access
- FactorySimulation.Services (class library) - Business logic
- FactorySimulation.Configurator (WPF App) - UI

## 2. Database: Copy layout.db → factory.db, then add:

```sql
CREATE TABLE Scenarios (
    Id INTEGER PRIMARY KEY, 
    Name TEXT UNIQUE NOT NULL,
    Description TEXT, 
    ParentScenarioId INTEGER REFERENCES Scenarios(Id),
    IsBase INTEGER DEFAULT 0, 
    CreatedAt TEXT, 
    ModifiedAt TEXT
);

INSERT INTO Scenarios (Id, Name, IsBase, CreatedAt) 
VALUES (1, 'Base', 1, datetime('now'));
```

## 3. Core models in FactorySimulation.Core/Models/

- Scenario.cs inheriting from CommunityToolkit.Mvvm.ComponentModel.ObservableObject
- Properties: Id, Name, Description, ParentScenarioId, IsBase, CreatedAt, ModifiedAt

## 4. Repository in FactorySimulation.Data/

- IScenarioRepository interface
- ScenarioRepository using Microsoft.Data.Sqlite + Dapper
- Methods: GetAllAsync(), GetByIdAsync(int), CreateAsync(Scenario), 
  UpdateAsync(Scenario), DeleteAsync(int), GetChildrenAsync(int parentId)

## 5. Service in FactorySimulation.Services/

- ScenarioService.cs
- CloneScenarioAsync(int sourceId, string newName) - creates child scenario
- ValidateDeleteAsync(int id) - returns error if Base or has children

## 6. Main window setup

- ScenarioSelectorViewModel with ObservableCollection<Scenario>
- ComboBox for scenario selection in toolbar area
- Commands: NewScenarioCommand, CloneScenarioCommand, DeleteScenarioCommand
- TabControl placeholder for future configuration tabs

Use CommunityToolkit.Mvvm for ObservableObject and RelayCommand.
Connection string should reference factory.db in application directory.

## Test

Run app, verify Base scenario appears, create child scenario "Test Config",
verify it shows in dropdown with correct parent reference.
