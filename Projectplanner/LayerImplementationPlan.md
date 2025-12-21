# Layout Editor Layer Implementation Plan

## Instructions and Planning Steps

---

## Overview

This plan implements an 8-layer architecture for the layout editor over 6 weeks. Each week focuses on specific layers with clear tasks, tests, and completion criteria. No code is provided — only instructions for what to build and how to verify it works.

---

## Week 1: Layer Infrastructure

### Objective
Create the foundation that all layers depend on: enums, metadata, manager service, and UI panel.

### Tasks

**Task 1.1: Create Layer Type Enumeration**
- Create enum with 8 values: Infrastructure(0), Spatial(1), Equipment(2), LocalFlow(3), GuidedTransport(4), OverheadTransport(5), FlexibleTransport(6), Pedestrian(7)
- Place in Models/Layers/ folder
- Values must be sequential integers starting at 0

**Task 1.2: Create Layer Metadata Class**
- Define metadata for each layer: name, description, Z-order base, default color
- Z-order must increase with layer number (Infrastructure lowest, Pedestrian highest)
- Include visibility, editability, and locked flags
- Provide static array of all layer metadata

**Task 1.3: Create Transport Layer Manager Service**
- Service tracks active layer, visibility states, editability states
- Fires events when active layer changes
- Fires events when visibility changes
- Methods: SetVisibility, SetEditable, SetLocked, GetVisibleLayers, IsEditable
- Default active layer: Equipment
- Default visibility: all layers visible

**Task 1.4: Wire Manager to MainWindow**
- Add manager as field in MainWindow
- Initialize in constructor after InitializeComponent
- Subscribe to visibility changed event → trigger Redraw
- Subscribe to active layer changed → update status bar

**Task 1.5: Create Layer Panel UI**
- UserControl with list of all 8 layers
- Each row shows: visibility checkbox, color indicator, layer name, lock toggle
- Visibility checkbox toggles layer visibility via manager
- Lock toggle sets layer locked state
- Clicking layer name sets it as active layer
- Active layer highlighted visually

**Task 1.6: Add Layer Property to Existing Models**
- WallData: return Infrastructure
- ColumnData: return Infrastructure  
- ZoneData: return Spatial
- NodeData: return Equipment
- PathData: return LocalFlow (default, can be changed)

### Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| T1.1 | Layer enum has 8 values | Count equals 8 |
| T1.2 | Infrastructure is value 0 | (int)Infrastructure == 0 |
| T1.3 | Pedestrian is value 7 | (int)Pedestrian == 7 |
| T1.4 | All layers visible by default | IsVisible returns true for all |
| T1.5 | Active layer default is Equipment | ActiveLayer == Equipment |
| T1.6 | Visibility change fires event | Event handler called |
| T1.7 | Locked layer not editable | IsEditable returns false when locked |
| T1.8 | Application starts without error | No exceptions on startup |
| T1.9 | Existing layouts still load | Open old file, no errors |

### Completion Criteria
- [ ] LayerType enum exists
- [ ] LayerMetadata class exists
- [ ] TransportLayerManager service works
- [ ] Layer panel displays in UI
- [ ] All 8 tests pass
- [ ] Existing functionality unchanged

---

## Week 2: Layer 0 (Infrastructure) + Layer 1 (Spatial)

### Objective
Formalize infrastructure elements (walls, columns, doors, runways) and spatial elements (zones, aisles, restricted areas) into their respective layers with dedicated renderers.

### Tasks

**Task 2.1: Create Door Model**
- Properties: Id, Name, X, Y, Width, Height, Rotation
- Door types: Personnel, Dock, Fire, Emergency
- Swing properties: angle, direction (inward/outward/sliding)
- Layer property returns Infrastructure

**Task 2.2: Create Crane Runway Model**
- Properties: Id, Name, X1, Y1, X2, Y2 (line endpoints)
- Height above floor, capacity (kg)
- RunwayPairId to link parallel runways
- Layer property returns Infrastructure

