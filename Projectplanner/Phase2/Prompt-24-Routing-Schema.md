# Prompt 24: Routing Schema

Continuing Factory Configurator. Prompt 23 complete, 116 tests passing.

Objective: Add routing schema for visual flow builder. All tests must pass.

## 1. Database schema

Add CreateRoutingsSchemaAsync:

```sql
CREATE TABLE cfg_Routings (
    Id INTEGER PRIMARY KEY,
    ScenarioId INTEGER NOT NULL REFERENCES Scenarios(Id) ON DELETE CASCADE,
    VariantId INTEGER NOT NULL REFERENCES part_Variants(Id) ON DELETE CASCADE,
    Name TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    UNIQUE(ScenarioId, VariantId)
);

CREATE TABLE cfg_RoutingNodes (
    Id INTEGER PRIMARY KEY,
    RoutingId INTEGER NOT NULL REFERENCES cfg_Routings(Id) ON DELETE CASCADE,
    NodeType TEXT NOT NULL,
    WorkstationId INTEGER NULL REFERENCES cfg_Workstations(Id) ON DELETE SET NULL,
    PositionX REAL NOT NULL DEFAULT 0,
    PositionY REAL NOT NULL DEFAULT 0,
    Description TEXT NULL
);

CREATE TABLE cfg_RoutingConnections (
    Id INTEGER PRIMARY KEY,
    RoutingId INTEGER NOT NULL REFERENCES cfg_Routings(Id) ON DELETE CASCADE,
    FromNodeId INTEGER NOT NULL REFERENCES cfg_RoutingNodes(Id) ON DELETE CASCADE,
    ToNodeId INTEGER NOT NULL REFERENCES cfg_RoutingNodes(Id) ON DELETE CASCADE,
    TransportCategory TEXT NULL,
    TransportTimeSec REAL NULL,
    UNIQUE(FromNodeId, ToNodeId)
);
```

## 2. Enums

```csharp
public enum RoutingNodeType
{
    Start,
    End,
    Process,
    Assemble,
    Inspect,
    Package,
    Store,
    Decision
}
```

## 3. Models

### Routing.cs:
- Id, ScenarioId, VariantId, Name, IsActive
- Nodes (List<RoutingNode>) - navigation
- Connections (List<RoutingConnection>) - navigation

### RoutingNode.cs:
- Id, RoutingId, NodeType, WorkstationId, PositionX, PositionY, Description
- WorkstationName (string?) - for display

### RoutingConnection.cs:
- Id, RoutingId, FromNodeId, ToNodeId, TransportCategory, TransportTimeSec

## 4. Repository

### IRoutingRepository:
- Task<Routing?> GetByVariantAsync(int scenarioId, int variantId)
- Task<Routing?> GetWithNodesAndConnectionsAsync(int routingId)
- Task<int> CreateAsync(Routing routing)
- Task UpdateAsync(Routing routing)
- Task DeleteAsync(int routingId)
- Task<int> AddNodeAsync(RoutingNode node)
- Task UpdateNodeAsync(RoutingNode node)
- Task DeleteNodeAsync(int nodeId)
- Task<int> AddConnectionAsync(RoutingConnection connection)
- Task DeleteConnectionAsync(int connectionId)

## 5. Tests in Tests/Repositories/RoutingRepositoryTests.cs

- CreateRouting_ValidData_ReturnsId
- CreateRouting_DuplicateScenarioVariant_ThrowsException
- GetWithNodesAndConnections_PopulatesAll
- AddNode_SetsId
- DeleteNode_CascadesConnections
- DeleteRouting_CascadesNodesAndConnections

## Run Tests

Run dotnet test. All 122 tests should pass.
