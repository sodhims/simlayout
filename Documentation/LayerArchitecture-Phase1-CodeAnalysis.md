# Phase 1: Code Analysis Report

**Layout Editor Multi-Layer Architecture**  
**Analysis Date:** December 20, 2025  
**Analyst:** GitHub Copilot  

---

## Executive Summary

This WPF layout editor is a **discrete event simulation layout tool** with substantial functionality already implemented. The codebase has a solid foundation with ~35 handler classes, 19 model files, and multiple specialized renderers. The architecture shows good separation of concerns with distinct Models, Services, Handlers, and Renderers.

**Key Finding:** A basic layer system exists (`LayerManager`, `LayerData` in `LayerModels.cs`) but it's underutilized. The current implementation has **implicit layering** through rendering order but lacks **explicit layer-aware constraints, validation, and tool behavior**. The proposed 8-layer architecture can build on this foundation with minimal disruption.

---

## 1. Inventory of Existing Elements

### 1.1 Model Classes (Data Structures)

#### Core Simulation Elements
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `NodeData` | NodeModels.cs | Equipment, machines, buffers, workstations | Type, Position (X,Y,Width,Height), Terminals (input/output), Visual (icon, color), Simulation params (servers, capacity, process times) |
| `PathData` | PathModels.cs | Connections between nodes | From/To nodes, ConnectionType (partFlow/AGV/conveyor/etc), Waypoints, Visual (color, thickness), Simulation (speed, capacity) |
| `GroupData` | GroupModels.cs | Logical grouping of nodes, cells | Members list, IsCell flag, InputTerminal, OutputTerminal, InternalRouting, CellType |

#### Building Infrastructure
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `WallData` | WallModels.cs | Wall segments | X1,Y1,X2,Y2, Thickness, WallType (standard/glass/safety/partition), Color, LineStyle, Layer |
| `DoorData` | WallModels.cs | Doors in walls | WallId, Position, Width, DoorType (single/double/overhead) |
| `ColumnData` | WallModels.cs | Structural columns | X, Y, Diameter/Width/Height, Shape (circle/rectangle/I-beam) |
| `MeasurementData` | WallModels.cs | Dimension lines | Start/End points, Text, Color |

#### Spatial Planning
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `ZoneData` | ZoneModels.cs | Spatial zones (production, storage, shipping, etc) | Type, Name, Points (polygon), Visual (fill color, border), Bounds (X,Y,Width,Height) |
| `CorridorData` | ZoneModels.cs | Transport corridors/aisles | Name, Points, Width, Bidirectional, Color |

#### Transport Systems
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `TransportNetworkData` | TransportModels.cs | Container for transport system | Name, Stations, Waypoints, Segments, Transporters, Visibility, Visual settings |
| `TransportStationData` | TransportModels.cs | Pickup/dropoff points | Type (pickup/dropoff/home/charging), Position, LinkedNodeId, NetworkId, Visual |
| `WaypointData` | TransportModels.cs | Navigation points | Position, NetworkId, IsIntersection |
| `TransporterTrackData` | TransportModels.cs | Track definition | Name, Segments, NetworkId, Color |
| `TrackSegmentData` | TransportModels.cs | Track connection | From/To waypoint IDs, Bidirectional, Speed, Width, TrafficZone |
| `TransporterData` | TransportModels.cs | Vehicle definition | Type (AGV/forklift/tugger), HomeStationId, Speed, Capacity, Count |

#### Crane Systems
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `RunwayData` | CraneModels.cs | Crane runway rails | StartX/Y, EndX/Y, Height, Color |
| `EOTCraneData` | CraneModels.cs | Electric Overhead Traveling crane | RunwayId, ReachLeft/Right, ZoneMin/Max, Speeds (bridge/trolley/hoist), Color |
| `JibCraneData` | CraneModels.cs | Jib/swing crane | X, Y, Radius, MinAngle/MaxAngle, Height, Speed |
| `HandoffPointData` | CraneModels.cs | Crane pickup/dropoff zones | RunwayId, Position, LinkedNodeId, Type |

#### Transport Marker System (NEW - recently added)
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `TransportMarker` | TransportMarkerModels.cs | Backbone junction points | Position, NetworkId, MarkerType (junction/terminus/crossing), Color (orange diamond) |
| `TransportLink` | TransportMarkerModels.cs | Machine terminal → transport path | NodeId, TerminalType, MarkerId, LinkType |
| `TransportGroup` | TransportMarkerModels.cs | Resource collection for zones | Name (T01, C01, etc), DisplayName, Color, ResourceList, BoxPosition |