**Task 2.3: Create Opening Base Model**
- Properties: Id, Name, X, Y, OpeningType
- Capacity: 0 = unconstrained (no token), N = limited (token required)
- State: Open, Closed, Locked, Emergency (all openings have state)
- Physical limits: ClearWidth, ClearHeight, MaxLoadWeight
- AllowedEntityTypes array (null = all allowed)
- DirectionMode: Bidirectional, InboundOnly, OutboundOnly
- TraversalTime (seconds, can be distribution)
- Connections: FromZoneId, ToZoneId
- BlockingConditions array (e.g., "CraneInZone", "FireAlarm")
- Layer property returns Infrastructure

**Task 2.4: Create Opening Subtypes**
- UnconstrainedOpening (capacity = 0): Aisle, BayEntrance, EmergencyExit
- ConstrainedOpening (capacity >= 1): base for doors, hatches, etc.
- Door: SwingDirection, AutoClose, AccessControl
- Hatch: IsVertical, LadderTime
- Manhole: ConfinedSpaceProtocol, RequiresPermit
- Gate: VehicleOnly, BarrierType

**Task 2.5: Create Temporary Opening Extension**
- For shipbuilding: openings that exist only during certain assembly states
- ExistsFromState, ExistsUntilState, CreatedByOperationId
- Pathfinder excludes openings that don't exist in current state

**Task 2.6: Add Infrastructure Collections to LayoutData**
- Add Doors collection (ObservableCollection)
- Add CraneRunways collection (ObservableCollection)
- Add Openings collection (ObservableCollection) - base type for all openings
- Ensure JSON serialization handles opening subtypes correctly

**Task 2.7: Create Infrastructure Renderer**
- Implements ILayerRenderer interface
- Layer property returns Infrastructure
- ZOrderBase is lowest (0-99 range)
- Renders: walls, columns, openings, crane runways
- Extract existing wall/column rendering into this renderer
- Openings render as gap in wall with symbol indicating type

**Task 2.8: Create Primary Aisle Model**
- Properties: Id, Name, Centerline (list of points), Width
- IsEmergencyRoute flag
- AisleType: Main, Secondary, Emergency
- Layer property returns Spatial

**Task 2.9: Create Restricted Area Model**
- Properties: Id, Name, X, Y, Width, Height
- RestrictionType: AuthorizedOnly, Hazmat, Cleanroom, HighVoltage
- RequiredPPE field (text)
- Layer property returns Spatial

**Task 2.10: Add Spatial Collections to LayoutData**
- Add PrimaryAisles collection
- Add RestrictedAreas collection
- Zones collection already exists

**Task 2.11: Create Spatial Renderer**
- Implements ILayerRenderer interface
- Layer property returns Spatial
- ZOrderBase is 100-199 range
- Renders: zones, primary aisles, restricted areas
- Extract existing zone rendering into this renderer
- Aisles render as corridors with dashed edges
- Emergency routes render in yellow
- Restricted areas render with warning pattern

**Task 2.12: Modify Redraw to Use Layer Renderers**
- Clear canvas
- Draw grid
- Iterate visible layers in Z-order (from GetVisibleLayers)
- Call appropriate renderer for each layer
- Draw selection handles last

**Task 2.13: Modify Selection to Respect Layers**
- Hit test only visible layers
- Hit test only editable layers
- Test layers in reverse Z-order (top first)
- Return first hit found

### Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| T2.1 | DoorData has Infrastructure layer | Layer == Infrastructure |
| T2.2 | CraneRunwayData has Infrastructure layer | Layer == Infrastructure |
| T2.3 | Opening base class has Infrastructure layer | Layer == Infrastructure |
| T2.4 | Unconstrained opening has capacity 0 | Capacity == 0 |
| T2.5 | Constrained opening has capacity >= 1 | Capacity >= 1 |
| T2.6 | Opening state defaults to Open | State == Open |
| T2.7 | All openings have state machine | Can transition Open → Closed → Open |
| T2.8 | Closed opening blocks traversal | Even capacity=0 opening blocks when closed |
| T2.9 | PrimaryAisleData has Spatial layer | Layer == Spatial |
| T2.10 | RestrictedAreaData has Spatial layer | Layer == Spatial |
| T2.11 | Hide Infrastructure hides walls and openings | Toggle off, elements disappear |
| T2.12 | Hide Spatial hides zones | Toggle off, zones disappear |
| T2.13 | Hidden layer elements not selectable | Click passes through |
| T2.14 | Locked layer elements not editable | Cannot move or modify |
| T2.15 | Save includes openings with subtypes | Save, reload, door properties intact |
| T2.16 | Opening connects two zones | FromZoneId and ToZoneId valid |

