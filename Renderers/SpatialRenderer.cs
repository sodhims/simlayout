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
            DebugLogger.Log($"[SpatialRenderer] RenderZones called, zone count: {layout.Zones.Count}");

            foreach (var zone in layout.Zones)
            {
                Shape shape;
                double labelX, labelY;

                // Check if zone is defined by Points (polygon) or by X/Y/Width/Height (rectangle)
                if (zone.Points.Count > 0)
                {
                    // Polygon zone from Points collection
                    DebugLogger.Log($"[SpatialRenderer] Rendering polygon zone '{zone.Name}' with {zone.Points.Count} points");

                    var polygon = new Polygon
                    {
                        Fill = GetZoneBrush(zone),
                        Stroke = GetZoneBorderBrush(zone),
                        StrokeThickness = 2,
                        StrokeDashArray = GetBorderDashArray(zone)
                    };

                    foreach (var pt in zone.Points)
                    {
                        polygon.Points.Add(new Point(pt.X, pt.Y));
                        DebugLogger.Log($"    Point: ({pt.X}, {pt.Y})");
                    }

                    canvas.Children.Add(polygon);
                    registerElement($"zone:{zone.Id}", polygon);
                    shape = polygon;

                    // Label at first point
                    labelX = zone.Points[0].X + 4;
                    labelY = zone.Points[0].Y + 4;
                }
                else if (zone.Width > 0 && zone.Height > 0)
                {
                    // Rectangle zone from X/Y/Width/Height
                    DebugLogger.Log($"[SpatialRenderer] Rendering rectangle zone '{zone.Name}' at ({zone.X}, {zone.Y}), size: {zone.Width}x{zone.Height}");

                    var rect = new Rectangle
                    {
                        Width = zone.Width,
                        Height = zone.Height,
                        Fill = GetZoneBrush(zone),
                        Stroke = GetZoneBorderBrush(zone),
                        StrokeThickness = 2,
                        StrokeDashArray = GetBorderDashArray(zone),
                        RadiusX = 4,
                        RadiusY = 4
                    };

                    Canvas.SetLeft(rect, zone.X);
                    Canvas.SetTop(rect, zone.Y);
                    canvas.Children.Add(rect);
                    registerElement($"zone:{zone.Id}", rect);
                    shape = rect;

                    labelX = zone.X + 4;
                    labelY = zone.Y + 4;
                }
                else
                {
                    DebugLogger.Log($"[SpatialRenderer] Skipping zone '{zone.Name}' - no valid geometry (Points={zone.Points.Count}, W={zone.Width}, H={zone.Height})");
                    continue;
                }

                DebugLogger.Log($"[SpatialRenderer] Zone '{zone.Name}' added to canvas");

                // Zone label
                if (!string.IsNullOrEmpty(zone.Name))
                {
                    var label = new TextBlock
                    {
                        Text = zone.Name,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.DarkGoldenrod,
                        Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                    };
                    Canvas.SetLeft(label, labelX);
                    Canvas.SetTop(label, labelY);
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

            // Very light pastel colors - highly visible, semi-transparent
            var color2 = zone.Type?.ToLower() switch
            {
                "warehouse" => Color.FromArgb(50, 255, 255, 150),      // Very light yellow
                "storage" => Color.FromArgb(50, 150, 255, 150),        // Very light mint green
                "production" => Color.FromArgb(50, 150, 200, 255),     // Very light sky blue
                "shipping" => Color.FromArgb(50, 255, 180, 150),       // Very light peach
                "receiving" => Color.FromArgb(50, 200, 150, 255),      // Very light lavender
                "restricted" => Color.FromArgb(50, 255, 150, 150),     // Very light pink
                "safety" => Color.FromArgb(50, 255, 255, 100),         // Very light yellow
                "maintenance" => Color.FromArgb(50, 255, 200, 130),    // Very light orange
                _ => Color.FromArgb(40, 255, 255, 180)                 // Very light cream (default)
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

            // Matching border colors - darker versions of fill colors
            return zone.Type?.ToLower() switch
            {
                "warehouse" => new SolidColorBrush(Color.FromRgb(180, 150, 50)),    // Dark yellow/gold
                "storage" => new SolidColorBrush(Color.FromRgb(50, 150, 80)),       // Dark green
                "production" => new SolidColorBrush(Color.FromRgb(70, 130, 180)),   // Steel blue
                "shipping" => new SolidColorBrush(Color.FromRgb(200, 120, 80)),     // Dark peach/coral
                "receiving" => new SolidColorBrush(Color.FromRgb(130, 100, 180)),   // Dark lavender
                "restricted" => Brushes.DarkRed,
                "safety" => Brushes.DarkOrange,
                "maintenance" => Brushes.Chocolate,
                _ => new SolidColorBrush(Color.FromRgb(150, 140, 100))              // Dark beige
            };
        }

        private DoubleCollection? GetBorderDashArray(ZoneData zone)
        {
            if (zone.Visual != null && zone.Visual.BorderStyle == "dashed")
                return new DoubleCollection { 5, 5 };
            if (zone.Visual != null && zone.Visual.BorderStyle == "dotted")
                return new DoubleCollection { 2, 2 };

            return null; // Default solid border for better visibility
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
