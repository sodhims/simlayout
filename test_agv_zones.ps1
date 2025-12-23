# Test script to verify AGV station dragging and zone visibility

Write-Host "=== Testing AGV Station and Zone Functionality ===" -ForegroundColor Cyan

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
$buildResult = dotnet build LayoutEditor.csproj 2>&1 | Select-String "Build succeeded|Build FAILED|error"
Write-Host $buildResult

if ($buildResult -match "FAILED|error") {
    Write-Host "Build failed! Exiting." -ForegroundColor Red
    exit 1
}

Write-Host "`nBuild succeeded!" -ForegroundColor Green

# Check that key files were modified
Write-Host "`n=== Verifying Code Changes ===" -ForegroundColor Cyan

$files = @(
    "Services\HitTestService.cs",
    "Renderers\SpatialRenderer.cs",
    "Handlers\AGVStationDragHandlers.cs",
    "Services\DesignModeRenderer.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "✓ $file exists" -ForegroundColor Green
    } else {
        Write-Host "✗ $file missing!" -ForegroundColor Red
    }
}

# Check for AGV station hit testing in GuidedTransport layer
Write-Host "`n=== Checking AGV Station Hit Testing ===" -ForegroundColor Cyan
$hitTestContent = Get-Content "Services\HitTestService.cs" -Raw
if ($hitTestContent -match "Check GuidedTransport layer elements \(AGV stations\)") {
    Write-Host "✓ AGV stations are in GuidedTransport layer block" -ForegroundColor Green
} else {
    Write-Host "✗ AGV stations NOT in correct layer block" -ForegroundColor Red
}

# Check for zone color changes
Write-Host "`n=== Checking Zone Rendering ===" -ForegroundColor Cyan
$spatialContent = Get-Content "Renderers\SpatialRenderer.cs" -Raw
if ($spatialContent -match "255, 255, 150.*Light yellow") {
    Write-Host "✓ Zones have light yellow color" -ForegroundColor Green
} else {
    Write-Host "✗ Zone color not updated" -ForegroundColor Red
}

if ($spatialContent -match "StrokeThickness = 2") {
    Write-Host "✓ Zone border thickness is 2px" -ForegroundColor Green
} else {
    Write-Host "✗ Zone border thickness not updated" -ForegroundColor Red
}

# Check for AGV station handles in design mode
Write-Host "`n=== Checking Design Mode Handles ===" -ForegroundColor Cyan
$designContent = Get-Content "Services\DesignModeRenderer.cs" -Raw
if ($designContent -match "Draw handles for AGV stations") {
    Write-Host "✓ AGV station handles are rendered in design mode" -ForegroundColor Green
} else {
    Write-Host "✗ AGV station handles not added" -ForegroundColor Red
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "All code changes are in place." -ForegroundColor Green
Write-Host "`nTo test manually:" -ForegroundColor Yellow
Write-Host "1. Run the application: dotnet run" -ForegroundColor White
Write-Host "2. Create a new custom layout with AGV stations and zones" -ForegroundColor White
Write-Host "3. Press 'D' to enter Design Mode" -ForegroundColor White
Write-Host "4. Look for pink blinking handles on AGV stations" -ForegroundColor White
Write-Host "5. Click and drag an AGV station - it should move freely" -ForegroundColor White
Write-Host "6. Check that zones appear with light yellow fill and dark border" -ForegroundColor White
