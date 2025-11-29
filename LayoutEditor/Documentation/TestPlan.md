# Layout Editor - Comprehensive Test Plan

## Test Environment
- **OS:** Windows 10/11
- **Framework:** .NET 8.0
- **Build:** Debug/Release

---

## 1. APPLICATION STARTUP

### 1.1 Initial Launch
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 1.1.1 | App launches | Double-click LayoutEditor.exe | Window opens, no errors | |
| 1.1.2 | Default layout | Check canvas | Empty canvas with grid visible | |
| 1.1.3 | Panels visible | Check UI | Left panel (Properties/Layers/Toolbox), Right panel (Topology) visible | |
| 1.1.4 | Status bar | Check bottom | Shows "Nodes: 0  Paths: 0  Walls: 0" | |
| 1.1.5 | Title bar | Check title | Shows "Simulation Layout Editor - New Layout" | |

---

## 2. FILE OPERATIONS

### 2.1 New File
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 2.1.1 | New layout | File → New (Ctrl+N) | Canvas clears, title shows "New Layout" | |
| 2.1.2 | Dirty prompt | Add node, then Ctrl+N | Prompts "Save changes?" | |

### 2.2 Save/Open
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 2.2.1 | Save As | File → Save As (Ctrl+Shift+S) | Save dialog opens, .json filter | |
| 2.2.2 | Save | Add nodes, Ctrl+S | First time shows Save As dialog | |
| 2.2.3 | Open | File → Open (Ctrl+O) | Open dialog, loads .json file | |
| 2.2.4 | Recent files | Check File menu | Shows recently opened files | |

### 2.3 Sample Files
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 2.3.1 | Load sample | File → Open → SampleLayout.json | Layout loads with nodes and paths | |
| 2.3.2 | Load cell sample | Open SampleCellLayout.json | Layout with cells loads correctly | |

---

## 3. NODE OPERATIONS

### 3.1 Creating Nodes
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 3.1.1 | From toolbox | Click Machine icon in toolbox | Cursor changes, click canvas to place | |
| 3.1.2 | Source node | Click Source icon, place | Green arrow icon appears | |
| 3.1.3 | Sink node | Click Sink icon, place | Red flag icon appears | |
| 3.1.4 | Buffer node | Click Buffer icon, place | Orange striped icon appears | |
| 3.1.5 | Conveyor | Click Conveyor icon, place | Gray conveyor icon appears | |
| 3.1.6 | Cancel placement | Press Esc during placement | Cursor returns to normal | |
| 3.1.7 | Grid snap | Enable snap, place node | Node aligns to grid | |

### 3.2 Selecting Nodes
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 3.2.1 | Single select | Click on node | Blue selection border appears | |
| 3.2.2 | Multi-select Ctrl | Ctrl+click multiple nodes | All clicked nodes selected | |
| 3.2.3 | Multi-select Shift | Shift+click nodes | Toggles selection | |
| 3.2.4 | Deselect | Click empty canvas | Selection cleared | |
| 3.2.5 | Select All | Ctrl+A | All nodes selected | |

### 3.3 Moving Nodes
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 3.3.1 | Drag single | Select node, drag | Node moves with cursor | |
| 3.3.2 | Drag multiple | Select 3 nodes, drag one | All 3 move together | |
| 3.3.3 | Arrow keys | Select node, press arrows | Node moves by grid size | |
| 3.3.4 | Snap while drag | Enable snap, drag node | Snaps to grid on release | |

### 3.4 Node Properties
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 3.4.1 | View properties | Select node | Properties panel shows node info | |
| 3.4.2 | Change name | Edit Name field | Node label updates on canvas | |
| 3.4.3 | Change type | Change Type dropdown | Icon changes accordingly | |
| 3.4.4 | Change size | Edit Width/Height | Node resizes on canvas | |
| 3.4.5 | Change position | Edit X/Y fields | Node moves to position | |

### 3.5 Deleting Nodes
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 3.5.1 | Delete key | Select node, press Delete | Node removed | |
| 3.5.2 | Delete multiple | Select 3 nodes, Delete | All 3 removed | |
| 3.5.3 | Delete with paths | Delete node with connected path | Node and connected paths removed | |

---

## 4. PATH OPERATIONS