### Completion Criteria
- [ ] Opening base model with capacity and state
- [ ] Unconstrained openings (capacity=0) work
- [ ] Constrained openings (capacity>=1) work
- [ ] Door, Hatch, Manhole subtypes implemented
- [ ] Temporary opening model working
- [ ] Opening renders as passage between zones
- [ ] State machine affects all openings
- [ ] Primary aisle model and renderer working
- [ ] Restricted area model and renderer working
- [ ] Infrastructure renderer handles openings
- [ ] Spatial renderer extracts existing zone code
- [ ] Layer visibility controls what renders
- [ ] Layer editability controls what's selectable
- [ ] Z-order correct (Infrastructure below Spatial)
- [ ] All 16 tests pass

---

## Week 3: Layer 2 (Equipment) + Layer 3 (Local Flow)

### Objective
Formalize equipment layer with clearance zones; create local flow layer for cells, conveyors, and direct paths.

### Tasks

**Task 3.1: Add Clearance Properties to Equipment**
- Add to NodeVisualData: OperationalClearance (4 sides), MaintenanceClearance (4 sides)
- Default operational: 50 units each side
- Default maintenance: 100 units each side
- Clearance is distance from equipment edge

**Task 3.2: Create Equipment Renderer**
- Implements ILayerRenderer interface
- Layer property returns Equipment
- ZOrderBase is 200-299 range
- Extract existing node rendering into this renderer
- Add clearance zone rendering (toggleable)
- Operational clearance: blue tint, dashed border
- Maintenance clearance: orange tint, dashed border

**Task 3.3: Add Clearance Toggle to View Menu**
- Menu item: Show Operational Clearance (checkable)
- Menu item: Show Maintenance Clearance (checkable)
- Toggles affect equipment renderer

**Task 3.4: Create Conveyor Model**
- Properties: Id, Name, Path (list of points), Width, Speed
- ConveyorType: Belt, Roller, Chain, Overhead
- Direction: Forward, Reverse, Bidirectional
- IsAccumulating flag
- FromNodeId, ToNodeId for endpoint connections
- Layer property returns LocalFlow

**Task 3.5: Create Direct Path Model**
- Properties: Id, FromNodeId, ToNodeId
- TransferType: Manual, Robot, Gravity, Pneumatic
- TransferTime (seconds)
- Layer property returns LocalFlow
- Represents within-cell material movement

**Task 3.6: Add Local Flow Collections to LayoutData**
- Add Conveyors collection
- Add DirectPaths collection
- Cells use existing Groups collection (filter by IsCell)

**Task 3.7: Create Local Flow Renderer**
- Implements ILayerRenderer interface
- Layer property returns LocalFlow
- ZOrderBase is 300-399 range
- Renders cell boundaries around grouped equipment
- Renders conveyors as wide lines with direction chevrons
- Renders direct paths as thin arrows between equipment

**Task 3.8: Create Conveyor Tool**
- Tool for drawing conveyor paths
- Click to add points, double-click to finish
- Snaps to equipment terminals
- Sets width via property panel

**Task 3.9: Cell Boundary Calculation**
- Given a cell (group with IsCell=true), compute bounding polygon
- Include clearance buffers
- Render as colored boundary with cell name label

### Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| T3.1 | Equipment has clearance properties | Properties exist, defaults correct |
| T3.2 | Clearance renders when toggled on | Blue/orange zones visible |
| T3.3 | Clearance hidden when toggled off | Zones not visible |
| T3.4 | ConveyorData has LocalFlow layer | Layer == LocalFlow |
| T3.5 | DirectPathData has LocalFlow layer | Layer == LocalFlow |
| T3.6 | Conveyor renders with direction | Chevrons point correct way |
| T3.7 | Cell boundary contains members | All member equipment inside boundary |
| T3.8 | Conveyor snaps to terminals | Endpoint aligns with equipment terminal |
| T3.9 | Hide LocalFlow hides conveyors | Toggle off, conveyors disappear |
| T3.10 | Save/load conveyors | Create, save, reload, still present |

