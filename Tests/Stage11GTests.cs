using System;
using System.IO;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 11G Tests: Database Integration
    /// Tests persistence of frictionless mode flag
    /// </summary>
    public static class Stage11GTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 11G Tests: Database Integration ===\n");

            var tests = new Func<bool>[]
            {
                Test1_FrictionlessModePersistence,
                Test2_FrictionlessModeDefaultValue,
                Test3_FrictionlessModeRoundTrip
            };

            int passed = 0;
            int failed = 0;

            for (int i = 0; i < tests.Length; i++)
            {
                try
                {
                    bool result = tests[i]();
                    if (result)
                    {
                        passed++;
                        Console.WriteLine($"✓ Test {i + 1} passed");
                    }
                    else
                    {
                        failed++;
                        Console.WriteLine($"✗ Test {i + 1} failed");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"✗ Test {i + 1} failed with exception: {ex.Message}");
                }
            }

            Console.WriteLine($"\nStage 11G Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: FrictionlessMode persists to database
        /// </summary>
        private static bool Test1_FrictionlessModePersistence()
        {
            string testDbPath = Path.Combine(Path.GetTempPath(), $"frictionless_test_{Guid.NewGuid()}.db");

            try
            {
                var service = new SqliteLayoutService();

                // Create layout with frictionless mode enabled
                var layout = new LayoutData();
                layout.FrictionlessMode = true;
                layout.Canvas = new CanvasSettings { Width = 1000, Height = 800 };

                // Save layout
                service.SaveLayout(layout, testDbPath, "Test Layout");

                // Force connection cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Load layout
                var loadedLayout = service.LoadLayout(testDbPath);

                bool modePreserved = loadedLayout != null && loadedLayout.FrictionlessMode == true;

                return modePreserved;
            }
            finally
            {
                try
                {
                    // Give time for connections to close
                    System.Threading.Thread.Sleep(100);

                    if (File.Exists(testDbPath))
                        File.Delete(testDbPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Test 2: FrictionlessMode defaults to false
        /// </summary>
        private static bool Test2_FrictionlessModeDefaultValue()
        {
            string testDbPath = Path.Combine(Path.GetTempPath(), $"frictionless_test_{Guid.NewGuid()}.db");

            try
            {
                var service = new SqliteLayoutService();

                // Create layout without setting frictionless mode
                var layout = new LayoutData();
                layout.Canvas = new CanvasSettings { Width = 1000, Height = 800 };

                // Save layout
                service.SaveLayout(layout, testDbPath, "Test Layout");

                // Force connection cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Load layout
                var loadedLayout = service.LoadLayout(testDbPath);

                bool defaultsFalse = loadedLayout != null && loadedLayout.FrictionlessMode == false;

                return defaultsFalse;
            }
            finally
            {
                try
                {
                    System.Threading.Thread.Sleep(100);
                    if (File.Exists(testDbPath))
                        File.Delete(testDbPath);
                }
                catch { }
            }
        }

        /// <summary>
        /// Test 3: FrictionlessMode round-trip (save enabled, save disabled)
        /// </summary>
        private static bool Test3_FrictionlessModeRoundTrip()
        {
            string testDbPath = Path.Combine(Path.GetTempPath(), $"frictionless_test_{Guid.NewGuid()}.db");

            try
            {
                var service = new SqliteLayoutService();

                // First save: frictionless mode ON
                var layout1 = new LayoutData();
                layout1.FrictionlessMode = true;
                layout1.Canvas = new CanvasSettings { Width = 1000, Height = 800 };
                service.SaveLayout(layout1, testDbPath, "Test Layout");

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var loaded1 = service.LoadLayout(testDbPath);
                bool firstSaveCorrect = loaded1 != null && loaded1.FrictionlessMode == true;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Second save: frictionless mode OFF
                loaded1.FrictionlessMode = false;
                service.SaveLayout(loaded1, testDbPath, "Test Layout");

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var loaded2 = service.LoadLayout(testDbPath);
                bool secondSaveCorrect = loaded2 != null && loaded2.FrictionlessMode == false;

                return firstSaveCorrect && secondSaveCorrect;
            }
            finally
            {
                try
                {
                    System.Threading.Thread.Sleep(100);
                    if (File.Exists(testDbPath))
                        File.Delete(testDbPath);
                }
                catch { }
            }
        }
    }
}
