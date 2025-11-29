using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Represents a drawing layer with visibility, lock, and style properties
    /// </summary>
    public class LayerData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Layer";
        private bool _isVisible = true;
        private bool _isLocked = false;
        private bool _isExpanded = true;
        private double _opacity = 1.0;
        private int _zOrder = 0;
        private LayerStyle _style = new();
        private string _layerType = LayerTypes.Custom;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Whether layer is visible (eye icon)
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// Whether layer is locked (lock icon) - prevents editing
        /// </summary>
        public bool IsLocked
        {
            get => _isLocked;
            set => SetProperty(ref _isLocked, value);
        }

        /// <summary>
        /// Whether layer is expanded in the panel to show children
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// Layer opacity (0-1)
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, Math.Clamp(value, 0, 1));
        }

        /// <summary>
        /// Draw order (higher = on top)
        /// </summary>
        public int ZOrder
        {
            get => _zOrder;
            set => SetProperty(ref _zOrder, value);
        }

        /// <summary>
        /// Visual style for this layer
        /// </summary>
        public LayerStyle Style
        {
            get => _style;
            set => SetProperty(ref _style, value);
        }

        /// <summary>
        /// Built-in layer type or "custom"
        /// </summary>
        public string LayerType
        {
            get => _layerType;
            set => SetProperty(ref _layerType, value);
        }
    }

    /// <summary>
    /// Visual style properties for a layer
    /// </summary>
    public class LayerStyle : NotifyBase
    {
        private string _strokeColor = "#808080";
        private double _strokeWidth = 1.0;
        private string _strokeDashArray = "";  // e.g., "5,3" for dashed
        private string _fillColor = "#FFFFFF";
        private double _fillOpacity = 0.0;
        private string _lineCapStyle = "Flat";  // Flat, Round, Square
        private string _lineJoinStyle = "Miter"; // Miter, Bevel, Round

        public string StrokeColor
        {
            get => _strokeColor;
            set => SetProperty(ref _strokeColor, value);
        }

        public double StrokeWidth
        {
            get => _strokeWidth;
            set => SetProperty(ref _strokeWidth, Math.Max(0.1, value));
        }

        public string StrokeDashArray
        {
            get => _strokeDashArray;
            set => SetProperty(ref _strokeDashArray, value);
        }

        public string FillColor
        {
            get => _fillColor;
            set => SetProperty(ref _fillColor, value);
        }

        public double FillOpacity
        {
            get => _fillOpacity;
            set => SetProperty(ref _fillOpacity, Math.Clamp(value, 0, 1));
        }

        public string LineCapStyle
        {
            get => _lineCapStyle;
            set => SetProperty(ref _lineCapStyle, value);
        }

        public string LineJoinStyle
        {
            get => _lineJoinStyle;
            set => SetProperty(ref _lineJoinStyle, value);
        }

        // Helper to get WPF brush
        public Brush GetStrokeBrush()
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(StrokeColor));
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        public Brush GetFillBrush()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(FillColor);
                color.A = (byte)(FillOpacity * 255);
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public DoubleCollection GetDashArray()
        {
            if (string.IsNullOrWhiteSpace(StrokeDashArray))
                return null!;

            try
            {
                var parts = StrokeDashArray.Split(',');
                var values = new DoubleCollection();
                foreach (var part in parts)
                {
                    if (double.TryParse(part.Trim(), out var val))
                        values.Add(val);
                }
                return values.Count > 0 ? values : null!;
            }
            catch
            {
                return null!;
            }
        }
    }

    /// <summary>
    /// Built-in layer types
    /// </summary>
    public static class LayerTypes
    {
        public const string Background = "background";
        public const string BackgroundImage = "background_image";
        public const string Grid = "grid";
        public const string Walls = "walls";
        public const string Columns = "columns";
        public const string Zones = "zones";
        public const string Corridors = "corridors";
        public const string Paths = "paths";
        public const string Nodes = "nodes";
        public const string Cells = "cells";
        public const string Measurements = "measurements";
        public const string Annotations = "annotations";
        public const string Custom = "custom";
    }

    /// <summary>
    /// Layer manager that holds all layers and tracks active layer
    /// </summary>
    public class LayerManager : NotifyBase
    {
        private ObservableCollection<LayerData> _layers = new();
        private string _activeLayerId = "";

        public ObservableCollection<LayerData> Layers
        {
            get => _layers;
            set => SetProperty(ref _layers, value);
        }

        public string ActiveLayerId
        {
            get => _activeLayerId;
            set => SetProperty(ref _activeLayerId, value);
        }

        public LayerData? ActiveLayer => 
            string.IsNullOrEmpty(_activeLayerId) ? null : 
            GetLayer(_activeLayerId);

        public LayerData? GetLayer(string id) =>
            _layers.FirstOrDefault(l => l.Id == id);

        public LayerData? GetLayerByType(string layerType) =>
            _layers.FirstOrDefault(l => l.LayerType == layerType);

        public bool IsLayerVisible(string layerType)
        {
            var layer = GetLayerByType(layerType);
            return layer?.IsVisible ?? true;
        }

        public bool IsLayerLocked(string layerType)
        {
            var layer = GetLayerByType(layerType);
            return layer?.IsLocked ?? false;
        }

        public void SetActiveLayer(string id)
        {
            ActiveLayerId = id;
        }

        public void MoveLayerUp(string id)
        {
            var layer = GetLayer(id);
            if (layer == null) return;

            var index = _layers.IndexOf(layer);
            if (index > 0)
            {
                _layers.Move(index, index - 1);
                UpdateZOrders();
            }
        }

        public void MoveLayerDown(string id)
        {
            var layer = GetLayer(id);
            if (layer == null) return;

            var index = _layers.IndexOf(layer);
            if (index < _layers.Count - 1)
            {
                _layers.Move(index, index + 1);
                UpdateZOrders();
            }
        }

        private void UpdateZOrders()
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].ZOrder = _layers.Count - i; // Higher in list = higher z-order
            }
        }

        /// <summary>
        /// Initialize with default system layers
        /// </summary>
        public void InitializeDefaultLayers()
        {
            _layers.Clear();

            // Add layers from bottom to top
            _layers.Add(new LayerData
            {
                Name = "Background Image",
                LayerType = LayerTypes.BackgroundImage,
                ZOrder = 1,
                Style = new LayerStyle { StrokeWidth = 0 }
            });

            _layers.Add(new LayerData
            {
                Name = "Grid",
                LayerType = LayerTypes.Grid,
                ZOrder = 2,
                IsLocked = true,
                Style = new LayerStyle { StrokeColor = "#E0E0E0", StrokeWidth = 0.5 }
            });

            _layers.Add(new LayerData
            {
                Name = "Zones",
                LayerType = LayerTypes.Zones,
                ZOrder = 3,
                Style = new LayerStyle { StrokeColor = "#3498DB", StrokeWidth = 2, FillOpacity = 0.1 }
            });

            _layers.Add(new LayerData
            {
                Name = "Corridors",
                LayerType = LayerTypes.Corridors,
                ZOrder = 4,
                Style = new LayerStyle { StrokeColor = "#95A5A6", StrokeWidth = 1, StrokeDashArray = "5,5" }
            });

            _layers.Add(new LayerData
            {
                Name = "Walls",
                LayerType = LayerTypes.Walls,
                ZOrder = 5,
                Style = new LayerStyle { StrokeColor = "#505050", StrokeWidth = 6 }
            });

            _layers.Add(new LayerData
            {
                Name = "Columns",
                LayerType = LayerTypes.Columns,
                ZOrder = 6,
                Style = new LayerStyle { StrokeColor = "#606060", StrokeWidth = 1, FillColor = "#808080", FillOpacity = 1 }
            });

            _layers.Add(new LayerData
            {
                Name = "Paths",
                LayerType = LayerTypes.Paths,
                ZOrder = 7,
                Style = new LayerStyle { StrokeColor = "#27AE60", StrokeWidth = 2 }
            });

            _layers.Add(new LayerData
            {
                Name = "Nodes",
                LayerType = LayerTypes.Nodes,
                ZOrder = 8,
                Style = new LayerStyle { StrokeColor = "#2C3E50", StrokeWidth = 1 }
            });

            _layers.Add(new LayerData
            {
                Name = "Cells",
                LayerType = LayerTypes.Cells,
                ZOrder = 9,
                Style = new LayerStyle { StrokeColor = "#E67E22", StrokeWidth = 2, StrokeDashArray = "8,4" }
            });

            _layers.Add(new LayerData
            {
                Name = "Measurements",
                LayerType = LayerTypes.Measurements,
                ZOrder = 10,
                Style = new LayerStyle { StrokeColor = "#E74C3C", StrokeWidth = 1 }
            });

            _layers.Add(new LayerData
            {
                Name = "Annotations",
                LayerType = LayerTypes.Annotations,
                ZOrder = 11,
                Style = new LayerStyle { StrokeColor = "#9B59B6", StrokeWidth = 1 }
            });

            // Set Walls as default active layer
            ActiveLayerId = GetLayerByType(LayerTypes.Walls)?.Id ?? "";
        }
    }
}