### Completion Criteria
- [ ] Equipment clearance properties exist
- [ ] Clearance visualization toggles work
- [ ] Conveyor model and renderer working
- [ ] Direct path model and renderer working
- [ ] Cell boundaries render correctly
- [ ] Conveyor tool creates conveyors
- [ ] Local flow layer visibility works
- [ ] All 10 tests pass

---

## Week 4: Layer 4 (Guided Transport / AGV)

### Objective
Implement AGV path network with waypoints, stations, and traffic zones.

### Tasks

**Task 4.1: Create AGV Waypoint Model**
- Properties: Id, X, Y
- WaypointType: Through, Decision, Stop, SpeedChange, ChargingAccess
- StopTime for mandatory delays
- SpeedAfter for speed change waypoints
- Layer property returns GuidedTransport

**Task 4.2: Create AGV Path Segment Model**
- Properties: Id, FromWaypointId, ToWaypointId
- Width (vehicle width + buffer)
- SpeedLimit
- Direction: Unidirectional, Bidirectional
- Priority for conflict resolution
- PathType: Wire, Magnetic, Painted, Natural
- Layer property returns GuidedTransport

**Task 4.3: Create AGV Station Model**
- Properties: Id, Name, X, Y, Rotation
- StationType: LoadUnload, LoadOnly, UnloadOnly, Charging, Parking
- LinkedEquipmentId (which equipment this serves)
- LinkedWaypointId (network access point)
- DockingTolerance, ServiceTime
- Layer property returns GuidedTransport

**Task 4.4: Create Traffic Zone Model**
- Properties: Id, Boundary (polygon points)
- MaxVehicles (capacity, usually 1)
- ZoneType: Exclusive, Intersection, Passing
- Layer property returns GuidedTransport

**Task 4.5: Add AGV Collections to LayoutData**
- Add AGVWaypoints collection
- Add AGVPaths collection
- Add AGVStations collection
- Add TrafficZones collection

**Task 4.6: Create Guided Transport Renderer**
- Implements ILayerRenderer interface
- Layer property returns GuidedTransport
- ZOrderBase is 400-499 range
- Render order: traffic zones (back), paths, waypoints, stations (front)
- Paths render as double lines (showing width)
- Unidirectional paths show direction arrows
- Waypoints render as diamonds, color-coded by type
- Stations render as rectangles with docking direction indicator

**Task 4.7: Create AGV Path Tool**
- Activated from toolbar
- Click creates/selects waypoint
- Clicking second waypoint creates path segment
- Path continues from last waypoint until Escape or double-click
- Waypoints snap together within threshold (20 pixels)
- Hold Shift for bidirectional path

**Task 4.8: Create AGV Station Tool**
- Click to place station
- Automatically finds nearest waypoint and links
- Rotation handle to set docking direction
- Property panel to set station type and link to equipment

**Task 4.9: Create Traffic Zone Tool**
- Click to place polygon points
- Double-click or close polygon to finish
- Set capacity and zone type in property panel

**Task 4.10: AGV Path Validation**
- Warn if path segment overlaps another (same layer)
- Warn if station has no linked waypoint
- Warn if path network is disconnected (unreachable waypoints)

### Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| T4.1 | Waypoint types render different colors | Visual distinction |
| T4.2 | Path connects two waypoints | FromId/ToId resolve correctly |
| T4.3 | Unidirectional path shows arrow | Arrow visible, correct direction |
| T4.4 | Bidirectional path shows no arrow | No direction indicator |
| T4.5 | Station links to waypoint | LinkedWaypointId valid |
| T4.6 | Station links to equipment | LinkedEquipmentId valid |
| T4.7 | Traffic zone renders as polygon | Boundary visible |
| T4.8 | Hide GuidedTransport hides AGV | Toggle off, all AGV hidden |
| T4.9 | Path tool creates connected network | Can build loop |
| T4.10 | Validation warns on disconnect | Warning shown for orphan waypoint |

### Completion Criteria
- [ ] Waypoint model and renderer working
- [ ] Path segment model and renderer working
- [ ] Station model and renderer working
- [ ] Traffic zone model and renderer working
- [ ] AGV path tool creates networks
- [ ] AGV station tool places and links stations
- [ ] Validation identifies network issues
- [ ] Layer visibility and editability work
- [ ] All 10 tests pass

---

## Week 5: Layer 5 (Overhead/Cranes) + Layer 6 (Flexible/Forklift)

