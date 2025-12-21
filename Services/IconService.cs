using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LayoutEditor.Models;
using IOPath = System.IO.Path;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Central service for rendering icons throughout the UI.
    /// Provides consistent icon rendering for canvas, properties panel, toolbox, etc.
    /// Singleton pattern with Instance property for compatibility.
    /// </summary>
    public class IconService
    {
        #region Singleton

        private static IconService? _instance;
        private static readonly object _lock = new();

        public static IconService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new IconService();
                    }
                }
                return _instance;
            }
        }

        private IconService()
        {
            _iconFolderPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeIcons");
            if (!Directory.Exists(_iconFolderPath))
            {
                try { Directory.CreateDirectory(_iconFolderPath); } catch { }
            }
        }

        #endregion

        #region Fields

        private readonly string _iconFolderPath;
        private readonly Dictionary<string, BitmapImage> _imageCache = new();

        #endregion

        #region Bitmap Icon Methods (for toolbox, dialogs)

        /// <summary>
        /// Gets a bitmap icon by name and size. Returns null if not found.
        /// First checks for image files, then falls back to rendering vector icon.
        /// </summary>
        public BitmapImage? GetIcon(string? iconName, int size = 32)
        {
            if (string.IsNullOrEmpty(iconName))
                return null;

            var cacheKey = $"{iconName}_{size}";
            if (_imageCache.TryGetValue(cacheKey, out var cached))
                return cached;

            // Try to load from file first
            var fileIcon = TryLoadIconFile(iconName, size);
            if (fileIcon != null)
            {
                _imageCache[cacheKey] = fileIcon;
                return fileIcon;
            }

            // Render vector icon to bitmap
            var rendered = RenderVectorIconToBitmap(iconName, size);
            if (rendered != null)
            {
                _imageCache[cacheKey] = rendered;
            }
            return rendered;
        }

        private BitmapImage? TryLoadIconFile(string iconName, int size)
        {
            foreach (var ext in new[] { ".png", ".gif", ".jpg", ".jpeg", ".bmp" })
            {
                var filePath = IOPath.Combine(_iconFolderPath, iconName + ext);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = size * 2; // Higher res for quality
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch { }
                }
            }
            return null;
        }

        private BitmapImage? RenderVectorIconToBitmap(string iconName, int size)
        {
            var path = CreateIconPath(iconName, size);
            if (path == null)
                return null;

            try
            {
                // Render the path to a bitmap
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var brush = path.Fill ?? path.Stroke ?? Brushes.Gray;
                    var pen = path.Stroke != null ? new Pen(path.Stroke, path.StrokeThickness) : null;
                    context.DrawGeometry(path.Fill, pen, path.Data);
                }

                var renderTarget = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                stream.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets list of available icon names (from library + custom files)
        /// </summary>
        public IEnumerable<string> GetAvailableIcons()
        {
            var icons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add all icons from IconLibrary
            foreach (var key in IconLibrary.Icons.Keys)
            {
                icons.Add(key);
            }

            // Add custom icons from folder
            if (Directory.Exists(_iconFolderPath))
            {
                foreach (var file in Directory.GetFiles(_iconFolderPath, "*.*"))
                {
                    var ext = IOPath.GetExtension(file).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp")
                    {
                        icons.Add(IOPath.GetFileNameWithoutExtension(file));
                    }
                }
            }

            return icons.OrderBy(i => i);
        }

        /// <summary>
        /// Imports an icon file into the icons folder
        /// </summary>
        public string? ImportIcon(string sourcePath, string name)
        {
            if (!File.Exists(sourcePath))
                return null;

            try
            {
                var ext = IOPath.GetExtension(sourcePath).ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif" && ext != ".bmp")
                    return null;

                var safeName = name.ToLower().Replace(" ", "_");
                var destPath = IOPath.Combine(_iconFolderPath, safeName + ext);

                File.Copy(sourcePath, destPath, true);

                // Clear cache for this icon
                var keysToRemove = _imageCache.Keys.Where(k => k.StartsWith(safeName + "_")).ToList();
                foreach (var key in keysToRemove)
                    _imageCache.Remove(key);

                return safeName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the icon folder path
        /// </summary>
        public string GetIconFolderPath() => _iconFolderPath;

        /// <summary>
        /// Clears the icon cache
        /// </summary>
        public void ClearCache() => _imageCache.Clear();

        #endregion

        #region Vector Icon Rendering (Static Methods)

        /// <summary>
        /// Creates a Path element for an icon with specified size and optional color override
        /// </summary>
        public static System.Windows.Shapes.Path? CreateIconPath(string? iconKey, double size = 24, string? color = null)
        {
            if (string.IsNullOrEmpty(iconKey))
                return null;

            if (!IconLibrary.Icons.TryGetValue(iconKey, out var iconDef))
                return null;

            Geometry geometry;
            try
            {
                geometry = Geometry.Parse(iconDef.Path);
            }
            catch
            {
                // Fallback to simple rectangle if path is invalid
                geometry = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");
            }

            var actualColor = color ?? iconDef.DefaultColor;
            Brush brush;
            try
            {
                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(actualColor));
            }
            catch
            {
                brush = Brushes.Gray;
            }

            return new System.Windows.Shapes.Path
            {
                Data = geometry,
                Stroke = iconDef.IsFilled ? null : brush,
                Fill = iconDef.IsFilled ? brush : null,
                StrokeThickness = size > 20 ? 1.5 : 1.2,
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform
            };
        }

        /// <summary>
        /// Creates a preview container with the icon centered inside a border
        /// </summary>
        public static Border CreateIconPreview(string iconKey, double size = 32, string? color = null, bool showBorder = true)
        {
            var path = CreateIconPath(iconKey, size - 8, color);
            
            var container = new Border
            {
                Width = size,
                Height = size,
                Background = Brushes.White,
                BorderBrush = showBorder ? Brushes.LightGray : Brushes.Transparent,
                BorderThickness = new Thickness(showBorder ? 1 : 0),
                CornerRadius = new CornerRadius(3),
                Child = path
            };

            if (path != null)
            {
                path.HorizontalAlignment = HorizontalAlignment.Center;
                path.VerticalAlignment = VerticalAlignment.Center;
            }

            return container;
        }

        /// <summary>
        /// Creates a FrameworkElement with icon and label for use in lists/dropdowns
        /// </summary>
        public static FrameworkElement CreateIconWithLabel(string iconKey, double iconSize = 20)
        {
            if (!IconLibrary.Icons.TryGetValue(iconKey, out var iconDef))
            {
                return new TextBlock { Text = iconKey, VerticalAlignment = VerticalAlignment.Center };
            }

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(2)
            };

            var path = CreateIconPath(iconKey, iconSize);
            if (path != null)
            {
                path.Margin = new Thickness(0, 0, 6, 0);
                path.VerticalAlignment = VerticalAlignment.Center;
                panel.Children.Add(path);
            }

            panel.Children.Add(new TextBlock
            {
                Text = iconDef.Name,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });

            return panel;
        }

        #endregion

        #region ComboBox Integration (Static Methods)

        /// <summary>
        /// Creates ComboBoxItems with visual icon previews for a dropdown
        /// </summary>
        public static List<ComboBoxItem> GetIconComboBoxItems(string? category = null)
        {
            var items = new List<ComboBoxItem>();

            // Add "default" option
            items.Add(new ComboBoxItem
            {
                Content = CreateDefaultItemContent(),
                Tag = "default",
                ToolTip = "Use default icon for node type"
            });

            foreach (var kvp in IconLibrary.Icons)
            {
                if (category != null && kvp.Value.Category != category)
                    continue;

                items.Add(new ComboBoxItem
                {
                    Content = CreateIconWithLabel(kvp.Key, 18),
                    Tag = kvp.Key,
                    ToolTip = $"{kvp.Value.Name} ({kvp.Value.Category})"
                });
            }

            return items;
        }

        private static FrameworkElement CreateDefaultItemContent()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new Border
            {
                Width = 18,
                Height = 18,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 6, 0),
                Child = new TextBlock
                {
                    Text = "?",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 10,
                    Foreground = Brushes.Gray
                }
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Default",
                VerticalAlignment = VerticalAlignment.Center,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray
            });
            return panel;
        }

        #endregion

        #region Node Type Helpers (Static Methods)

        /// <summary>
        /// Gets the appropriate icon key for a node, considering custom icon or type default
        /// </summary>
        public static string GetEffectiveIconKey(string? customIcon, string? nodeType)
        {
            // If custom icon is set and valid, use it
            if (!string.IsNullOrEmpty(customIcon) && customIcon != "default" && IconLibrary.Icons.ContainsKey(customIcon))
                return customIcon;

            // Otherwise use the default for the node type
            if (!string.IsNullOrEmpty(nodeType))
                return IconLibrary.GetDefaultIcon(nodeType);

            return "cnc_mill"; // Ultimate fallback
        }

        /// <summary>
        /// Gets icon key for a transport station type
        /// </summary>
        public static string GetStationIconKey(string stationType)
        {
            return stationType?.ToLower() switch
            {
                "pickup" => "station_pickup",
                "dropoff" => "station_dropoff",
                "home" => "station_home",
                "buffer" => "station_buffer",
                "crossing" => "station_crossing",
                "waypoint" => "waypoint",
                _ => "station_pickup"
            };
        }

        /// <summary>
        /// Gets default color for a transport station type
        /// </summary>
        public static string GetStationColor(string stationType)
        {
            return stationType?.ToLower() switch
            {
                "pickup" => "#27AE60",
                "dropoff" => "#E74C3C",
                "home" => "#F39C12",
                "buffer" => "#9B59B6",
                "crossing" => "#3498DB",
                "waypoint" => "#95A5A6",
                _ => "#9B59B6"
            };
        }

        /// <summary>
        /// Checks if an icon key exists in the library
        /// </summary>
        public static bool IconExists(string iconKey) =>
            !string.IsNullOrEmpty(iconKey) && IconLibrary.Icons.ContainsKey(iconKey);

        /// <summary>
        /// Gets the display name for an icon key
        /// </summary>
        public static string GetIconDisplayName(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey) || iconKey == "default")
                return "Default";
                
            if (IconLibrary.Icons.TryGetValue(iconKey, out var iconDef))
                return iconDef.Name;
                
            return iconKey;
        }

        /// <summary>
        /// Gets all available categories
        /// </summary>
        public static IEnumerable<string> GetCategories() => IconLibrary.GetCategories();

        #endregion
    }
}
