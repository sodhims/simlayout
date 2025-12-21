using System;
using System.Windows;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Zoom Handlers

        private double _zoomLevel = 1.0;
        private const double ZoomMin = 0.1;
        private const double ZoomMax = 5.0;
        private const double ZoomStep = 0.1;

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(_zoomLevel + ZoomStep);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(_zoomLevel - ZoomStep);
        }

        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            // Calculate bounds of all content
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var node in _layout.Nodes)
            {
                minX = Math.Min(minX, node.Visual.X);
                minY = Math.Min(minY, node.Visual.Y);
                maxX = Math.Max(maxX, node.Visual.X + node.Visual.Width);
                maxY = Math.Max(maxY, node.Visual.Y + node.Visual.Height);
            }

            foreach (var wall in _layout.Walls)
            {
                minX = Math.Min(minX, Math.Min(wall.X1, wall.X2));
                minY = Math.Min(minY, Math.Min(wall.Y1, wall.Y2));
                maxX = Math.Max(maxX, Math.Max(wall.X1, wall.X2));
                maxY = Math.Max(maxY, Math.Max(wall.Y1, wall.Y2));
            }

            if (minX == double.MaxValue) return; // No content

            double contentWidth = maxX - minX + 100; // Add padding
            double contentHeight = maxY - minY + 100;

            double viewWidth = CanvasScroller.ActualWidth;
            double viewHeight = CanvasScroller.ActualHeight;

            if (viewWidth <= 0 || viewHeight <= 0) return;

            double zoomX = viewWidth / contentWidth;
            double zoomY = viewHeight / contentHeight;
            double newZoom = Math.Min(zoomX, zoomY);

            SetZoom(Math.Max(ZoomMin, Math.Min(ZoomMax, newZoom)));

            // Center on content
            CanvasScroller.ScrollToHorizontalOffset((minX - 50) * _zoomLevel);
            CanvasScroller.ScrollToVerticalOffset((minY - 50) * _zoomLevel);
        }

        private void SetZoom(double zoom)
        {
            _zoomLevel = Math.Max(ZoomMin, Math.Min(ZoomMax, zoom));
            CanvasScale.ScaleX = _zoomLevel;
            CanvasScale.ScaleY = _zoomLevel;
            
            int percent = (int)(_zoomLevel * 100);
            ZoomLabel.Text = $"{percent}%";
            ZoomText.Text = $"{percent}%";
        }

        #endregion
    }
}
