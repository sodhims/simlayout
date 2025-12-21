using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Transport.AGV;
using LayoutEditor.Transport;

namespace LayoutEditor.Transport.AGV
{
    /// <summary>
    /// Renderer for AGV networks with double-line chevron styling
    /// </summary>
    public class AgvRenderer : ITransportRenderer
    {
        private readonly AgvNetwork _network;
        private Canvas? _canvas;
        
        public TransportVisualSettings Settings { get; set; } = new()
        {
            TrackColor = Color.FromRgb(230, 126, 34),    // Orange
            StationColor = Color.FromRgb(155, 89, 182),  // Purple
            WaypointColor = Color.FromRgb(230, 126, 34), // Orange
            TrackWidth = 24,
            ChevronSpacing = 20,
            ChevronSize = 8
        };

        public AgvRenderer(AgvNetwork network)
        {
            _network = network;
        }

        #region ITransportRenderer

        public void Render(Canvas canvas)
        {
            _canvas = canvas;
            
            // Render in order: tracks (bottom), waypoints, stations (top)
            foreach (var track in _network.Tracks)
                RenderTrack(track);

            foreach (var waypoint in _network.Waypoints)
                RenderWaypoint(waypoint);

            foreach (var station in _network.Stations)
                RenderStation(station);

            foreach (var vehicle in _network.Vehicles)
                RenderVehicle(vehicle);
        }

        public void Clear(Canvas canvas)
        {
            // Remove elements tagged with this network ID
            var toRemove = new System.Collections.Generic.List<UIElement>();
            foreach (UIElement element in canvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == _network.Id)
                    toRemove.Add(element);
            }
            foreach (var element in toRemove)
                canvas.Children.Remove(element);
        }

        public object? HitTest(Point position)
        {
            // Check stations first (top layer)
            foreach (var station in _network.Stations)
            {
                if (position.X >= station.X && position.X <= station.X + station.Width &&
                    position.Y >= station.Y && position.Y <= station.Y + station.Height)
                    return station;
            }

            // Check waypoints
            foreach (var waypoint in _network.Waypoints)
            {
                var dist = Math.Sqrt(Math.Pow(position.X - waypoint.X, 2) + Math.Pow(position.Y - waypoint.Y, 2));
                if (dist <= Settings.WaypointRadius * 2)
                    return waypoint;
            }

            // Check tracks
            foreach (var track in _network.Tracks)
            {
                if (IsPointNearTrack(position, track, Settings.TrackWidth / 2))
                    return track;
            }

            return null;
        }

        public void Highlight(string elementId, bool highlight)
        {
            // Implementation would find and update visual elements
        }

        public void Select(string elementId, bool select)
        {
            // Implementation would find and update visual elements
        }

        #endregion

        #region Track Rendering

        private void RenderTrack(AgvTrack track)
        {
            if (_canvas == null) return;

            var fromPos = GetPointPosition(track.From);
            var toPos = GetPointPosition(track.To);
            if (fromPos == null || toPos == null) return;

            var from = fromPos.Value;
            var to = toPos.Value;
            
            var color = string.IsNullOrEmpty(track.Color)
                ? Settings.TrackColor
                : (Color)ColorConverter.ConvertFromString(track.Color);

            // Track bed (wide background)
            var trackBed = new Line
            {
                X1 = from.X, Y1 = from.Y,
                X2 = to.X, Y2 = to.Y,
                Stroke = new SolidColorBrush(color) { Opacity = Settings.TrackBedOpacity },
                StrokeThickness = Settings.TrackWidth,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = _network.Id
            };
            _canvas.Children.Add(trackBed);

            // Double rail lines
            DrawRails(from, to, color);

            // Direction chevrons
            DrawChevrons(from, to, track.Bidirectional, color);

            // Blocked indicator
            if (track.IsBlocked)
                DrawBlockedIndicator(from, to);
        }

        private void DrawRails(Point from, Point to, Color color)
        {
            if (_canvas == null) return;

            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1) return;

            var offset = Settings.RailSpacing;
            var perpX = -dy / length * offset;
            var perpY = dx / length * offset;

            var rail1 = new Line
            {
                X1 = from.X + perpX, Y1 = from.Y + perpY,
                X2 = to.X + perpX, Y2 = to.Y + perpY,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = Settings.RailWidth,
                Tag = _network.Id
            };

            var rail2 = new Line
            {
                X1 = from.X - perpX, Y1 = from.Y - perpY,
                X2 = to.X - perpX, Y2 = to.Y - perpY,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = Settings.RailWidth,
                Tag = _network.Id
            };

