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
    /// Renders Overhead Transport layer (Crane coverage, handoff points, drop zones)
    /// Z-order: 500-599 range
    /// </summary>
    public class OverheadTransportRenderer : ILayerRenderer
    {
        private readonly SelectionService _selectionService;

        public LayerType Layer => LayerType.OverheadTransport;
        public int ZOrderBase => 500;

        public OverheadTransportRenderer(SelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: crane coverage (back), drop zones, handoff points (front)
            RenderCraneCoverage(canvas, layout);
            RenderDropZones(canvas, layout);
            RenderHandoffPoints(canvas, layout, registerElement);
        }

        #region Crane Coverage

        private void RenderCraneCoverage(Canvas canvas, LayoutData layout)
        {
            // Render EOT crane coverage
            foreach (var crane in layout.EOTCranes)
            {
                var runway = layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
                if (runway == null) continue;

                var coverage = crane.GetCoveragePolygon(runway);
                if (coverage.Count < 3) continue;

                var polygon = new Polygon
                {
                    Fill = new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.2 },
                    Stroke = new SolidColorBrush(Colors.CornflowerBlue),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };

                foreach (var point in coverage)
                {
                    polygon.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polygon, ZOrderBase + 1);
                canvas.Children.Add(polygon);

                // Add label
                if (coverage.Any())
                {
                    var centerX = coverage.Average(p => p.X);
                    var centerY = coverage.Average(p => p.Y);

                    var label = new TextBlock
                    {
                        Text = $"EOT: {crane.Name}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Colors.CornflowerBlue),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, centerX - 25);
                    Canvas.SetTop(label, centerY - 8);
                    Panel.SetZIndex(label, ZOrderBase + 2);
                    canvas.Children.Add(label);
                }
            }

            // Render Jib crane coverage
            foreach (var crane in layout.JibCranes)
            {
                var coverage = crane.GetCoveragePolygon(segments: 32);
                if (coverage.Count < 3) continue;

                var polygon = new Polygon
                {
                    Fill = new SolidColorBrush(Colors.MediumPurple) { Opacity = 0.2 },
                    Stroke = new SolidColorBrush(Colors.MediumPurple),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };

                foreach (var point in coverage)
                {
                    polygon.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polygon, ZOrderBase + 1);
                canvas.Children.Add(polygon);

                // Add label
                var label = new TextBlock
                {
                    Text = $"Jib: {crane.Name}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.MediumPurple),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(label, crane.CenterX - 20);
                Canvas.SetTop(label, crane.CenterY - 8);
                Panel.SetZIndex(label, ZOrderBase + 2);
                canvas.Children.Add(label);
            }
        }

        #endregion

        #region Drop Zones

        private void RenderDropZones(Canvas canvas, LayoutData layout)
        {
            foreach (var dropZone in layout.DropZones)
            {
                if (dropZone.Boundary.Count < 3) continue;

                // Parse color or use default orange
                Color fillColor;
                try
                {
                    fillColor = (Color)ColorConverter.ConvertFromString(dropZone.Color);
                }
                catch
                {
                    fillColor = Colors.Orange;
                }

                var polygon = new Polygon
                {
                    Fill = new SolidColorBrush(fillColor) { Opacity = 0.25 },
                    Stroke = new SolidColorBrush(fillColor),
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection { 6, 4 }
                };

                foreach (var point in dropZone.Boundary)
                {
                    polygon.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polygon, ZOrderBase + 10);
                canvas.Children.Add(polygon);

                // Draw label
                if (dropZone.Boundary.Any())
                {
                    var centerX = dropZone.Boundary.Average(p => p.X);
                    var centerY = dropZone.Boundary.Average(p => p.Y);

                    var labelText = dropZone.Name;
                    if (dropZone.IsPedestrianExclusion)
                    {
                        labelText += " ⚠";
                    }

                    var label = new TextBlock
                    {
                        Text = labelText,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(fillColor),
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                        Padding = new Thickness(3),
                        FontWeight = FontWeights.Bold
                    };

                    Canvas.SetLeft(label, centerX - 25);
                    Canvas.SetTop(label, centerY - 10);
                    Panel.SetZIndex(label, ZOrderBase + 11);
                    canvas.Children.Add(label);
                }
            }
        }

        #endregion

        #region Handoff Points

        private void RenderHandoffPoints(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var handoff in layout.HandoffPoints)
            {
                var size = 14;
                var color = handoff.HandoffType == HandoffTypes.Direct
                    ? Colors.Gold
                    : Colors.LightGreen;

                // Render as star for handoff points
                var star = CreateStarPolygon(handoff.X, handoff.Y, size, size * 0.4);
                star.Fill = new SolidColorBrush(color);
                star.Stroke = Brushes.Black;
                star.StrokeThickness = 2;

                Panel.SetZIndex(star, ZOrderBase + 20);
                canvas.Children.Add(star);

                // Add label if handoff has a name
                if (!string.IsNullOrEmpty(handoff.Name))
                {
                    var label = new TextBlock
                    {
                        Text = handoff.Name,
                        FontSize = 9,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, handoff.X + size + 3);
                    Canvas.SetTop(label, handoff.Y - 6);
                    Panel.SetZIndex(label, ZOrderBase + 21);
                    canvas.Children.Add(label);
                }
            }
        }

        private Polygon CreateStarPolygon(double centerX, double centerY, double outerRadius, double innerRadius)
        {
            var points = new PointCollection();
            var numPoints = 5; // 5-pointed star

            for (int i = 0; i < numPoints * 2; i++)
            {
                var angle = Math.PI / 2 + i * Math.PI / numPoints; // Start from top
                var radius = (i % 2 == 0) ? outerRadius : innerRadius;

                var x = centerX + radius * Math.Cos(angle);
                var y = centerY - radius * Math.Sin(angle); // Subtract because Y increases downward

                points.Add(new Point(x, y));
            }

            return new Polygon { Points = points };
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
