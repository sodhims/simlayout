# Manual AGV Station Dragging Test
# Run the application with a test layout and interact manually

Write-Host "=== Manual AGV Station Dragging Test ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will launch the application with debugging output enabled." -ForegroundColor Yellow
Write-Host ""
Write-Host "Instructions:" -ForegroundColor Green
Write-Host "1. The app will launch with test output in this console"
Write-Host "2. Create a new custom layout with 3-5 AGV stations"
Write-Host "3. Press 'D' to enter Design Mode"
Write-Host "4. Look for pink blinking handles on AGV stations"
Write-Host "5. Try clicking and dragging an AGV station"
Write-Host "6. Watch this console for debug output"
Write-Host ""
Write-Host "Press any key to launch..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Run with console output visible
dotnet run --project LayoutEditor.csproj
