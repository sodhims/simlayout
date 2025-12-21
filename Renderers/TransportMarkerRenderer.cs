using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Renderers
{
    /// <summary>
    /// Renders transport markers (orange diamonds) and legend circles
    /// </summary>
    public class TransportMarkerRenderer
    {
        private readonly Canvas _canvas;
        private readonly Dictionary<string, List<UIElement>> _markerElements = new();
        private readonly Dictionary<string, List<UIElement>> _pathElements = new();
        private readonly Dictionary<string, List<UIElement>> _linkElements = new();
        
        public bool ShowLegend { get; set; } = true;
        public double MarkerSize { get; set; } = 24;
        public double LegendCircleSize { get; set; } = 10;
        public double PathThickness { get; set; } = 3;
        public double LinkThickness { get; set; } = 1.5;

        public TransportMarkerRenderer(Canvas canvas)
        {
            _canvas = canvas;
        }

        #region Marker Rendering

        /// <summary>
        /// Render a single marker (orange diamond)
        /// </summary>
        public void RenderMarker(TransportMarker marker, IEnumerable<TransportGroup>? groups = null)
        {
            ClearMarker(marker.Id);
            var elements = new List<UIElement>();

            var color = (Color)ColorConverter.ConvertFromString(marker.Color);
            if (marker.IsSelected)
                color = Colors.DodgerBlue;
            else if (marker.IsHighlighted)
                color = Colors.LimeGreen;

            // Diamond shape (rotated square)
            var diamond = new Rectangle
            {
                Width = marker.Size,
                Height = marker.Size,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                RenderTransform = new RotateTransform(45, marker.Size / 2, marker.Size / 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = $"marker:{marker.Id}"
            };

            Canvas.SetLeft(diamond, marker.X - marker.Size / 2);
            Canvas.SetTop(diamond, marker.Y - marker.Size / 2);
            Canvas.SetZIndex(diamond, 100);
            _canvas.Children.Add(diamond);
            elements.Add(diamond);

            // Marker name label
            var label = new TextBlock
            {
                Text = marker.Name,
                FontSize = 9,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(color) { Opacity = 0.8 },
                Padding = new Thickness(3, 1, 3, 1),
                Tag = $"marker:{marker.Id}"
            };
            Canvas.SetLeft(label, marker.X - 20);
            Canvas.SetTop(label, marker.Y + marker.Size / 2 + 4);
            Canvas.SetZIndex(label, 101);
            _canvas.Children.Add(label);
            elements.Add(label);

            // Legend circles (groups passing through this marker)
            if (ShowLegend && groups != null)
            {
                var passingGroups = groups
                    .Where(g => g.PathMarkerIds.Contains(marker.Id))
                    .ToList();

                if (passingGroups.Count > 0)
                {
                    RenderLegendCircles(marker, passingGroups, elements);
                }
            }

            _markerElements[marker.Id] = elements;
        }

        /// <summary>
        /// Render legend circles showing which groups pass through marker
        /// </summary>
        private void RenderLegendCircles(TransportMarker marker, List<TransportGroup> groups, List<UIElement> elements)
        {
            var startX = marker.X - ((groups.Count - 1) * (LegendCircleSize + 2)) / 2;
            var y = marker.Y - marker.Size / 2 - LegendCircleSize - 4;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var x = startX + i * (LegendCircleSize + 2);

                var circle = new Ellipse
                {
                    Width = LegendCircleSize,
                    Height = LegendCircleSize,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(group.Color)),
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    ToolTip = $"{group.Name}: {group.DisplayName}",
                    Tag = $"legend:{marker.Id}:{group.Id}"
                };

                Canvas.SetLeft(circle, x - LegendCircleSize / 2);
                Canvas.SetTop(circle, y);
                Canvas.SetZIndex(circle, 102);
                _canvas.Children.Add(circle);
                elements.Add(circle);
            }
        }

        /// <summary>
        /// Clear a specific marker
        /// </summary>
        public void ClearMarker(string markerId)
        {
            if (_markerElements.TryGetValue(markerId, out var elements))
            {
                foreach (var element in elements)
                    _canvas.Children.Remove(element);
                _markerElements.Remove(markerId);
            }
        }

        /// <summary>
        /// Clear all markers
        /// </summary>
        public void ClearAllMarkers()
        {
            foreach (var elements in _markerElements.Values)
                foreach (var element in elements)
                    _canvas.Children.Remove(element);
            _markerElements.Clear();
        }

        #endregion

        #region Path Rendering

        /// <summary>
        /// Render path segment between two markers
        /// </summary>
        public void RenderPathSegment(TrackSegmentData segment, 
            TransportMarker fromMarker, TransportMarker toMarker,
            string? color = null)
        {
            ClearPathSegment(segment.Id);
            var elements = new List<UIElement>();

            var pathColor = string.IsNullOrEmpty(color) 
                ? Colors.Gray 
                : (Color)ColorConverter.ConvertFromString(color);

            // Main path line
            var line = new Line
            {
                X1 = fromMarker.X,
                Y1 = fromMarker.Y,
                X2 = toMarker.X,
                Y2 = toMarker.Y,
                Stroke = new SolidColorBrush(pathColor),
                StrokeThickness = PathThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = $"path:{segment.Id}"
            };
            Canvas.SetZIndex(line, 50);
            _canvas.Children.Add(line);
            elements.Add(line);

            // Direction arrow (if unidirectional)
            if (!segment.Bidirectional)
            {
                var arrow = CreateArrow(fromMarker.X, fromMarker.Y, toMarker.X, toMarker.Y, pathColor);
                Canvas.SetZIndex(arrow, 51);
                _canvas.Children.Add(arrow);
                elements.Add(arrow);
            }

            // Blocked indicator
            if (segment.IsBlocked)
            {
                var blocked = CreateBlockedIndicator(
                    (fromMarker.X + toMarker.X) / 2,
                    (fromMarker.Y + toMarker.Y) / 2);
                foreach (var el in blocked)
                {
                    Canvas.SetZIndex(el, 52);
                    _canvas.Children.Add(el);
                    elements.Add(el);
                }
            }

            _pathElements[segment.Id] = elements;
        }

        /// <summary>
        /// Render path with waypoints
        /// </summary>
        public void RenderPathWithWaypoints(TrackSegmentData segment,
            Point start, Point end, IEnumerable<WaypointData> waypoints,
            string? color = null)
        {
            ClearPathSegment(segment.Id);
            var elements = new List<UIElement>();

            var pathColor = string.IsNullOrEmpty(color)
                ? Colors.Gray
                : (Color)ColorConverter.ConvertFromString(color);

            var points = new List<Point> { start };
            points.AddRange(waypoints.Select(w => new Point(w.X, w.Y)));
            points.Add(end);

            // Draw polyline
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(pathColor),
                StrokeThickness = PathThickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = $"path:{segment.Id}"
            };
            foreach (var pt in points)
                polyline.Points.Add(pt);

            Canvas.SetZIndex(polyline, 50);
            _canvas.Children.Add(polyline);
            elements.Add(polyline);

            // Draw waypoint dots
            foreach (var wp in waypoints)
            {
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(pathColor),
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = $"waypoint:{wp.Id}"
                };
                Canvas.SetLeft(dot, wp.X - 4);
                Canvas.SetTop(dot, wp.Y - 4);
                Canvas.SetZIndex(dot, 53);
                _canvas.Children.Add(dot);
                elements.Add(dot);
            }

            _pathElements[segment.Id] = elements;
        }

        private Polygon CreateArrow(double x1, double y1, double x2, double y2, Color color)
        {
            var midX = (x1 + x2) / 2;
            var midY = (y1 + y2) / 2;
            var angle = Math.Atan2(y2 - y1, x2 - x1);
            var size = 8;

            var arrow = new Polygon
            {
                Fill = new SolidColorBrush(color),
                Points = new PointCollection
                {
                    new Point(0, -size / 2),
                    new Point(size, 0),
                    new Point(0, size / 2)
                },
                RenderTransform = new TransformGroup
                {
                    Children =
                    {
                        new RotateTransform(angle * 180 / Math.PI),
                        new TranslateTransform(midX, midY)
                    }
                }
            };
            return arrow;
        }

        private List<UIElement> CreateBlockedIndicator(double x, double y)
        {
            var elements = new List<UIElement>();

            var circle = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.Red,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(circle, x - 8);
            Canvas.SetTop(circle, y - 8);
            elements.Add(circle);

            var xMark = new TextBlock
            {
                Text = "✕",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            Canvas.SetLeft(xMark, x - 5);
            Canvas.SetTop(xMark, y - 8);
            elements.Add(xMark);

            return elements;
        }

        public void ClearPathSegment(string segmentId)
        {
            if (_pathElements.TryGetValue(segmentId, out var elements))
            {
                foreach (var element in elements)
                    _canvas.Children.Remove(element);
                _pathElements.Remove(segmentId);
            }
        }

        public void ClearAllPaths()
        {
            foreach (var elements in _pathElements.Values)
                foreach (var element in elements)
                    _canvas.Children.Remove(element);
            _pathElements.Clear();
        }

        #endregion

        #region Link Rendering

        /// <summary>
        /// Render link from node terminal to transport path (thin dashed line)
        /// </summary>
        public void RenderLink(TransportLink link, Point fromPoint, Point toPoint)
        {
            ClearLink(link.Id);
            var elements = new List<UIElement>();

            var linkColor = string.IsNullOrEmpty(link.Color)
                ? Colors.Gray
                : (Color)ColorConverter.ConvertFromString(link.Color);

            // Dashed line
            var line = new Line
            {
                X1 = fromPoint.X,
                Y1 = fromPoint.Y,
                X2 = toPoint.X,
                Y2 = toPoint.Y,
                Stroke = new SolidColorBrush(linkColor),
                StrokeThickness = LinkThickness,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Tag = $"link:{link.Id}"
            };
            Canvas.SetZIndex(line, 40);
            _canvas.Children.Add(line);
            elements.Add(line);

            // Connection dot at transport end
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(linkColor),
                Tag = $"link:{link.Id}"
            };
            Canvas.SetLeft(dot, toPoint.X - 3);
            Canvas.SetTop(dot, toPoint.Y - 3);
            Canvas.SetZIndex(dot, 41);
            _canvas.Children.Add(dot);
            elements.Add(dot);

            // Link type indicator
            var icon = link.LinkType switch
            {
                LinkTypes.Pickup => "↑",
                LinkTypes.Dropoff => "↓",
                LinkTypes.Both => "↕",
                _ => ""
            };

            if (!string.IsNullOrEmpty(icon))
            {
                var midX = (fromPoint.X + toPoint.X) / 2;
                var midY = (fromPoint.Y + toPoint.Y) / 2;
                var indicator = new TextBlock
                {
                    Text = icon,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(linkColor),
                    Background = Brushes.White,
                    Padding = new Thickness(1),
                    Tag = $"link:{link.Id}"
                };
                Canvas.SetLeft(indicator, midX - 6);
                Canvas.SetTop(indicator, midY - 8);
                Canvas.SetZIndex(indicator, 42);
                _canvas.Children.Add(indicator);
                elements.Add(indicator);
            }

            _linkElements[link.Id] = elements;
        }

        public void ClearLink(string linkId)
        {
            if (_linkElements.TryGetValue(linkId, out var elements))
            {
                foreach (var element in elements)
                    _canvas.Children.Remove(element);
                _linkElements.Remove(linkId);
            }
        }

        public void ClearAllLinks()
        {
            foreach (var elements in _linkElements.Values)
                foreach (var element in elements)
                    _canvas.Children.Remove(element);
            _linkElements.Clear();
        }

        #endregion

        #region Full Render

        /// <summary>
        /// Render complete transport network
        /// </summary>
        public void RenderAll(
            IEnumerable<TransportMarker> markers,
            IEnumerable<TrackSegmentData> segments,
            IEnumerable<TransportLink> links,
            IEnumerable<TransportGroup> groups,
            Func<string, Point?> getPointPosition)
        {
            ClearAllMarkers();
            ClearAllPaths();
            ClearAllLinks();

            var markerDict = markers.ToDictionary(m => m.Id);

            // Render paths first (below markers)
            foreach (var segment in segments)
            {
                if (markerDict.TryGetValue(segment.From, out var fromMarker) &&
                    markerDict.TryGetValue(segment.To, out var toMarker))
                {
                    RenderPathSegment(segment, fromMarker, toMarker);
                }
            }

            // Render markers
            foreach (var marker in markers)
            {
                RenderMarker(marker, groups);
            }

            // Render links
            foreach (var link in links)
            {
                var fromPoint = getPointPosition(link.FromNodeId);
                Point? toPoint = null;

                if (!string.IsNullOrEmpty(link.ToMarkerId) && markerDict.TryGetValue(link.ToMarkerId, out var marker))
                {
                    toPoint = new Point(marker.X, marker.Y);
                }
                else if (link.ConnectionPointX != 0 || link.ConnectionPointY != 0)
                {
                    toPoint = new Point(link.ConnectionPointX, link.ConnectionPointY);
                }

                if (fromPoint.HasValue && toPoint.HasValue)
                {
                    RenderLink(link, fromPoint.Value, toPoint.Value);
                }
            }
        }

        #endregion

        #region Hit Testing

        /// <summary>
        /// Hit test for markers
        /// </summary>
        public TransportMarker? HitTestMarker(Point position, IEnumerable<TransportMarker> markers)
        {
            foreach (var marker in markers)
            {
                var dx = position.X - marker.X;
                var dy = position.Y - marker.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist <= marker.Size / 2 + 4)
                    return marker;
            }
            return null;
        }

        /// <summary>
        /// Hit test for path segments
        /// </summary>
        public (TrackSegmentData? segment, Point nearestPoint) HitTestPath(
            Point position, 
            IEnumerable<TrackSegmentData> segments,
            Func<string, Point?> getPointPosition,
            double threshold = 10)
        {
            foreach (var segment in segments)
            {
                var fromPoint = getPointPosition(segment.From);
                var toPoint = getPointPosition(segment.To);
                if (!fromPoint.HasValue || !toPoint.HasValue) continue;

                var nearest = GetNearestPointOnLine(position, fromPoint.Value, toPoint.Value);
                var dist = Math.Sqrt(Math.Pow(position.X - nearest.X, 2) + Math.Pow(position.Y - nearest.Y, 2));

                if (dist <= threshold)
                    return (segment, nearest);
            }
            return (null, default);
        }

        private Point GetNearestPointOnLine(Point point, Point lineStart, Point lineEnd)
        {
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return lineStart;

            var t = Math.Max(0, Math.Min(1,
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSq));

            return new Point(
                lineStart.X + t * dx,
                lineStart.Y + t * dy);
        }

        #endregion
    }
}