#### Layer System (Existing but Underutilized)
| Model Class | File | Purpose | Key Properties |
|------------|------|---------|----------------|
| `LayerData` | LayerModels.cs | Drawing layer | Name, IsVisible, IsLocked, Opacity, ZOrder, Style, LayerType |
| `LayerManager` | LayerModels.cs | Layer management | Layers collection, ActiveLayerId, visibility/lock helpers |
| `LayerStyle` | LayerModels.cs | Visual style per layer | StrokeColor/Width, FillColor/Opacity, DashArray, LineCap/Join |

**Layer Types Already Defined:**
```csharp
BackgroundImage, Grid, Walls, Columns, Zones, Corridors, 
Paths, Nodes, Cells, Measurements, Annotations, Custom
```

#### Supporting Models
| Model Class | File | Purpose |
|------------|------|---------|
| `TemplateData` | LayoutData.cs | Saved node/path groups for reuse |
| `LayoutMetadata` | SettingsModels.cs | Name, author, dates, units, pixels-per-unit |
| `CanvasSettings` | SettingsModels.cs | Canvas size, grid size, snap mode |
| `DisplaySettings` | SettingsModels.cs | ShowGrid, ShowRulers, PathRoutingDefault, LayerVisibility |
| `BackgroundImage` | (referenced in LayoutData) | Background CAD image |

### 1.2 Z-Ordering Analysis

Current rendering order (from `MainWindow.xaml.cs` Redraw method):
```
Layer 1: Background image (if present)
Layer 2: Grid
Layer 3: Zones
Layer 4: Walls and columns
Layer 5: Paths
Layer 6: Nodes
Layer 7: Groups/Cells (borders and terminals)
Layer 8: Transport networks (stations, waypoints, tracks)
Layer 9: Cranes (runways, envelopes, handoff points)
Layer 10: Transport markers (orange diamonds)
Layer 11: Measurements
Layer 12: Selection highlights
```

**Observations:**
- Z-ordering is **implicit** - determined by order of draw calls in `Redraw()`
- No formal layer constraint enforcement
- Visibility toggles exist (`_layout.Display.Layers.*`) but are boolean flags, not layer objects
- Rendering can be toggled per type but not organized into formal layers

---

## 2. Current Rendering Architecture

### 2.1 Renderer Classes

| Renderer | File | Responsibilities |
|----------|------|------------------|
| `GridRenderer` | GridRenderer.cs | Grid lines, zoom-aware density |
| `NodeRenderer` | NodeRenderer.cs | Nodes (machines, buffers, etc), terminals, icons, badges, labels |
| `PathRenderer` | PathRenderer.cs | Paths between nodes/cells, arrows, waypoint handles |
| `GroupRenderer` | GroupRenderer.cs | Group/cell borders, cell terminals (protruding), zones (rectangles/polygons) |
| `WallRenderer` | WallRenderer.cs | Walls, doors, columns, with selection highlights and endpoint handles |
| `TransportRenderer` | TransportRenderer.cs | Transport networks, stations (circles), waypoints (squares), track segments, transporters at home |
| `CraneRenderer` | CraneRenderer.cs | Runways (lines with rails), EOT crane envelopes (rectangles), jib crane arcs, handoff points |
| `TransportMarkerRenderer` | TransportMarkerRenderer.cs | Orange diamond markers, transport links (dotted lines from nodes), transport groups (dockable boxes) |
| `PathConnectionRenderer` | PathConnectionRenderer.cs | Helper for path waypoint/segment connections |

### 2.2 Rendering Pattern

All renderers follow similar patterns:
1. **Element Creation**: Each element → Canvas UIElement (Rectangle, Line, Ellipse, etc)
2. **Selection Awareness**: Use SelectionService to highlight selected items
3. **Registration**: Elements registered in `_elementMap` dictionary by ID for hit testing
4. **Layered Composition**: Parent (MainWindow) calls renderers in sequence

**Renderer Characteristics:**
- **Stateful**: Renderers hold selection state internally (e.g., `_selectedStationIds`)
- **Canvas-based**: All rendering to WPF Canvas, no separate visual layers
- **Event Agnostic**: Renderers are passive - don't handle mouse events directly

---

## 3. Handler Architecture

### 3.1 Handler Categories

**35 Handler Files** organized by concern:

#### Mouse/Canvas Interaction
- `CanvasMouseHandlers.cs` - MouseDown/Move/Up, drag detection, waypoint dragging
- `CanvasClickHandlers.cs` - Click handling, tool routing, placement logic
- `ContextMenuHandlers.cs` - Right-click menus for nodes, paths, walls, cells

