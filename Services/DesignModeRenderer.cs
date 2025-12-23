using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders visual handles and feedback for design mode
    /// </summary>
    public class DesignModeRenderer
    {
        private readonly LayoutData _layout;
        private readonly ArchitectureLayerManager? _layerManager;
        private bool _handleVisible = true;

        public DesignModeRenderer(LayoutData layout, ArchitectureLayerManager? layerManager = null)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _layerManager = layerManager;
        }

        /// <summary>
        /// Set handle visibility state (called by blinking timer)
        /// </summary>
        public void SetHandleVisibility(bool visible)
        {
            _handleVisible = visible;
        }

        /// <summary>
        /// Draw design mode overlays (vertex handles, grid, etc.)
        /// </summary>
        public void DrawDesignModeOverlay(Canvas canvas, string? selectedZoneId)
        {
            if (!_layout.DesignMode)
                return;

            // Draw blinking handles for all movable entities
            if (_handleVisible)
            {
                DrawMovableEntityHandles(canvas);
            }

            // Draw vertex handles for ALL zones in design mode (for reshaping)
            foreach (var zone in _layout.Zones)
            {
                // Skip if Spatial layer is locked
                if (_layerManager != null && _layerManager.IsLocked(LayerType.Spatial))
                    continue;

                bool isSelected = zone.Id == selectedZoneId;
                DrawZoneVertexHandlesForZone(canvas, zone, isSelected);
            }

            // Future: Add grid overlay, alignment guides, etc.
        }

        /// <summary>
        /// Draw blinking handles for all movable entities in design mode
        /// </summary>
        private void DrawMovableEntityHandles(Canvas canvas)
        {
            // Soft pink color for design mode handles
            var fillColor = new SolidColorBrush(Color.FromArgb(200, 255, 182, 193)); // LightPink with transparency
            var strokeColor = new SolidColorBrush(Color.FromArgb(255, 255, 105, 180)); // HotPink

            // Draw handles for nodes (all movable in design mode, unless layer is locked)
            foreach (var node in _layout.Nodes)
            {
                // Skip if layer is locked
                if (_layerManager != null && _layerManager.IsLocked(node.ArchitectureLayer))
                    continue;

                var centerX = node.Visual.X + node.Visual.Width / 2;
                var centerY = node.Visual.Y + node.Visual.Height / 2;
                DrawHandle(canvas, centerX, centerY, fillColor, strokeColor);
            }

            // Draw handles for groups (cells, zones - all movable in design mode)
            foreach (var group in _layout.Groups)
            {
                if (group.Members.Count == 0) continue;

                // Calculate group center
                double sumX = 0, sumY = 0;
                foreach (var memberId in group.Members)
                {
                    var node = _layout.Nodes.FirstOrDefault(n => n.Id == memberId);
                    if (node != null)
                    {
                        sumX += node.Visual.X + node.Visual.Width / 2;
                        sumY += node.Visual.Y + node.Visual.Height / 2;
                    }
                }
                double centerX = sumX / group.Members.Count;
                double centerY = sumY / group.Members.Count;

                DrawHandle(canvas, centerX, centerY, fillColor, strokeColor);
            }

            // EOT cranes are NOT movable in design mode (only runways move)
            // Jib cranes ARE movable in design mode (can slide along walls)
            // Draw handles for Jib cranes (movable in design mode, unless layer is locked)
            foreach (var crane in _layout.JibCranes)
            {
                // Skip if layer is locked
                if (_layerManager != null && _layerManager.IsLocked(crane.ArchitectureLayer))
                    continue;

                DrawHandle(canvas, crane.CenterX, crane.CenterY, fillColor, strokeColor);
            }

            // Draw handles for runways (EOT bays - movable in design mode)
            if (_layout.Runways != null)
            {
                foreach (var runway in _layout.Runways)
                {
                    // Skip if layer is locked
                    if (_layerManager != null && _layerManager.IsLocked(runway.ArchitectureLayer))
                        continue;

                    // Draw handle at center of runway
                    double centerX = (runway.StartX + runway.EndX) / 2;
                    double centerY = (runway.StartY + runway.EndY) / 2;
                    DrawHandle(canvas, centerX, centerY, fillColor, strokeColor);
                }
            }

            // Draw handles for AGV stations (now movable in design mode!)
            foreach (var station in _layout.AGVStations)
            {
                // Skip if layer is locked
                if (_layerManager != null && _layerManager.IsLocked(station.ArchitectureLayer))
                    continue;

                DrawHandle(canvas, station.X, station.Y, fillColor, strokeColor);
            }

            // Draw handles for zones (movable in design mode, at zone centroid)
            foreach (var zone in _layout.Zones)
            {
                // Skip if Spatial layer is locked
                if (_layerManager != null && _layerManager.IsLocked(LayerType.Spatial))
                    continue;

                // Calculate zone centroid from polygon points
                if (zone.Points.Count > 0)
                {
                    double sumX = 0, sumY = 0;
                    foreach (var pt in zone.Points)
                    {
                        sumX += pt.X;
                        sumY += pt.Y;
                    }
                    double centerX = sumX / zone.Points.Count;
                    double centerY = sumY / zone.Points.Count;
                    DrawHandle(canvas, centerX, centerY, fillColor, strokeColor);
                }
                else if (zone.Width > 0 && zone.Height > 0)
                {
                    // Rectangle zone
                    double centerX = zone.X + zone.Width / 2;
                    double centerY = zone.Y + zone.Height / 2;
                    DrawHandle(canvas, centerX, centerY, fillColor, strokeColor);
                }
            }
        }

        /// <summary>
        /// Draw a single handle at the specified position
        /// </summary>
        private void DrawHandle(Canvas canvas, double x, double y, Brush fillColor, Brush strokeColor)
        {
            const double handleRadius = 10.0;
            const double strokeThickness = 2.5;

            // Outer circle (main handle)
            var outerCircle = new Ellipse
            {
                Width = handleRadius * 2,
                Height = handleRadius * 2,
                Fill = fillColor,
                Stroke = strokeColor,
                StrokeThickness = strokeThickness,
                Opacity = 1.0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(outerCircle, x - handleRadius);
            Canvas.SetTop(outerCircle, y - handleRadius);
            Canvas.SetZIndex(outerCircle, 9998);  // Below vertex handles
            canvas.Children.Add(outerCircle);

            // Inner highlight (makes it look 3D and more visible)
            var innerCircle = new Ellipse
            {
                Width = handleRadius * 0.6,
                Height = handleRadius * 0.6,
                Fill = Brushes.White,
                Opacity = 0.6,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(innerCircle, x - handleRadius * 0.3);
            Canvas.SetTop(innerCircle, y - handleRadius * 0.3);
            Canvas.SetZIndex(innerCircle, 9999);
            canvas.Children.Add(innerCircle);

            // Add a soft glow effect
            outerCircle.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = ((SolidColorBrush)fillColor).Color,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.8
            };
        }

        /// <summary>
        /// Draw vertex handles for a selected zone
        /// </summary>
        private void DrawZoneVertexHandles(Canvas canvas, string zoneId)
        {
            var zone = _layout.Zones.FirstOrDefault(z => z.Id == zoneId);
            if (zone == null || zone.Points == null)
                return;

            const double handleRadius = 8.0;
            const double strokeThickness = 2.0;

            for (int i = 0; i < zone.Points.Count; i++)
            {
                var point = zone.Points[i];

                // Outer circle (handle)
                var handle = new Ellipse
                {
                    Width = handleRadius * 2,
                    Height = handleRadius * 2,
                    Fill = Brushes.White,
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = strokeThickness,
                    Opacity = 1.0,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(handle, point.X - handleRadius);
                Canvas.SetTop(handle, point.Y - handleRadius);
                Canvas.SetZIndex(handle, 10000);
                canvas.Children.Add(handle);

                // Inner dot for visibility
                var innerDot = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.DodgerBlue,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(innerDot, point.X - 2);
                Canvas.SetTop(innerDot, point.Y - 2);
                Canvas.SetZIndex(innerDot, 10001);
                canvas.Children.Add(innerDot);

                // Vertex number label
                var label = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(200, 30, 144, 255)), // DodgerBlue with alpha
                    Padding = new Thickness(3, 1, 3, 1),
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(label, point.X + handleRadius + 4);
                Canvas.SetTop(label, point.Y - 10);
                Canvas.SetZIndex(label, 10002);
                canvas.Children.Add(label);
            }

            // Draw zone outline highlight
            if (zone.Points.Count >= 3)
            {
                var highlight = new Polygon
                {
                    Points = new PointCollection(zone.Points.Select(p => new Point(p.X, p.Y))),
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = false
                };

                Canvas.SetZIndex(highlight, 9999);
                canvas.Children.Add(highlight);
            }
        }

        /// <summary>
        /// Draw vertex handles for a zone (for reshaping in design mode)
        /// </summary>
        private void DrawZoneVertexHandlesForZone(Canvas canvas, ZoneData zone, bool isSelected)
        {
            if (zone.Points == null || zone.Points.Count == 0)
                return;

            DebugLogger.Log($"[DesignMode] Drawing vertex handles for zone '{zone.Name}' ({zone.Points.Count} points)");

            // Different styles for selected vs unselected zones - LARGER handles for easier grabbing
            // Use bright green color to make very visible
            double handleRadius = isSelected ? 14.0 : 12.0;
            double strokeThickness = isSelected ? 3.0 : 2.5;
            var strokeColor = isSelected ? Brushes.DodgerBlue : Brushes.DarkGreen;
            var fillColor = isSelected ? Brushes.White : Brushes.LimeGreen;

            for (int i = 0; i < zone.Points.Count; i++)
            {
                var point = zone.Points[i];
                DebugLogger.Log($"[DesignMode]   Vertex {i}: ({point.X:F1}, {point.Y:F1})");

                // Vertex handle (square for corners to distinguish from center handle)
                var handle = new Rectangle
                {
                    Width = handleRadius * 2,
                    Height = handleRadius * 2,
                    Fill = fillColor,
                    Stroke = strokeColor,
                    StrokeThickness = strokeThickness,
                    RadiusX = 2,
                    RadiusY = 2,
                    Opacity = 1.0,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(handle, point.X - handleRadius);
                Canvas.SetTop(handle, point.Y - handleRadius);
                Canvas.SetZIndex(handle, isSelected ? 10005 : 10003);  // Very high Z to ensure visible
                canvas.Children.Add(handle);

                DebugLogger.Log($"[DesignMode]   Handle drawn at canvas pos ({point.X - handleRadius:F1}, {point.Y - handleRadius:F1})");

                // Vertex number label (only for selected zone)
                if (isSelected)
                {
                    var label = new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        Background = new SolidColorBrush(Color.FromArgb(200, 30, 144, 255)),
                        Padding = new Thickness(2, 0, 2, 0),
                        IsHitTestVisible = false
                    };

                    Canvas.SetLeft(label, point.X + handleRadius + 2);
                    Canvas.SetTop(label, point.Y - 8);
                    Canvas.SetZIndex(label, 10002);
                    canvas.Children.Add(label);
                }
            }

            // Draw zone outline highlight for selected zone
            if (isSelected && zone.Points.Count >= 3)
            {
                var highlight = new Polygon
                {
                    Points = new PointCollection(zone.Points.Select(p => new Point(p.X, p.Y))),
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = false
                };

                Canvas.SetZIndex(highlight, 9999);
                canvas.Children.Add(highlight);
            }
        }
    }
}
