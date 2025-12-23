using System;
using System.IO;

namespace LayoutEditor
{
    public static class DebugLogger
    {
        private static readonly string LogPath;
        private static readonly object _lock = new object();
        public static bool IsEnabled { get; set; } = true;

        static DebugLogger()
        {
            // Create log directory if it doesn't exist
            string logDir = @"R:\Layoutbak\Logs";
            try
            {
                Directory.CreateDirectory(logDir);
            }
            catch { }

            // Create log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            LogPath = Path.Combine(logDir, $"LayoutLog.{timestamp}.log");

            // Initialize log file
            try
            {
                File.WriteAllText(LogPath, $"=== LayoutEditor Debug Log Started at {DateTime.Now} ===\n");
                Console.WriteLine($"[DEBUG] Logging to: {LogPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create log file: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            if (!IsEnabled) return;

            var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";

            // Write to console
            Console.WriteLine(timestamped);

            // Write to file
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(LogPath, timestamped + "\n");
                }
                catch { }
            }
        }

        public static string GetLogPath() => LogPath;
    }
}