#### Tools
- `ToolboxHandlers.cs` - Tool switching (select, path, wall, pan, area select)
- `WallDrawingHandlers.cs` - Wall placement, orthogonal/free modes, wall types
- `PathHandlers.cs` - Path drawing, waypoint editing, connection logic
- `NodeHandlers.cs` - Node placement, auto-connect, property editing
- `MeasurementHandlers.cs` - Measurement tool, column placement

#### Editing Operations
- `EditHandlers.cs` - Copy, paste, delete, duplicate
- `AlignmentHandlers.cs` - Align left/right/top/bottom/center, distribute
- `GroupHandlers.cs` - Group/ungroup, cell creation, cell editing
- `PropertyHandlers.cs` - Property panel updates, validation

#### Transport
- `TransportHandlers.cs` - Transport network creation, station placement, track drawing
- `TransportMarkerHandlers.cs` - Marker placement, link creation, group management
- `CraneHandlers.cs` - Runway placement, crane configuration, handoff point creation
- `WaypointHandlers.cs` - Waypoint manipulation in transport networks

#### File Operations
- `FileHandlers.cs` - New, open, save, save as (JSON and SQLite)
- `ExportHandlers.cs` - Export to various formats
- `CadImportHandlers.cs` - DXF import
- `LayoutImportHandlers.cs` - Import from other sources
- `RecoveryHandlers.cs` - Auto-save, recovery

#### View
- `ViewHandlers.cs` - Zoom, pan, fit to window
- `ZoomHandlers.cs` - Zoom-specific logic
- `ViewPanelHandlers.cs` - Panel visibility, docking

#### Validation & Analysis
- `ValidationHandlers.cs` - Connectivity, overlap checks
- `AutoPathHandlers.cs` - Automatic path generation
- `TopologyTreeHandlers.cs` - Tree view of layout structure

#### Styling
- `LineStyleHandlers.cs` - Line style selection for walls
- `BackgroundHandlers.cs` - Background image management
- `LayerHandlers.cs` - Layer visibility toggles (basic)
- `IconBrowserHandlers.cs` - Icon selection for nodes

#### Miscellaneous
- `KeyboardHandlers.cs` - Keyboard shortcuts
- `ToolbarHandlers.cs` - Toolbar button actions
- `TemplateHandlers.cs` - Template save/load

### 3.2 Tool State Management

**Current Tool System:**
```csharp
private string _currentTool = "select";  // Global tool state
```

**Available Tools:**
- `"select"` - Default selection/move
- `"path"` - Path drawing mode
- `"wall"` - Wall drawing mode
- `"column"` - Column placement
- `"measure"` - Measurement tool
- `"pan"` - Pan/scroll canvas
- `"area"` - Area selection box

**Tool Behavior:**
- Tools are **global state** - not layer-aware
- Mouse handlers check `_currentTool` to route behavior
- No formal tool/layer binding

### 3.3 Selection Service

`SelectionService.cs` - Centralized selection management:

```csharp
// Tracks multiple selection types simultaneously
private HashSet<string> _selectedNodeIds
private HashSet<string> _selectedGroupIds
private HashSet<string> _selectedWallIds
private HashSet<string> _selectedPathIds
private string? _selectedPathId      // Single path for editing
private string? _editingCellId       // Cell edit mode
```

**Features:**
- Multi-select support (Shift-click)
- Group selection with member highlighting
- Cell edit mode (enter/exit)
- Selection changed events
- No layer awareness

---

## 4. Data Persistence Architecture

### 4.1 Current Save/Load Formats

**Primary Format: JSON**
- File extension: `.layout.json`
- Full serialization of `LayoutData` object
- Human-readable, version control friendly

**Secondary Format: SQLite**
- File extension: `.layout.db`
- Service: `SqliteLayoutService.cs`
- Relational schema with 20+ tables
- Portable single-file database

**Schema Highlights** (from `SqliteLayoutService.cs`):
```sql
layouts, nodes, paths, path_waypoints, cells, cell_members,
cell_entry_points, cell_exit_points, cell_internal_paths,
walls, doors, columns, zones, transport_networks, 
transport_stations, transport_waypoints, transport_segments,
transporters, runways, cranes, jib_cranes, handoff_points
```

**Postgres Schema** (from `Config/LayoutSchema.sql`):
- More comprehensive schema for server deployment
- Includes timestamps, foreign keys, indexes
- Migration scripts present

### 4.2 Data Flow

