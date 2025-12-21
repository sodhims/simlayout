using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Renderers
{
    /// <summary>
    /// Renders Spatial layer (Layer 1): zones, primary aisles, restricted areas
    /// </summary>
    public class SpatialRenderer : ILayerRenderer
    {
        public LayerType Layer => LayerType.Spatial;

        public int ZOrderBase => 100; // 100-199 range for Spatial

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: zones → aisles → restricted areas
            RenderZones(canvas, layout, registerElement);
            RenderPrimaryAisles(canvas, layout, registerElement);
            RenderRestrictedAreas(canvas, layout, registerElement);
        }

        private void RenderZones(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var zone in layout.Zones)
            {
                var rect = new Rectangle
                {
                    Width = zone.Width,
                    Height = zone.Height,
                    Fill = GetZoneBrush(zone),
                    Stroke = GetZoneBorderBrush(zone),
                    StrokeThickness = 1,
                    StrokeDashArray = GetBorderDashArray(zone),
                    RadiusX = 4,
                    RadiusY = 4
                };

                Canvas.SetLeft(rect, zone.X);
                Canvas.SetTop(rect, zone.Y);
                canvas.Children.Add(rect);
                registerElement($"zone:{zone.Id}", rect);

                // Zone label
                if (!string.IsNullOrEmpty(zone.Name))
                {
                    var label = new TextBlock
                    {
                        Text = zone.Name,
                        FontSize = 10,
                        Foreground = Brushes.DimGray
                    };
                    Canvas.SetLeft(label, zone.X + 4);
                    Canvas.SetTop(label, zone.Y + 4);
                    canvas.Children.Add(label);
                }
            }
        }

        private void RenderPrimaryAisles(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var aisle in layout.PrimaryAisles)
            {
                if (aisle.Centerline.Count < 2)
                    continue;

                // Create polyline for aisle centerline
                var points = new PointCollection();
                foreach (var pt in aisle.Centerline)
                    points.Add(new Point(pt.X, pt.Y));

                var polyline = new Polyline
                {
                    Points = points,
                    Stroke = aisle.IsEmergencyRoute ? Brushes.Yellow : Brushes.LightGray,
                    StrokeThickness = aisle.Width,
                    StrokeDashArray = new DoubleCollection { 10, 5 }, // Dashed edges
                    Opacity = 0.5
                };

                canvas.Children.Add(polyline);
                registerElement($"aisle:{aisle.Id}", polyline);

                // Aisle label at midpoint
                if (!string.IsNullOrEmpty(aisle.Name) && aisle.Centerline.Count >= 2)
                {
                    var mid = aisle.Centerline[aisle.Centerline.Count / 2];
                    var label = new TextBlock
                    {
                        Text = aisle.Name,
                        FontSize = 10,
                        Foreground = aisle.IsEmergencyRoute ? Brushes.DarkOrange : Brushes.Gray,
                        FontWeight = aisle.IsEmergencyRoute ? FontWeights.Bold : FontWeights.Normal
                    };
                    Canvas.SetLeft(label, mid.X);
                    Canvas.SetTop(label, mid.Y);
                    canvas.Children.Add(label);
                }
            }
        }

        private void RenderRestrictedAreas(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var area in layout.RestrictedAreas)
            {
                // Create warning pattern fill
                var patternBrush = CreateWarningPattern(area.RestrictionType);

                var rect = new Rectangle
                {
                    Width = area.Width,
                    Height = area.Height,
                    Fill = patternBrush,
                    Stroke = GetRestrictionBorderBrush(area.RestrictionType),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };

                Canvas.SetLeft(rect, area.X);
                Canvas.SetTop(rect, area.Y);
                canvas.Children.Add(rect);
                registerElement($"restricted:{area.Id}", rect);

                // Area label
                if (!string.IsNullOrEmpty(area.Name))
                {
                    var label = new TextBlock
                    {
                        Text = area.Name,
                        FontSize = 10,
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(label, area.X + 4);
                    Canvas.SetTop(label, area.Y + 4);
                    canvas.Children.Add(label);
                }

                // PPE label if specified
                if (!string.IsNullOrEmpty(area.RequiredPPE))
                {
                    var ppeLabel = new TextBlock
                    {
                        Text = $"PPE: {area.RequiredPPE}",
                        FontSize = 8,
                        Foreground = Brushes.DarkRed
                    };
                    Canvas.SetLeft(ppeLabel, area.X + 4);
                    Canvas.SetTop(ppeLabel, area.Y + area.Height - 14);
                    canvas.Children.Add(ppeLabel);
                }
            }
        }

        private Brush GetZoneBrush(ZoneData zone)
        {
            // Parse zone visual fill color or use type-based default
            if (zone.Visual != null && !string.IsNullOrEmpty(zone.Visual.FillColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(zone.Visual.FillColor);
                    return new SolidColorBrush(color);
                }
                catch { }
            }

            // Default colors by type
            var color2 = zone.Type?.ToLower() switch
            {
                "restricted" => Color.FromArgb(30, 255, 0, 0),
                "safety" => Color.FromArgb(30, 255, 255, 0),
                "storage" => Color.FromArgb(30, 0, 0, 255),
                "maintenance" => Color.FromArgb(30, 255, 165, 0),
                _ => Color.FromArgb(20, 128, 128, 128)
            };

            return new SolidColorBrush(color2);
        }

        private Brush GetZoneBorderBrush(ZoneData zone)
        {
            if (zone.Visual != null && !string.IsNullOrEmpty(zone.Visual.BorderColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(zone.Visual.BorderColor);
                    return new SolidColorBrush(color);
                }
                catch { }
            }

            return Brushes.Gray;
        }

        private DoubleCollection? GetBorderDashArray(ZoneData zone)
        {
            if (zone.Visual != null && zone.Visual.BorderStyle == "dashed")
                return new DoubleCollection { 5, 5 };
            if (zone.Visual != null && zone.Visual.BorderStyle == "dotted")
                return new DoubleCollection { 2, 2 };

            return new DoubleCollection { 2, 2 }; // Default dashed
        }

        private Brush CreateWarningPattern(string restrictionType)
        {
            var baseColor = restrictionType switch
            {
                RestrictionTypes.Hazmat => Color.FromArgb(50, 255, 140, 0), // Orange
                RestrictionTypes.HighVoltage => Color.FromArgb(50, 255, 255, 0), // Yellow
                RestrictionTypes.Cleanroom => Color.FromArgb(50, 0, 255, 255), // Cyan
                RestrictionTypes.AuthorizedOnly => Color.FromArgb(50, 255, 0, 0), // Red
                _ => Color.FromArgb(50, 255, 0, 0) // Default red
            };

            // For now, simple solid brush - can be enhanced with actual pattern later
            return new SolidColorBrush(baseColor);
        }

        private Brush GetRestrictionBorderBrush(string restrictionType)
        {
            return restrictionType switch
            {
                RestrictionTypes.Hazmat => Brushes.Orange,
                RestrictionTypes.HighVoltage => Brushes.Yellow,
                RestrictionTypes.Cleanroom => Brushes.Cyan,
                RestrictionTypes.AuthorizedOnly => Brushes.Red,
                _ => Brushes.Red
            };
        }
    }
}
