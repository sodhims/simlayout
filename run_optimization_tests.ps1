# Run OptimizationService and FrictionlessMode tests
# Usage: .\run_optimization_tests.ps1

Write-Host "Building project..." -ForegroundColor Cyan
dotnet build LayoutEditor.csproj --nologo -v q 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Use the compiled assembly to run tests
Add-Type -Path "bin\Debug\net8.0-windows\LayoutEditor.dll" -ErrorAction SilentlyContinue

$totalFailed = 0

# Run OptimizationService tests
Write-Host ""
$testType = [LayoutEditor.Tests.OptimizationServiceTests]
$method = $testType.GetMethod("RunAllTests")
$task = $method.Invoke($null, @())
$result = $task.GetAwaiter().GetResult()
$totalFailed += $result

# Run FrictionlessMode tests
Write-Host ""
$testType2 = [LayoutEditor.Tests.FrictionlessModeTests]
$method2 = $testType2.GetMethod("RunAllTests")
$result2 = $method2.Invoke($null, @())
$totalFailed += $result2

Write-Host ""
if ($totalFailed -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "$totalFailed test(s) failed" -ForegroundColor Red
}
