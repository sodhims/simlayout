using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region View Operations

        private const double MinZoom = 0.1;
        private const double MaxZoom = 4.0;

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(_currentZoom + 0.1);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(_currentZoom - 0.1);
        }

        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Nodes.Count == 0)
            {
                SetZoom(1.0);
                return;
            }

            // Calculate bounds of all nodes
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var node in _layout.Nodes)
            {
                minX = Math.Min(minX, node.Visual.X);
                minY = Math.Min(minY, node.Visual.Y);
                maxX = Math.Max(maxX, node.Visual.X + node.Visual.Width);
                maxY = Math.Max(maxY, node.Visual.Y + node.Visual.Height);
            }

            // Add padding
            minX -= 50; minY -= 50;
            maxX += 50; maxY += 50;

            // Calculate zoom to fit
            var contentWidth = maxX - minX;
            var contentHeight = maxY - minY;

            var viewWidth = CanvasScroller.ActualWidth;
            var viewHeight = CanvasScroller.ActualHeight;

            var zoomX = viewWidth / contentWidth;
            var zoomY = viewHeight / contentHeight;

            SetZoom(Math.Min(zoomX, zoomY) * 0.9);

            // Scroll to center content
            CanvasScroller.ScrollToHorizontalOffset(minX * _currentZoom);
            CanvasScroller.ScrollToVerticalOffset(minY * _currentZoom);
        }

        private void SetZoom(double zoom)
        {
            _currentZoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            if (CanvasScale != null)
            {
                CanvasScale.ScaleX = _currentZoom;
                CanvasScale.ScaleY = _currentZoom;
            }
            if (ZoomLabel != null)
                ZoomLabel.Text = $"{(int)(_currentZoom * 100)}%";
        }

        private void Zoom100_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0);
        }

        private void ShowGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;
            _layout.Canvas.ShowGrid = ShowGridMenu?.IsChecked ?? true;
            Redraw();
        }

        private void SnapToGrid_Click(object sender, RoutedEventArgs e)
        {
            // Toggle handled by menu item
        }

        private void ShowRulers_Click(object sender, RoutedEventArgs e)
        {
            if (ShowRulersMenu == null || HorizontalRuler == null || VerticalRuler == null) return;
            var show = ShowRulersMenu.IsChecked;
            HorizontalRuler.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            VerticalRuler.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Layer_Changed(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;  // Not initialized yet
            
            // Update Display.Layers flags
            _layout.Display.Layers.Background = LayerBackground?.IsChecked ?? true;
            _layout.Display.Layers.BackgroundImage = LayerBackgroundImage?.IsChecked ?? true;
            _layout.Display.Layers.Walls = LayerWalls?.IsChecked ?? true;
            _layout.Display.Layers.Corridors = LayerCorridors?.IsChecked ?? true;
            _layout.Display.Layers.Zones = LayerZones?.IsChecked ?? true;
            _layout.Display.Layers.Paths = LayerPaths?.IsChecked ?? true;
            _layout.Display.Layers.Nodes = LayerNodes?.IsChecked ?? true;
            _layout.Display.Layers.Labels = LayerLabels?.IsChecked ?? true;
            _layout.Display.Layers.Measurements = LayerMeasurements?.IsChecked ?? true;

            // Sync to LayerManager
            SyncCheckboxesToLayerManager();
            
            Redraw();
        }

        private void SyncCheckboxesToLayerManager()
        {
            if (_layout?.LayerManager == null) return;

            var lm = _layout.LayerManager;
            
            SetLayerVisibility(lm, LayerTypes.Grid, LayerBackground?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.BackgroundImage, LayerBackgroundImage?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Walls, LayerWalls?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Corridors, LayerCorridors?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Zones, LayerZones?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Paths, LayerPaths?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Nodes, LayerNodes?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Annotations, LayerLabels?.IsChecked ?? true);
            SetLayerVisibility(lm, LayerTypes.Measurements, LayerMeasurements?.IsChecked ?? true);
        }

        private void SetLayerVisibility(Models.LayerManager lm, string layerType, bool visible)
        {
            var layer = lm.GetLayerByType(layerType);
            if (layer != null)
                layer.IsVisible = visible;
        }

        private void WallType_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (WallTypeCombo?.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                _currentWallType = item.Tag?.ToString() ?? Models.WallTypes.Standard;
            }
        }

        private void Units_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_layout == null) return;  // Not initialized yet
            
            if (UnitsCombo?.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                _layout.Metadata.Units = item.Content?.ToString()?.ToLower() ?? "meters";
            }
        }

        #endregion
    }
}
