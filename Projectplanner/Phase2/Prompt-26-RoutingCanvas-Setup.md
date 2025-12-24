# Prompt 26: Visual Routing Canvas - Setup

Continuing Factory Configurator. Prompt 25 complete, 133 tests passing.

Objective: Set up NodeNetwork for visual routing editor. Manual verification.

## 1. NuGet packages

Add to FactorySimulation.Configurator:
- NodeNetwork (latest stable)
- DynamicData (dependency)

## 2. Custom node types (Configurator/Routing/Nodes/)

Create node ViewModels inheriting from NodeNetwork:

### RoutingNodeViewModelBase : NodeViewModel
- RoutingNode Model (the database entity)
- NodeType property
- WorkstationId property
- Abstract color/icon per type

### StartNodeViewModel : RoutingNodeViewModelBase
- Single output port
- Green color (#4CAF50)
- No workstation assignment

### EndNodeViewModel : RoutingNodeViewModelBase
- Single input port
- Red color (#F44336)
- No workstation assignment

### ProcessNodeViewModel : RoutingNodeViewModelBase
- One input port, one output port
- Blue color (#2196F3)
- WorkstationId assignment
- Shows workstation name and process time

### InspectNodeViewModel : RoutingNodeViewModelBase
- One input, one output
- Yellow color (#FFC107)
- WorkstationId assignment

### AssembleNodeViewModel : RoutingNodeViewModelBase
- Multiple inputs (for components), one output
- Purple color (#9C27B0)
- WorkstationId assignment

### DecisionNodeViewModel : RoutingNodeViewModelBase
- One input, multiple outputs (for branching)
- Orange color (#FF9800)
- Condition property (future: rule-based routing)

## 3. Connection ViewModel

### RoutingConnectionViewModel : ConnectionViewModel
- TransportCategory property
- TransportTimeSec property
- Color based on transport type:
  - Forklift: Brown
  - AGV: Blue
  - Crane: Gray
  - Manual: Black dashed

## 4. Network ViewModel

### RoutingNetworkViewModel : NetworkViewModel
- Routing Model (database entity)
- IRoutingService (injected)

#### Methods:
- LoadFromRoutingAsync(Routing routing) - creates node VMs from model
- SaveToRoutingAsync() - updates model from node VMs
- AddNode(RoutingNodeType type, Point position)
- DeleteSelectedNodes()

## 5. Basic view setup

### RoutingCanvasView.xaml:
```xml
<UserControl xmlns:nodenetwork="clr-namespace:NodeNetwork.Views;assembly=NodeNetwork">
    <Grid>
        <nodenetwork:NetworkView 
            ViewModel="{Binding Network}"
            Background="#F5F5F5"/>
    </Grid>
</UserControl>
```

## 6. Manual verification

- Create empty routing for a variant
- Verify canvas appears with Start and End nodes
- Nodes are displayed at their saved positions
- Can select nodes
- Canvas is pannable/zoomable

No new automated tests (NodeNetwork is UI framework).
Existing 133 tests should pass.
