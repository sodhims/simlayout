// Standalone test runner for Equipment Browser Tests
// Compile: csc /r:bin\Debug\net8.0-windows\LayoutEditor.dll TestRunner.cs
// Or run via: dotnet run --project LayoutEditor.csproj -- --test-only

using System;
using LayoutEditor.Tests;

namespace LayoutEditor
{
    public static class TestRunner
    {
        public static void RunEquipmentBrowserTestsOnly()
        {
            Console.WriteLine("=== Running Equipment Browser Tests Only ===\n");
            bool result = EquipmentBrowserTests.RunAllTests();
            Console.WriteLine(result ? "\nALL TESTS PASSED!" : "\nSOME TESTS FAILED!");
        }
    }
}
