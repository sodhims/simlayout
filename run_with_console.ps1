# Launch LayoutEditor with visible console output
# This keeps the console window open and shows debug output

Write-Host "=== Starting LayoutEditor with Console Output ===" -ForegroundColor Cyan
Write-Host "The console will remain open to show debug output." -ForegroundColor Yellow
Write-Host "Tests will run first, then the GUI will open." -ForegroundColor Yellow
Write-Host ""

# Run the application - output goes to this console
dotnet run --project LayoutEditor.csproj

Write-Host ""
Write-Host "=== Application Closed ===" -ForegroundColor Cyan
Write-Host "Press any key to exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