### 4.1 Creating Paths
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 4.1.1 | Path tool | Press P or click Path button | Path tool activated | |
| 4.1.2 | Draw path | Click source node, click dest node | Path created with arrow | |
| 4.1.3 | Cancel path | Start path, press Esc | Path cancelled | |
| 4.1.4 | Auto-routing | Create path, check routing | Uses Manhattan routing by default | |

### 4.2 Path Selection
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 4.2.1 | Select path | Click on path line | Path highlighted, properties show | |
| 4.2.2 | Path properties | Select path | Shows From, To, Distance | |

### 4.3 Waypoint Editing
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 4.3.1 | Edit mode | Click ✎ Edit button or press E | Edit mode enabled | |
| 4.3.2 | Add waypoint | In edit mode, click on path | Waypoint added, drag to position | |
| 4.3.3 | Drag waypoint | Drag existing waypoint | Waypoint moves, path redraws | |
| 4.3.4 | Delete waypoint | Right-click waypoint → Delete | Waypoint removed | |
| 4.3.5 | Clear waypoints | Right-click → Clear All | All waypoints removed | |

### 4.4 Path Properties
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 4.4.1 | Change type | Select path, change Type | Visual style updates | |
| 4.4.2 | Bidirectional | Toggle Bidirectional | Arrows on both ends | |

---

## 5. CELL/GROUP OPERATIONS

### 5.1 Creating Cells
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 5.1.1 | Define cell | Select nodes, click "Create Cell" | Orange dashed border appears | |
| 5.1.2 | Cell name | Create cell | Auto-named "Cell 1", "Cell 2", etc. | |
| 5.1.3 | Empty selection | Click Create Cell with nothing selected | Error message or no action | |

### 5.2 Cell Selection
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 5.2.1 | Select cell | Click on cell border | Entire cell selected | |
| 5.2.2 | Click inside | Click node inside cell | Cell selected (not individual node) | |
| 5.2.3 | Multi-select cells | Shift+click multiple cells | Multiple cells selected | |
| 5.2.4 | Cell properties | Select cell | Shows cell name, entry/exit points | |

### 5.3 Cell Edit Mode
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 5.3.1 | Enter edit mode | Double-click cell | Status shows "Editing cell: X" | |
| 5.3.2 | Select inside | In edit mode, click node | Individual node selected | |
| 5.3.3 | Move inside | In edit mode, drag node | Node moves within cell | |
| 5.3.4 | Exit edit mode | Press Esc | Returns to normal mode | |

### 5.4 Cell Alignment
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 5.4.1 | Align left | Select 2+ cells, click ⫷ | Cells align to leftmost | |
| 5.4.2 | Align right | Select 2+ cells, click ⫸ | Cells align to rightmost | |
| 5.4.3 | Align top | Select 2+ cells, click ⫯ | Cells align to topmost | |
| 5.4.4 | Align bottom | Select 2+ cells, click ⫰ | Cells align to bottommost | |
| 5.4.5 | Distribute H | Select 3+ cells, click ⋯ H | Equal horizontal spacing | |
| 5.4.6 | Distribute V | Select 3+ cells, click ⋮ V | Equal vertical spacing | |

---

## 6. WALL OPERATIONS (NEW)

### 6.1 Wall Tool
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 6.1.1 | Activate | Press W or click ▬ button | Wall tool active, crosshair cursor | |
| 6.1.2 | Draw wall | Click start, click end | Wall line appears | |
| 6.1.3 | Shift constraint | Hold Shift while drawing | Snaps to H/V/45° | |
| 6.1.4 | Cancel | Press Esc during draw | Wall cancelled | |
| 6.1.5 | Too short | Draw very short wall | Wall rejected (< 10px) | |

### 6.2 Wall Types
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 6.2.1 | Standard | Select Standard type | Gray wall, medium thickness | |
| 6.2.2 | Exterior | Select Exterior type | Darker, thicker wall | |
| 6.2.3 | Partition | Select Partition type | Thinner wall | |
| 6.2.4 | Glass | Select Glass type | Dashed line, thin | |
| 6.2.5 | Safety | Select Safety type | Yellow dashed line | |

