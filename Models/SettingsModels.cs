using System;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Layout metadata (name, author, dates, units)
    /// </summary>
    public class LayoutMetadata : NotifyBase
    {
        private string _name = "Untitled Layout";
        private string _author = "";
        private DateTime _created = DateTime.Now;
        private DateTime _modified = DateTime.Now;
        private string _description = "";
        private string _units = "meters";
        private double _pixelsPerUnit = 20.0;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public DateTime Created
        {
            get => _created;
            set => SetProperty(ref _created, value);
        }

        public DateTime Modified
        {
            get => _modified;
            set => SetProperty(ref _modified, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Units
        {
            get => _units;
            set => SetProperty(ref _units, value);
        }

        public double PixelsPerUnit
        {
            get => _pixelsPerUnit;
            set => SetProperty(ref _pixelsPerUnit, value);
        }
    }

    /// <summary>
    /// Canvas size and grid settings
    /// </summary>
    public enum SnapMode
    {
        None = 0,
        Grid = 1,
        // Future modes: Nodes, Guides, All
    }
    
    public class CanvasSettings : NotifyBase
    {
        private double _width = 1200;
        private double _height = 800;
        private int _gridSize = 20;
        private bool _snapToGrid = true;
        private SnapMode _snapMode = SnapMode.Grid;
        private bool _showGrid = true;
        private string? _backgroundImage;

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public int GridSize
        {
            get => _gridSize;
            set => SetProperty(ref _gridSize, value);
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set => SetProperty(ref _snapToGrid, value);
        }

        public SnapMode SnapMode
        {
            get => _snapMode;
            set => SetProperty(ref _snapMode, value);
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set => SetProperty(ref _showGrid, value);
        }

        public string? BackgroundImage
        {
            get => _backgroundImage;
            set => SetProperty(ref _backgroundImage, value);
        }
    }

    /// <summary>
    /// Display and visibility settings
    /// </summary>
    public class DisplaySettings : NotifyBase
    {
        private bool _showGrid = true;
        private bool _showRulers = true;
        private bool _showMinimap = false;
        private bool _showLabels = true;
        private string _pathRoutingDefault = "direct";
        private LayerVisibility _layers = new();

        public bool ShowGrid
        {
            get => _showGrid;
            set => SetProperty(ref _showGrid, value);
        }

        public bool ShowRulers
        {
            get => _showRulers;
            set => SetProperty(ref _showRulers, value);
        }

        public bool ShowMinimap
        {
            get => _showMinimap;
            set => SetProperty(ref _showMinimap, value);
        }

        public bool ShowLabels
        {
            get => _showLabels;
            set => SetProperty(ref _showLabels, value);
        }

        public string PathRoutingDefault
        {
            get => _pathRoutingDefault;
            set => SetProperty(ref _pathRoutingDefault, value);
        }

        public LayerVisibility Layers
        {
            get => _layers;
            set => SetProperty(ref _layers, value);
        }
    }

    /// <summary>
    /// Layer visibility toggles
    /// </summary>
    public class LayerVisibility : NotifyBase
    {
        private bool _backgroundImage = true;
        private bool _background = true;
        private bool _walls = true;
        private bool _corridors = true;
        private bool _zones = true;
        private bool _paths = true;
        private bool _nodes = true;
        private bool _labels = true;
        private bool _measurements = true;

        public bool BackgroundImage
        {
            get => _backgroundImage;
            set => SetProperty(ref _backgroundImage, value);
        }

        public bool Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        public bool Walls
        {
            get => _walls;
            set => SetProperty(ref _walls, value);
        }

        public bool Corridors
        {
            get => _corridors;
            set => SetProperty(ref _corridors, value);
        }

        public bool Zones
        {
            get => _zones;
            set => SetProperty(ref _zones, value);
        }

        public bool Paths
        {
            get => _paths;
            set => SetProperty(ref _paths, value);
        }

        public bool Nodes
        {
            get => _nodes;
            set => SetProperty(ref _nodes, value);
        }

        public bool Labels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
        }

        public bool Measurements
        {
            get => _measurements;
            set => SetProperty(ref _measurements, value);
        }
    }

    /// <summary>
    /// Editor UI preferences (saved to user settings file)
    /// </summary>
    public class EditorSettings : NotifyBase
    {
        private static EditorSettings? _instance;
        public static EditorSettings Instance => _instance ??= Load();

        // Panel visibility
        private bool _showToolbox = true;
        private bool _showExplorer = true;
        private bool _showProperties = true;
        private bool _showLayersPanel = true;
        private bool _showTemplates = true;

        // UI sizing
        private double _panelFontSize = 7;
        private double _panelPadding = 2;
        private double _lineThickness = 1.0;
        private double _pathThickness = 1.5;

        public bool ShowToolbox { get => _showToolbox; set => SetProperty(ref _showToolbox, value); }
        public bool ShowExplorer { get => _showExplorer; set => SetProperty(ref _showExplorer, value); }
        public bool ShowProperties { get => _showProperties; set => SetProperty(ref _showProperties, value); }
        public bool ShowLayersPanel { get => _showLayersPanel; set => SetProperty(ref _showLayersPanel, value); }
        public bool ShowTemplates { get => _showTemplates; set => SetProperty(ref _showTemplates, value); }

        public double PanelFontSize { get => _panelFontSize; set => SetProperty(ref _panelFontSize, value); }
        public double PanelPadding { get => _panelPadding; set => SetProperty(ref _panelPadding, value); }
        public double LineThickness { get => _lineThickness; set => SetProperty(ref _lineThickness, value); }
        public double PathThickness { get => _pathThickness; set => SetProperty(ref _pathThickness, value); }

        private static string SettingsPath => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LayoutEditor", "editor_settings.json");

        public static EditorSettings Load()
        {
            try
            {
                var path = SettingsPath;
                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    return System.Text.Json.JsonSerializer.Deserialize<EditorSettings>(json) ?? new EditorSettings();
                }
            }
            catch { }
            return new EditorSettings();
        }

        public void Save()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
