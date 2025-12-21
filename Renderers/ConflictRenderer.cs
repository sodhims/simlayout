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
    /// Renders conflict highlights on the canvas
    /// </summary>
    public class ConflictRenderer
    {
        private const int ZOrderBase = 9000; // Very high to appear on top

        /// <summary>
        /// Renders all conflicts on the canvas
        /// </summary>
        public void RenderConflicts(Canvas canvas, List<Conflict> conflicts)
        {
            if (canvas == null || conflicts == null)
                return;

            foreach (var conflict in conflicts)
            {
                if (conflict.IsAcknowledged)
                    continue; // Skip acknowledged conflicts

                RenderConflictHighlight(canvas, conflict);
            }
        }

        /// <summary>
        /// Renders a single conflict highlight
        /// </summary>
        private void RenderConflictHighlight(Canvas canvas, Conflict conflict)
        {
            var color = GetConflictColor(conflict.Severity);
            var size = 40;

            // Draw highlight circle at conflict location
            var highlight = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color) { Opacity = 0.3 },
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3
            };

            Canvas.SetLeft(highlight, conflict.Location.X - size / 2);
            Canvas.SetTop(highlight, conflict.Location.Y - size / 2);
            Panel.SetZIndex(highlight, ZOrderBase);
            canvas.Children.Add(highlight);

            // Draw icon
            var icon = CreateConflictIcon(conflict.Severity);
            Canvas.SetLeft(icon, conflict.Location.X - 10);
            Canvas.SetTop(icon, conflict.Location.Y - 10);
            Panel.SetZIndex(icon, ZOrderBase + 1);
            canvas.Children.Add(icon);

            // Draw tooltip text
            var tooltip = new TextBlock
            {
                Text = conflict.Description,
                FontSize = 10,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Colors.Black) { Opacity = 0.8 },
                Padding = new Thickness(4),
                MaxWidth = 200,
                TextWrapping = TextWrapping.Wrap
            };

            ToolTipService.SetToolTip(highlight, tooltip);
            ToolTipService.SetToolTip(icon, tooltip);
        }

        /// <summary>
        /// Creates an icon for the conflict
        /// </summary>
        private UIElement CreateConflictIcon(ConflictSeverity severity)
        {
            var size = 20;
            var canvas = new Canvas { Width = size, Height = size };

            if (severity == ConflictSeverity.Error)
            {
                // Red X for errors
                var line1 = new Line
                {
                    X1 = 2, Y1 = 2,
                    X2 = size - 2, Y2 = size - 2,
                    Stroke = Brushes.Red,
                    StrokeThickness = 3
                };
                var line2 = new Line
                {
                    X1 = size - 2, Y1 = 2,
                    X2 = 2, Y2 = size - 2,
                    Stroke = Brushes.Red,
                    StrokeThickness = 3
                };
                canvas.Children.Add(line1);
                canvas.Children.Add(line2);
            }
            else
            {
                // Yellow ! for warnings
                var exclamation = new TextBlock
                {
                    Text = "!",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Orange
                };
                canvas.Children.Add(exclamation);
            }

            return canvas;
        }

        /// <summary>
        /// Gets the color for a conflict severity
        /// </summary>
        private Color GetConflictColor(ConflictSeverity severity)
        {
            return severity switch
            {
                ConflictSeverity.Error => Colors.Red,
                ConflictSeverity.Warning => Colors.Orange,
                _ => Colors.Yellow
            };
        }

        /// <summary>
        /// Clears all conflict highlights from the canvas
        /// </summary>
        public void ClearConflicts(Canvas canvas)
        {
            if (canvas == null)
                return;

            // Remove all elements with Z-order >= ZOrderBase
            var toRemove = canvas.Children.OfType<UIElement>()
                .Where(e => Panel.GetZIndex(e) >= ZOrderBase)
                .ToList();

            foreach (var element in toRemove)
            {
                canvas.Children.Remove(element);
            }
        }
    }
}
