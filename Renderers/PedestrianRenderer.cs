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
    /// Renders Pedestrian layer (walkways, crossings, safety zones)
    /// Z-order: 700-799 range
    /// </summary>
    public class PedestrianRenderer : ILayerRenderer
    {
        private readonly SelectionService _selectionService;

        public LayerType Layer => LayerType.Pedestrian;
        public int ZOrderBase => 700;

        public PedestrianRenderer(SelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: safety zones (back), walkways, crossings (front)
            RenderSafetyZones(canvas, layout);
            RenderWalkways(canvas, layout);
            RenderCrossings(canvas, layout);
        }

        #region Walkways

        private void RenderWalkways(Canvas canvas, LayoutData layout)
        {
            foreach (var walkway in layout.Walkways)
            {
                if (walkway.Centerline.Count < 2) continue;

                var color = GetWalkwayColor(walkway.WalkwayType);
                var brush = new SolidColorBrush(ColorFromHex(walkway.Color ?? color)) { Opacity = 0.3 };
                var edgeBrush = new SolidColorBrush(ColorFromHex(walkway.Color ?? color));

                // Render filled corridor along centerline
                for (int i = 0; i < walkway.Centerline.Count - 1; i++)
                {
                    var p1 = walkway.Centerline[i];
                    var p2 = walkway.Centerline[i + 1];

                    // Calculate perpendicular offset for width
                    var dx = p2.X - p1.X;
                    var dy = p2.Y - p1.Y;
                    var length = Math.Sqrt(dx * dx + dy * dy);
                    if (length == 0) continue;

                    var perpX = -dy / length * walkway.Width / 2;
                    var perpY = dx / length * walkway.Width / 2;

                    // Create corridor segment as polygon
                    var polygon = new Polygon
                    {
                        Fill = brush,
                        Stroke = edgeBrush,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Points = new PointCollection
                        {
                            new Point(p1.X + perpX, p1.Y + perpY),
                            new Point(p2.X + perpX, p2.Y + perpY),
                            new Point(p2.X - perpX, p2.Y - perpY),
                            new Point(p1.X - perpX, p1.Y - perpY)
                        }
                    };

                    Panel.SetZIndex(polygon, ZOrderBase + 10);
                    canvas.Children.Add(polygon);
                }

                // Draw label at first point
                if (walkway.Centerline.Any() && !string.IsNullOrEmpty(walkway.Name))
                {
                    var firstPoint = walkway.Centerline[0];
                    var label = new TextBlock
                    {
                        Text = walkway.Name,
                        FontSize = 10,
                        Foreground = edgeBrush,
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, firstPoint.X + 5);
                    Canvas.SetTop(label, firstPoint.Y - 15);
                    Panel.SetZIndex(label, ZOrderBase + 11);
                    canvas.Children.Add(label);
                }
            }
        }

        private string GetWalkwayColor(string walkwayType)
        {
            return walkwayType switch
            {
                WalkwayTypes.Primary => "#4A90E2",
                WalkwayTypes.Secondary => "#7ED321",
                WalkwayTypes.Emergency => "#F5A623",
                _ => "#CCCCCC"
            };
        }

        #endregion

        #region Crossings

        private void RenderCrossings(Canvas canvas, LayoutData layout)
        {
            foreach (var crossing in layout.PedestrianCrossings)
            {
                if (crossing.Location.Count < 2) continue;

                var color = ColorFromHex(crossing.Color ?? "#FFFFFF");

                switch (crossing.CrossingType)
                {
                    case PedestrianCrossingTypes.Zebra:
                        RenderZebraCrossing(canvas, crossing, color);
                        break;
                    case PedestrianCrossingTypes.Signal:
                        RenderSignalCrossing(canvas, crossing, color);
                        break;
                    case PedestrianCrossingTypes.Unmarked:
                        RenderUnmarkedCrossing(canvas, crossing, color);
                        break;
                }

                // Draw label
                if (crossing.Location.Any() && !string.IsNullOrEmpty(crossing.Name))
                {
                    var centerX = crossing.Location.Average(p => p.X);
                    var centerY = crossing.Location.Average(p => p.Y);

                    var label = new TextBlock
                    {
                        Text = crossing.Name,
                        FontSize = 9,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, centerX - 20);
                    Canvas.SetTop(label, centerY - 10);
                    Panel.SetZIndex(label, ZOrderBase + 31);
                    canvas.Children.Add(label);
                }
            }
        }

        private void RenderZebraCrossing(Canvas canvas, PedestrianCrossingData crossing, Color color)
        {
            if (crossing.Location.Count < 2) return;

            var p1 = crossing.Location[0];
            var p2 = crossing.Location[crossing.Location.Count - 1];

            // Calculate perpendicular for stripe width
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;

            var stripeWidth = 8;
            var stripeSpacing = 12;
            var crossingWidth = 40;

            var perpX = -dy / length * crossingWidth / 2;
            var perpY = dx / length * crossingWidth / 2;

            // Draw zebra stripes
            for (double offset = 0; offset < length; offset += stripeSpacing)
            {
                var t = offset / length;
                var cx = p1.X + (p2.X - p1.X) * t;
                var cy = p1.Y + (p2.Y - p1.Y) * t;

                var stripe = new Polygon
                {
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Points = new PointCollection
                    {
                        new Point(cx + perpX, cy + perpY),
                        new Point(cx + perpX + dx / length * stripeWidth, cy + perpY + dy / length * stripeWidth),
                        new Point(cx - perpX + dx / length * stripeWidth, cy - perpY + dy / length * stripeWidth),
                        new Point(cx - perpX, cy - perpY)
                    }
                };

                Panel.SetZIndex(stripe, ZOrderBase + 20);
                canvas.Children.Add(stripe);
            }
        }

        private void RenderSignalCrossing(Canvas canvas, PedestrianCrossingData crossing, Color color)
        {
            // Draw polygon outline
            var polygon = new Polygon
            {
                Fill = new SolidColorBrush(color) { Opacity = 0.2 },
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            foreach (var point in crossing.Location)
            {
                polygon.Points.Add(new Point(point.X, point.Y));
            }

            Panel.SetZIndex(polygon, ZOrderBase + 20);
            canvas.Children.Add(polygon);

            // Draw signal icon (traffic light)
            if (crossing.Location.Any())
            {
                var centerX = crossing.Location.Average(p => p.X);
                var centerY = crossing.Location.Average(p => p.Y);

                // Traffic light rectangle
                var signalBox = new Rectangle
                {
                    Width = 12,
                    Height = 30,
                    Fill = Brushes.Black,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(signalBox, centerX - 6);
                Canvas.SetTop(signalBox, centerY - 15);
                Panel.SetZIndex(signalBox, ZOrderBase + 21);
                canvas.Children.Add(signalBox);

                // Red light
                var redLight = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(redLight, centerX - 4);
                Canvas.SetTop(redLight, centerY - 12);
                Panel.SetZIndex(redLight, ZOrderBase + 22);
                canvas.Children.Add(redLight);

                // Green light
                var greenLight = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.Green
                };
                Canvas.SetLeft(greenLight, centerX - 4);
                Canvas.SetTop(greenLight, centerY + 4);
                Panel.SetZIndex(greenLight, ZOrderBase + 22);
                canvas.Children.Add(greenLight);
            }
        }

        private void RenderUnmarkedCrossing(Canvas canvas, PedestrianCrossingData crossing, Color color)
        {
            var polygon = new Polygon
            {
                Fill = new SolidColorBrush(color) { Opacity = 0.1 },
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 }
            };

            foreach (var point in crossing.Location)
            {
                polygon.Points.Add(new Point(point.X, point.Y));
            }

            Panel.SetZIndex(polygon, ZOrderBase + 20);
            canvas.Children.Add(polygon);
        }

        #endregion

        #region Safety Zones

        private void RenderSafetyZones(Canvas canvas, LayoutData layout)
        {
            foreach (var zone in layout.SafetyZones)
            {
                if (zone.Boundary.Count < 3) continue;

                var color = GetSafetyZoneColor(zone.ZoneType);
                var zoneColor = ColorFromHex(zone.Color ?? color);

                // Create hatched pattern brush
                var hatchBrush = CreateHatchBrush(zoneColor);

                // Draw boundary polygon with hatched fill
                var polygon = new Polygon
                {
                    Fill = hatchBrush,
                    Stroke = new SolidColorBrush(zoneColor),
                    StrokeThickness = 3
                };

                foreach (var point in zone.Boundary)
                {
                    polygon.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polygon, ZOrderBase + 1);
                canvas.Children.Add(polygon);

                // Draw label
                if (zone.Boundary.Any() && !string.IsNullOrEmpty(zone.Name))
                {
                    var centerX = zone.Boundary.Average(p => p.X);
                    var centerY = zone.Boundary.Average(p => p.Y);

                    var label = new TextBlock
                    {
                        Text = $"{zone.Name}\n({zone.ZoneType})",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(zoneColor),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                        Padding = new Thickness(3),
                        TextAlignment = TextAlignment.Center
                    };

                    Canvas.SetLeft(label, centerX - 30);
                    Canvas.SetTop(label, centerY - 15);
                    Panel.SetZIndex(label, ZOrderBase + 2);
                    canvas.Children.Add(label);
                }
            }
        }

        private string GetSafetyZoneColor(string zoneType)
        {
            return zoneType switch
            {
                SafetyZoneTypes.KeepOut => "#FF0000",
                SafetyZoneTypes.HardHat => "#FFAA00",
                SafetyZoneTypes.HighVis => "#FFD700",
                SafetyZoneTypes.Restricted => "#FF6600",
                _ => "#CCCCCC"
            };
        }

        private Brush CreateHatchBrush(Color color)
        {
            var visualBrush = new VisualBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 10, 10),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var canvas = new Canvas { Width = 10, Height = 10 };

            // Diagonal hatch lines
            var line1 = new Line
            {
                X1 = 0, Y1 = 0,
                X2 = 10, Y2 = 10,
                Stroke = new SolidColorBrush(color) { Opacity = 0.4 },
                StrokeThickness = 2
            };

            var line2 = new Line
            {
                X1 = 0, Y1 = 10,
                X2 = 10, Y2 = 0,
                Stroke = new SolidColorBrush(color) { Opacity = 0.4 },
                StrokeThickness = 2
            };

            canvas.Children.Add(line1);
            canvas.Children.Add(line2);

            visualBrush.Visual = canvas;
            return visualBrush;
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