```
User Edits → LayoutData (in-memory ObservableCollection) 
          → MarkDirty() 
          → Auto-save timer 
          → JSON/SQLite serialization
```

**Key Files:**
- `FileHandlers.cs` - Open/Save/SaveAs logic
- `SqliteLayoutService.cs` - SQLite read/write
- `PostgresExportService.cs` - Postgres export
- `RecoveryHandlers.cs` - Auto-save to `.recovery.json`

### 4.3 Migration Path Consideration

**Current Situation:**
- No explicit layer assignment in existing files
- Elements are "flat" in collections (Nodes, Paths, Walls, Zones, etc.)
- LayerManager exists but layers are mostly empty or unused

**Migration Strategy Needed:**
- Read old files → assign elements to layers based on type
- Add layer field to each element type
- Maintain backward compatibility

---

## 5. Service Architecture

### 5.1 Core Services

| Service | File | Purpose | Layer Relevance |
|---------|------|---------|-----------------|
| `SelectionService` | SelectionService.cs | Multi-type selection tracking | Should be layer-aware |
| `HitTestService` | HitTestService.cs | Point-to-element hit detection | Should respect layer visibility/lock |
| `UndoService` | UndoService.cs | Undo/redo stack | Layer-agnostic |
| `AlignmentService` | AlignmentService.cs | Alignment and distribution | Should work within active layer |
| `AutoRouterService` | AutoRouterService.cs | Automatic path generation | Layer 3/4 specific |
| `AutoPathDetector` | AutoPathDetector.cs | Detect missing paths | Layer-aware validation |
| `TransportPathService` | TransportPathService.cs | Transport network graph operations | Layer 4/5/6 specific |
| `TransportMarkerService` | TransportMarkerService.cs | Marker/link/group management | Cross-layer coordination |

### 5.2 Validation Services

| Service | File | Purpose |
|---------|------|---------|
| `ConnectivityValidator` | Helpers/ConnectivityValidator.cs | Verify all nodes reachable |
| `OverlapValidator` | Helpers/OverlapValidator.cs | Check node/wall overlaps |
| `PathValidator` | Helpers/PathValidator.cs | Validate path connections |
| `NodeValidator` | Helpers/NodeValidator.cs | Node property validation |
| `SimulationValidator` | Helpers/SimulationValidator.cs | Simulation parameter checks |

**Observation:** Validators are type-specific, not layer-aware. Should check layer-specific constraints.

### 5.3 Import/Export Services

| Service | File | Purpose |
|---------|------|---------|
| `CadImportService` | CadImportService.cs | DXF import to walls/zones |
| `ExportService` | ExportService.cs | Export to various formats |
| `PostgresExportService` | PostgresExportService.cs | Export to Postgres DB |
| `PostgresImportService` | PostgresImportService.cs | Import from Postgres |
| `SqlFileImporter` | SqlFileImporter.cs | Import SQL scripts |

---

## 6. Gaps and Opportunities

### 6.1 What's Missing

#### Layer System
- ✅ **Exists:** LayerData, LayerManager models
- ❌ **Missing:** Layer-aware tools, constraints, validation
- ❌ **Missing:** UI for layer management (panel, toolbar)
- ❌ **Missing:** Layer-specific rendering pipelines
- ❌ **Missing:** Cross-layer dependency validation

#### Transport Layer Separation
- **Current:** All transport types mixed (AGV, forklift, crane)
- **Needed:** Separate layers for Layer 4 (AGV), Layer 5 (Crane), Layer 6 (Forklift)
- **Needed:** Crossing zone management between transport layers

#### Cell System (Layer 3)
- ✅ **Exists:** GroupData with IsCell flag, internal routing
- ❌ **Missing:** Formal cell topology validation
- ❌ **Missing:** Conveyor subtype distinction
- ❌ **Missing:** Cell-level constraint enforcement

#### Infrastructure Layer (Layer 0)
- ✅ **Exists:** Walls, columns, doors
- ❌ **Missing:** Immutability flag (prevent accidental editing)
- ❌ **Missing:** Crane runways in Layer 0 (currently in crane-specific collection)
- ❌ **Missing:** Fixed utilities, conduits

#### Pedestrian Layer (Layer 7)
- ❌ **Missing:** Completely absent
- **Needed:** Walkable regions, exclusion zones, crossings
- **Needed:** Path validation to exits

### 6.2 Tight Coupling Issues

1. **Renderers + Selection:** Each renderer manages own selection state → should use centralized layer-aware selection
2. **Handlers + Tools:** Tool state is global string → should be tool object with layer context
3. **Hit Testing:** Flat traversal of all elements → should traverse by layer with early exit
4. **Validation:** Type-specific validators → should be layer-specific with cross-layer rules

