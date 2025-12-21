using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace LayoutEditor.Models
{
    public class IconMeta
    {
        public string Key { get; set; } = "";
        public string File { get; set; } = ""; // relative path under NodeIcons
        public int Width { get; set; } = 64;
        public int Height { get; set; } = 64;
        public bool Animated { get; set; } = false;
        public Point? TerminalInNorm { get; set; } = null;
        public Point? TerminalOutNorm { get; set; } = null;
    }

    public static class IconRegistry
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Icons.json");
        private static readonly string IconFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeIcons");
        private static List<IconMeta> _icons = new();

        static IconRegistry()
        {
            try { if (!Directory.Exists(IconFolder)) Directory.CreateDirectory(IconFolder); } catch { }
            Load();
        }

        public static IReadOnlyList<IconMeta> Icons => _icons.AsReadOnly();

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) { _icons = new List<IconMeta>(); return; }
                var json = File.ReadAllText(ConfigPath);
                _icons = JsonSerializer.Deserialize<List<IconMeta>>(json) ?? new List<IconMeta>();
            }
            catch { _icons = new List<IconMeta>(); }
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_icons, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        public static IconMeta? Get(string key)
        {
            return _icons.FirstOrDefault(i => i.Key == key);
        }

        public static IconMeta? RegisterFromFile(string filePath, string desiredKey = null)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var animated = ext == ".gif";
                var fileName = Path.GetFileName(filePath);
                var key = desiredKey ?? Path.GetFileNameWithoutExtension(fileName);
                var dest = Path.Combine(IconFolder, fileName);
                File.Copy(filePath, dest, true);

                var meta = Get(key) ?? new IconMeta { Key = key };
                meta.File = Path.Combine("NodeIcons", fileName);
                meta.Animated = animated;
                // default terminals: left-center and right-center
                meta.TerminalInNorm ??= new Point(0, 0.5);
                meta.TerminalOutNorm ??= new Point(1, 0.5);
                meta.Width = 64; meta.Height = 64;

                if (!_icons.Any(i => i.Key == meta.Key)) _icons.Add(meta);
                Save();
                return meta;
            }
            catch { return null; }
        }
    }
}
