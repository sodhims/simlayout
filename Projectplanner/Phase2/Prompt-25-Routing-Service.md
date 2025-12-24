# Prompt 25: Routing Service

Continuing Factory Configurator. Prompt 24 complete, 122 tests passing.

Objective: Implement routing service with validation. All tests must pass.

## 1. Service

### IRoutingService:
- Task<Routing?> GetRoutingAsync(int scenarioId, int variantId)
- Task<Routing> CreateRoutingAsync(int scenarioId, int variantId, string? name)
- Task<RoutingNode> AddNodeAsync(int routingId, RoutingNodeType type, int? workstationId, double x, double y)
- Task UpdateNodePositionAsync(int nodeId, double x, double y)
- Task DeleteNodeAsync(int nodeId)
- Task<RoutingConnection> ConnectNodesAsync(int routingId, int fromNodeId, int toNodeId, TransportCategory? category, decimal? timeSec)
- Task DeleteConnectionAsync(int connectionId)
- Task<bool> ValidateRoutingAsync(int routingId)
- Task<decimal> CalculateTotalTimeAsync(int routingId, int scenarioId)
- Task<Routing> CloneRoutingAsync(int routingId, int targetScenarioId)

### RoutingService Implementation:

#### CreateRoutingAsync:
- Creates routing with default Start and End nodes
- Start node at position (100, 300)
- End node at position (700, 300)

#### ValidateRoutingAsync:
Returns false if:
- No Start node (must have exactly one)
- No End node (must have at least one)
- Unreachable nodes (not connected from Start)
- Dead-end nodes (paths that don't reach End)
- Process/Assemble/Inspect nodes without WorkstationId (warning, not error)

#### CalculateTotalTimeAsync:
- Find longest path from Start to End
- Sum: process times at each workstation + transport times on connections
- Return total

#### CloneRoutingAsync:
- Copy routing, all nodes, and all connections to target scenario
- Preserve relative positions

## 2. Tests in Tests/Services/RoutingServiceTests.cs

- CreateRouting_CreatesWithStartAndEndNodes
- AddNode_AddsToRouting
- UpdateNodePosition_UpdatesCoordinates
- ConnectNodes_CreatesConnection
- ConnectNodes_SameNode_ThrowsException
- DeleteNode_RemovesNode
- ValidateRouting_ValidGraph_ReturnsTrue
- ValidateRouting_NoStart_ReturnsFalse
- ValidateRouting_UnreachableNode_ReturnsFalse
- CalculateTotalTime_SumsCorrectly
- CloneRouting_CopiesAllNodesAndConnections

## Run Tests

Run dotnet test. All 133 tests should pass.