### 6.3 Architectural Strengths

**What's Working Well:**
1. ✅ **Clean Model Separation:** Models are POCOs with `NotifyBase` for property changes
2. ✅ **Observable Collections:** Easy binding to UI, automatic updates
3. ✅ **Renderer Isolation:** Each renderer is self-contained, testable
4. ✅ **Handler Modularity:** 35 partial classes keep concerns separated
5. ✅ **Service Layer:** Clear separation of business logic from UI
6. ✅ **Multiple Persistence:** JSON (dev), SQLite (portable), Postgres (server)
7. ✅ **Extensible Node Types:** Icon-based system, JSON config

---

## 7. Current Element to Layer Mapping

Based on existing structure, here's how current elements naturally map to the proposed 8 layers:

| Layer | Layer Name | Current Elements | Model Classes |
|-------|-----------|------------------|---------------|
| **0** | Fixed Infrastructure | Walls (WallType=exterior), Columns, **Runways** | WallData, ColumnData, RunwayData |
| **1** | Spatial Planning | Zones, Corridors | ZoneData, CorridorData |
| **2** | Equipment | Nodes (machines, buffers, storage, workstations) | NodeData |
| **3** | Local Flow (Cells) | Groups (IsCell=true), Paths (ConnectionType=conveyor), InternalPaths | GroupData, PathData (filtered) |
| **4** | Guided Transport (AGV) | TransportNetworks (TransporterType=AGV/AMR), Stations, Waypoints, Tracks | TransportNetworkData, TransportStationData, WaypointData, TrackSegmentData |
| **5** | Overhead Transport (Cranes) | EOTCranes, JibCranes, HandoffPoints, **Coverage Polygons** (new) | EOTCraneData, JibCraneData, HandoffPointData |
| **6** | Flexible Transport (Forklift) | **Aisles** (new - derived from Corridors?), **Crossing Zones** (new) | *New models needed* |
| **7** | Pedestrian | **Walkable Regions** (new), **Exclusion Zones** (new), **Crossings** (new) | *New models needed* |

**Doors:** Special case - associated with walls (Layer 0) but can be Layer 1 if interior partitions.

**Measurements:** Annotation layer, not part of 8-layer system (always on top).

**Paths (Logical):** Paths with `ConnectionType=partFlow/resourceBinding/controlSignal` are logical, span layers 2-3.

---

## 8. Renderer Strategy Analysis

### 8.1 Current Pattern: Type-Based Renderers

**Pros:**
- Clear ownership: NodeRenderer owns all node rendering
- Easy to find rendering code for a specific element type
- Independent development of renderers

**Cons:**
- Not layer-aware
- Z-order implicit, fragile
- Hard to toggle layer visibility consistently

### 8.2 Proposed: Hybrid Approach

**Recommendation:** Keep type-based renderers, wrap them in layer renderers.

```
CompositeLayerRenderer
├── Layer0Renderer (uses WallRenderer for walls, ColumnRenderer for columns)
├── Layer1Renderer (uses GroupRenderer for zones)
├── Layer2Renderer (uses NodeRenderer)
├── Layer3Renderer (uses GroupRenderer for cells + PathRenderer for conveyors)
├── Layer4Renderer (uses TransportRenderer filtered for AGV)
├── Layer5Renderer (uses CraneRenderer)
├── Layer6Renderer (new ForkliftRenderer)
└── Layer7Renderer (new PedestrianRenderer)
```

**Benefits:**
- Reuse existing renderers
- Enforce Z-order by layer
- Easy layer visibility/lock implementation
- Minimal disruption to existing code

---

## 9. Tool-Layer Binding Strategy

### 9.1 Current Tool State
```csharp
private string _currentTool = "select";
```

### 9.2 Proposed Tool Context
```csharp
public class ToolContext
{
    public string ToolName { get; set; }
    public int ActiveLayer { get; set; }  // 0-7
    public bool RespectLayerLock { get; set; } = true;
}

private ToolContext _toolContext = new() { ToolName = "select", ActiveLayer = 2 };
```

### 9.3 Tool-Layer Matrix

