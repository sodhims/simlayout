# Prompt 27: Visual Routing Canvas - Node Palette & Drag-Drop

Continuing Factory Configurator. Prompt 26 complete, 133 tests passing, basic canvas working.

Objective: Add node palette and drag-drop creation. Manual verification.

## 1. Node palette (Configurator/Routing/Views/)

### RoutingPaletteView.xaml:
- Vertical StackPanel or WrapPanel
- Draggable node templates:

| Icon | Node Type | Notes |
|------|-----------|-------|
| 📥 | Start | Disabled if Start exists |
| 📤 | End | Can have multiple |
| ⚙️ | Process | General processing |
| 🔍 | Inspect | Quality check |
| 🔧 | Assemble | Assembly operation |
| 📦 | Package | Packaging operation |
| ◇ | Decision | Conditional branching |

Each item shows icon + label

## 2. Drag from palette to canvas

- Palette items are drag sources (use GongSolutions.WPF.DragDrop)
- Canvas is drop target
- On drop:
  - Get drop position in canvas coordinates
  - Call RoutingNetworkViewModel.AddNode(type, position)
  - New node appears at drop location

## 3. Node appearance

Each node type has distinct:
- Color/gradient fill
- Icon (displayed in node header)
- Shape (rectangle for most, diamond for decision)

Node content shows:
- Type icon
- Workstation name (if assigned) or "[Click to assign]"
- Process time (if available)

## 4. Node context menu

Right-click on node:
- Assign Workstation... → opens picker
- Set Transport... → for outgoing connection (if connected)
- Delete Node
- (Decision node) Add Output Port
- (Assemble node) Add Input Port

## 5. Connection creation

- Drag from output port to input port
- Visual feedback during drag:
  - Rubber band line from source port to cursor
  - Valid ports highlight on hover
  - Invalid ports show denied indicator

- On connect:
  - Creates RoutingConnection in model
  - Opens transport configuration dialog:
    - Transport type dropdown (Forklift, AGV, Crane, Conveyor, Manual)
    - Time (seconds) - manual entry or auto-calculated

## 6. Delete operations

- Select node(s), press Delete key
- Select connection, press Delete key
- Confirmation dialog for nodes with connections:
  "Delete this node and X connections?"

## 7. Manual verification

- Drag Process node from palette onto canvas
- Verify node appears at drop position
- Drag from Start output port to Process input port
- Verify connection line appears
- Transport dialog opens
- Set transport to "AGV", time "30"
- Connection shows transport info
- Right-click Process node, select "Assign Workstation..."
- Workstation picker opens
- Select a workstation
- Node updates to show workstation name
- Delete the connection (select + Delete key)
- Delete the node (select + Delete key)

No new automated tests. Existing 133 tests should pass.
