using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Provides visual indicators for selected equipment on the canvas.
    /// Supports two modes:
    /// - Blinker: Yellow-Red alternating (slow pulse)
    /// - Flasher: Yellow-Yellow-Red-Red pattern (attention-grabbing)
    /// </summary>
    public class SelectionIndicatorService : IDisposable
    {
        private readonly Canvas _canvas;
        private readonly LayoutData _layout;
        private readonly DispatcherTimer _timer;
        private readonly Action _redrawCallback;

        // Visual elements
        private Ellipse? _indicator;
        private Rectangle? _boundingBox;
        private Path? _targetLines;

        // State
        private object? _selectedEntity;
        private int _frameCount;
        private bool _isActive;

        // Animation settings
        private const int TickIntervalMs = 150; // Animation speed
        private const double IndicatorSize = 20;
        private const double BoxPadding = 8;

        // Colors for blinker/flasher
        private static readonly Color ColorYellow = Color.FromArgb(200, 255, 220, 0);
        private static readonly Color ColorRed = Color.FromArgb(200, 255, 60, 60);
        private static readonly Color ColorGreen = Color.FromArgb(180, 60, 255, 60);

        public enum IndicatorMode
        {
            Blinker,  // Yellow-Red alternating
            Flasher   // Yellow-Yellow-Red-Red pattern
        }

        public IndicatorMode Mode { get; set; } = IndicatorMode.Blinker;

        public SelectionIndicatorService(Canvas canvas, LayoutData layout, Action redrawCallback)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _redrawCallback = redrawCallback ?? throw new ArgumentNullException(nameof(redrawCallback));

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TickIntervalMs)
            };
            _timer.Tick += OnTick;
        }

        /// <summary>
        /// Start indicating the selected entity
        /// </summary>
        public void ShowIndicator(object entity)
        {
            if (entity == null)
            {
                HideIndicator();
                return;
            }

            _selectedEntity = entity;
            _frameCount = 0;
            _isActive = true;

            CreateVisualElements();
            UpdateIndicatorPosition();
            _timer.Start();
        }

        /// <summary>
        /// Stop and hide the indicator
        /// </summary>
        public void HideIndicator()
        {
            _timer.Stop();
            _isActive = false;
            _selectedEntity = null;

            RemoveVisualElements();
        }

        /// <summary>
        /// Update indicator if entity has moved
        /// </summary>
        public void UpdatePosition()
        {
            if (_isActive && _selectedEntity != null)
            {
                UpdateIndicatorPosition();
            }
        }

        /// <summary>
        /// Restore visual elements after canvas was cleared (e.g., by Redraw)
        /// </summary>
        public void RestoreVisuals()
        {
            if (_isActive && _selectedEntity != null)
            {
                // Recreate visual elements (they were removed when canvas was cleared)
                CreateVisualElements();
                UpdateIndicatorPosition();
            }
        }

        private void CreateVisualElements()
        {
            RemoveVisualElements();

            // Main pulsing indicator (circle)
            _indicator = new Ellipse
            {
                Width = IndicatorSize,
                Height = IndicatorSize,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(ColorYellow),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.6
                }
            };
            Panel.SetZIndex(_indicator, 9999);
            _canvas.Children.Add(_indicator);

            // Bounding box highlight
            _boundingBox = new Rectangle
            {
                Stroke = new SolidColorBrush(ColorGreen),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent
            };
            Panel.SetZIndex(_boundingBox, 9998);
            _canvas.Children.Add(_boundingBox);

            // Target lines (crosshair pointing to entity)
            _targetLines = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 6, 3 }
            };
            Panel.SetZIndex(_targetLines, 9997);
            _canvas.Children.Add(_targetLines);
        }

        private void RemoveVisualElements()
        {
            if (_indicator != null)
            {
                _canvas.Children.Remove(_indicator);
                _indicator = null;
            }
            if (_boundingBox != null)
            {
                _canvas.Children.Remove(_boundingBox);
                _boundingBox = null;
            }
            if (_targetLines != null)
            {
                _canvas.Children.Remove(_targetLines);
                _targetLines = null;
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (!_isActive || _selectedEntity == null)
            {
                _timer.Stop();
                return;
            }

            _frameCount++;
            UpdateIndicatorColor();
            UpdateIndicatorPosition();
        }

        private void UpdateIndicatorColor()
        {
            if (_indicator == null) return;

            Color currentColor;

            if (Mode == IndicatorMode.Blinker)
            {
                // Simple yellow-red alternation
                currentColor = (_frameCount % 2 == 0) ? ColorYellow : ColorRed;
            }
            else // Flasher
            {
                // Yellow-Yellow-Red-Red pattern
                int phase = _frameCount % 4;
                currentColor = (phase < 2) ? ColorYellow : ColorRed;
            }

            _indicator.Fill = new SolidColorBrush(currentColor);

            // Also pulse the size slightly
            double scale = 1.0 + 0.15 * Math.Sin(_frameCount * 0.5);
            _indicator.Width = IndicatorSize * scale;
            _indicator.Height = IndicatorSize * scale;

            // Pulse the bounding box opacity
            if (_boundingBox != null)
            {
                double opacity = 0.6 + 0.4 * Math.Sin(_frameCount * 0.3);
                _boundingBox.Stroke = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 60, 255, 60));
            }
        }

        private void UpdateIndicatorPosition()
        {
            if (_selectedEntity == null || _indicator == null) return;

            var (centerX, centerY, width, height) = GetEntityBounds(_selectedEntity);

            if (double.IsNaN(centerX) || double.IsNaN(centerY))
            {
                HideIndicator();
                return;
            }

            // Position the indicator above the entity
            double indicatorX = centerX - _indicator.Width / 2;
            double indicatorY = centerY - height / 2 - IndicatorSize - 10;

            // Keep indicator on screen
            indicatorX = Math.Max(5, Math.Min(indicatorX, _canvas.ActualWidth - _indicator.Width - 5));
            indicatorY = Math.Max(5, indicatorY);

            Canvas.SetLeft(_indicator, indicatorX);
            Canvas.SetTop(_indicator, indicatorY);

            // Position bounding box around entity
            if (_boundingBox != null)
            {
                _boundingBox.Width = width + BoxPadding * 2;
                _boundingBox.Height = height + BoxPadding * 2;
                Canvas.SetLeft(_boundingBox, centerX - width / 2 - BoxPadding);
                Canvas.SetTop(_boundingBox, centerY - height / 2 - BoxPadding);
            }

            // Draw target lines from indicator to entity center
            if (_targetLines != null)
            {
                var geometry = new PathGeometry();

                // Line from indicator to entity
                double indicatorCenterX = indicatorX + _indicator.Width / 2;
                double indicatorCenterY = indicatorY + _indicator.Height / 2;

                var figure = new PathFigure { StartPoint = new Point(indicatorCenterX, indicatorCenterY + _indicator.Height / 2) };
                figure.Segments.Add(new LineSegment(new Point(centerX, centerY - height / 2 - BoxPadding), true));
                geometry.Figures.Add(figure);

                _targetLines.Data = geometry;
            }
        }

        /// <summary>
        /// Get the center position and size of an entity
        /// </summary>
        private (double centerX, double centerY, double width, double height) GetEntityBounds(object entity)
        {
            switch (entity)
            {
                case EOTCraneData crane:
                    return GetEOTCraneBounds(crane);

                case JibCraneData jib:
                    return (jib.CenterX, jib.CenterY, jib.Radius * 2, jib.Radius * 2);

                case RunwayData runway:
                    double runwayCenterX = (runway.StartX + runway.EndX) / 2;
                    double runwayCenterY = (runway.StartY + runway.EndY) / 2;
                    double runwayLength = Math.Sqrt(Math.Pow(runway.EndX - runway.StartX, 2) +
                                                    Math.Pow(runway.EndY - runway.StartY, 2));
                    return (runwayCenterX, runwayCenterY, runwayLength, 30);

                case ZoneData zone:
                    return (zone.X + zone.Width / 2, zone.Y + zone.Height / 2, zone.Width, zone.Height);

                case AGVStationData station:
                    return (station.X, station.Y, 40, 40);

                case ConveyorData conveyor:
                    if (conveyor.Path != null && conveyor.Path.Count >= 2)
                    {
                        var first = conveyor.Path[0];
                        var last = conveyor.Path[conveyor.Path.Count - 1];
                        double cx = (first.X + last.X) / 2;
                        double cy = (first.Y + last.Y) / 2;
                        double len = Math.Sqrt(Math.Pow(last.X - first.X, 2) + Math.Pow(last.Y - first.Y, 2));
                        return (cx, cy, len, conveyor.Width * 10);
                    }
                    return (0, 0, 50, 20);

                case OpeningData opening:
                    return (opening.X, opening.Y, opening.ClearWidth, opening.ClearHeight);

                case NodeData node:
                    double nodeX = node.Visual?.X ?? 0;
                    double nodeY = node.Visual?.Y ?? 0;
                    double nodeW = node.Visual?.Width ?? 40;
                    double nodeH = node.Visual?.Height ?? 40;
                    return (nodeX + nodeW / 2, nodeY + nodeH / 2, nodeW, nodeH);

                case PathData path:
                    // Find connected nodes to get path center
                    var fromNode = FindNode(path.From);
                    var toNode = FindNode(path.To);
                    if (fromNode != null && toNode != null)
                    {
                        double px = ((fromNode.Visual?.X ?? 0) + (toNode.Visual?.X ?? 0)) / 2;
                        double py = ((fromNode.Visual?.Y ?? 0) + (toNode.Visual?.Y ?? 0)) / 2;
                        return (px, py, 60, 20);
                    }
                    return (double.NaN, double.NaN, 0, 0);

                case WallData wall:
                    double wallCx = (wall.X1 + wall.X2) / 2;
                    double wallCy = (wall.Y1 + wall.Y2) / 2;
                    double wallLen = Math.Sqrt(Math.Pow(wall.X2 - wall.X1, 2) + Math.Pow(wall.Y2 - wall.Y1, 2));
                    return (wallCx, wallCy, wallLen, wall.Thickness + 10);

                default:
                    return (double.NaN, double.NaN, 0, 0);
            }
        }

        private (double centerX, double centerY, double width, double height) GetEOTCraneBounds(EOTCraneData crane)
        {
            var runway = _layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
            if (runway == null)
            {
                return (double.NaN, double.NaN, 0, 0);
            }

            // Get crane position on runway
            var (craneX, craneY) = runway.GetPositionAt(crane.BridgePosition);
            double bayWidth = crane.BayWidth;
            double reachTotal = crane.ReachLeft + crane.ReachRight;

            return (craneX, craneY, bayWidth, reachTotal);
        }

        private NodeData? FindNode(string id)
        {
            return _layout.Nodes.FirstOrDefault(n => n.Id == id);
        }

        /// <summary>
        /// Check if indicator is currently active
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Get currently indicated entity
        /// </summary>
        public object? CurrentEntity => _selectedEntity;

        public void Dispose()
        {
            HideIndicator();
            _timer.Tick -= OnTick;
        }
    }
}
