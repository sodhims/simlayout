using System;
using System.IO;

namespace LayoutEditor.Helpers
{
    public static class DebugLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LayoutEditor", "debug.log");
        
        private static readonly object _lock = new();
        
        static DebugLogger()
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch { }
        }
        
        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
                }
            }
            catch { }
        }
        
        public static void Clear()
        {
            try
            {
                if (File.Exists(LogPath))
                    File.Delete(LogPath);
            }
            catch { }
        }
        
        public static string GetLogPath() => LogPath;
    }
}
