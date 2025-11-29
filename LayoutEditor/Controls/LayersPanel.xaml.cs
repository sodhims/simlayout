using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    public partial class LayersPanel : UserControl
    {
        private LayerManager? _layerManager;
        private LayerData? _selectedLayer;
        private bool _isUpdating = false;

        public event EventHandler? LayerChanged;
        public event EventHandler? ActiveLayerChanged;
        public event EventHandler<LayerData>? LayerStyleChanged;

        public LayersPanel()
        {
            InitializeComponent();
        }

        public void SetLayerManager(LayerManager manager)
        {
            _layerManager = manager;
            RefreshLayerList();
        }

        public void RefreshLayerList()
        {
            if (_layerManager == null) return;

            // Create view models with IsActive property
            var items = _layerManager.Layers
                .OrderByDescending(l => l.ZOrder)
                .Select(l => new LayerViewModel
                {
                    Id = l.Id,
                    Name = l.Name,
                    IsVisible = l.IsVisible,
                    IsLocked = l.IsLocked,
                    IsActive = l.Id == _layerManager.ActiveLayerId,
                    Style = l.Style,
                    LayerType = l.LayerType
                })
                .ToList();

            LayersList.ItemsSource = items;

            // Update selected layer properties
            if (_selectedLayer != null)
            {
                UpdatePropertyPanel(_selectedLayer);
            }
        }

        private void Layer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string layerId)
            {
                SelectLayer(layerId);
                
                // Double-click to toggle visibility
                if (e.ClickCount == 2)
                {
                    var layer = _layerManager?.GetLayer(layerId);
                    if (layer != null)
                    {
                        layer.IsVisible = !layer.IsVisible;
                        RefreshLayerList();
                        LayerChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void SelectLayer(string layerId)
        {
            if (_layerManager == null) return;

            _layerManager.SetActiveLayer(layerId);
            _selectedLayer = _layerManager.GetLayer(layerId);

            RefreshLayerList();
            UpdatePropertyPanel(_selectedLayer);
            ActiveLayerChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdatePropertyPanel(LayerData? layer)
        {
            _isUpdating = true;

            if (layer == null)
            {
                PropertiesGrid.IsEnabled = false;
                return;
            }

            PropertiesGrid.IsEnabled = true;

            PropName.Text = layer.Name;
            PropStrokeColor.Text = layer.Style.StrokeColor;
            PropStrokeWidth.Value = layer.Style.StrokeWidth;
            StrokeWidthText.Text = layer.Style.StrokeWidth.ToString("F1");
            PropOpacity.Value = layer.Opacity;
            OpacityText.Text = $"{(int)(layer.Opacity * 100)}%";
            PropFillOpacity.Value = layer.Style.FillOpacity;

            // Update color previews
            try
            {
                StrokeColorPreview.Color = (Color)ColorConverter.ConvertFromString(layer.Style.StrokeColor);
            }
            catch { StrokeColorPreview.Color = Colors.Gray; }

            try
            {
                FillColorPreview.Color = (Color)ColorConverter.ConvertFromString(layer.Style.FillColor);
            }
            catch { FillColorPreview.Color = Colors.White; }

            // Set dash pattern
            var dashArray = layer.Style.StrokeDashArray;
            PropDashPattern.SelectedIndex = dashArray switch
            {
                "" => 0,
                "5,5" => 1,
                "2,2" => 2,
                "8,4,2,4" => 3,
                "12,6" => 4,
                _ => 0
            };

            _isUpdating = false;
        }

        private void Visibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && btn.Tag is string layerId)
            {
                var layer = _layerManager?.GetLayer(layerId);
                if (layer != null)
                {
                    layer.IsVisible = btn.IsChecked ?? true;
                    RefreshLayerList();
                    LayerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && btn.Tag is string layerId)
            {
                var layer = _layerManager?.GetLayer(layerId);
                if (layer != null)
                {
                    layer.IsLocked = btn.IsChecked ?? false;
                    RefreshLayerList();
                    LayerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer != null && _layerManager != null)
            {
                _layerManager.MoveLayerUp(_selectedLayer.Id);
                RefreshLayerList();
                LayerChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer != null && _layerManager != null)
            {
                _layerManager.MoveLayerDown(_selectedLayer.Id);
                RefreshLayerList();
                LayerChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            if (_layerManager == null) return;

            var newLayer = new LayerData
            {
                Name = $"Custom Layer {_layerManager.Layers.Count(l => l.LayerType == LayerTypes.Custom) + 1}",
                LayerType = LayerTypes.Custom,
                ZOrder = _layerManager.Layers.Max(l => l.ZOrder) + 1,
                Style = new LayerStyle
                {
                    StrokeColor = "#9B59B6",
                    StrokeWidth = 2
                }
            };

            _layerManager.Layers.Add(newLayer);
            SelectLayer(newLayer.Id);
            RefreshLayerList();
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer == null || _layerManager == null) return;

            // Only allow deleting custom layers
            if (_selectedLayer.LayerType != LayerTypes.Custom)
            {
                MessageBox.Show("Cannot delete built-in layers.", "Delete Layer",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _layerManager.Layers.Remove(_selectedLayer);
            _selectedLayer = null;
            _layerManager.SetActiveLayer(_layerManager.Layers.FirstOrDefault()?.Id ?? "");
            RefreshLayerList();
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Property Changed Handlers

        private void PropName_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || _selectedLayer == null) return;
            
            // Only allow renaming custom layers
            if (_selectedLayer.LayerType == LayerTypes.Custom)
            {
                _selectedLayer.Name = PropName.Text;
                RefreshLayerList();
            }
        }

        private void PropStrokeColor_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || _selectedLayer == null) return;

            var colorText = PropStrokeColor.Text;
            if (!colorText.StartsWith("#")) colorText = "#" + colorText;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorText);
                _selectedLayer.Style.StrokeColor = colorText;
                StrokeColorPreview.Color = color;
                RefreshLayerList();
                LayerStyleChanged?.Invoke(this, _selectedLayer);
            }
            catch { }
        }

        private void PropStrokeWidth_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating || _selectedLayer == null) return;

            _selectedLayer.Style.StrokeWidth = e.NewValue;
            if (StrokeWidthText != null)
                StrokeWidthText.Text = e.NewValue.ToString("F1");
            LayerStyleChanged?.Invoke(this, _selectedLayer);
        }

        private void PropDashPattern_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || _selectedLayer == null) return;

            if (PropDashPattern.SelectedItem is ComboBoxItem item && item.Tag is string dash)
            {
                _selectedLayer.Style.StrokeDashArray = dash;
                LayerStyleChanged?.Invoke(this, _selectedLayer);
            }
        }

        private void PropOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating || _selectedLayer == null) return;

            _selectedLayer.Opacity = e.NewValue;
            if (OpacityText != null)
                OpacityText.Text = $"{(int)(e.NewValue * 100)}%";
            LayerStyleChanged?.Invoke(this, _selectedLayer);
        }

        private void PropFillOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating || _selectedLayer == null) return;

            _selectedLayer.Style.FillOpacity = e.NewValue;
            LayerStyleChanged?.Invoke(this, _selectedLayer);
        }

        private void StrokeColor_Click(object sender, MouseButtonEventArgs e)
        {
            ShowColorPicker(true);
        }

        private void FillColor_Click(object sender, MouseButtonEventArgs e)
        {
            ShowColorPicker(false);
        }

        private void ShowColorPicker(bool isStroke)
        {
            if (_selectedLayer == null) return;

            // Create a simple color picker dialog
            var dialog = new Window
            {
                Title = isStroke ? "Stroke Color" : "Fill Color",
                Width = 300,
                Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var currentColor = isStroke ? _selectedLayer.Style.StrokeColor : _selectedLayer.Style.FillColor;

            var panel = new StackPanel { Margin = new Thickness(10) };

            // Preset colors
            var presets = new[]
            {
                "#000000", "#FFFFFF", "#808080", "#C0C0C0",
                "#FF0000", "#00FF00", "#0000FF", "#FFFF00",
                "#FF00FF", "#00FFFF", "#FFA500", "#800080",
                "#2C3E50", "#E74C3C", "#27AE60", "#3498DB",
                "#9B59B6", "#F39C12", "#1ABC9C", "#34495E"
            };

            var colorGrid = new WrapPanel();
            foreach (var preset in presets)
            {
                var btn = new Button
                {
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset)),
                    Tag = preset
                };
                btn.Click += (s, e) =>
                {
                    if (isStroke)
                    {
                        _selectedLayer.Style.StrokeColor = preset;
                        PropStrokeColor.Text = preset;
                        StrokeColorPreview.Color = (Color)ColorConverter.ConvertFromString(preset);
                    }
                    else
                    {
                        _selectedLayer.Style.FillColor = preset;
                        FillColorPreview.Color = (Color)ColorConverter.ConvertFromString(preset);
                    }
                    RefreshLayerList();
                    LayerStyleChanged?.Invoke(this, _selectedLayer);
                    dialog.Close();
                };
                colorGrid.Children.Add(btn);
            }
            panel.Children.Add(colorGrid);

            // Custom color input
            panel.Children.Add(new TextBlock { Text = "Custom (hex):", Margin = new Thickness(0, 10, 0, 5) });
            var customInput = new TextBox { Text = currentColor, Margin = new Thickness(0, 0, 0, 10) };
            panel.Children.Add(customInput);

            var applyBtn = new Button { Content = "Apply", Padding = new Thickness(20, 5, 20, 5) };
            applyBtn.Click += (s, e) =>
            {
                try
                {
                    var color = customInput.Text;
                    if (!color.StartsWith("#")) color = "#" + color;
                    ColorConverter.ConvertFromString(color); // Validate

                    if (isStroke)
                    {
                        _selectedLayer.Style.StrokeColor = color;
                        PropStrokeColor.Text = color;
                        StrokeColorPreview.Color = (Color)ColorConverter.ConvertFromString(color);
                    }
                    else
                    {
                        _selectedLayer.Style.FillColor = color;
                        FillColorPreview.Color = (Color)ColorConverter.ConvertFromString(color);
                    }
                    RefreshLayerList();
                    LayerStyleChanged?.Invoke(this, _selectedLayer);
                    dialog.Close();
                }
                catch
                {
                    MessageBox.Show("Invalid color format. Use hex like #FF0000", "Error");
                }
            };
            panel.Children.Add(applyBtn);

            dialog.Content = panel;
            dialog.ShowDialog();
        }

        #endregion
    }

    /// <summary>
    /// View model for layer display
    /// </summary>
    public class LayerViewModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public bool IsActive { get; set; }
        public LayerStyle Style { get; set; } = new();
        public string LayerType { get; set; } = "";
    }
}
