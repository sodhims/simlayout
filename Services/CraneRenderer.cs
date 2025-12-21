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
    /// Renders runways, EOT cranes, jib cranes, and handoff points
    /// </summary>
    public class CraneRenderer
    {
        private readonly HashSet<string> _selectedRunwayIds = new();
        private readonly HashSet<string> _selectedCraneIds = new();
        private readonly HashSet<string> _selectedJibIds = new();

        #region Selection

        public void SelectRunway(string id)
        {
            _selectedRunwayIds.Clear();
            _selectedRunwayIds.Add(id);
        }

        public void SelectCrane(string id)
        {
            _selectedCraneIds.Clear();
            _selectedCraneIds.Add(id);
        }

        public void SelectJib(string id)
        {
            _selectedJibIds.Clear();
            _selectedJibIds.Add(id);
        }

        public void ClearSelection()
        {
            _selectedRunwayIds.Clear();
            _selectedCraneIds.Clear();
            _selectedJibIds.Clear();
        }

        public bool IsRunwaySelected(string id) => _selectedRunwayIds.Contains(id);
        public bool IsCraneSelected(string id) => _selectedCraneIds.Contains(id);
        public bool IsJibSelected(string id) => _selectedJibIds.Contains(id);

        #endregion

        #region Main Draw

        public void Draw(Canvas canvas, LayoutData layout,
            Action<FrameworkElement, string, string>? registerElement = null)
        {
            // Runways
            if (layout.Runways != null)
            {
                foreach (var runway in layout.Runways)
                    DrawRunway(canvas, runway, registerElement);
            }

            // EOT Cranes
            if (layout.EOTCranes != null && layout.Runways != null)
            {
                foreach (var crane in layout.EOTCranes)
                {
                    var runway = layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
                    if (runway != null)
                        DrawCraneEnvelope(canvas, crane, runway, registerElement);
                }
            }

            // Handoff Points
            if (layout.HandoffPoints != null && layout.Runways != null)
            {
                foreach (var handoff in layout.HandoffPoints)
                {
                    var runway = layout.Runways.FirstOrDefault(r => r.Id == handoff.RunwayId);
                    if (runway != null)
                        DrawHandoffPoint(canvas, handoff, runway, registerElement);
                }
            }

            // Jib Cranes
            if (layout.JibCranes != null)
            {
                foreach (var jib in layout.JibCranes)
                    DrawJibCrane(canvas, jib, registerElement);
            }
        }

        #endregion

        #region Runway

        private void DrawRunway(Canvas canvas, RunwayData runway,
            Action<FrameworkElement, string, string>? registerElement)
        {
            var isSelected = _selectedRunwayIds.Contains(runway.Id);
            var color = ParseColor(runway.Color);
            var width = isSelected ? CraneRenderConstants.RunwaySelectedWidth : CraneRenderConstants.RunwayLineWidth;

            // Main runway line
            var runwayLine = new Line
            {
                X1 = runway.StartX,
                Y1 = runway.StartY,
                X2 = runway.EndX,
                Y2 = runway.EndY,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = width,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = runway.Id,
                Cursor = Cursors.Hand
            };

            if (isSelected)
                runwayLine.StrokeDashArray = new DoubleCollection { 4, 2 };

            canvas.Children.Add(runwayLine);

            // Rails
            var (perpX, perpY) = runway.GetPerpendicular();
            var railOffset = CraneRenderConstants.RunwayRailOffset;

            var rail1 = CreateLine(
                runway.StartX + perpX * railOffset, runway.StartY + perpY * railOffset,
                runway.EndX + perpX * railOffset, runway.EndY + perpY * railOffset,
                Color.FromArgb(180, color.R, color.G, color.B), CraneRenderConstants.RunwayRailWidth);
            rail1.IsHitTestVisible = false;
            canvas.Children.Add(rail1);

            var rail2 = CreateLine(
                runway.StartX - perpX * railOffset, runway.StartY - perpY * railOffset,
                runway.EndX - perpX * railOffset, runway.EndY - perpY * railOffset,
                Color.FromArgb(180, color.R, color.G, color.B), CraneRenderConstants.RunwayRailWidth);
            rail2.IsHitTestVisible = false;
            canvas.Children.Add(rail2);

            // End markers
            DrawEndMarker(canvas, runway.StartX, runway.StartY, color);
            DrawEndMarker(canvas, runway.EndX, runway.EndY, color);

            // Label
            var midX = (runway.StartX + runway.EndX) / 2;
            var midY = (runway.StartY + runway.EndY) / 2;
            var label = CreateLabel(runway.Name, midX - 20, midY - 20, color);
            canvas.Children.Add(label);

            registerElement?.Invoke(runwayLine, runway.Id, "runway");
        }

        private void DrawEndMarker(Canvas canvas, double x, double y, Color color)
        {
            var size = CraneRenderConstants.RunwayEndMarkerSize;
            var marker = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(marker, x - size / 2);
            Canvas.SetTop(marker, y - size / 2);
            canvas.Children.Add(marker);
        }

        #endregion

        #region EOT Crane

        private void DrawCraneEnvelope(Canvas canvas, EOTCraneData crane, RunwayData runway,
            Action<FrameworkElement, string, string>? registerElement)
        {
            var isSelected = _selectedCraneIds.Contains(crane.Id);
            var color = ParseColor(crane.Color);

            var envelope = CraneZoneCalculator.GetEnvelopePolygon(crane, runway);
            if (envelope.Length < 4) return;

            var polygon = new Polygon
            {
                Points = new PointCollection(envelope.Select(p => new Point(p.x, p.y))),
                Fill = new SolidColorBrush(Color.FromArgb(CraneRenderConstants.CraneEnvelopeFillAlpha, color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = isSelected ? CraneRenderConstants.CraneEnvelopeSelectedWidth : CraneRenderConstants.CraneEnvelopeStrokeWidth,
                Tag = crane.Id,
                Cursor = Cursors.Hand
            };

            if (isSelected)
                polygon.StrokeDashArray = new DoubleCollection { 6, 3 };

            canvas.Children.Add(polygon);

            // Bridge at zone center
            var bridgePos = (crane.ZoneMin + crane.ZoneMax) / 2;
            DrawCraneBridge(canvas, crane, runway, bridgePos, color);

            // Label
            var centerX = envelope.Average(p => p.x);
            var centerY = envelope.Average(p => p.y);
            var label = CreateLabel(crane.Name, centerX - 15, centerY - 8, color, false);
            canvas.Children.Add(label);

            registerElement?.Invoke(polygon, crane.Id, "eotCrane");
        }

        private void DrawCraneBridge(Canvas canvas, EOTCraneData crane, RunwayData runway,
            double bridgePosition, Color color)
        {
            var (posX, posY) = runway.GetPositionAt(bridgePosition);
            var (perpX, perpY) = runway.GetPerpendicular();

            // Bridge line
            var bridge = new Line
            {
                X1 = posX + perpX * crane.ReachLeft,
                Y1 = posY + perpY * crane.ReachLeft,
                X2 = posX - perpX * crane.ReachRight,
                Y2 = posY - perpY * crane.ReachRight,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = CraneRenderConstants.CraneBridgeWidth,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };
            canvas.Children.Add(bridge);

            // Trolley
            var trolleyX = posX;
            var trolleyY = posY;
            var trolley = new Rectangle
            {
                Width = CraneRenderConstants.CraneTrolleyWidth,
                Height = CraneRenderConstants.CraneTrolleyHeight,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                RenderTransform = new RotateTransform(runway.Angle,
                    CraneRenderConstants.CraneTrolleyWidth / 2,
                    CraneRenderConstants.CraneTrolleyHeight / 2)
            };
            Canvas.SetLeft(trolley, trolleyX - CraneRenderConstants.CraneTrolleyWidth / 2);
            Canvas.SetTop(trolley, trolleyY - CraneRenderConstants.CraneTrolleyHeight / 2);
            canvas.Children.Add(trolley);
        }

        #endregion

        #region Jib Crane

        private void DrawJibCrane(Canvas canvas, JibCraneData jib,
            Action<FrameworkElement, string, string>? registerElement)
        {
            var isSelected = _selectedJibIds.Contains(jib.Id);
            var color = ParseColor(jib.Color);

            // Coverage arc/circle
            Shape coverage;
            if (jib.IsFullCircle)
            {
                coverage = new Ellipse
                {
                    Width = jib.Radius * 2,
                    Height = jib.Radius * 2,
                    Fill = new SolidColorBrush(Color.FromArgb(CraneRenderConstants.JibCraneFillAlpha, color.R, color.G, color.B)),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = isSelected ? 3 : CraneRenderConstants.JibCraneArcWidth,
                    Tag = jib.Id,
                    Cursor = Cursors.Hand
                };
                Canvas.SetLeft(coverage, jib.CenterX - jib.Radius);
                Canvas.SetTop(coverage, jib.CenterY - jib.Radius);
            }
            else
            {
                var arcGeometry = CreateArcGeometry(jib.CenterX, jib.CenterY, jib.Radius, jib.ArcStart, jib.ArcEnd);
                coverage = new Path
                {
                    Data = arcGeometry,
                    Fill = new SolidColorBrush(Color.FromArgb(CraneRenderConstants.JibCraneFillAlpha, color.R, color.G, color.B)),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = isSelected ? 3 : CraneRenderConstants.JibCraneArcWidth,
                    Tag = jib.Id,
                    Cursor = Cursors.Hand
                };
            }

            if (isSelected && coverage is Shape s)
                s.StrokeDashArray = new DoubleCollection { 6, 3 };

            canvas.Children.Add(coverage);

            // Pivot point
            var pivotSize = CraneRenderConstants.JibCranePivotSize;
            var pivot = new Ellipse
            {
                Width = pivotSize,
                Height = pivotSize,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(pivot, jib.CenterX - pivotSize / 2);
            Canvas.SetTop(pivot, jib.CenterY - pivotSize / 2);
            canvas.Children.Add(pivot);

            // Label
            var label = CreateLabel(jib.Name, jib.CenterX + pivotSize, jib.CenterY - 8, color, false);
            canvas.Children.Add(label);

            registerElement?.Invoke(coverage as FrameworkElement ?? pivot, jib.Id, "jibCrane");
        }

        private Geometry CreateArcGeometry(double cx, double cy, double radius, double startAngle, double endAngle)
        {
            var startRad = startAngle * Math.PI / 180;
            var endRad = endAngle * Math.PI / 180;

            var startX = cx + radius * Math.Cos(startRad);
            var startY = cy + radius * Math.Sin(startRad);
            var endX = cx + radius * Math.Cos(endRad);
            var endY = cy + radius * Math.Sin(endRad);

            var arcAngle = endAngle - startAngle;
            if (arcAngle < 0) arcAngle += 360;
            var isLargeArc = arcAngle > 180;

            var figure = new PathFigure
            {
                StartPoint = new Point(cx, cy),
                IsClosed = true
            };
            figure.Segments.Add(new LineSegment(new Point(startX, startY), true));
            figure.Segments.Add(new ArcSegment(
                new Point(endX, endY),
                new Size(radius, radius),
                0, isLargeArc,
                SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(new Point(cx, cy), true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        #endregion

        #region Handoff Point

        private void DrawHandoffPoint(Canvas canvas, HandoffPointData handoff, RunwayData runway,
            Action<FrameworkElement, string, string>? registerElement)
        {
            var (x, y) = runway.GetPositionAt(handoff.Position);
            var size = CraneRenderConstants.HandoffMarkerSize;

            var diamond = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(x, y - size),
                    new Point(x + size, y),
                    new Point(x, y + size),
                    new Point(x - size, y)
                },
                Fill = new SolidColorBrush(CraneColors.HandoffPoint),
                Stroke = new SolidColorBrush(CraneColors.HandoffStroke),
                StrokeThickness = 2,
                Tag = handoff.Id,
                Cursor = Cursors.Hand
            };
            canvas.Children.Add(diamond);

            var label = new TextBlock
            {
                Text = "H",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(label, x - 4);
            Canvas.SetTop(label, y - 6);
            canvas.Children.Add(label);

            registerElement?.Invoke(diamond, handoff.Id, "handoff");
        }

        #endregion

        #region Preview

        public void DrawRunwayPreview(Canvas canvas, double startX, double startY, double endX, double endY)
        {
            ClearPreview(canvas);

            var line = new Line
            {
                X1 = startX, Y1 = startY, X2 = endX, Y2 = endY,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = CraneRenderConstants.RunwayLineWidth,
                StrokeDashArray = new DoubleCollection { 8, 4 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = "cranePreview",
                IsHitTestVisible = false
            };
            canvas.Children.Add(line);

            var length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
            var midX = (startX + endX) / 2;
            var midY = (startY + endY) / 2;

            var lengthLabel = new TextBlock
            {
                Text = $"{length:F1}",
                FontSize = 10,
                Background = Brushes.White,
                Padding = new Thickness(2),
                Tag = "cranePreview"
            };
            Canvas.SetLeft(lengthLabel, midX + 10);
            Canvas.SetTop(lengthLabel, midY - 10);
            canvas.Children.Add(lengthLabel);
        }

        public void DrawJibPreview(Canvas canvas, double cx, double cy, double radius)
        {
            ClearPreview(canvas);

            var circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 8, 4 },
                Fill = new SolidColorBrush(Color.FromArgb(20, 100, 100, 100)),
                Tag = "cranePreview",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(circle, cx - radius);
            Canvas.SetTop(circle, cy - radius);
            canvas.Children.Add(circle);
        }

        public void ClearPreview(Canvas canvas)
        {
            var previews = canvas.Children.OfType<FrameworkElement>()
                .Where(e => e.Tag as string == "cranePreview")
                .ToList();
            foreach (var p in previews)
                canvas.Children.Remove(p);
        }

        public void DrawZoneOverlap(Canvas canvas, EOTCraneData crane1, EOTCraneData crane2, RunwayData runway)
        {
            var overlap = CraneZoneCalculator.FindOverlap(crane1, crane2);
            if (overlap == null) return;

            var (overlapMin, overlapMax) = overlap.Value;
            var (perpX, perpY) = runway.GetPerpendicular();

            var maxReachLeft = Math.Max(crane1.ReachLeft, crane2.ReachLeft);
            var maxReachRight = Math.Max(crane1.ReachRight, crane2.ReachRight);

            var (startX, startY) = runway.GetPositionAt(overlapMin);
            var (endX, endY) = runway.GetPositionAt(overlapMax);

            var overlapPolygon = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(startX + perpX * maxReachLeft, startY + perpY * maxReachLeft),
                    new Point(endX + perpX * maxReachLeft, endY + perpY * maxReachLeft),
                    new Point(endX - perpX * maxReachRight, endY - perpY * maxReachRight),
                    new Point(startX - perpX * maxReachRight, startY - perpY * maxReachRight)
                },
                Fill = new SolidColorBrush(CraneColors.OverlapZone),
                Stroke = new SolidColorBrush(CraneColors.HandoffStroke),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                IsHitTestVisible = false,
                Tag = "overlapZone"
            };
            canvas.Children.Add(overlapPolygon);
        }

        #endregion

        #region Helpers

        private Line CreateLine(double x1, double y1, double x2, double y2, Color color, double thickness)
        {
            return new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness
            };
        }

        private TextBlock CreateLabel(string text, double x, double y, Color color, bool hasBackground = true)
        {
            var label = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color)
            };
            if (hasBackground)
            {
                label.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
                label.Padding = new Thickness(2);
            }
            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);
            return label;
        }

        private Color ParseColor(string colorStr)
        {
            try { return (Color)ColorConverter.ConvertFromString(colorStr); }
            catch { return Colors.Gray; }
        }

        #endregion
    }
}
