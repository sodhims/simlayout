# Prompt 29: Visual Routing Canvas - Save & Polish

Continuing Factory Configurator. Prompt 28 complete, 133 tests passing, times displaying.

Objective: Complete routing canvas with save/load and visual polish. Manual verification.

## 1. Save/Load

### RoutingNetworkViewModel:

#### SaveAsync():
- Saves all node positions to database
- Saves all connections
- Called on:
  - Node position change (debounced, 500ms delay)
  - Connection add/remove
  - Workstation assignment change
- Or: explicit Save button in toolbar

#### LoadFromRoutingAsync(Routing routing):
- When variant selected, load routing if exists
- Create default routing (Start → End) if none exists
- Position nodes from saved PositionX/PositionY
- Recreate all connections

## 2. Canvas controls

### Toolbar above canvas:
- Zoom slider (50% - 200%)
- "Fit to View" button - zooms to show all nodes
- "Validate" button - runs ValidateRoutingAsync, shows results
- "Clone to Scenario" button - clones routing to selected scenario
- "Delete Routing" button - with confirmation

### Navigation:
- Pan: Middle mouse button drag or Ctrl+Left drag
- Zoom: Mouse wheel (centered on cursor)
- Select: Left click on node/connection
- Multi-select: Shift+click or box select (drag on empty canvas)

## 3. Visual polish

### Grid background:
- Light gray grid lines, 20px spacing
- Optional snap-to-grid (toggle in toolbar)

### Connection curves:
- Bezier curves with smooth bends
- Control points auto-calculated for clean curves
- Avoid overlapping with nodes

### Node effects:
- Drop shadow for depth
- Hover: slight scale up (1.02x)
- Selected: blue border glow
- Dragging: higher shadow, slight transparency

### Animation:
- Connection creation: line draws from source to target
- Node deletion: fade out
- Validation error: brief shake on invalid nodes

## 4. Mini-map (optional enhancement)

Small overview panel in corner:
- Shows all nodes as colored dots
- Viewport rectangle shows current view
- Click to navigate
- Draggable viewport

## 5. Keyboard shortcuts

| Key | Action |
|-----|--------|
| Delete | Remove selected nodes/connections |
| Ctrl+A | Select all |
| Escape | Deselect all |
| Ctrl+S | Force save |
| Ctrl+Z | Undo (if implementing undo stack) |
| + / - | Zoom in/out |
| Home | Fit to view |

## 6. Routing summary panel

Panel below or beside canvas:

```
┌────────────────────────────────────────┐
│ Routing for: MOT-001 (Small Motor)     │
├────────────────────────────────────────┤
│ Scenario: Base                         │
│ Nodes: 7  |  Connections: 8            │
│ Status: ✓ Valid                        │
├────────────────────────────────────────┤
│ Process Time:   245s                   │
│ Transport Time:  95s                   │
│ Total Cycle:    340s                   │
└────────────────────────────────────────┘
```

- Status shows: ✓ Valid, ⚠ 2 warnings, or ✗ 3 errors
- Click on status to see validation details

## 7. Integration with main window

- Add "Routing" tab to main TabControl
- Tab contains:
  - Variant selector dropdown (shows variants, indicates which have routings)
  - Scenario indicator (from main toolbar)
  - Canvas area with palette on left
  - Summary panel at bottom

## 8. Manual verification

- Create complete routing with 5+ nodes
- Drag nodes around, verify positions save
- Close app, reopen
- Verify routing loads with correct positions
- Zoom and pan around canvas
- Validate routing, fix any warnings
- Clone routing to child scenario
- Verify clone has same structure but is independent
- Modify clone, verify original unchanged
- Delete routing, confirm dialog, verify deleted

No new automated tests. Existing 133 tests should pass.
