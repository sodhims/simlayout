# Testing Instructions - AGV Station Dragging with Debug Logging

## Log File Location
Debug output is now written to:
```
R:\Layoutbak\Logs\LayoutLog.{timestamp}.log
```

The timestamp format is: `yyyyMMdd_HHmmss` (e.g., `LayoutLog.20251221_143052.log`)

A new log file is created each time you start the application.

## How to Test

### Step 1: Launch the Application
Open PowerShell and run:
```powershell
cd r:\layoutbak
dotnet run --project LayoutEditor.csproj
```

When the app starts, the status bar will show the log file path.

### Step 2: Create a Layout with AGV Stations
1. In the app: **File → New Custom Layout**
2. Set:
   - Number of AGV Stations: **3-5**
   - Number of Zones: **2-3**
   - Check: **Auto-generate traffic zones**
3. Click **Generate**

### Step 3: Enter Design Mode
- Press the **D** key
- Status bar should say: "Design Mode Enabled"
- Pink blinking handles should appear on draggable items

### Step 4: Try to Drag an AGV Station
1. Look for red diamond shapes (AGV stations)
2. Click on one and try to drag it
3. Watch what happens (or doesn't happen)

### Step 5: Check the Log File
Open the log file in `R:\Layoutbak\Logs\` and look for these key events:

**When you click on an AGV station, you should see:**
```
[HH:mm:ss.fff] === HitTest START at (X, Y), frictionless=false, design=true ===
[HH:mm:ss.fff] Checking GuidedTransport layer, visible=true
[HH:mm:ss.fff] Checking AGV stations (count=N)...
[HH:mm:ss.fff] HitTestAGVStations at (X, Y), designMode=true, radius=25
[HH:mm:ss.fff] Checking N AGV stations
[HH:mm:ss.fff]   Station 'AGV Station 1' at (X, Y), distance=D
[HH:mm:ss.fff]   HIT! Returning station 'AGV Station 1'
[HH:mm:ss.fff] HIT: AGV Station 'AGV Station 1'
[HH:mm:ss.fff] >>> CLICK RESULT: Hit type = AGVStation at (X, Y)
[HH:mm:ss.fff] AGV Station clicked: AGV Station 1 at (X, Y)
[HH:mm:ss.fff] DesignMode: true
[HH:mm:ss.fff] Starting AGV station drag for AGV Station 1
```

**When you move the mouse while dragging, you should see:**
```
[HH:mm:ss.fff] Dragging AGV station to (X, Y)
[HH:mm:ss.fff] Updating linked waypoint Waypoint_1
```

## What to Look For

### If AGV Stations Don't Move:

**Check the log for:**
1. **Is HitTest being called?** Look for "=== HitTest START"
2. **What hit type is returned?** Look for ">>> CLICK RESULT: Hit type = ..."
3. **Is GuidedTransport layer visible?** Look for "Checking GuidedTransport layer, visible=..."
4. **Are AGV stations being checked?** Look for "Checking AGV stations (count=...)"
5. **Is the click hitting an AGV station?** Look for "HIT! Returning station"
6. **Is DesignMode true?** Look for "DesignMode: true"
7. **Is the layer locked?** Look for "Layer ... is locked"

### If Zones Don't Appear:

**Check the log for:**
1. Look for: "GroupRenderer.DrawZone: 'Zone Name' at (X, Y), size: WxH"
2. Look for: "Zone brush created with alpha=..."
3. Count how many zones are being rendered

## Sending Results

When you report the issue, please include:
1. **The full log file** from `R:\Layoutbak\Logs\`
2. **Screenshot** of the app window showing the layout
3. **What you did** (step-by-step)
4. **What happened** (or didn't happen)

## Quick Commands

```powershell
# Navigate to project
cd r:\layoutbak

# Run the app
dotnet run --project LayoutEditor.csproj

# View the latest log file
Get-ChildItem R:\Layoutbak\Logs\ | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content

# Kill the app if stuck
Stop-Process -Name LayoutEditor -Force

# Rebuild after code changes
dotnet build LayoutEditor.csproj
```