| Tool | Layer 0 | Layer 1 | Layer 2 | Layer 3 | Layer 4 | Layer 5 | Layer 6 | Layer 7 |
|------|---------|---------|---------|---------|---------|---------|---------|---------|
| Select | Read | Edit | Edit | Edit | Edit | Edit | Edit | Edit |
| Node Place | ❌ | ❌ | ✅ | Cell interior | ❌ | ❌ | ❌ | ❌ |
| Path Draw | ❌ | ❌ | ✅ (logical) | ✅ (conveyor) | ❌ | ❌ | ❌ | ❌ |
| Wall Draw | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Zone Draw | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| AGV Track | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Crane Place | ❌ (runway) | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Forklift Aisle | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Pedestrian Zone | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

---

## 10. Backward Compatibility Analysis

### 10.1 Existing File Format

**JSON Layout Structure:**
```json
{
  "Metadata": {...},
  "Canvas": {...},
  "Display": {...},
  "LayerManager": {
    "Layers": [],
    "ActiveLayerId": ""
  },
  "Nodes": [...],
  "Paths": [...],
  "Walls": [...],
  "Zones": [...],
  "TransportNetworks": [...],
  etc.
}
```

**Observation:** `LayerManager` already exists in serialized format but is typically empty or unused.

### 10.2 Migration Strategy

**Option 1: Additive Migration (Recommended)**
```csharp
// On load, if elements don't have LayerId assigned:
foreach (var node in layout.Nodes)
{
    if (string.IsNullOrEmpty(node.LayerId))
        node.LayerId = layout.LayerManager.GetLayerByType(LayerTypes.Nodes)?.Id;
}
```

**Option 2: Schema Version**
```json
{
  "SchemaVersion": 2,  // Add version field
  "Metadata": {...}
}
```

### 10.3 Required Model Changes

**Minimal Changes to Existing Models:**
1. Add `LayerId` property to: `NodeData`, `PathData`, `WallData`, `ZoneData`, `GroupData`, transport models
2. Make LayerId **optional** (nullable) for backward compatibility
3. In renderer, if LayerId is null, use default layer for that type

**Example:**
```csharp
public class NodeData : NotifyBase
{
    // EXISTING PROPERTIES
    private string _id = Guid.NewGuid().ToString();
    private string _type = "machine";
    // ... 20+ existing properties ...
    
    // NEW - ADDITIVE
    private string? _layerId;  // Optional, default to Equipment layer
    
    public string? LayerId
    {
        get => _layerId;
        set => SetProperty(ref _layerId, value);
    }
}
```

---

## 11. Performance Considerations

### 11.1 Current Rendering Performance

**Rendering Approach:** Full redraw on any change
- `Redraw()` clears entire canvas, redraws everything
- Inefficient for large layouts (1000+ elements)

**Opportunity:** Layer-based dirty tracking
- Only redraw layers that changed
- Render layers to separate Canvas or VisualBrush for caching

### 11.2 Hit Testing Performance

**Current:** Linear scan of all elements
```csharp
foreach (var node in layout.Nodes.Reverse<NodeData>()) {
    if (HitTest(point, node)) return node;
}
```

**Opportunity:** Spatial indexing + layer filtering
- Check only visible, unlocked layers
- Use R-tree or quadtree for large element counts
- Early exit when hit found in top layer

---

## 12. UI Integration Points

### 12.1 Existing UI Components

**Main Window:**
- `MainWindow.xaml` - Main canvas, toolbars, menus
- AvalonDock integration for panels

**Panels (Controls/):**
- `LayersPanel.xaml` - **EXISTS but underused** - perfect foundation for layer UI
- `DynamicToolbox.xaml` - Node type palette
- `PropertiesPanel.cs` - Element property editing
- `ExplorerPanel.cs` - Tree view of layout
- `TransportGroupPanel.xaml` - Transport resource management

**Observation:** `LayersPanel` already exists! It just needs to be populated and wired up to LayerManager.

### 12.2 Layer Panel Requirements

**Must Have:**
1. List of 8 layers (0-7) in order
2. Eye icon - toggle visibility
3. Lock icon - toggle editability
4. Active layer indicator (radio button or highlight)
5. Opacity slider per layer
6. Layer order reordering (drag-drop) - **but fixed order for 0-7**

**Nice to Have:**
1. Layer presets (e.g., "All", "Simulation", "Transport Only")
2. Layer color indicators
3. Element count per layer
4. Quick filter: Show only active layer

### 12.3 Status Bar Integration

**Add Layer Indicator:**
```
[Status] | Active Layer: Equipment (2) | Snap: Grid | Zoom: 100%
```

### 12.4 Toolbar Integration

**Layer Quick Buttons:**
- Dropdown: Select active layer
- Toggles: Show/Hide Walls, Zones, Transport, Cranes

---

## 13. Validation & Constraint System

### 13.1 Current Validation

