# Floating Panels Auto-Integration Script
# Run from your LayoutEditor solution directory

param(
    [string]$ProjectPath = ".\LayoutEditor"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Floating Panels Auto-Integration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$ControlsDir = Join-Path $ProjectPath "Controls"
$MainWindowCs = Join-Path $ProjectPath "MainWindow.xaml.cs"
$MainWindowXaml = Join-Path $ProjectPath "MainWindow.xaml"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Verify project exists
if (-not (Test-Path $MainWindowCs)) {
    Write-Host "ERROR: Cannot find $MainWindowCs" -ForegroundColor Red
    Write-Host "Run this script from your solution directory" -ForegroundColor Yellow
    exit 1
}

# Create Controls directory
if (-not (Test-Path $ControlsDir)) {
    Write-Host "Creating Controls directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $ControlsDir | Out-Null
}

# Copy panel files
Write-Host "Copying panel files to $ControlsDir..." -ForegroundColor Yellow
$panelFiles = @(
    "FloatingPanel.cs",
    "ToolboxPanel.cs", 
    "PropertiesPanel.cs",
    "LayersPanel.cs",
    "ExplorerPanel.cs",
    "LayoutsPanel.cs",
    "PanelManager.cs"
)

foreach ($file in $panelFiles) {
    $sourcePath = Join-Path $ScriptDir "Controls\$file"
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath $ControlsDir -Force
        Write-Host "  Copied $file" -ForegroundColor Green
    } else {
        Write-Host "  WARNING: $file not found at $sourcePath" -ForegroundColor Yellow
    }
}

# Copy MainWindow.Panels.cs
$panelsCs = Join-Path $ScriptDir "MainWindow.Panels.cs"
if (Test-Path $panelsCs) {
    Copy-Item $panelsCs $ProjectPath -Force
    Write-Host "  Copied MainWindow.Panels.cs" -ForegroundColor Green
}

# Update MainWindow.xaml.cs
Write-Host ""
Write-Host "Updating MainWindow.xaml.cs..." -ForegroundColor Yellow

$content = Get-Content $MainWindowCs -Raw

# Check if already integrated
if ($content -match "InitializeFloatingPanels") {
    Write-Host "  Already integrated (InitializeFloatingPanels found)" -ForegroundColor Cyan
} else {
    # Find InitializeComponent() and add InitializeFloatingPanels() after it
    $pattern = "(InitializeComponent\s*\(\s*\)\s*;)"
    $replacement = "`$1`r`n            InitializeFloatingPanels();"
    
    if ($content -match $pattern) {
        $content = $content -replace $pattern, $replacement
        Set-Content $MainWindowCs $content -NoNewline
        Write-Host "  Added InitializeFloatingPanels() call" -ForegroundColor Green
    } else {
        Write-Host "  WARNING: Could not find InitializeComponent()" -ForegroundColor Yellow
        Write-Host "  Please manually add: InitializeFloatingPanels();" -ForegroundColor Yellow
    }
}

# Add CleanupFloatingPanels to Window_Closing if not present
if ($content -match "CleanupFloatingPanels") {
    Write-Host "  CleanupFloatingPanels already present" -ForegroundColor Cyan
} else {
    # Try to find Window_Closing method and add cleanup
    $closingPattern = "(private\s+void\s+Window_Closing[^{]*\{)"
    if ($content -match $closingPattern) {
        $content = Get-Content $MainWindowCs -Raw
        $replacement = "`$1`r`n            CleanupFloatingPanels();"
        $content = $content -replace $closingPattern, $replacement
        Set-Content $MainWindowCs $content -NoNewline
        Write-Host "  Added CleanupFloatingPanels() to Window_Closing" -ForegroundColor Green
    } else {
        Write-Host "  NOTE: Add CleanupFloatingPanels() to your Window_Closing handler" -ForegroundColor Yellow
    }
}

# Update MainWindow.xaml - Add AllowDrop to Canvas
Write-Host ""
Write-Host "Updating MainWindow.xaml..." -ForegroundColor Yellow

$xamlContent = Get-Content $MainWindowXaml -Raw

if ($xamlContent -match 'x:Name="MainCanvas"[^>]*AllowDrop') {
    Write-Host "  Canvas already has AllowDrop" -ForegroundColor Cyan
} else {
    # Find MainCanvas and add AllowDrop and Drop handler
    $canvasPattern = '(<Canvas\s+x:Name="MainCanvas")'
    if ($xamlContent -match $canvasPattern) {
        $replacement = '$1 AllowDrop="True" Drop="MainCanvas_Drop"'
        $xamlContent = $xamlContent -replace $canvasPattern, $replacement
        Set-Content $MainWindowXaml $xamlContent -NoNewline
        Write-Host "  Added AllowDrop and Drop handler to MainCanvas" -ForegroundColor Green
    } else {
        Write-Host "  NOTE: Manually add AllowDrop=""True"" Drop=""MainCanvas_Drop"" to your canvas" -ForegroundColor Yellow
    }
}

# Add View menu items for panels
if ($xamlContent -match 'ToggleToolbox_Click') {
    Write-Host "  Panel menu items already present" -ForegroundColor Cyan
} else {
    Write-Host "  NOTE: Add panel toggle menu items to View menu (see INTEGRATION.md)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Integration Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Build the project: dotnet build" -ForegroundColor White
Write-Host "2. Run and test: dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Keyboard shortcuts:" -ForegroundColor Yellow
Write-Host "  Ctrl+T = Toolbox" -ForegroundColor White
Write-Host "  Ctrl+1 = Properties" -ForegroundColor White
Write-Host "  Ctrl+2 = Layers" -ForegroundColor White
Write-Host "  Ctrl+3 = Explorer" -ForegroundColor White
Write-Host "  Ctrl+4 = Templates" -ForegroundColor White
Write-Host ""
