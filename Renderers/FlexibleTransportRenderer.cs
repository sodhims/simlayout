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
    /// Renders Flexible Transport layer (Forklift aisles, staging areas, crossing zones)
    /// Z-order: 600-699 range
    /// </summary>
    public class FlexibleTransportRenderer : ILayerRenderer
    {
        private readonly SelectionService _selectionService;

        public LayerType Layer => LayerType.FlexibleTransport;
        public int ZOrderBase => 600;

        public FlexibleTransportRenderer(SelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: aisles (back), staging areas, crossing zones (front)
            RenderForkliftAisles(canvas, layout);
            RenderStagingAreas(canvas, layout);
            RenderCrossingZones(canvas, layout);
        }

        #region Forklift Aisles

        private void RenderForkliftAisles(Canvas canvas, LayoutData layout)
        {
            foreach (var aisle in layout.ForkliftAisles)
            {
                if (aisle.Centerline.Count < 2) continue;

                // Parse color or use default
                Color lineColor;
                try
                {
                    lineColor = (Color)ColorConverter.ConvertFromString(aisle.Color);
                }
                catch
                {
                    lineColor = Colors.LightGreen;
                }

                // Render aisle as wide corridor with parallel lines
                for (int i = 0; i < aisle.Centerline.Count - 1; i++)
                {
                    var from = aisle.Centerline[i];
                    var to = aisle.Centerline[i + 1];

                    RenderAisleSegment(canvas, from, to, aisle.Width, lineColor);
                }

                // Add label at midpoint
                if (aisle.Centerline.Any() && !string.IsNullOrEmpty(aisle.Name))
                {
                    var midIndex = aisle.Centerline.Count / 2;
                    var labelPoint = aisle.Centerline[midIndex];

                    var label = new TextBlock
                    {
                        Text = $"{aisle.Name} ({aisle.Width:F1}m)",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(lineColor),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, labelPoint.X + 5);
                    Canvas.SetTop(label, labelPoint.Y - 10);
                    Panel.SetZIndex(label, ZOrderBase + 2);
                    canvas.Children.Add(label);
                }
            }
        }

        private void RenderAisleSegment(Canvas canvas, PointData from, PointData to, double width, Color color)
        {
            // Calculate perpendicular offset for width visualization
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;

            var perpX = -dy / length * width;
            var perpY = dx / length * width;

            // Draw two parallel lines showing aisle edges
            var line1 = new Line
            {
                X1 = from.X + perpX / 2,
                Y1 = from.Y + perpY / 2,
                X2 = to.X + perpX / 2,
                Y2 = to.Y + perpY / 2,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };

            var line2 = new Line
            {
                X1 = from.X - perpX / 2,
                Y1 = from.Y - perpY / 2,
                X2 = to.X - perpX / 2,
                Y2 = to.Y - perpY / 2,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };

            // Draw centerline as dashed
            var centerline = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(color) { Opacity = 0.5 },
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 }
            };

            Panel.SetZIndex(line1, ZOrderBase + 1);
            Panel.SetZIndex(line2, ZOrderBase + 1);
            Panel.SetZIndex(centerline, ZOrderBase + 1);

            canvas.Children.Add(line1);
            canvas.Children.Add(line2);
            canvas.Children.Add(centerline);
        }

        #endregion

        #region Staging Areas

        private void RenderStagingAreas(Canvas canvas, LayoutData layout)
        {
            foreach (var staging in layout.StagingAreas)
            {
                if (staging.Boundary.Count < 3) continue;

                // Parse color or use default
                Color fillColor;
                try
                {
                    fillColor = (Color)ColorConverter.ConvertFromString(staging.Color);
                }
                catch
                {
                    fillColor = Colors.Gold;
                }

                var polygon = new Polygon
                {
                    Fill = new SolidColorBrush(fillColor) { Opacity = 0.2 },
                    Stroke = new SolidColorBrush(fillColor),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };

                foreach (var point in staging.Boundary)
                {
                    polygon.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polygon, ZOrderBase + 10);
                canvas.Children.Add(polygon);

                // Draw label with capacity info
                if (staging.Boundary.Any())
                {
                    var centerX = staging.Boundary.Average(p => p.X);
                    var centerY = staging.Boundary.Average(p => p.Y);

                    var label = new TextBlock
                    {
                        Text = $"{staging.Name}\n({staging.Capacity} pallets)",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(fillColor),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                        Padding = new Thickness(3),
                        TextAlignment = TextAlignment.Center
                    };

                    Canvas.SetLeft(label, centerX - 30);
                    Canvas.SetTop(label, centerY - 15);
                    Panel.SetZIndex(label, ZOrderBase + 11);
                    canvas.Children.Add(label);
                }
            }
        }

        #endregion

        #region Crossing Zones

        private void RenderCrossingZones(Canvas canvas, LayoutData layout)
        {
            foreach (var crossing in layout.CrossingZones)
            {
                // Parse color or use default orange
                Color fillColor;
                try
                {
                    fillColor = (Color)ColorConverter.ConvertFromString(crossing.Color);
                }
                catch
                {
                    fillColor = Colors.Orange;
                }

                // If boundary exists, render as polygon
                if (crossing.Boundary != null && crossing.Boundary.Count >= 3)
                {
                    var polygon = new Polygon
                    {
                        Fill = CreateZebraStripeBrush(fillColor),
                        Stroke = new SolidColorBrush(fillColor),
                        StrokeThickness = 3
                    };

                    foreach (var point in crossing.Boundary)
                    {
                        polygon.Points.Add(new Point(point.X, point.Y));
                    }

                    Panel.SetZIndex(polygon, ZOrderBase + 20);
                    canvas.Children.Add(polygon);
                }
                else
                {
                    // Render as circle at crossing point
                    var circle = new Ellipse
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(fillColor) { Opacity = 0.5 },
                        Stroke = new SolidColorBrush(fillColor),
                        StrokeThickness = 3
                    };

                    Canvas.SetLeft(circle, crossing.X - 10);
                    Canvas.SetTop(circle, crossing.Y - 10);
                    Panel.SetZIndex(circle, ZOrderBase + 20);
                    canvas.Children.Add(circle);
                }

                // Add label
                if (!string.IsNullOrEmpty(crossing.Name))
                {
                    var labelX = crossing.Boundary != null && crossing.Boundary.Any()
                        ? crossing.Boundary.Average(p => p.X)
                        : crossing.X;
                    var labelY = crossing.Boundary != null && crossing.Boundary.Any()
                        ? crossing.Boundary.Average(p => p.Y)
                        : crossing.Y;

                    var typeLabel = crossing.CrossingType switch
                    {
                        CrossingTypes.SignalControlled => "🚦",
                        CrossingTypes.PriorityBased => "⚠",
                        CrossingTypes.TimeWindowed => "⏱",
                        _ => "⚠"
                    };

                    var label = new TextBlock
                    {
                        Text = $"{typeLabel} {crossing.Name}",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(fillColor),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                        Padding = new Thickness(2),
                        FontWeight = FontWeights.Bold
                    };

                    Canvas.SetLeft(label, labelX - 25);
                    Canvas.SetTop(label, labelY + 15);
                    Panel.SetZIndex(label, ZOrderBase + 21);
                    canvas.Children.Add(label);
                }
            }
        }

        private Brush CreateZebraStripeBrush(Color baseColor)
        {
            // Create diagonal stripe pattern for crossing zones
            var drawingBrush = new DrawingBrush
            {
                Viewport = new Rect(0, 0, 10, 10),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile
            };

            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 5, 10)));
            geometryGroup.Children.Add(new RectangleGeometry(new Rect(5, 0, 5, 10)));

            var drawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 5, 10)),
                Brush = new SolidColorBrush(baseColor) { Opacity = 0.4 }
            };

            drawingBrush.Drawing = drawing;
            return drawingBrush;
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
