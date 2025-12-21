using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Renderers
{
    /// <summary>
    /// Renders Guided Transport layer (AGV waypoints, paths, stations, traffic zones)
    /// Z-order: 400-499 range
    /// </summary>
    public class GuidedTransportRenderer : ILayerRenderer
    {
        private readonly SelectionService _selectionService;

        public LayerType Layer => LayerType.GuidedTransport;
        public int ZOrderBase => 400;

        public GuidedTransportRenderer(SelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: traffic zones (back), paths, waypoints, stations (front)
            RenderTrafficZones(canvas, layout);
            RenderAGVPaths(canvas, layout);
            RenderAGVWaypoints(canvas, layout, registerElement);
            RenderAGVStations(canvas, layout, registerElement);
        }

        #region Traffic Zones

        private void RenderTrafficZones(Canvas canvas, LayoutData layout)
        {
            foreach (var zone in layout.TrafficZones)
            {
                if (zone.Boundary.Count < 3) continue;

                var polygon = new Polygon
                {
                    Fill = new SolidColorBrush(ColorFromHex(zone.Color)) { Opacity = 0.15 },
                    Stroke = new SolidColorBrush(ColorFromHex(zone.Color)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 3 }
                };

                foreach (var point in zone.Boundary)
                {
                    polygon.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polygon, ZOrderBase + 1);
                canvas.Children.Add(polygon);

                // Draw label
                if (zone.Boundary.Any())
                {
                    var centerX = zone.Boundary.Average(p => p.X);
                    var centerY = zone.Boundary.Average(p => p.Y);

                    var label = new TextBlock
                    {
                        Text = $"{zone.Name}\n(Max: {zone.MaxVehicles})",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(ColorFromHex(zone.Color)),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, centerX - 20);
                    Canvas.SetTop(label, centerY - 10);
                    Panel.SetZIndex(label, ZOrderBase + 2);
                    canvas.Children.Add(label);
                }
            }
        }

        #endregion

        #region AGV Paths

        private void RenderAGVPaths(Canvas canvas, LayoutData layout)
        {
            foreach (var path in layout.AGVPaths)
            {
                var fromWaypoint = layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.FromWaypointId);
                var toWaypoint = layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.ToWaypointId);

                if (fromWaypoint == null || toWaypoint == null) continue;

                var fromPoint = new Point(fromWaypoint.X, fromWaypoint.Y);
                var toPoint = new Point(toWaypoint.X, toWaypoint.Y);

                // Render path as double line showing width
                RenderPathWithWidth(canvas, fromPoint, toPoint, path);

                // Add direction arrow for unidirectional paths
                if (path.Direction == PathDirections.Unidirectional)
                {
                    RenderDirectionArrow(canvas, fromPoint, toPoint, path);
                }
            }
        }

        private void RenderPathWithWidth(Canvas canvas, Point from, Point to, AGVPathData path)
        {
            var color = new SolidColorBrush(ColorFromHex(path.Color));

            // Calculate perpendicular offset for double line
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;

            var perpX = -dy / length * (path.Width * 10); // Scale width for visibility
            var perpY = dx / length * (path.Width * 10);

            // Draw two parallel lines
            var line1 = new Line
            {
                X1 = from.X + perpX / 2,
                Y1 = from.Y + perpY / 2,
                X2 = to.X + perpX / 2,
                Y2 = to.Y + perpY / 2,
                Stroke = color,
                StrokeThickness = 2
            };

            var line2 = new Line
            {
                X1 = from.X - perpX / 2,
                Y1 = from.Y - perpY / 2,
                X2 = to.X - perpX / 2,
                Y2 = to.Y - perpY / 2,
                Stroke = color,
                StrokeThickness = 2
            };

            Panel.SetZIndex(line1, ZOrderBase + 10);
            Panel.SetZIndex(line2, ZOrderBase + 10);
            canvas.Children.Add(line1);
            canvas.Children.Add(line2);
        }

        private void RenderDirectionArrow(Canvas canvas, Point from, Point to, AGVPathData path)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;

            // Arrow at midpoint
            var midX = (from.X + to.X) / 2;
            var midY = (from.Y + to.Y) / 2;

            // Arrow size
            var arrowSize = 12;
            var angle = Math.Atan2(dy, dx);

            var arrow = new Polygon
            {
                Fill = new SolidColorBrush(ColorFromHex(path.Color)),
                Points = new PointCollection
                {
                    new Point(midX + arrowSize * Math.Cos(angle), midY + arrowSize * Math.Sin(angle)),
                    new Point(midX + arrowSize * Math.Cos(angle + 2.5), midY + arrowSize * Math.Sin(angle + 2.5)),
                    new Point(midX + arrowSize * Math.Cos(angle - 2.5), midY + arrowSize * Math.Sin(angle - 2.5))
                }
            };

            Panel.SetZIndex(arrow, ZOrderBase + 11);
            canvas.Children.Add(arrow);
        }

        #endregion

        #region AGV Waypoints

        private void RenderAGVWaypoints(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var waypoint in layout.AGVWaypoints)
            {
                var color = GetWaypointColor(waypoint.WaypointType);
                var size = 12;

                // Render as diamond
                var diamond = new Polygon
                {
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Points = new PointCollection
                    {
                        new Point(waypoint.X, waypoint.Y - size),      // Top
                        new Point(waypoint.X + size, waypoint.Y),      // Right
                        new Point(waypoint.X, waypoint.Y + size),      // Bottom
                        new Point(waypoint.X - size, waypoint.Y)       // Left
                    }
                };

                Panel.SetZIndex(diamond, ZOrderBase + 20);
                canvas.Children.Add(diamond);

                // Add label if waypoint has a name
                if (!string.IsNullOrEmpty(waypoint.Name))
                {
                    var label = new TextBlock
                    {
                        Text = waypoint.Name,
                        FontSize = 9,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.7 }
                    };

                    Canvas.SetLeft(label, waypoint.X + size + 2);
                    Canvas.SetTop(label, waypoint.Y - 6);
                    Panel.SetZIndex(label, ZOrderBase + 21);
                    canvas.Children.Add(label);
                }
            }
        }

        private Color GetWaypointColor(string waypointType)
        {
            return waypointType switch
            {
                WaypointTypes.Through => Colors.LightBlue,
                WaypointTypes.Decision => Colors.Yellow,
                WaypointTypes.Stop => Colors.Red,
                WaypointTypes.SpeedChange => Colors.Orange,
                WaypointTypes.ChargingAccess => Colors.Green,
                _ => Colors.Gray
            };
        }

        #endregion

        #region AGV Stations

        private void RenderAGVStations(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var station in layout.AGVStations)
            {
                var width = 40;
                var height = 30;
                var color = new SolidColorBrush(ColorFromHex(station.Color));

                // Draw station rectangle
                var rect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = color,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(rect, station.X - width / 2);
                Canvas.SetTop(rect, station.Y - height / 2);
                Panel.SetZIndex(rect, ZOrderBase + 30);

                // Apply rotation
                var rotateTransform = new RotateTransform(station.Rotation, width / 2, height / 2);
                rect.RenderTransform = rotateTransform;

                canvas.Children.Add(rect);

                // Draw docking direction indicator (arrow)
                var arrowSize = 15;
                var arrow = new Polygon
                {
                    Fill = Brushes.White,
                    Points = new PointCollection
                    {
                        new Point(width / 2, 5),
                        new Point(width / 2 + 5, 15),
                        new Point(width / 2 - 5, 15)
                    }
                };

                var arrowCanvas = new Canvas { Width = width, Height = height };
                arrowCanvas.Children.Add(arrow);
                Canvas.SetLeft(arrowCanvas, station.X - width / 2);
                Canvas.SetTop(arrowCanvas, station.Y - height / 2);
                arrowCanvas.RenderTransform = rotateTransform;
                Panel.SetZIndex(arrowCanvas, ZOrderBase + 31);
                canvas.Children.Add(arrowCanvas);

                // Draw station label
                var label = new TextBlock
                {
                    Text = station.Name,
                    FontSize = 10,
                    Foreground = Brushes.Black,
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(label, station.X + width / 2 + 5);
                Canvas.SetTop(label, station.Y - 7);
                Panel.SetZIndex(label, ZOrderBase + 32);
                canvas.Children.Add(label);

                // Draw link to waypoint if exists
                if (!string.IsNullOrEmpty(station.LinkedWaypointId))
                {
                    var waypoint = layout.AGVWaypoints.FirstOrDefault(w => w.Id == station.LinkedWaypointId);
                    if (waypoint != null)
                    {
                        var linkLine = new Line
                        {
                            X1 = station.X,
                            Y1 = station.Y,
                            X2 = waypoint.X,
                            Y2 = waypoint.Y,
                            Stroke = Brushes.Gray,
                            StrokeThickness = 1,
                            StrokeDashArray = new DoubleCollection { 3, 2 }
                        };
                        Panel.SetZIndex(linkLine, ZOrderBase + 5);
                        canvas.Children.Add(linkLine);
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private Color ColorFromHex(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Colors.Gray;
            }
        }

        #endregion
    }
}