**Validators exist** but are type-specific:
- Node overlaps
- Path connectivity
- Simulation parameter ranges

### 13.2 Layer-Specific Constraints (Needed)

#### Layer 0 (Infrastructure)
- Walls must form closed boundaries (optional)
- Columns cannot overlap walls
- Runways must be linear

#### Layer 1 (Spatial Planning)
- Zones must be within building envelope (Layer 0 walls)
- Zones cannot overlap (optional - warn only)
- Corridors must connect zones

#### Layer 2 (Equipment)
- Nodes must be within zones (Layer 1)
- Nodes cannot overlap
- Nodes must have clearance from walls (Layer 0)

#### Layer 3 (Cells)
- Cell members must be contiguous
- Internal paths must connect cell terminals
- Conveyors must be within cell bounds

#### Layer 4 (AGV)
- AGV tracks cannot overlap
- Stations must dock to nodes (Layer 2)
- Network must be connected

#### Layer 5 (Cranes)
- Crane coverage must include required pickup/dropoff nodes
- Runways must not intersect equipment (except at handoff)

#### Layer 6 (Forklift)
- Aisle width must accommodate vehicle turning radius
- Crossings with AGV paths must have protocol

#### Layer 7 (Pedestrian)
- Path must exist to all exits
- Crossings at transport paths

### 13.3 Cross-Layer Dependencies

**Layer Dependency Graph:**
```
Layer 0 (Infrastructure) - No dependencies (foundation)
  ↓
Layer 1 (Zones) - Must be within Layer 0 walls
  ↓
Layer 2 (Equipment) - Must be within Layer 1 zones
  ↓ ↓ ↓
Layer 3 (Cells) - Groups Layer 2 equipment
Layer 4 (AGV) - Connects to Layer 2 nodes
Layer 5 (Cranes) - Picks/drops at Layer 2 nodes
  ↓
Layer 6 (Forklift) - May cross Layer 4 AGV paths
  ↓
Layer 7 (Pedestrian) - Avoids all above, crosses all
```

---

## 14. Testing Considerations

### 14.1 Testable Units

**Well-Separated:**
- ✅ Models (pure data, no WPF dependencies)
- ✅ Validators (pure logic)
- ✅ Services (most are stateless)

**Harder to Test:**
- ⚠️ Renderers (depend on WPF Canvas)
- ⚠️ Handlers (depend on MainWindow state)

### 14.2 Test Strategy

**Unit Tests:**
- Layer manager state transitions
- LayerId assignment on load
- Validation rules per layer

**Integration Tests:**
- Save layout → Load layout → Verify layer assignments
- Tool operations respect layer lock

**Visual Tests:**
- Snapshot testing (render to bitmap, compare)
- Layer Z-order correctness

---

## 15. Key Findings Summary

### Strengths
1. ✅ **Solid Foundation:** Well-architected with clear separation
2. ✅ **Layer System Exists:** LayerManager already implemented, just needs activation
3. ✅ **Modular Renderers:** Easy to wrap in layer-specific renderers
4. ✅ **Comprehensive Models:** Most element types already modeled
5. ✅ **Multiple Persistence:** JSON, SQLite, Postgres

### Weaknesses
1. ❌ **Layer System Unused:** Exists but not integrated into tools, validation, rendering
2. ❌ **Implicit Z-Order:** Fragile rendering order
3. ❌ **No Layer Constraints:** Tools and validators ignore layers
4. ❌ **Missing Layers:** Forklift (Layer 6), Pedestrian (Layer 7) not modeled
5. ❌ **Transport Types Mixed:** AGV/Crane/Forklift all in same collections

### Opportunities
1. 🎯 **Activate Existing Layer System:** LayersPanel, LayerManager are 80% done
2. 🎯 **Minimal Model Changes:** Just add LayerId property to existing models
3. 🎯 **Wrap, Don't Rewrite:** Composite renderer pattern reuses existing renderers
4. 🎯 **Backward Compatible:** Migration strategy is straightforward
5. 🎯 **Clear Layer Boundaries:** Current elements map cleanly to 8 layers

---

## 16. Recommended Next Steps

### Phase 2 (Layer Architecture Design) - Immediate
1. Formalize 8-layer specifications with complete element lists
2. Define cross-layer dependency rules
3. Design layer constraint validation system

### Phase 3 (Implementation Planning) - Next
1. Create layer renderer wrapper architecture
2. Design tool-layer binding system
3. Plan UI components (activate LayersPanel)
4. Define database schema additions

