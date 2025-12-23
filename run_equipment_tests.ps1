# Run Equipment Browser Tests
# This script runs the tests and captures output

Write-Host "Building project..."
$buildOutput = dotnet build LayoutEditor.csproj --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!"
    $buildOutput
    exit 1
}

Write-Host "Running Equipment Browser Tests..."
Write-Host ""

# Create a simple test harness that runs just the Equipment Browser tests
$testCode = @"
using System;
using LayoutEditor.Tests;

class TestRunner
{
    static void Main()
    {
        Console.WriteLine("Starting Equipment Browser Tests...");
        Console.WriteLine();

        bool result = EquipmentBrowserTests.RunAllTests();

        Console.WriteLine();
        Console.WriteLine(result ? "ALL TESTS PASSED!" : "SOME TESTS FAILED!");

        Environment.Exit(result ? 0 : 1);
    }
}
"@

# For now, just run the main app which includes all tests
# Tests output to console during startup
Write-Host "Starting LayoutEditor (tests run on startup)..."
Write-Host "Press Ctrl+C to exit after viewing test results"
Write-Host ""

# Run the built exe directly
& "$PSScriptRoot\bin\Debug\net8.0-windows\LayoutEditor.exe" 2>&1