            _canvas.Children.Add(rail1);
            _canvas.Children.Add(rail2);
        }

        private void DrawChevrons(Point from, Point to, bool bidirectional, Color color)
        {
            if (_canvas == null) return;

            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < Settings.ChevronSpacing * 2) return;

            var dirX = dx / length;
            var dirY = dy / length;
            var perpX = -dirY;
            var perpY = dirX;

            var chevronBrush = new SolidColorBrush(Color.FromRgb(
                (byte)(color.R * 0.8), (byte)(color.G * 0.8), (byte)(color.B * 0.8)));

            var numChevrons = (int)(length / Settings.ChevronSpacing);

            for (int i = 1; i < numChevrons; i++)
            {
                var t = i / (double)numChevrons;
                var cx = from.X + dx * t;
                var cy = from.Y + dy * t;

                // Forward chevron
                var chevron = new Polyline
                {
                    Stroke = chevronBrush,
                    StrokeThickness = 2,
                    StrokeLineJoin = PenLineJoin.Round,
                    Tag = _network.Id
                };

                var size = Settings.ChevronSize;
                chevron.Points.Add(new Point(cx - dirX * size + perpX * size, cy - dirY * size + perpY * size));
                chevron.Points.Add(new Point(cx, cy));
                chevron.Points.Add(new Point(cx - dirX * size - perpX * size, cy - dirY * size - perpY * size));

                _canvas.Children.Add(chevron);

                // Reverse chevron for bidirectional
                if (bidirectional && i % 2 == 0)
                {
                    var revChevron = new Polyline
                    {
                        Stroke = chevronBrush,
                        StrokeThickness = 2,
                        StrokeLineJoin = PenLineJoin.Round,
                        Opacity = 0.5,
                        Tag = _network.Id
                    };

                    revChevron.Points.Add(new Point(cx + dirX * size + perpX * size * 0.6, cy + dirY * size + perpY * size * 0.6));
                    revChevron.Points.Add(new Point(cx, cy));
                    revChevron.Points.Add(new Point(cx + dirX * size - perpX * size * 0.6, cy + dirY * size - perpY * size * 0.6));

                    _canvas.Children.Add(revChevron);
                }
            }
        }

        private void DrawBlockedIndicator(Point from, Point to)
        {
            if (_canvas == null) return;

            var midX = (from.X + to.X) / 2;
            var midY = (from.Y + to.Y) / 2;

            var blocked = new Ellipse
            {
                Width = 20, Height = 20,
                Fill = new SolidColorBrush(Settings.BlockedColor),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Tag = _network.Id
            };

            Canvas.SetLeft(blocked, midX - 10);
            Canvas.SetTop(blocked, midY - 10);
            _canvas.Children.Add(blocked);

            var x = new TextBlock
            {
                Text = "✕",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Tag = _network.Id
            };
            Canvas.SetLeft(x, midX - 5);
            Canvas.SetTop(x, midY - 10);
            _canvas.Children.Add(x);
        }

        #endregion

        #region Station Rendering

        private void RenderStation(AgvStation station)
        {
            if (_canvas == null) return;

            var color = string.IsNullOrEmpty(station.Color)
                ? Settings.StationColor
                : (Color)ColorConverter.ConvertFromString(station.Color);

            if (station.IsSelected)
                color = Settings.SelectedColor;
            else if (station.IsHighlighted)
                color = Settings.HighlightColor;

            // Station rectangle
            var rect = new Rectangle
            {
                Width = station.Width,
                Height = station.Height,
                Fill = new SolidColorBrush(color) { Opacity = Settings.StationFillOpacity },
                Stroke = new SolidColorBrush(color),
                StrokeThickness = Settings.StationBorderWidth,
                RadiusX = Settings.StationCornerRadius,
                RadiusY = Settings.StationCornerRadius,
                Tag = _network.Id
            };

            Canvas.SetLeft(rect, station.X);
            Canvas.SetTop(rect, station.Y);
            _canvas.Children.Add(rect);

            // Station type icon
            var icon = GetStationIcon(station.StationType);
            if (!string.IsNullOrEmpty(icon))
            {
                var iconText = new TextBlock
                {
                    Text = icon,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(color),
                    Tag = _network.Id
                };
                Canvas.SetLeft(iconText, station.X + station.Width / 2 - 8);
                Canvas.SetTop(iconText, station.Y + station.Height / 2 - 10);
                _canvas.Children.Add(iconText);
            }

            // Name label
            var label = new TextBlock
            {
                Text = station.Name,
                FontSize = 10,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(color) { Opacity = 0.8 },
                Padding = new Thickness(3, 1, 3, 1),
                Tag = _network.Id
            };
            Canvas.SetLeft(label, station.X);
            Canvas.SetTop(label, station.Y - 16);
            _canvas.Children.Add(label);

            // Group indicator
            if (!string.IsNullOrEmpty(station.GroupName))
            {
                var groupLabel = new TextBlock
                {
                    Text = $"[{station.GroupName}]",
                    FontSize = 8,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Tag = _network.Id
                };
                Canvas.SetLeft(groupLabel, station.X);
                Canvas.SetTop(groupLabel, station.Y + station.Height + 2);
                _canvas.Children.Add(groupLabel);
            }

            // Charging indicator
            if (station.IsCharging)
            {
                var chargeIcon = new TextBlock
                {
                    Text = "⚡",
                    FontSize = 12,
                    Foreground = Brushes.Yellow,
                    Tag = _network.Id
                };
                Canvas.SetLeft(chargeIcon, station.X + station.Width - 14);
                Canvas.SetTop(chargeIcon, station.Y + 2);
                _canvas.Children.Add(chargeIcon);
            }
        }

        private string GetStationIcon(StationType type)
        {
            return type switch
            {
                StationType.Pickup => "↑",
                StationType.Dropoff => "↓",
                StationType.PickupDropoff => "↕",
                StationType.Charging => "⚡",
                StationType.Parking => "P",
                StationType.Maintenance => "🔧",
                _ => ""
            };
        }

        #endregion

        #region Waypoint Rendering

        private void RenderWaypoint(AgvWaypoint waypoint)
        {
            if (_canvas == null) return;

            var color = string.IsNullOrEmpty(waypoint.Color)
                ? Settings.WaypointColor
                : (Color)ColorConverter.ConvertFromString(waypoint.Color);

            if (waypoint.IsSelected)
                color = Settings.SelectedColor;

            var radius = Settings.WaypointRadius;
            if (waypoint.IsJunction)
                radius *= 1.5;

            var circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Tag = _network.Id
            };

            Canvas.SetLeft(circle, waypoint.X - radius);
            Canvas.SetTop(circle, waypoint.Y - radius);
            _canvas.Children.Add(circle);

            // Junction indicator
            if (waypoint.IsJunction)
            {
                var junction = new Ellipse
                {
                    Width = radius,
                    Height = radius,
                    Fill = Brushes.White,
                    Tag = _network.Id
                };
                Canvas.SetLeft(junction, waypoint.X - radius / 2);
                Canvas.SetTop(junction, waypoint.Y - radius / 2);
                _canvas.Children.Add(junction);
            }
        }

        #endregion

        #region Vehicle Rendering

        private void RenderVehicle(AgvVehicle vehicle)
        {
            if (_canvas == null) return;

            var color = string.IsNullOrEmpty(vehicle.Color)
                ? Colors.Red
                : (Color)ColorConverter.ConvertFromString(vehicle.Color);

            // Vehicle body
            var body = new Rectangle
            {
                Width = vehicle.Length * 20, // Scale to pixels
                Height = vehicle.Width * 20,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                RadiusX = 3,
                RadiusY = 3,
                Tag = _network.Id
            };

            Canvas.SetLeft(body, vehicle.CurrentX - body.Width / 2);
            Canvas.SetTop(body, vehicle.CurrentY - body.Height / 2);
            _canvas.Children.Add(body);

            // Battery indicator
            var batteryWidth = body.Width - 4;
            var batteryHeight = 4;
            var batteryLevel = vehicle.BatteryLevel / 100.0;

            var batteryBg = new Rectangle
            {
                Width = batteryWidth,
                Height = batteryHeight,
                Fill = Brushes.DarkGray,
                Tag = _network.Id
            };
            Canvas.SetLeft(batteryBg, vehicle.CurrentX - batteryWidth / 2);
            Canvas.SetTop(batteryBg, vehicle.CurrentY + body.Height / 2 + 2);
            _canvas.Children.Add(batteryBg);

            var batteryFill = new Rectangle
            {
                Width = batteryWidth * batteryLevel,
                Height = batteryHeight,
                Fill = vehicle.NeedsCharging ? Brushes.Red : Brushes.LimeGreen,
                Tag = _network.Id
            };
            Canvas.SetLeft(batteryFill, vehicle.CurrentX - batteryWidth / 2);
            Canvas.SetTop(batteryFill, vehicle.CurrentY + body.Height / 2 + 2);
            _canvas.Children.Add(batteryFill);
        }

        #endregion

        #region Helpers

        private Point? GetPointPosition(string pointId)
        {
            foreach (var station in _network.Stations)
                if (station.Id == pointId)
                    return new Point(station.X + station.Width / 2, station.Y + station.Height / 2);

            foreach (var waypoint in _network.Waypoints)
                if (waypoint.Id == pointId)
                    return new Point(waypoint.X, waypoint.Y);

            return null;
        }

        private bool IsPointNearTrack(Point point, AgvTrack track, double threshold)
        {
            var from = GetPointPosition(track.From);
            var to = GetPointPosition(track.To);
            if (from == null || to == null) return false;

            return DistanceToLine(point, from.Value, to.Value) <= threshold;
        }

        private double DistanceToLine(Point point, Point lineStart, Point lineEnd)
        {
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

            var t = Math.Max(0, Math.Min(1, 
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSq));

            var projX = lineStart.X + t * dx;
            var projY = lineStart.Y + t * dy;

            return Math.Sqrt(Math.Pow(point.X - projX, 2) + Math.Pow(point.Y - projY, 2));
        }

        #endregion
    }
}