### Objective
Implement crane layer with coverage polygons and handoffs; implement forklift layer with aisles and crossings.

### Tasks

**Task 5.1: Extend Existing Crane Models**
- EOTCraneData and JibCraneData likely exist
- Ensure Layer property returns OverheadTransport
- Add method to compute coverage polygon from runway + bridge geometry

**Task 5.2: Create Crane Coverage Model**
- Computed from crane parameters
- Polygon representing reachable floor area
- For EOT: rectangle from runway limits × bridge travel
- For Jib: arc/sector from pivot point

**Task 5.3: Create Handoff Point Model**
- Properties: Id, X, Y
- Crane1Id, Crane2Id (the two cranes that meet here)
- HandoffType: Direct, GroundBuffer
- Located where crane coverages overlap
- Layer property returns OverheadTransport

**Task 5.4: Create Drop Zone Model**
- Polygon under crane where loads may be suspended
- IsPedestrianExclusion flag (feeds pedestrian layer)
- Layer property returns OverheadTransport

**Task 5.5: Add Overhead Collections to LayoutData**
- Add HandoffPoints collection
- Add DropZones collection
- Cranes collection likely exists

**Task 5.6: Create Overhead Transport Renderer**
- Layer property returns OverheadTransport
- ZOrderBase is 500-599 range
- Render crane runways (from Infrastructure, reference only)
- Render crane coverage as semi-transparent polygon
- Render jib crane as arc sector
- Render handoff points as star markers
- Render drop zones with hazard stripes

**Task 5.7: Create Forklift Aisle Model**
- Properties: Id, Name, Centerline (points), Width
- MinTurningRadius
- AisleType: Main, Secondary, Deadend
- Layer property returns FlexibleTransport

**Task 5.8: Create Staging Area Model**
- Properties: Id, Name, Boundary (polygon)
- Capacity (number of pallets)
- StagingType: Temporary, Buffer, Dock
- Layer property returns FlexibleTransport

**Task 5.9: Create Crossing Zone Model**
- Where forklift aisle crosses AGV path
- Properties: Id, AisleId, AGVPathId
- CrossingType: SignalControlled, PriorityBased, TimeWindowed
- Layer property returns FlexibleTransport

**Task 5.10: Add Flexible Transport Collections to LayoutData**
- Add ForkliftAisles collection
- Add StagingAreas collection
- Add CrossingZones collection

**Task 5.11: Create Flexible Transport Renderer**
- Layer property returns FlexibleTransport
- ZOrderBase is 600-699 range
- Render aisles as wide corridors
- Show turning radius arcs at corners
- Render staging areas with dotted boundary
- Render crossings with zebra stripe pattern

**Task 5.12: Create Forklift Aisle Tool**
- Click to place centerline points
- Width property in panel
- Auto-calculate turning radius requirements at corners

**Task 5.13: Crossing Detection**
- Automatically detect where forklift aisles cross AGV paths
- Prompt user to define crossing type
- Create CrossingZone automatically

### Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| T5.1 | Crane coverage computes correctly | Polygon matches expected area |
| T5.2 | Handoff point at coverage overlap | Located where cranes both reach |
| T5.3 | Drop zone renders with stripes | Visual hazard indication |
| T5.4 | Forklift aisle renders with width | Corridor visible |
| T5.5 | Staging area renders | Boundary visible |
| T5.6 | Crossing detected automatically | System finds AGV/forklift intersection |
| T5.7 | Crossing renders with pattern | Zebra stripes visible |
| T5.8 | Hide OverheadTransport hides cranes | Toggle off, coverage hidden |
| T5.9 | Hide FlexibleTransport hides aisles | Toggle off, aisles hidden |
| T5.10 | All layers render in correct order | Visual Z-order correct |

### Completion Criteria
- [ ] Crane coverage calculation works
- [ ] Handoff points model and render
- [ ] Drop zones model and render
- [ ] Forklift aisle model and renderer working
- [ ] Staging area model and renderer working
- [ ] Crossing zone detection and rendering working
- [ ] Overhead and Flexible layers have visibility control
- [ ] All 10 tests pass

---

## Week 6: Layer 7 (Pedestrian) + Integration + Polish

### Objective
Implement pedestrian mesh (generated from other layers), complete integration testing, and polish UI.