### 6.3 Column Tool
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 6.3.1 | Place square | Click Column tool, click canvas | Square column placed | |
| 6.3.2 | Place round | Shift+click on canvas | Round column placed | |

---

## 7. MEASUREMENT TOOL (NEW)

### 7.1 Measuring
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 7.1.1 | Activate | Press R or click 📏 button | Measure tool active | |
| 7.1.2 | Measure distance | Click two points | Red dimension line with length | |
| 7.1.3 | Shift constraint | Hold Shift | Snaps to H/V | |
| 7.1.4 | Unit display | Check measurement label | Shows real units (ft, m, etc.) | |
| 7.1.5 | Clear all | Edit → Clear Measurements | All measurements removed | |

---

## 8. BACKGROUND IMAGE (NEW)

### 8.1 Import Background
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 8.1.1 | Import | File → Import → Background Image | File dialog for images | |
| 8.1.2 | Display | Select image file | Image appears on canvas | |
| 8.1.3 | Opacity | Check default opacity | Semi-transparent (40%) | |
| 8.1.4 | Scale | File → Import → Scale Background | Options to scale 50%/150% | |
| 8.1.5 | Clear | File → Import → Clear Background | Image removed | |

---

## 9. LAYERS PANEL (NEW)

### 9.1 Layer Visibility
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 9.1.1 | Panel visible | Check left panel | Layers section between Properties and Toolbox | |
| 9.1.2 | Toggle visibility | Click 👁 icon on layer | Layer shows/hides on canvas | |
| 9.1.3 | Hide walls | Toggle Walls layer off | Walls disappear from canvas | |
| 9.1.4 | Hide nodes | Toggle Nodes layer off | Nodes disappear from canvas | |
| 9.1.5 | Double-click | Double-click layer | Toggles visibility | |

### 9.2 Layer Lock
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 9.2.1 | Lock layer | Click 🔓 icon | Changes to 🔒 | |
| 9.2.2 | Grid locked | Check Grid layer | Should be locked by default | |

### 9.3 Active Layer
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 9.3.1 | Select layer | Click on layer | Layer highlighted, ● indicator | |
| 9.3.2 | Status update | Select Walls layer | Status bar shows layer hint | |

### 9.4 Layer Properties
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 9.4.1 | Expand panel | Expand "Layer Properties" | Shows stroke/fill options | |
| 9.4.2 | Change color | Click color swatch | Color picker opens | |
| 9.4.3 | Apply color | Select new color | Canvas updates immediately | |
| 9.4.4 | Stroke width | Adjust width slider | Lines get thicker/thinner | |
| 9.4.5 | Dash pattern | Select "Dashed" | Lines become dashed | |
| 9.4.6 | Opacity | Adjust opacity slider | Layer becomes transparent | |

### 9.5 Layer Order
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 9.5.1 | Move up | Select layer, click ▲ | Layer moves up in list | |
| 9.5.2 | Move down | Select layer, click ▼ | Layer moves down in list | |

### 9.6 Custom Layers
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 9.6.1 | Add layer | Click + button | New "Custom Layer" added | |
| 9.6.2 | Rename | Edit name in properties | Layer name updates | |
| 9.6.3 | Delete custom | Select custom layer, click 🗑 | Layer removed | |
| 9.6.4 | Delete builtin | Try to delete Walls | Error: cannot delete built-in | |

---

## 10. EXPORT OPERATIONS (NEW)

### 10.1 SVG Export
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 10.1.1 | Export | File → Export → SVG | Save dialog opens | |
| 10.1.2 | Open SVG | Open in browser/Inkscape | Layout renders correctly | |
| 10.1.3 | Layers | Check SVG source | Contains layer groups | |

### 10.2 DXF Export
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 10.2.1 | Export | File → Export → DXF | Save dialog opens | |
| 10.2.2 | Open DXF | Open in AutoCAD/viewer | Layout visible with layers | |
| 10.2.3 | Coordinates | Check scale | Uses real-world units | |

### 10.3 Bill of Materials
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 10.3.1 | Export CSV | File → Export → BOM (CSV) | CSV file created | |
| 10.3.2 | Open CSV | Open in Excel | Columns: Type, Name, ID, X, Y, etc. | |
| 10.3.3 | Equipment list | File → Export → Equipment List | Text report created | |
| 10.3.4 | Summary | Check report | Shows counts by type and cell | |

