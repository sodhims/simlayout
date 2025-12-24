# Prompt 28: Visual Routing Canvas - Workstation & Time Display

Continuing Factory Configurator. Prompt 27 complete, 133 tests passing, drag-drop working.

Objective: Show workstation assignments and times on nodes. Manual verification.

## 1. Workstation picker dialog

### WorkstationPickerDialog.xaml:
- Filter by WorkstationType (optionally match node type)
- List of workstations with columns:
  - Name
  - Element
  - Type
  - Capacity
- Search/filter box
- OK/Cancel buttons

## 2. Node display updates

### ProcessNodeViewModel display:
```
┌─────────────────────┐
│ ⚙️ Process          │
│ CNC Machine #3      │  ← Workstation name
│ Process: 120s       │  ← From process times
│ Load/Unload: 15s    │
└─────────────────────┘
```

### Node without workstation:
```
┌─────────────────────┐
│ ⚙️ Process          │
│ [Click to assign]   │  ← Clickable link
│ Time: --            │
└─────────────────────┘
```

### Node types and their display:
- Start: "▶ Start" (no times)
- End: "◼ End" (no times)
- Process: Workstation + process time
- Inspect: Workstation + inspection time
- Assemble: Workstation + assembly time
- Package: Workstation + packaging time
- Decision: Condition or "[Configure]"

## 3. Connection display

Show transport info on connection line:
- Small label at midpoint: "🚜 45s" or "🤖 30s"
- Or: thicker line with tooltip on hover
- Color coding by transport type

## 4. Time calculations

Each node shows its process time from cfg_ProcessTimes:
- Lookup: (scenarioId, workstationId, variantId)
- With fallback logic from ProcessTimeService

### Footer panel shows:
- Total process time: Σ all node process times
- Total transport time: Σ all connection times
- Total cycle time: process + transport
- Critical path: highlighted in red (optional)

## 5. Real-time updates

- When workstation assigned → fetch and display process time
- When scenario changes (from main toolbar) → refresh all times
- Times from base scenario shown in gray/italic if no override

## 6. Validation warnings on canvas

Visual indicators for issues:
- Node without workstation: orange border + ⚠️ icon
- Missing transport time: orange dashed connection
- Unreachable node: red border
- Dead-end (doesn't reach End): red dashed border

Tooltip on warning icon shows the issue message.

## 7. Manual verification

- Assign workstations to all Process nodes
- Verify times appear on nodes
- Change scenario in main toolbar
- Verify times update (or show inherited indicator)
- Add connection without configuring transport
- Verify warning appears (orange dashed line)
- Configure transport on the connection
- Verify warning clears, time displays
- Check footer panel shows correct totals

No new automated tests. Existing 133 tests should pass.