### Tasks

**Task 6.1: Create Pedestrian Exclusion Model**
- Auto-generated from: equipment footprints + buffer, AGV paths + buffer, drop zones
- Manual exclusions can also be painted
- Layer property returns Pedestrian

**Task 6.2: Create Pedestrian Crossing Model**
- Designated safe crossing points over AGV paths or forklift aisles
- Properties: Id, X, Y, Width, CrossingType
- Layer property returns Pedestrian

**Task 6.3: Create Walkable Region Model**
- Computed: Zone polygons minus all exclusions
- Stored as simplified polygons (not triangulated mesh for now)
- Layer property returns Pedestrian

**Task 6.4: Add Pedestrian Collections to LayoutData**
- Add PedestrianExclusions collection
- Add PedestrianCrossings collection
- Add WalkableRegions collection (computed)

**Task 6.5: Create Pedestrian Mesh Generator Service**
- Input: zones, equipment, AGV paths, drop zones
- Algorithm:
  1. Start with zone polygons as walkable
  2. Buffer equipment by safety margin (e.g., 50 units)
  3. Buffer AGV paths by vehicle width + margin
  4. Add drop zones as exclusions
  5. Subtract all exclusions from walkable zones
  6. Store resulting polygons
- Trigger regeneration when relevant elements change

**Task 6.6: Create Pedestrian Renderer**
- Layer property returns Pedestrian
- ZOrderBase is 700-799 range
- Render walkable regions as subtle green tint (optional, toggleable)
- Render exclusion boundaries as red dashed lines
- Render crossings with pedestrian symbol

**Task 6.7: Layer Panel Polish**
- Double-click layer to make active and show only that layer
- Right-click menu: Show Only This, Show All, Hide All Others
- Drag to reorder (if desired)
- Keyboard shortcuts: 1-8 to toggle layer visibility

**Task 6.8: Status Bar Integration**
- Show active layer name
- Show count of elements on active layer
- Show validation warnings count

**Task 6.9: Layer Presets in View Menu**
- Infrastructure Only (L0)
- Facility Planning (L0, L1, L2)
- Transport Design (L4, L5, L6)
- Full Layout (all layers)
- Custom (remember last manual selection)

**Task 6.10: Full Integration Test**
- Create layout with elements on all 8 layers
- Toggle each layer visibility, verify correct elements hide/show
- Lock each layer, verify elements not editable
- Save layout, close, reopen, verify all layers intact
- Run validation, verify warnings for each layer

**Task 6.11: Database Schema Finalization**
- Review all model properties
- Create comprehensive SQLite schema
- Ensure all collections serialize correctly
- Test round-trip: create → save → load → compare

**Task 6.12: Migration Tool for Old Files**
- Detect old file format (missing layer properties)
- Auto-assign layers based on element type
- Prompt user to review and save updated file

### Tests

| Test ID | Description | Pass Criteria |
|---------|-------------|---------------|
| T6.1 | Exclusion generated from equipment | Equipment + buffer becomes exclusion |
| T6.2 | Exclusion generated from AGV path | Path + buffer becomes exclusion |
| T6.3 | Walkable region excludes equipment | Equipment area not walkable |
| T6.4 | Crossing renders on path | Symbol visible at crossing point |
| T6.5 | Regeneration triggers on change | Move equipment, mesh updates |
| T6.6 | All 8 layers toggle independently | Each checkbox works |
| T6.7 | Layer presets apply correctly | Preset shows correct layers |
| T6.8 | Old file migrates successfully | Opens, layers assigned, can save |
| T6.9 | Full save/load round-trip | No data loss |
| T6.10 | Validation covers all layers | Warnings for each layer type |

### Completion Criteria
- [ ] Pedestrian exclusions generate automatically
- [ ] Walkable regions compute correctly
- [ ] Pedestrian crossings can be placed
- [ ] Pedestrian renderer works
- [ ] Layer panel fully functional
- [ ] Layer presets work
- [ ] Status bar shows layer info
- [ ] Old files migrate correctly
- [ ] Full integration test passes
- [ ] All 10 tests pass

---

## Final Acceptance Tests

### End-to-End Scenarios

