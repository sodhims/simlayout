# Simulation Layout Editor - User Manual

## Version 2.1

---

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Interface Overview](#interface-overview)
4. [Working with Nodes](#working-with-nodes)
5. [Working with Paths](#working-with-paths)
6. [Groups vs Cells](#groups-vs-cells)
7. [Properties Panel](#properties-panel)
8. [Layers](#layers)
9. [Validation](#validation)
10. [Export Options](#export-options)
11. [Keyboard Shortcuts](#keyboard-shortcuts)

---

## Overview

The Simulation Layout Editor is a visual tool for creating factory and manufacturing simulation layouts. It allows you to:

- Design factory floor layouts with various node types
- Connect nodes with material flow paths
- Organize nodes into groups and work cells
- Export layouts for discrete event simulation software
- Validate layouts for simulation readiness

---

## Getting Started

### Creating a New Layout

1. Launch the application
2. Click **File → New** or press `Ctrl+N`
3. A blank canvas appears with a grid

### Opening an Existing Layout

1. Click **File → Open** or press `Ctrl+O`
2. Select a `.json` layout file
3. The layout loads onto the canvas

### Saving Your Work

- **Save**: `Ctrl+S` or **File → Save**
- **Save As**: **File → Save As...** to save with a new name

---

## Interface Overview

### Main Areas

| Area | Description |
|------|-------------|
| **Menu Bar** | File, Edit, View, Tools, Validate, Help menus |
| **Toolbar** | Quick access to tools and common actions |
| **Left Panel** | Layers control and Toolbox with node types |
| **Canvas** | Main drawing area with grid |
| **Right Panel** | Explorer (node tree) and Validation results |
| **Status Bar** | Current mode, line styles, coordinates |

### Toolbox

The toolbox shows the most common node types:

| Icon | Type | Description |
|------|------|-------------|
| → | Source | Parts enter the system |
| ⊐ | Sink | Parts exit the system |
| ▭ | Machine | Automated processing station |
| ≡ | Buffer | Queue/waiting area |
| ⊏ | Workstation | Manual work station |
| ⌕ | Inspection | Quality check point |
| ⊞ | Storage | Warehouse/inventory |
| ═ | Conveyor | Belt/roller transport |
| + | Junction | Split or merge flows |
| ⌂ | AGV Station | AGV pickup/dropoff point |
| ⊕ | Robot | Robotic arm/handler |
| ⋈ | Assembly | Combine multiple parts |

Click **"More Nodes..."** to see all available node types with descriptions.

---

## Working with Nodes

### Adding Nodes

**Method 1: Toolbox**
1. Click a node type in the toolbox
2. Click on the canvas to place it

**Method 2: Right-Click Menu**
1. Right-click on the canvas
2. Select **Add Node → [Type]**

**Method 3: Node Palette**
1. Click **"More Nodes..."** in the toolbox
2. Browse all node types by category
3. Click to select and place

### Selecting Nodes

- **Single select**: Click on a node
- **Multi-select**: Hold `Ctrl` and click multiple nodes
- **Box select**: Click and drag on empty canvas area
- **Select all**: `Ctrl+A`

### Moving Nodes

- **Drag**: Click and drag selected nodes
- **Nudge**: Use arrow keys to move by grid increment
- **Snap to grid**: Enabled by default (View → Snap to Grid)

### Node Properties

Right-click a node and select **Properties** to open the floating Properties panel:

- **Basic**: ID, Type, Name, Label
- **Position**: X, Y, Width, Height, Rotation
- **Visual**: Icon, Color, Label Position
- **Simulation**: Capacity, Servers, Process Time, Setup Time (varies by type)
- **Connections**: Shows incoming/outgoing paths

---

## Working with Paths

### Creating Paths

**Method 1: Path Tool**
1. Select the Path tool (`P` key or toolbar)
2. Click on the source node
3. Click on the destination node
4. Path is created

**Method 2: Right-Click Menu**
1. Right-click on a node
2. Select **Start Path From Here**
3. Click on the destination node

### Path Properties

- **Type**: Single or Double (bidirectional visual)
- **Routing**: Direct, Manhattan (right-angle), Corridor
- **Transport**: Conveyor, AGV, Manual, Crane
- **Speed**: Transport speed
- **Capacity**: Maximum items in transit

### Editing Paths

1. Enable **Edit Paths** checkbox in toolbar
2. Click on a path to select it
3. Drag waypoints to adjust the path shape
4. Click on the path to add new waypoints

---

## Groups vs Cells

### Understanding the Difference

| Feature | Group | Cell |
|---------|-------|------|
| **Purpose** | Visual organization | Simulation unit |
| **Border** | Dashed line | Solid line with terminals |
| **Entry/Exit** | None | Defined input/output nodes |
| **Routing** | None | Internal routing rules |
| **Export** | Just visual grouping | Exports as work cell |
| **Usage** | Organize related nodes | Define manufacturing cells |

### Groups

A **Group** is purely for visual organization:
- Keeps related nodes together
- Makes complex layouts easier to manage
- Has no simulation meaning
- Can be created/ungrouped freely

**To create a Group:**
1. Select multiple nodes
2. Press `Ctrl+G` or **Edit → Group**

**To ungroup:**
1. Select the group
2. Press `Ctrl+Shift+G` or **Edit → Ungroup**

### Cells (Work Cells)

A **Cell** is a simulation-aware manufacturing unit:
- Represents a physical work cell
- Has defined entry and exit points
- Contains internal routing logic
- Exports as a single unit to simulation

**To create a Cell:**
1. Select multiple nodes
2. Press `Ctrl+Shift+C` or **Edit → Define as Cell**

**Cell Properties:**
- **Is Cell**: Checkbox to toggle cell mode
- **Input Position**: Where parts enter (left/right/top/bottom)
- **Output Position**: Where parts exit
- **Members**: List of nodes in the cell

**Cell Types (auto-detected):**
- **Simple**: Linear flow through cell
- **Parallel**: Multiple parallel paths
- **Assembly**: Multiple inputs combine
- **Workcell**: Complex internal routing

---

## Properties Panel

The Properties panel is a floating window that shows details for the selected element.

### Opening Properties

- Right-click an element → **Properties**
- Or select element and press `F4`

### Node Properties

| Section | Fields |
|---------|--------|
| Basic | ID (read-only), Type, Name, Label |
| Position | X, Y, Width, Height, Rotation |
| Visual | Icon, Color (click to pick), Label Position |
| Simulation | Varies by node type |
| Connections | Lists connected paths |

### Path Properties

| Section | Fields |
|---------|--------|
| Path | ID, From, To, Type, Routing, Color |
| Transport | Type, Speed, Capacity, Distance |

### Group/Cell Properties

| Section | Fields |
|---------|--------|
| Group/Cell | ID, Name, Is Cell |
| Terminals | Input Position, Output Position |
| Members | List of contained nodes |

---

## Layers

Control visibility of different element types:

| Layer | Content |
|-------|---------|
| Img | Background image |
| Wall | Walls and barriers |
| Node | All nodes |
| Path | Connection paths |
| Lbl | Node labels |
| Meas | Measurements |
| Zone | Zones/areas |
| Grid | Background grid |
| Corridors | Corridor paths |

Toggle layers using checkboxes in the left panel.

---

## Validation

The editor validates your layout for simulation readiness.

### Running Validation

- Press `F5` or **Validate → Validate Layout**
- Results appear in the right panel

### Validation Checks

| Check | Description |
|-------|-------------|
| Orphan Nodes | Nodes with no connections |
| Missing Source/Sink | Layout needs at least one of each |
| Disconnected Paths | Paths with missing endpoints |
| Invalid Node Types | Unknown node types |
| Cell Integrity | Cells have proper entry/exit |

### Fixing Issues

Click on a validation issue to highlight the problematic element on the canvas.

---

## Export Options

### Export Simulation JSON

**File → Export → Simulation JSON**

Exports the layout in a format suitable for discrete event simulation tools:
- Node definitions with simulation parameters
- Path/connection data
- Cell definitions with routing rules

### Export Image (PNG)

**File → Export → Image (PNG)**

Exports the canvas as a high-resolution image.

### Export SVG

**File → Export → SVG**

Exports as scalable vector graphics.

### Export BOM CSV

**File → Export → BOM CSV**

Exports a Bill of Materials with all nodes and their properties.

---

## Keyboard Shortcuts

### File Operations
| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New layout |
| `Ctrl+O` | Open layout |
| `Ctrl+S` | Save layout |

### Edit Operations
| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+C` | Copy |
| `Ctrl+V` | Paste |
| `Ctrl+X` | Cut |
| `Ctrl+D` | Duplicate |
| `Ctrl+A` | Select all |
| `Delete` | Delete selected |

### Grouping
| Shortcut | Action |
|----------|--------|
| `Ctrl+G` | Group selected |
| `Ctrl+Shift+G` | Ungroup |
| `Ctrl+Shift+C` | Define as Cell |

### Tools
| Shortcut | Action |
|----------|--------|
| `V` or `S` | Select tool |
| `P` | Path tool |
| `W` | Wall tool |
| `H` | Pan/Hand tool |
| `M` | Move tool |
| `R` | Measure tool |
| `E` | Toggle path edit mode |

### View
| Shortcut | Action |
|----------|--------|
| `Ctrl++` | Zoom in |
| `Ctrl+-` | Zoom out |
| `Ctrl+0` | Zoom to fit |
| `F5` | Validate layout |

### Navigation
| Shortcut | Action |
|----------|--------|
| `Arrow keys` | Nudge selection |
| `Scroll wheel` | Zoom in/out |
| `Middle-drag` | Pan canvas |

---

## Tips and Best Practices

1. **Start with Sources and Sinks** - Define where parts enter and exit first

2. **Use Cells for Reusable Units** - If you have repeated patterns, define them as cells

3. **Validate Often** - Run validation to catch issues early

4. **Use Canonical Names** - **Tools → Apply Canonical Names** gives consistent naming

5. **Layer Management** - Hide layers you're not working on to reduce clutter

6. **Save Frequently** - Use `Ctrl+S` often to avoid losing work

7. **Use Grid Snap** - Keep nodes aligned for cleaner layouts

---

## Troubleshooting

### Nodes Won't Connect

- Ensure you're using the Path tool
- Click directly on the node, not near it
- Check that both nodes exist

### Layout Won't Export

- Run validation first
- Fix any errors (red items)
- Ensure at least one source and sink exist

### Performance Issues

- Hide unused layers
- Reduce zoom level for large layouts
- Close Properties panel when not needed

---

## Version History

### Version 2.1
- Improved toolbox with smaller icons and more node types
- Added "More Nodes..." palette for all node types
- Enhanced Properties panel with comprehensive fields
- Added Group vs Cell documentation
- Improved validation feedback

### Version 2.0
- Initial release with floating Properties panel
- Support for Groups and Cells
- Multiple export formats
- Layer management

---

*© 2024 Simulation Layout Editor*
