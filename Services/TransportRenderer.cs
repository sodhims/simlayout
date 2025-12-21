using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders transport networks, stations, waypoints, tracks, and transporters
    /// </summary>
    public class TransportRenderer
    {
        #region Selection State

        private readonly HashSet<string> _selectedStationIds = new();
        private readonly HashSet<string> _selectedWaypointIds = new();
        private readonly HashSet<string> _selectedSegmentIds = new();
        private readonly HashSet<string> _selectedNetworkIds = new();
        private string? _hoveredSegmentId = null;

        #endregion

        #region Selection Methods

        public void SelectStation(string id)
        {
            _selectedStationIds.Clear();
            _selectedStationIds.Add(id);
        }

        public void SelectStations(IEnumerable<string> ids)
        {
            _selectedStationIds.Clear();
            foreach (var id in ids) _selectedStationIds.Add(id);
        }

        public void AddToSelection(string id, string elementType)
        {
            switch (elementType)
            {
                case "station":
                    _selectedStationIds.Add(id);
                    break;
                case "waypoint":
                    _selectedWaypointIds.Add(id);
                    break;
                case "segment":
                    _selectedSegmentIds.Add(id);
                    break;
            }
        }

        public void SelectWaypoint(string id)
        {
            _selectedWaypointIds.Clear();
            _selectedWaypointIds.Add(id);
        }

        public void SelectSegment(string id)
        {
            _selectedSegmentIds.Clear();
            _selectedSegmentIds.Add(id);
        }

        public void SelectNetwork(string id)
        {
            _selectedNetworkIds.Clear();
            _selectedNetworkIds.Add(id);
        }

        public void ClearSelection()
        {
            _selectedStationIds.Clear();
            _selectedWaypointIds.Clear();
            _selectedSegmentIds.Clear();
            _selectedNetworkIds.Clear();
        }

        public void SetHoveredSegment(string? id)
        {
            _hoveredSegmentId = id;
        }

        public bool IsStationSelected(string id) => _selectedStationIds.Contains(id);
        public bool IsWaypointSelected(string id) => _selectedWaypointIds.Contains(id);
        public bool IsSegmentSelected(string id) => _selectedSegmentIds.Contains(id);
        public bool IsNetworkSelected(string id) => _selectedNetworkIds.Contains(id);

        public IEnumerable<string> GetSelectedStationIds() => _selectedStationIds;
        public IEnumerable<string> GetSelectedWaypointIds() => _selectedWaypointIds;
        public IEnumerable<string> GetSelectedSegmentIds() => _selectedSegmentIds;

        #endregion

        #region Draw Networks

        /// <summary>
        /// Draw all transport networks
        /// </summary>
        public void DrawNetworks(Canvas canvas, LayoutData layout, Action<FrameworkElement, string, string>? registerElement = null)
        {
            if (layout.TransportNetworks == null) return;

            foreach (var network in layout.TransportNetworks.Where(n => n.IsVisible))
            {
                DrawNetwork(canvas, network, layout, registerElement);
            }
        }

        private void DrawNetwork(Canvas canvas, TransportNetworkData network, LayoutData layout,
            Action<FrameworkElement, string, string>? registerElement)
        {
            var networkColor = ParseColor(network.Visual.Color);

            // Draw segments first (under everything)
            foreach (var segment in network.Segments)
            {
                DrawNetworkSegment(canvas, segment, network, layout, registerElement);
            }

            // Draw waypoints
            foreach (var waypoint in network.Waypoints)
            {
                DrawWaypoint(canvas, waypoint, networkColor, registerElement);
            }

            // Draw stations
            foreach (var station in network.Stations)
            {
                DrawStation(canvas, station, registerElement);
            }

            // Draw transporters
            foreach (var transporter in network.Transporters)
            {
                DrawTransporterAtHome(canvas, transporter, network);
            }
        }

        private void DrawNetworkSegment(Canvas canvas, TrackSegmentData segment, TransportNetworkData network,
            LayoutData layout, Action<FrameworkElement, string, string>? registerElement)
        {
            var fromPos = GetPointPositionInNetwork(segment.From, network);
            var toPos = GetPointPositionInNetwork(segment.To, network);

            if (fromPos == null || toPos == null) return;

            // Use segment color if specified, otherwise network color
            var colorStr = !string.IsNullOrEmpty(segment.Color) ? segment.Color : network.Visual.Color;
            var color = ParseColor(colorStr);
            var isSelected = _selectedSegmentIds.Contains(segment.Id);
            var isHovered = _hoveredSegmentId == segment.Id;

            // Draw the double-line track with chevrons
            DrawDoubleLineTrack(canvas, fromPos.Value, toPos.Value, color, segment.Bidirectional, 
                segment.IsBlocked, isSelected, isHovered, segment.Id);

            // Draw lane markers
            if (segment.LaneCount > 1)
            {
                DrawLaneMarkers(canvas, fromPos.Value, toPos.Value, segment.LaneCount, 
                    TransportRenderConstants.DefaultTrackWidth, color);
            }

            // Draw speed limit indicator for non-default speeds
            if (segment.SpeedLimit != 2.0)
            {
                DrawSpeedIndicator(canvas, fromPos.Value, toPos.Value, segment.SpeedLimit);
            }

            // Get the clickable element for registration (we'll use a transparent hit-test area)
            var hitArea = new Line
            {
                X1 = fromPos.Value.X,
                Y1 = fromPos.Value.Y,
                X2 = toPos.Value.X,
                Y2 = toPos.Value.Y,
                Stroke = Brushes.Transparent,
                StrokeThickness = TransportRenderConstants.DefaultTrackWidth,
                Cursor = Cursors.Hand,
                Tag = segment.Id
            };
            Panel.SetZIndex(hitArea, TransportRenderConstants.ZIndexTrackCenterLine + 5);
            canvas.Children.Add(hitArea);

            registerElement?.Invoke(hitArea, segment.Id, "trackSegment");
        }

        /// <summary>
        /// Draw double-line track with direction chevrons
        /// Style: ══►══►══ (unidirectional) or ══►══◄══►══◄══ (bidirectional)
        /// </summary>
        private void DrawDoubleLineTrack(Canvas canvas, Point from, Point to, Color color,
            bool bidirectional, bool isBlocked, bool isSelected, bool isHovered, string segmentId)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1) return;

            // Unit vectors
            var ux = dx / length;  // Along track
            var uy = dy / length;
            var px = -uy;          // Perpendicular
            var py = ux;

            // Track parameters
            var lineSpacing = 4.0;  // Distance between the two parallel lines
            var lineThickness = isSelected ? 3.0 : (isHovered ? 2.5 : 2.0);
            var chevronSpacing = 40.0;  // Distance between chevrons
            var chevronSize = 6.0;

            // Adjust color for blocked state
            var drawColor = isBlocked ? TransportRenderConstants.BlockedColor : color;
            var brush = new SolidColorBrush(drawColor);

            // Selection highlight (draw first, underneath)
            if (isSelected)
            {
                var selectLine1 = new Line
                {
                    X1 = from.X + px * (lineSpacing + 3),
                    Y1 = from.Y + py * (lineSpacing + 3),
                    X2 = to.X + px * (lineSpacing + 3),
                    Y2 = to.Y + py * (lineSpacing + 3),
                    Stroke = new SolidColorBrush(TransportRenderConstants.SelectionColor),
                    StrokeThickness = lineThickness + 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    IsHitTestVisible = false,
                    Opacity = 0.6
                };
                var selectLine2 = new Line
                {
                    X1 = from.X - px * (lineSpacing + 3),
                    Y1 = from.Y - py * (lineSpacing + 3),
                    X2 = to.X - px * (lineSpacing + 3),
                    Y2 = to.Y - py * (lineSpacing + 3),
                    Stroke = new SolidColorBrush(TransportRenderConstants.SelectionColor),
                    StrokeThickness = lineThickness + 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    IsHitTestVisible = false,
                    Opacity = 0.6
                };
                Panel.SetZIndex(selectLine1, TransportRenderConstants.ZIndexSelection);
                Panel.SetZIndex(selectLine2, TransportRenderConstants.ZIndexSelection);
                canvas.Children.Add(selectLine1);
                canvas.Children.Add(selectLine2);
            }

            // Draw the two parallel lines
            var line1 = new Line
            {
                X1 = from.X + px * lineSpacing,
                Y1 = from.Y + py * lineSpacing,
                X2 = to.X + px * lineSpacing,
                Y2 = to.Y + py * lineSpacing,
                Stroke = brush,
                StrokeThickness = lineThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            var line2 = new Line
            {
                X1 = from.X - px * lineSpacing,
                Y1 = from.Y - py * lineSpacing,
                X2 = to.X - px * lineSpacing,
                Y2 = to.Y - py * lineSpacing,
                Stroke = brush,
                StrokeThickness = lineThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(line1, TransportRenderConstants.ZIndexTrackCenterLine);
            Panel.SetZIndex(line2, TransportRenderConstants.ZIndexTrackCenterLine);
            canvas.Children.Add(line1);
            canvas.Children.Add(line2);

            // Draw chevrons along the track
            var chevronCount = Math.Max(1, (int)(length / chevronSpacing));
            var actualSpacing = length / (chevronCount + 1);

            for (int i = 1; i <= chevronCount; i++)
            {
                var t = i * actualSpacing;
                var cx = from.X + ux * t;
                var cy = from.Y + uy * t;

                if (bidirectional)
                {
                    // Alternate direction: ►◄►◄
                    bool pointForward = (i % 2 == 1);
                    DrawChevron(canvas, cx, cy, ux, uy, chevronSize, drawColor, pointForward);
                }
                else
                {
                    // All point in From→To direction: ►►►
                    DrawChevron(canvas, cx, cy, ux, uy, chevronSize, drawColor, true);
                }
            }
        }

        /// <summary>
        /// Draw a single chevron (► or ◄)
        /// </summary>
        private void DrawChevron(Canvas canvas, double cx, double cy, double ux, double uy, 
            double size, Color color, bool pointForward)
        {
            // Perpendicular vector
            var px = -uy;
            var py = ux;

            // Direction multiplier
            var dir = pointForward ? 1.0 : -1.0;

            // Chevron points: < or > shape
            var points = new PointCollection
            {
                new Point(cx - ux * size * 0.5 * dir + px * size * 0.5, cy - uy * size * 0.5 * dir + py * size * 0.5),
                new Point(cx + ux * size * 0.5 * dir, cy + uy * size * 0.5 * dir),
                new Point(cx - ux * size * 0.5 * dir - px * size * 0.5, cy - uy * size * 0.5 * dir - py * size * 0.5)
            };

            var chevron = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(chevron, TransportRenderConstants.ZIndexArrows);
            canvas.Children.Add(chevron);
        }

        #endregion

        #region Draw Legacy Tracks (Backward Compatibility)

        public void DrawTracks(Canvas canvas, LayoutData layout, Action<FrameworkElement, string, string>? registerElement = null)
        {
            if (layout.TransporterTracks == null) return;

            foreach (var track in layout.TransporterTracks)
            {
                foreach (var segment in track.Segments)
                {
                    DrawTrackSegment(canvas, track, segment, layout, registerElement);
                }
            }
        }

        private void DrawTrackSegment(Canvas canvas, TransporterTrackData track, TrackSegmentData segment,
            LayoutData layout, Action<FrameworkElement, string, string>? registerElement)
        {
            var fromPos = GetPointPosition(segment.From, layout);
            var toPos = GetPointPosition(segment.To, layout);

            if (fromPos == null || toPos == null) return;

            var color = ParseColor(track.Visual.Color);
            var isSelected = _selectedSegmentIds.Contains(segment.Id);
            var isHovered = _hoveredSegmentId == segment.Id;

            // Use the same double-line track style
            DrawDoubleLineTrack(canvas, fromPos.Value, toPos.Value, color, segment.Bidirectional,
                segment.IsBlocked, isSelected, isHovered, segment.Id);

            // Draw lane markers
            if (segment.LaneCount > 1)
            {
                DrawLaneMarkers(canvas, fromPos.Value, toPos.Value, segment.LaneCount, track.Visual.Width, color);
            }

            // Hit area for click detection
            var hitArea = new Line
            {
                X1 = fromPos.Value.X,
                Y1 = fromPos.Value.Y,
                X2 = toPos.Value.X,
                Y2 = toPos.Value.Y,
                Stroke = Brushes.Transparent,
                StrokeThickness = TransportRenderConstants.DefaultTrackWidth,
                Cursor = Cursors.Hand,
                Tag = segment.Id
            };
            Panel.SetZIndex(hitArea, TransportRenderConstants.ZIndexTrackCenterLine + 5);
            canvas.Children.Add(hitArea);

            registerElement?.Invoke(hitArea, segment.Id, "trackSegment");
        }

        #endregion

        #region Direction Arrows

        private void DrawDirectionArrows(Canvas canvas, Point from, Point to, Color color)
        {
            var dist = Distance(from, to);
            var arrowCount = Math.Max(1, (int)(dist / TransportRenderConstants.ArrowSpacing));

            for (int i = 0; i < arrowCount; i++)
            {
                var t = (i + 1.0) / (arrowCount + 1.0);
                var pos = new Point(
                    from.X + (to.X - from.X) * t,
                    from.Y + (to.Y - from.Y) * t
                );
                DrawArrow(canvas, pos, from, to, color);
            }
        }

        private void DrawArrow(Canvas canvas, Point position, Point from, Point to, Color color)
        {
            var angle = Math.Atan2(to.Y - from.Y, to.X - from.X) * 180 / Math.PI;
            var size = TransportRenderConstants.ArrowWidth;

            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(-size/2, -size/2),
                    new Point(size/2, 0),
                    new Point(-size/2, size/2)
                },
                Fill = new SolidColorBrush(color),
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new RotateTransform(angle),
                        new TranslateTransform(position.X, position.Y)
                    }
                },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(arrow, TransportRenderConstants.ZIndexArrows);
            canvas.Children.Add(arrow);
        }

        private void DrawBidirectionalIndicator(Canvas canvas, Point from, Point to, Color color)
        {
            var mid = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
            var angle = Math.Atan2(to.Y - from.Y, to.X - from.X) * 180 / Math.PI;
            var size = TransportRenderConstants.ArrowWidth * 0.7;

            // Draw << >> style indicator
            var indicator = new Path
            {
                Data = Geometry.Parse($"M -{size},-{size/2} L 0,0 L -{size},{size/2} M {size},-{size/2} L 0,0 L {size},{size/2}"),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new RotateTransform(angle),
                        new TranslateTransform(mid.X, mid.Y)
                    }
                },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(indicator, TransportRenderConstants.ZIndexArrows);
            canvas.Children.Add(indicator);
        }

        #endregion

        #region Lane Markers

        private void DrawLaneMarkers(Canvas canvas, Point from, Point to, int laneCount, double width, Color color)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;

            // Perpendicular unit vector
            var px = -dy / len;
            var py = dx / len;

            var laneSpacing = width / laneCount;

            for (int i = 1; i < laneCount; i++)
            {
                var offset = (i - laneCount / 2.0) * laneSpacing;
                var line = new Line
                {
                    X1 = from.X + px * offset,
                    Y1 = from.Y + py * offset,
                    X2 = to.X + px * offset,
                    Y2 = to.Y + py * offset,
                    Stroke = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = TransportRenderConstants.LaneMarkerDashArray,
                    IsHitTestVisible = false
                };
                canvas.Children.Add(line);
            }
        }

        #endregion

        #region Speed Indicator

        private void DrawSpeedIndicator(Canvas canvas, Point from, Point to, double speedLimit)
        {
            var mid = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);

            // Small speed label
            var speedLabel = new TextBlock
            {
                Text = $"{speedLimit:F1}",
                FontSize = 8,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Padding = new Thickness(2, 1, 2, 1),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(speedLabel, mid.X - 12);
            Canvas.SetTop(speedLabel, mid.Y - 6);
            Panel.SetZIndex(speedLabel, TransportRenderConstants.ZIndexArrows);
            canvas.Children.Add(speedLabel);
        }

        #endregion

        #region Draw Stations

        public void DrawStations(Canvas canvas, LayoutData layout, Action<FrameworkElement, string, string>? registerElement = null)
        {
            if (layout.TransportStations == null) return;

            foreach (var station in layout.TransportStations)
            {
                DrawStation(canvas, station, registerElement);
            }
        }

        private void DrawStation(Canvas canvas, TransportStationData station, Action<FrameworkElement, string, string>? registerElement)
        {
            var x = station.Visual.X;
            var y = station.Visual.Y;
            var w = station.Visual.Width;
            var h = station.Visual.Height;
            var color = ParseColor(station.Visual.Color);
            var isSelected = _selectedStationIds.Contains(station.Id);

            // Container canvas
            var container = new Canvas { Width = w, Height = h };
            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);
            Panel.SetZIndex(container, TransportRenderConstants.ZIndexStations);

            // Background rectangle
            var bg = new Rectangle
            {
                Width = w,
                Height = h,
                RadiusX = TransportRenderConstants.StationCornerRadius,
                RadiusY = TransportRenderConstants.StationCornerRadius,
                Fill = new SolidColorBrush(Color.FromArgb(
                    (byte)(TransportRenderConstants.StationBackgroundOpacity * 255),
                    color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(isSelected ? TransportRenderConstants.SelectionColor : color),
                StrokeThickness = isSelected ? 
                    TransportRenderConstants.StationSelectedBorderWidth : 
                    TransportRenderConstants.StationBorderWidth
            };
            if (isSelected)
            {
                bg.StrokeDashArray = new DoubleCollection { 4, 2 };
            }
            container.Children.Add(bg);

            // Draw terminals if enabled
            if (station.Visual.ShowTerminals)
            {
                DrawStationTerminals(container, station.Visual.TerminalLayout, w, h, color);
            }

            // Draw station icon
            DrawStationIcon(container, station.Simulation.StationType, w, h, color);

            // Station name label
            if (station.Visual.ShowLabel)
            {
                var label = new TextBlock
                {
                    Text = station.Name,
                    FontSize = TransportRenderConstants.StationLabelFontSize,
                    Foreground = Brushes.Black,
                    TextAlignment = TextAlignment.Center,
                    Width = w + 20,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(label, -10);
                Canvas.SetTop(label, h + 2);
                container.Children.Add(label);
            }

            // Station type indicator
            var typeLabel = new TextBlock
            {
                Text = GetStationTypeAbbrev(station.Simulation.StationType),
                FontSize = TransportRenderConstants.StationTypeIndicatorFontSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color)
            };
            Canvas.SetLeft(typeLabel, 3);
            Canvas.SetTop(typeLabel, 3);
            container.Children.Add(typeLabel);

            // Queue capacity indicator
            if (station.Simulation.QueueCapacity > 1)
            {
                var qLabel = new TextBlock
                {
                    Text = $"Q:{station.Simulation.QueueCapacity}",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0))
                };
                Canvas.SetLeft(qLabel, w - 20);
                Canvas.SetTop(qLabel, 3);
                container.Children.Add(qLabel);
            }

            container.Cursor = Cursors.Hand;
            container.Tag = station.Id;
            canvas.Children.Add(container);

            registerElement?.Invoke(container, station.Id, "transportStation");
        }

        /// <summary>
        /// Draw terminal indicators on a station based on layout
        /// </summary>
        private void DrawStationTerminals(Canvas container, string layout, double w, double h, Color stationColor)
        {
            var terminalSize = 8.0;
            var inputColor = Color.FromRgb(46, 204, 113);   // Green
            var outputColor = Color.FromRgb(231, 76, 60);   // Red
            var singleColor = stationColor;                  // Use station color for single terminal

            // Get terminal positions based on layout
            var (inputPos, outputPos) = GetTerminalPositions(layout, w, h, terminalSize);

            if (inputPos.HasValue && outputPos.HasValue)
            {
                // Two terminals (input/output)
                DrawTerminalDot(container, inputPos.Value.X, inputPos.Value.Y, terminalSize, inputColor);
                DrawTerminalDot(container, outputPos.Value.X, outputPos.Value.Y, terminalSize, outputColor);
            }
            else if (inputPos.HasValue)
            {
                // Single terminal
                DrawTerminalDot(container, inputPos.Value.X, inputPos.Value.Y, terminalSize, singleColor);
            }
        }

        private (Point? input, Point? output) GetTerminalPositions(string layout, double w, double h, double terminalSize)
        {
            var offset = terminalSize / 2 + 2;  // Slight offset from edge
            var centerX = w / 2;
            var centerY = h / 2;

            return layout switch
            {
                "left-right" => (new Point(-offset, centerY), new Point(w + offset, centerY)),
                "right-left" => (new Point(w + offset, centerY), new Point(-offset, centerY)),
                "top-bottom" => (new Point(centerX, -offset), new Point(centerX, h + offset)),
                "bottom-top" => (new Point(centerX, h + offset), new Point(centerX, -offset)),
                "top" => (new Point(centerX, -offset), null),
                "bottom" => (new Point(centerX, h + offset), null),
                "left" => (new Point(-offset, centerY), null),
                "right" => (new Point(w + offset, centerY), null),
                "center" => (new Point(centerX, centerY), null),
                _ => (new Point(-offset, centerY), new Point(w + offset, centerY))  // Default left-right
            };
        }

        private void DrawTerminalDot(Canvas container, double x, double y, double size, Color color)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(dot, x - size / 2);
            Canvas.SetTop(dot, y - size / 2);
            container.Children.Add(dot);
        }

        private void DrawStationIcon(Canvas container, string stationType, double w, double h, Color color)
        {
            var centerX = w / 2;
            var centerY = h / 2;
            var iconSize = Math.Min(w, h) * TransportRenderConstants.StationIconRatio;

            Path? iconPath = null;

            switch (stationType)
            {
                case StationTypes.Pickup:
                    // Arrow pointing up (loading)
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX},{centerY - iconSize/2} L {centerX + iconSize/2},{centerY + iconSize/4} L {centerX + iconSize/4},{centerY + iconSize/4} L {centerX + iconSize/4},{centerY + iconSize/2} L {centerX - iconSize/4},{centerY + iconSize/2} L {centerX - iconSize/4},{centerY + iconSize/4} L {centerX - iconSize/2},{centerY + iconSize/4} Z"),
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    break;

                case StationTypes.Dropoff:
                    // Arrow pointing down (unloading)
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX},{centerY + iconSize/2} L {centerX + iconSize/2},{centerY - iconSize/4} L {centerX + iconSize/4},{centerY - iconSize/4} L {centerX + iconSize/4},{centerY - iconSize/2} L {centerX - iconSize/4},{centerY - iconSize/2} L {centerX - iconSize/4},{centerY - iconSize/4} L {centerX - iconSize/2},{centerY - iconSize/4} Z"),
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    break;

                case StationTypes.Home:
                    // House icon
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX},{centerY - iconSize/2} L {centerX + iconSize/2},{centerY} L {centerX + iconSize/2},{centerY + iconSize/2} L {centerX - iconSize/2},{centerY + iconSize/2} L {centerX - iconSize/2},{centerY} Z"),
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    // Add door
                    var door = new Rectangle
                    {
                        Width = iconSize / 3,
                        Height = iconSize / 2.5,
                        Fill = new SolidColorBrush(Colors.White)
                    };
                    Canvas.SetLeft(door, centerX - iconSize / 6);
                    Canvas.SetTop(door, centerY + iconSize / 6);
                    container.Children.Add(door);
                    break;

                case StationTypes.Buffer:
                    // Stack/queue icon
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX - iconSize/2},{centerY - iconSize/3} L {centerX + iconSize/2},{centerY - iconSize/3} M {centerX - iconSize/2},{centerY} L {centerX + iconSize/2},{centerY} M {centerX - iconSize/2},{centerY + iconSize/3} L {centerX + iconSize/2},{centerY + iconSize/3}"),
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 3,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    break;

                case StationTypes.Crossing:
                    // X intersection
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX - iconSize/2},{centerY - iconSize/2} L {centerX + iconSize/2},{centerY + iconSize/2} M {centerX + iconSize/2},{centerY - iconSize/2} L {centerX - iconSize/2},{centerY + iconSize/2}"),
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 3,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    break;

                case StationTypes.Charging:
                    // Lightning bolt
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX + iconSize/4},{centerY - iconSize/2} L {centerX - iconSize/4},{centerY} L {centerX + iconSize/6},{centerY} L {centerX - iconSize/4},{centerY + iconSize/2} L {centerX + iconSize/4},{centerY} L {centerX - iconSize/6},{centerY} Z"),
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    break;

                case StationTypes.Maintenance:
                    // Wrench icon (simplified)
                    iconPath = new Path
                    {
                        Data = Geometry.Parse($"M {centerX - iconSize/2},{centerY - iconSize/2} L {centerX},{centerY} L {centerX + iconSize/2},{centerY + iconSize/2}"),
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 4,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    break;

                default:
                    // Default circle
                    var circle = new Ellipse
                    {
                        Width = iconSize,
                        Height = iconSize,
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(circle, centerX - iconSize / 2);
                    Canvas.SetTop(circle, centerY - iconSize / 2);
                    container.Children.Add(circle);
                    return;
            }

            if (iconPath != null)
            {
                container.Children.Add(iconPath);
            }
        }

        private string GetStationTypeAbbrev(string stationType)
        {
            return stationType switch
            {
                StationTypes.Pickup => "P",
                StationTypes.Dropoff => "D",
                StationTypes.Home => "H",
                StationTypes.Buffer => "B",
                StationTypes.Crossing => "X",
                StationTypes.Charging => "⚡",
                StationTypes.Maintenance => "M",
                _ => "?"
            };
        }

        #endregion

        #region Draw Waypoints

        public void DrawWaypoints(Canvas canvas, LayoutData layout, Action<FrameworkElement, string, string>? registerElement = null)
        {
            if (layout.Waypoints == null) return;

            foreach (var wp in layout.Waypoints)
            {
                var color = ParseColor(wp.Color);
                DrawWaypoint(canvas, wp, color, registerElement);
            }
        }

        private void DrawWaypoint(Canvas canvas, WaypointData wp, Color color, Action<FrameworkElement, string, string>? registerElement)
        {
            var isSelected = _selectedWaypointIds.Contains(wp.Id);
            var size = wp.IsJunction ? 
                TransportRenderConstants.JunctionWaypointSize : 
                TransportRenderConstants.WaypointOuterSize;

            // Outer circle
            var outer = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Color.FromArgb(
                    (byte)(TransportRenderConstants.WaypointBackgroundOpacity * 255),
                    color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(isSelected ? TransportRenderConstants.SelectionColor : color),
                StrokeThickness = isSelected ? 
                    TransportRenderConstants.WaypointSelectedBorderWidth : 
                    TransportRenderConstants.WaypointBorderWidth,
                Cursor = Cursors.Hand,
                Tag = wp.Id
            };

            if (isSelected)
            {
                outer.StrokeDashArray = new DoubleCollection { 3, 1 };
            }

            Canvas.SetLeft(outer, wp.X - size / 2);
            Canvas.SetTop(outer, wp.Y - size / 2);
            Panel.SetZIndex(outer, TransportRenderConstants.ZIndexWaypoints);
            canvas.Children.Add(outer);

            // Inner dot (or junction symbol)
            if (wp.IsJunction)
            {
                // Draw junction cross
                var cross = new Path
                {
                    Data = Geometry.Parse($"M {wp.X - size/4},{wp.Y} L {wp.X + size/4},{wp.Y} M {wp.X},{wp.Y - size/4} L {wp.X},{wp.Y + size/4}"),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    IsHitTestVisible = false
                };
                Panel.SetZIndex(cross, TransportRenderConstants.ZIndexWaypoints + 1);
                canvas.Children.Add(cross);
            }
            else
            {
                var inner = new Ellipse
                {
                    Width = TransportRenderConstants.WaypointInnerSize,
                    Height = TransportRenderConstants.WaypointInnerSize,
                    Fill = new SolidColorBrush(color),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(inner, wp.X - TransportRenderConstants.WaypointInnerSize / 2);
                Canvas.SetTop(inner, wp.Y - TransportRenderConstants.WaypointInnerSize / 2);
                Panel.SetZIndex(inner, TransportRenderConstants.ZIndexWaypoints + 1);
                canvas.Children.Add(inner);
            }

            registerElement?.Invoke(outer, wp.Id, "waypoint");
        }

        #endregion

        #region Draw Transporters

        public void DrawTransporters(Canvas canvas, LayoutData layout)
        {
            if (layout.Transporters == null) return;

            foreach (var t in layout.Transporters)
            {
                var homeStation = layout.TransportStations?.FirstOrDefault(s => s.Id == t.HomeStationId);
                if (homeStation != null)
                {
                    var (cx, cy) = homeStation.GetCenter();
                    DrawTransporter(canvas, t, cx, cy);
                }
            }
        }

        private void DrawTransporterAtHome(Canvas canvas, TransporterData t, TransportNetworkData network)
        {
            var homeStation = network.Stations.FirstOrDefault(s => s.Id == t.HomeStationId);
            if (homeStation != null)
            {
                var (cx, cy) = homeStation.GetCenter();
                DrawTransporter(canvas, t, cx, cy);
            }
        }

        private void DrawTransporter(Canvas canvas, TransporterData t, double x, double y)
        {
            var color = ParseColor(t.Color);
            var size = TransportRenderConstants.TransporterSize;

            // AGV body
            var body = new Rectangle
            {
                Width = size,
                Height = size * TransportRenderConstants.TransporterAspectRatio,
                RadiusX = TransportRenderConstants.TransporterCornerRadius,
                RadiusY = TransportRenderConstants.TransporterCornerRadius,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1
            };
            Canvas.SetLeft(body, x - size / 2);
            Canvas.SetTop(body, y - size * TransportRenderConstants.TransporterAspectRatio / 2);
            Panel.SetZIndex(body, TransportRenderConstants.ZIndexTransporters);
            canvas.Children.Add(body);

            // Name label
            var label = new TextBlock
            {
                Text = t.Name,
                FontSize = TransportRenderConstants.TransporterLabelFontSize,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(label, x - size / 2 + 2);
            Canvas.SetTop(label, y - 5);
            Panel.SetZIndex(label, TransportRenderConstants.ZIndexTransporters + 1);
            canvas.Children.Add(label);

            // Transporter type indicator
            var typeIcon = GetTransporterTypeIcon(t.TransporterType);
            if (!string.IsNullOrEmpty(typeIcon))
            {
                var typeLabel = new TextBlock
                {
                    Text = typeIcon,
                    FontSize = 8,
                    Foreground = Brushes.White,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(typeLabel, x + size / 2 - 10);
                Canvas.SetTop(typeLabel, y - size * TransportRenderConstants.TransporterAspectRatio / 2 + 1);
                Panel.SetZIndex(typeLabel, TransportRenderConstants.ZIndexTransporters + 1);
                canvas.Children.Add(typeLabel);
            }
        }

        private string GetTransporterTypeIcon(string transporterType)
        {
            return transporterType switch
            {
                TransporterTypes.AGV => "A",
                TransporterTypes.AMR => "R",
                TransporterTypes.Forklift => "F",
                TransporterTypes.Tugger => "T",
                TransporterTypes.Conveyor => "C",
                _ => ""
            };
        }

        #endregion

        #region Preview Drawing

        /// <summary>
        /// Draw track preview while user is drawing
        /// </summary>
        public void DrawTrackPreview(Canvas canvas, Point from, Point to, bool isValid = true)
        {
            // Remove old preview
            RemovePreviewElements(canvas);

            var color = isValid ? TransportRenderConstants.PreviewColor : TransportRenderConstants.ErrorColor;
            var length = Distance(from, to);
            if (length < 1) return;

            // Unit vectors
            var ux = (to.X - from.X) / length;
            var uy = (to.Y - from.Y) / length;
            var px = -uy;
            var py = ux;

            var lineSpacing = 4.0;
            var brush = new SolidColorBrush(color);

            // Draw two parallel dashed lines
            var line1 = new Line
            {
                X1 = from.X + px * lineSpacing,
                Y1 = from.Y + py * lineSpacing,
                X2 = to.X + px * lineSpacing,
                Y2 = to.Y + py * lineSpacing,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeDashArray = TransportRenderConstants.PreviewDashArray,
                Tag = "trackPreview",
                IsHitTestVisible = false
            };

            var line2 = new Line
            {
                X1 = from.X - px * lineSpacing,
                Y1 = from.Y - py * lineSpacing,
                X2 = to.X - px * lineSpacing,
                Y2 = to.Y - py * lineSpacing,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeDashArray = TransportRenderConstants.PreviewDashArray,
                Tag = "trackPreview",
                IsHitTestVisible = false
            };

            Panel.SetZIndex(line1, TransportRenderConstants.ZIndexPreview);
            Panel.SetZIndex(line2, TransportRenderConstants.ZIndexPreview);
            canvas.Children.Add(line1);
            canvas.Children.Add(line2);

            // Draw preview chevrons (showing bidirectional by default)
            var chevronSpacing = 40.0;
            var chevronSize = 6.0;
            var chevronCount = Math.Max(1, (int)(length / chevronSpacing));
            var actualSpacing = length / (chevronCount + 1);

            for (int i = 1; i <= chevronCount; i++)
            {
                var t = i * actualSpacing;
                var cx = from.X + ux * t;
                var cy = from.Y + uy * t;
                
                // Alternate direction for preview (bidirectional default)
                bool pointForward = (i % 2 == 1);
                DrawPreviewChevron(canvas, cx, cy, ux, uy, chevronSize, color, pointForward);
            }

            // Distance label
            var mid = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
            var distLabel = new TextBlock
            {
                Text = $"{length:F0}px",
                FontSize = 10,
                Foreground = new SolidColorBrush(color),
                Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Padding = new Thickness(3, 1, 3, 1),
                Tag = "trackPreview",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(distLabel, mid.X - 15);
            Canvas.SetTop(distLabel, mid.Y - 20);
            Panel.SetZIndex(distLabel, TransportRenderConstants.ZIndexPreview);
            canvas.Children.Add(distLabel);
        }

        private void DrawPreviewChevron(Canvas canvas, double cx, double cy, double ux, double uy,
            double size, Color color, bool pointForward)
        {
            var px = -uy;
            var py = ux;
            var dir = pointForward ? 1.0 : -1.0;

            var points = new PointCollection
            {
                new Point(cx - ux * size * 0.5 * dir + px * size * 0.5, cy - uy * size * 0.5 * dir + py * size * 0.5),
                new Point(cx + ux * size * 0.5 * dir, cy + uy * size * 0.5 * dir),
                new Point(cx - ux * size * 0.5 * dir - px * size * 0.5, cy - uy * size * 0.5 * dir - py * size * 0.5)
            };

            var chevron = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                StrokeLineJoin = PenLineJoin.Round,
                Tag = "trackPreview",
                IsHitTestVisible = false
            };

            Panel.SetZIndex(chevron, TransportRenderConstants.ZIndexPreview);
            canvas.Children.Add(chevron);
        }

        /// <summary>
        /// Draw snap indicator when near a valid connection point
        /// </summary>
        public void DrawSnapIndicator(Canvas canvas, Point position, double radius = 25)
        {
            var indicator = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = new SolidColorBrush(TransportRenderConstants.SnapIndicatorColor),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 46, 204, 113)),
                Tag = "snapIndicator",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(indicator, position.X - radius);
            Canvas.SetTop(indicator, position.Y - radius);
            Panel.SetZIndex(indicator, TransportRenderConstants.ZIndexPreview);
            canvas.Children.Add(indicator);
        }

        /// <summary>
        /// Draw waypoint insertion preview
        /// </summary>
        public void DrawWaypointInsertPreview(Canvas canvas, Point position, Point segmentFrom, Point segmentTo)
        {
            RemovePreviewElements(canvas);

            // Draw the two new segments that would be created
            var previewColor = TransportRenderConstants.SnapIndicatorColor;

            var line1 = new Line
            {
                X1 = segmentFrom.X,
                Y1 = segmentFrom.Y,
                X2 = position.X,
                Y2 = position.Y,
                Stroke = new SolidColorBrush(previewColor),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Tag = "trackPreview",
                IsHitTestVisible = false
            };
            canvas.Children.Add(line1);

            var line2 = new Line
            {
                X1 = position.X,
                Y1 = position.Y,
                X2 = segmentTo.X,
                Y2 = segmentTo.Y,
                Stroke = new SolidColorBrush(previewColor),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Tag = "trackPreview",
                IsHitTestVisible = false
            };
            canvas.Children.Add(line2);

            // Preview waypoint
            var wpPreview = new Ellipse
            {
                Width = TransportRenderConstants.WaypointOuterSize,
                Height = TransportRenderConstants.WaypointOuterSize,
                Stroke = new SolidColorBrush(previewColor),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(100, 46, 204, 113)),
                Tag = "trackPreview",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(wpPreview, position.X - TransportRenderConstants.WaypointOuterSize / 2);
            Canvas.SetTop(wpPreview, position.Y - TransportRenderConstants.WaypointOuterSize / 2);
            canvas.Children.Add(wpPreview);
        }

        public void RemovePreviewElements(Canvas canvas)
        {
            var toRemove = canvas.Children.OfType<FrameworkElement>()
                .Where(e => e.Tag as string == "trackPreview" || e.Tag as string == "snapIndicator")
                .ToList();
            foreach (var elem in toRemove)
            {
                canvas.Children.Remove(elem);
            }
        }

        #endregion

        #region Hit Testing

        /// <summary>
        /// Find the nearest segment to a point (for waypoint insertion)
        /// </summary>
        public (TrackSegmentData? segment, Point nearestPoint, double distance) FindNearestSegment(
            Point pos, LayoutData layout, double maxDistance = TransportRenderConstants.SegmentHitDistance)
        {
            TrackSegmentData? nearest = null;
            Point nearestPoint = pos;
            double minDist = maxDistance;

            // Check network segments
            if (layout.TransportNetworks != null)
            {
                foreach (var network in layout.TransportNetworks)
                {
                    foreach (var segment in network.Segments)
                    {
                        var fromPos = GetPointPositionInNetwork(segment.From, network);
                        var toPos = GetPointPositionInNetwork(segment.To, network);
                        if (fromPos == null || toPos == null) continue;

                        var (projPoint, dist) = PointToSegmentDistance(pos, fromPos.Value, toPos.Value);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearest = segment;
                            nearestPoint = projPoint;
                        }
                    }
                }
            }

            // Check legacy tracks
            if (layout.TransporterTracks != null)
            {
                foreach (var track in layout.TransporterTracks)
                {
                    foreach (var segment in track.Segments)
                    {
                        var fromPos = GetPointPosition(segment.From, layout);
                        var toPos = GetPointPosition(segment.To, layout);
                        if (fromPos == null || toPos == null) continue;

                        var (projPoint, dist) = PointToSegmentDistance(pos, fromPos.Value, toPos.Value);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearest = segment;
                            nearestPoint = projPoint;
                        }
                    }
                }
            }

            return (nearest, nearestPoint, minDist);
        }

        private (Point projection, double distance) PointToSegmentDistance(Point p, Point a, Point b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lenSq = dx * dx + dy * dy;

            if (lenSq < 0.0001)
            {
                return (a, Distance(p, a));
            }

            var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq));
            var proj = new Point(a.X + t * dx, a.Y + t * dy);
            return (proj, Distance(p, proj));
        }

        #endregion

        #region Helpers

        private Point? GetPointPositionInNetwork(string pointId, TransportNetworkData network)
        {
            var station = network.Stations.FirstOrDefault(s => s.Id == pointId);
            if (station != null)
            {
                return new Point(
                    station.Visual.X + station.Visual.Width / 2,
                    station.Visual.Y + station.Visual.Height / 2
                );
            }

            var waypoint = network.Waypoints.FirstOrDefault(w => w.Id == pointId);
            if (waypoint != null)
            {
                return new Point(waypoint.X, waypoint.Y);
            }

            return null;
        }

        private Point? GetPointPosition(string pointId, LayoutData layout)
        {
            // Check stations
            var station = layout.TransportStations?.FirstOrDefault(s => s.Id == pointId);
            if (station != null)
            {
                return new Point(
                    station.Visual.X + station.Visual.Width / 2,
                    station.Visual.Y + station.Visual.Height / 2
                );
            }

            // Check waypoints
            var waypoint = layout.Waypoints?.FirstOrDefault(w => w.Id == pointId);
            if (waypoint != null)
            {
                return new Point(waypoint.X, waypoint.Y);
            }

            // Check regular nodes
            var node = layout.Nodes?.FirstOrDefault(n => n.Id == pointId);
            if (node != null)
            {
                return new Point(
                    node.Visual.X + node.Visual.Width / 2,
                    node.Visual.Y + node.Visual.Height / 2
                );
            }

            // Check network stations and waypoints
            if (layout.TransportNetworks != null)
            {
                foreach (var network in layout.TransportNetworks)
                {
                    var pos = GetPointPositionInNetwork(pointId, network);
                    if (pos.HasValue) return pos;
                }
            }

            return null;
        }

        private double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        private Color ParseColor(string colorStr)
        {
            try
            {
                if (string.IsNullOrEmpty(colorStr))
                    return TransportRenderConstants.DefaultNetworkColor;

                if (colorStr.StartsWith("#"))
                {
                    return (Color)ColorConverter.ConvertFromString(colorStr);
                }
                return TransportRenderConstants.DefaultNetworkColor;
            }
            catch
            {
                return TransportRenderConstants.DefaultNetworkColor;
            }
        }

        #endregion
    }
}