### Phase 4 (Implementation Sequence) - Then
1. Implement Layer Manager infrastructure first
2. Start with Layer 0+1+2 (foundation layers)
3. Progressively add transport layers 4-6
4. Finish with derived layer 7 (pedestrian)

---

## 17. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing layouts | HIGH | Additive migration, LayerId optional |
| Performance degradation | MEDIUM | Layer-based dirty tracking, caching |
| UI complexity explosion | MEDIUM | Presets, layer groups, sensible defaults |
| Over-engineering | LOW | Start with MVP (Layers 0-2), iterate |
| User learning curve | MEDIUM | Progressive disclosure, tooltips, tutorial |

---

## 18. File Manifest Preview

**Files to Create (New):**
- `Services/LayerRenderOrchestrator.cs` - Composite layer renderer
- `Services/Layer0Renderer.cs` through `Layer7Renderer.cs` - Layer renderers
- `Services/LayerConstraintValidator.cs` - Cross-layer validation
- `Models/ForkliftModels.cs` - Layer 6 models
- `Models/PedestrianModels.cs` - Layer 7 models
- `Handlers/ForkliftHandlers.cs` - Layer 6 tools
- `Handlers/PedestrianHandlers.cs` - Layer 7 tools

**Files to Modify (Existing):**
- `Models/NodeModels.cs` - Add LayerId property
- `Models/PathModels.cs` - Add LayerId property
- `Models/WallModels.cs` - Add LayerId property
- `Models/ZoneModels.cs` - Add LayerId property
- `Models/LayerModels.cs` - Enhance LayerManager with new layer types
- `MainWindow.xaml.cs` - Replace Redraw() with layer orchestrator
- `Handlers/LayerHandlers.cs` - Wire up layer panel events
- `Controls/LayersPanel.xaml` - Populate with 8 layers
- `Services/HitTestService.cs` - Add layer awareness
- `Services/SelectionService.cs` - Add layer filtering
- `Services/SqliteLayoutService.cs` - Add layer fields to schema

**Files to Review (Dependencies):**
- All 35 handler files - Check for _currentTool usage
- All renderer files - Ensure consistent patterns

---

## 19. Database Schema Preview

**New Tables:**
```sql
CREATE TABLE layer_assignments (
    element_id TEXT NOT NULL,
    element_type TEXT NOT NULL,  -- 'node', 'path', 'wall', etc.
    layer_id TEXT NOT NULL,
    FOREIGN KEY (layer_id) REFERENCES layers(id)
);

CREATE TABLE forklift_aisles (
    id TEXT PRIMARY KEY,
    layout_id TEXT NOT NULL,
    layer_id TEXT NOT NULL,
    name TEXT,
    width REAL,
    points TEXT,  -- JSON array
    vehicle_type TEXT,
    FOREIGN KEY (layout_id) REFERENCES layouts(id)
);

CREATE TABLE pedestrian_regions (
    id TEXT PRIMARY KEY,
    layout_id TEXT NOT NULL,
    layer_id TEXT NOT NULL,
    region_type TEXT,  -- 'walkable', 'exclusion', 'crossing'
    points TEXT,  -- JSON array
    FOREIGN KEY (layout_id) REFERENCES layouts(id)
);

CREATE TABLE crossing_zones (
    id TEXT PRIMARY KEY,
    layout_id TEXT NOT NULL,
    agv_track_id TEXT,
    forklift_aisle_id TEXT,
    protocol TEXT,  -- 'agv_priority', 'forklift_priority', 'stop_sign'
    FOREIGN KEY (layout_id) REFERENCES layouts(id)
);
```

**Modified Tables:**
```sql
ALTER TABLE nodes ADD COLUMN layer_id TEXT;
ALTER TABLE paths ADD COLUMN layer_id TEXT;
ALTER TABLE walls ADD COLUMN layer_id TEXT;
ALTER TABLE zones ADD COLUMN layer_id TEXT;
-- etc.
```

---

## Conclusion

This layout editor has a **strong architectural foundation** with clear separation of concerns. The layer system infrastructure already exists but is inactive. The proposed 8-layer architecture can be implemented with:

1. **Minimal disruption:** Additive changes to models
2. **Backward compatibility:** Optional LayerId with migration logic
3. **Reuse of existing code:** Wrap renderers, enhance services
4. **Progressive implementation:** Layer-by-layer rollout

The biggest challenge is **not technical but organizational**: ensuring consistent layer awareness across 35 handler files. The technical debt is low, and the codebase is ready for this architectural enhancement.

**Recommendation:** Proceed to Phase 2 (Layer Architecture Design) with confidence.

---

**End of Phase 1 Code Analysis Report**

