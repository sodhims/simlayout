# Manual Testing Instructions for AGV Stations and Zones

## Setup

**Working Directory**: `r:\layoutbak`

## Running the Application with Debug Output

Open PowerShell and run:

```powershell
cd r:\layoutbak
dotnet run --project LayoutEditor.csproj
```

Keep the PowerShell window visible - it will show debug output while you interact with the application.

## Test 1: Verify Zones Are Visible

1. **Create a custom layout**:
   - In the app: File → New Custom Layout
   - Set:
     - 3-5 AGV Stations
     - Number of Zones: 2 or 3
     - Check "Auto-generate traffic zones"
   - Click "Generate"

2. **Check the PowerShell console** for zone debug output:
   ```
   [DEBUG] GroupRenderer.DrawZone: 'Zone Name' at (X, Y), size: WxH
   [DEBUG]   Zone brush created with alpha=50, R=255, G=255, B=150
   [DEBUG]   Zone rectangle added to canvas
   ```

3. **In the app window**:
   - Look for **light yellow rectangles** with **dark goldenrod borders** (2px solid)
   - Zones should be clearly visible on the canvas
   - If you don't see zones, check the console for how many zones were created

## Test 2: Verify AGV Station Dragging in Design Mode

1. **Enter Design Mode**:
   - Press the `D` key
   - Status bar should say "Design Mode Enabled"
   - Pink blinking handles should appear on draggable items

2. **Try to drag an AGV station**:
   - Click on a red diamond (AGV station)
   - Try to drag it around

3. **Watch the PowerShell console** for:
   ```
   [DEBUG] HitTestAGVStations at (X, Y), designMode=true, radius=25
   [DEBUG] Checking N AGV stations
   [DEBUG]   Station 'AGV Station 1' at (X, Y), distance=D
   [DEBUG]   HIT! Returning station 'AGV Station 1'
   [DEBUG] AGV Station clicked: AGV Station 1 at (X, Y)
   [DEBUG] DesignMode: true
   [DEBUG] Starting AGV station drag for AGV Station 1
   [DEBUG] Dragging AGV station to (X, Y)
   [DEBUG] Updating linked waypoint Waypoint_1
   ```

## Expected Results

### Zones:
- ✓ Zones should be visible as light yellow rectangles
- ✓ Should have dark goldenrod 2px solid borders
- ✓ Should have labels with zone names
- ✓ Console shows zones being rendered

### AGV Stations:
- ✓ AGV stations show pink blinking handles in Design Mode
- ✓ Clicking an AGV station triggers hit test (shown in console)
- ✓ Dragging updates position (shown in console)
- ✓ Linked waypoint follows the station (shown in console)
- ✓ Station visually moves on screen as you drag

## What to Report

If something doesn't work, please provide:

1. **What you did** (step by step)
2. **What you expected to see**
3. **What actually happened**
4. **Console output** from the PowerShell window (copy the relevant debug lines)
5. **Screenshot** of the application window (optional but helpful)

## Common Issues to Check

**If zones don't appear:**
- Check console: How many zones were created? (Look for "RenderZones called, zone count: N")
- Are zones created with 0 size? (Check the debug output)
- Is the Zones layer visible? (Check View menu or layer panel)

**If AGV stations don't drag:**
- Does the console show hit testing happening when you click?
- Does it say "AGV Station clicked"?
- Does it say "DesignMode: true" or "Not in design mode"?
- Does it say "Layer GuidedTransport is locked"?
- Do you see "Dragging AGV station to..." messages when moving the mouse?

## Quick Commands

```powershell
# Navigate to project directory
cd r:\layoutbak

# Run with console visible
dotnet run --project LayoutEditor.csproj

# If app is stuck, kill it
Stop-Process -Name LayoutEditor -Force

# Rebuild after code changes
dotnet build LayoutEditor.csproj
```
