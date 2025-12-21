# =============================================================================
# Fix-AutoIdGeneration.ps1
# =============================================================================
# Run this script from your LayoutEditor solution root directory:
#   .\Fix-AutoIdGeneration.ps1
#
# This script updates model classes to auto-generate unique IDs
# =============================================================================

$ErrorActionPreference = "Stop"

# Define the models folder path
$modelsPath = "LayoutEditor\Models"

if (-not (Test-Path $modelsPath)) {
    Write-Host "ERROR: Models folder not found at '$modelsPath'" -ForegroundColor Red
    Write-Host "Make sure you're running this from the solution root directory." -ForegroundColor Yellow
    exit 1
}

# Files to update
$filesToUpdate = @(
    "NodeData.cs",
    "PathData.cs",
    "WallData.cs",
    "GroupData.cs",
    "MeasurementData.cs",
    "ColumnData.cs",
    "LayerData.cs"
)

# Pattern to find and replace
$oldPattern = 'public string Id { get; set; }'
$newPattern = 'public string Id { get; set; } = Guid.NewGuid().ToString();'

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Auto-ID Generation Fix Script" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$updatedCount = 0
$skippedCount = 0

foreach ($file in $filesToUpdate) {
    $filePath = Join-Path $modelsPath $file
    
    if (-not (Test-Path $filePath)) {
        Write-Host "  SKIP: $file (not found)" -ForegroundColor Yellow
        $skippedCount++
        continue
    }
    
    $content = Get-Content $filePath -Raw
    
    # Check if already fixed
    if ($content -match 'public string Id { get; set; } = Guid\.NewGuid\(\)\.ToString\(\);') {
        Write-Host "  SKIP: $file (already has auto-ID)" -ForegroundColor Gray
        $skippedCount++
        continue
    }
    
    # Check if pattern exists
    if ($content -match [regex]::Escape($oldPattern)) {
        # Replace the pattern
        $newContent = $content -replace [regex]::Escape($oldPattern), $newPattern
        
        # Write back to file
        Set-Content -Path $filePath -Value $newContent -NoNewline
        
        Write-Host "  UPDATED: $file" -ForegroundColor Green
        $updatedCount++
    }
    else {
        Write-Host "  SKIP: $file (pattern not found - may have different format)" -ForegroundColor Yellow
        $skippedCount++
    }
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Updated: $updatedCount files" -ForegroundColor Green
Write-Host "  Skipped: $skippedCount files" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

if ($updatedCount -gt 0) {
    Write-Host "Done! Now run 'dotnet build' to verify the changes compile." -ForegroundColor Green
}
else {
    Write-Host "No files were updated. Check if the pattern matches your code." -ForegroundColor Yellow
}