### 10.4 Image Export
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 10.4.1 | Export PNG | File → Export → Image | PNG save dialog | |
| 10.4.2 | Check image | Open PNG | Layout rendered correctly | |

---

## 11. VIEW OPERATIONS

### 11.1 Zoom
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 11.1.1 | Zoom in | View → Zoom In or Ctrl++ | Canvas zooms in | |
| 11.1.2 | Zoom out | View → Zoom Out or Ctrl+- | Canvas zooms out | |
| 11.1.3 | Mouse wheel | Ctrl+scroll | Zooms in/out | |
| 11.1.4 | Fit to window | View → Fit to Window | Shows entire layout | |
| 11.1.5 | Actual size | View → Actual Size | Returns to 100% | |
| 11.1.6 | Zoom limits | Zoom to extremes | Stops at 10% and 400% | |

### 11.2 Pan
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 11.2.1 | Pan tool | Press H or click Hand button | Pan cursor | |
| 11.2.2 | Pan drag | In pan mode, drag canvas | View scrolls | |
| 11.2.3 | Scrollbars | Use scrollbars | Canvas scrolls | |

### 11.3 Grid
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 11.3.1 | Toggle grid | View → Toggle Grid | Grid shows/hides | |
| 11.3.2 | Toggle snap | View → Toggle Snap | Snap enabled/disabled | |

---

## 12. EDIT OPERATIONS

### 12.1 Undo/Redo
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 12.1.1 | Undo | Add node, Ctrl+Z | Node removed | |
| 12.1.2 | Redo | After undo, Ctrl+Y | Node restored | |
| 12.1.3 | Multiple undo | Make 5 changes, Ctrl+Z x5 | All 5 reverted | |
| 12.1.4 | Undo limit | Make 100 changes | Undo works for recent ~50 | |

### 12.2 Copy/Paste
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 12.2.1 | Copy node | Select node, Ctrl+C | No visible change | |
| 12.2.2 | Paste node | Ctrl+V | Duplicate node appears offset | |
| 12.2.3 | Copy multiple | Select 3 nodes, Ctrl+C, Ctrl+V | 3 duplicates appear | |
| 12.2.4 | Copy cell | Select cell, Ctrl+C, Ctrl+V | Cell duplicated with internal paths | |
| 12.2.5 | Duplicate | Select node, Ctrl+D | Duplicate created immediately | |

### 12.3 Cut
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 12.3.1 | Cut | Select node, Ctrl+X | Node removed | |
| 12.3.2 | Paste after cut | Ctrl+V | Node appears at new position | |

---

## 13. KEYBOARD SHORTCUTS

### 13.1 Tool Shortcuts
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 13.1.1 | V - Select | Press V | Select tool active | |
| 13.1.2 | P - Path | Press P | Path tool active | |
| 13.1.3 | W - Wall | Press W | Wall tool active | |
| 13.1.4 | R - Measure | Press R | Measure tool active | |
| 13.1.5 | H - Pan | Press H | Pan tool active | |
| 13.1.6 | E - Edit | Press E | Path edit mode toggles | |
| 13.1.7 | Esc - Cancel | Press Esc during operation | Operation cancelled | |

### 13.2 File Shortcuts
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 13.2.1 | Ctrl+N | Press Ctrl+N | New file | |
| 13.2.2 | Ctrl+O | Press Ctrl+O | Open file | |
| 13.2.3 | Ctrl+S | Press Ctrl+S | Save file | |
| 13.2.4 | Ctrl+Shift+S | Press Ctrl+Shift+S | Save As | |

---

## 14. TOPOLOGY EXPLORER

### 14.1 Tree View
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 14.1.1 | Shows nodes | Check right panel | Lists all nodes by type | |
| 14.1.2 | Shows paths | Expand Paths | Lists all paths | |
| 14.1.3 | Shows cells | Expand Cells | Lists all cells | |
| 14.1.4 | Click to select | Click node in tree | Selects on canvas | |
| 14.1.5 | Refresh | Click Refresh button | Updates tree | |
| 14.1.6 | Expand All | Click Expand All | All categories expanded | |
| 14.1.7 | Collapse All | Click Collapse All | All categories collapsed | |

