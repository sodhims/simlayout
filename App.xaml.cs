using System;
using System.Windows;

namespace LayoutEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Run transport layer tests on startup (only in console mode)
            Startup += (s, e) =>
            {
                // Skip tests if --skip-tests argument is provided
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1 && args[1] == "--skip-tests")
                {
                    return; // Skip all tests and go straight to GUI
                }

                try
                {
                    // Disable debug logging during tests for faster execution
                    DebugLogger.IsEnabled = false;

                    // Run Equipment Browser tests FIRST to verify data sync
                    LayoutEditor.Tests.EquipmentBrowserTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.TransportLayerTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Week2Tests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Week3Tests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Week4Tests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage5ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage5BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage5CTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage5DTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage5ETests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage6ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage6BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage6CTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage6DTests.RunAllTests().Wait();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage6ETests.RunAllTests().Wait();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage7ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage7BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage7CTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage7DTests.RunAllTests().Wait();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage8ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage8BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage8CTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage8DTests.RunAllTests().Wait();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage9ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage9BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage9CTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage9DTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage9ETests.RunAllTests().Wait();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage10ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage10BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage10DTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11CTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11DTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11ETests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11FTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage11GTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage12ATests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.Stage12BTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.AGVStationDragTest.RunAllTests();
                    Console.WriteLine("\n"); // Separator
                    // Skip OptimizationServiceTests temporarily (causes async deadlock in WPF context)
                    // LayoutEditor.Tests.OptimizationServiceTests.RunAllTests().Wait();
                    Console.WriteLine("OptimizationServiceTests: SKIPPED (async deadlock in WPF context)");
                    Console.WriteLine("\n"); // Separator
                    LayoutEditor.Tests.FrictionlessModeTests.RunAllTests();
                    Console.WriteLine("\n"); // Separator

                    // Run comprehensive random layout tests (100 iterations)
                    LayoutEditor.Tests.ComprehensiveLayoutTests.RunAllTests(100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Test execution failed: {ex.Message}");
                }
            };
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Fatal exception: {ex.Message}\n\n{ex.StackTrace}", 
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