**Scenario A: New Factory Layout**
1. Create new layout
2. Draw walls on Infrastructure layer (L0)
3. Add doors to walls
4. Create zones on Spatial layer (L1)
5. Add primary aisles
6. Place equipment on Equipment layer (L2)
7. Show clearance zones
8. Group equipment into cells on LocalFlow layer (L3)
9. Draw conveyors between equipment
10. Create AGV network on GuidedTransport layer (L4)
11. Add AGV stations linked to equipment
12. Define crane coverage on OverheadTransport layer (L5)
13. Draw forklift aisles on FlexibleTransport layer (L6)
14. Generate pedestrian mesh on Pedestrian layer (L7)
15. Save layout
16. Close and reopen
17. Verify all elements present

**Scenario B: Layer Isolation**
1. Open existing layout
2. Hide all layers except one
3. Verify only that layer's elements visible
4. Try to select elements from hidden layer (should fail)
5. Lock visible layer
6. Try to move elements (should fail)
7. Unlock layer
8. Move elements (should succeed)
9. Show all layers
10. Verify Z-order correct

**Scenario C: Export Validation**
1. Open complex layout
2. Export to SQLite
3. Open SQLite in DB browser
4. Verify tables exist for each layer
5. Verify foreign keys correct
6. Verify element counts match

---

## Deliverables Checklist

### Models (8 files)
- [ ] Models/Layers/LayerType.cs
- [ ] Models/Layers/LayerMetadata.cs
- [ ] Models/Layers/InfrastructureModels.cs
- [ ] Models/Layers/SpatialModels.cs
- [ ] Models/Layers/LocalFlowModels.cs (conveyor, direct path)
- [ ] Models/Layers/GuidedTransportModels.cs
- [ ] Models/Layers/OverheadTransportModels.cs
- [ ] Models/Layers/FlexibleTransportModels.cs
- [ ] Models/Layers/PedestrianModels.cs
- [ ] Models/Layers/OpeningModels.cs (base + subtypes)

### Services (4 files)
- [ ] Services/Layers/TransportLayerManager.cs
- [ ] Services/Layers/PedestrianMeshGenerator.cs
- [ ] Services/Layers/LayerValidationService.cs
- [ ] Services/Layers/OpeningStateService.cs

### Renderers (9 files)
- [ ] Renderers/Layers/ILayerRenderer.cs
- [ ] Renderers/Layers/InfrastructureRenderer.cs
- [ ] Renderers/Layers/SpatialRenderer.cs
- [ ] Renderers/Layers/EquipmentRenderer.cs
- [ ] Renderers/Layers/LocalFlowRenderer.cs
- [ ] Renderers/Layers/GuidedTransportRenderer.cs
- [ ] Renderers/Layers/OverheadTransportRenderer.cs
- [ ] Renderers/Layers/FlexibleTransportRenderer.cs
- [ ] Renderers/Layers/PedestrianRenderer.cs

### UI (2 files)
- [ ] Controls/LayerPanel.xaml
- [ ] Controls/LayerPanel.xaml.cs

### Handlers (5 files)
- [ ] Handlers/ConveyorToolHandlers.cs
- [ ] Handlers/AGVToolHandlers.cs
- [ ] Handlers/ForkliftAisleToolHandlers.cs
- [ ] Handlers/PedestrianToolHandlers.cs
- [ ] Handlers/OpeningToolHandlers.cs

### Tests (70+ tests)
- [ ] Week 1: 9 tests
- [ ] Week 2: 16 tests (includes opening model)
- [ ] Week 3: 10 tests
- [ ] Week 4: 10 tests
- [ ] Week 5: 10 tests
- [ ] Week 6: 10 tests
- [ ] End-to-end: 3 scenarios

---

## Summary Schedule

| Week | Layers | Key Deliverables | Test Count |
|------|--------|------------------|------------|
| 1 | Infrastructure | LayerType, Manager, Panel | 9 |
| 2 | L0 + L1 | Infrastructure + Spatial + Opening model | 16 |
| 3 | L2 + L3 | Equipment clearance, Conveyors | 10 |
| 4 | L4 | AGV paths, waypoints, stations | 10 |
| 5 | L5 + L6 | Cranes, Forklifts, Crossings | 10 |
| 6 | L7 + Polish | Pedestrian mesh, Integration | 10 |

**Total: 6 weeks, 70+ tests, 28+ files**