---

## 15. TEMPLATES

### 15.1 Template Operations
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 15.1.1 | Open manager | Edit → Templates Manager | Template dialog opens | |
| 15.1.2 | Create template | Select nodes, create template | Template saved | |
| 15.1.3 | Place template | Click template in toolbox | Template placed on canvas | |

---

## 16. VALIDATION

### 16.1 Layout Validation
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 16.1.1 | Run validation | Edit → Validate Layout | Validation results shown | |
| 16.1.2 | Overlapping nodes | Place overlapping nodes, validate | Warning shown | |
| 16.1.3 | Disconnected | Create isolated node, validate | Warning about connectivity | |

---

## 17. CONTEXT MENUS

### 17.1 Canvas Context Menu
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 17.1.1 | Open menu | Right-click empty canvas | Context menu appears | |
| 17.1.2 | Add node options | Check menu | Shows node type submenu | |
| 17.1.3 | Paste option | Check menu | Paste option present | |

### 17.2 Node Context Menu
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 17.2.1 | Open menu | Right-click on node | Context menu appears | |
| 17.2.2 | Delete option | Check menu | Delete option present | |
| 17.2.3 | Duplicate | Check menu | Duplicate option present | |

### 17.3 Path Context Menu
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 17.3.1 | Open menu | Right-click on path | Context menu appears | |
| 17.3.2 | Delete option | Check menu | Delete path option | |
| 17.3.3 | Reverse option | Check menu | Reverse direction option | |

---

## 18. EDGE CASES & ERROR HANDLING

### 18.1 Error Handling
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 18.1.1 | Invalid file | Try to open non-JSON file | Error message, no crash | |
| 18.1.2 | Corrupt JSON | Open corrupted .json | Error message, no crash | |
| 18.1.3 | Large file | Open file with 1000+ nodes | Loads (may be slow), no crash | |
| 18.1.4 | No selection | Try delete with nothing selected | No crash, maybe status message | |

### 18.2 Edge Cases
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 18.2.1 | Self-loop path | Try to create path to same node | Prevented or handled | |
| 18.2.2 | Duplicate path | Create same path twice | Prevented or handled | |
| 18.2.3 | Negative position | Set negative X/Y | Handled appropriately | |
| 18.2.4 | Zero size | Set node width/height to 0 | Prevented or minimum enforced | |

---

## 19. PERFORMANCE

### 19.1 Performance Tests
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 19.1.1 | 100 nodes | Create 100 nodes | Smooth operation | |
| 19.1.2 | 50 paths | Create 50 paths | Smooth operation | |
| 19.1.3 | Zoom with many | Zoom with 100+ elements | Smooth zoom | |
| 19.1.4 | Undo performance | Undo with large layout | Responsive | |

---

## 20. DATA PERSISTENCE

### 20.1 Save/Load Integrity
| # | Test | Steps | Expected Result | Pass/Fail |
|---|------|-------|-----------------|-----------|
| 20.1.1 | Save all data | Create complex layout, save | File created | |
| 20.1.2 | Load all data | Reload saved file | All elements restored | |
| 20.1.3 | Node positions | Save, reload, check positions | Exact same positions | |
| 20.1.4 | Path waypoints | Create waypoints, save, reload | Waypoints preserved | |
| 20.1.5 | Cell structure | Create cells, save, reload | Cells and members preserved | |
| 20.1.6 | Walls preserved | Add walls, save, reload | Walls preserved | |
| 20.1.7 | Measurements | Add measurements, save, reload | Measurements preserved | |
| 20.1.8 | Background | Import background, save, reload | Background preserved | |
| 20.1.9 | Layer settings | Modify layer visibility, save, reload | Settings preserved | |

---

## TEST EXECUTION LOG

| Date | Tester | Version | Tests Run | Passed | Failed | Notes |
|------|--------|---------|-----------|--------|--------|-------|
| | | | | | | |

---

## DEFECT LOG

| # | Test ID | Description | Severity | Status | Notes |
|---|---------|-------------|----------|--------|-------|
| | | | | | |

---

## Severity Levels
- **Critical**: App crashes, data loss
- **High**: Feature completely broken
- **Medium**: Feature partially works
- **Low**: Minor visual/UX issue

