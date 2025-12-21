using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders the canvas grid and background
    /// </summary>
    public class GridRenderer
    {
        public void DrawGrid(Canvas canvas, LayoutData layout, double scale)
        {
            if (!layout.Canvas.ShowGrid) return;

            var gridSize = layout.Canvas.GridSize;
            var width = layout.Canvas.Width;
            var height = layout.Canvas.Height;

            var gridBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));

            // Vertical lines
            for (double x = 0; x <= width; x += gridSize)
            {
                var line = new Line
                {
                    X1 = x, Y1 = 0,
                    X2 = x, Y2 = height,
                    Stroke = gridBrush,
                    StrokeThickness = x % (gridSize * 5) == 0 ? 0.5 : 0.25
                };
                canvas.Children.Add(line);
            }

            // Horizontal lines
            for (double y = 0; y <= height; y += gridSize)
            {
                var line = new Line
                {
                    X1 = 0, Y1 = y,
                    X2 = width, Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = y % (gridSize * 5) == 0 ? 0.5 : 0.25
                };
                canvas.Children.Add(line);
            }
        }

        public void DrawBackground(Canvas canvas, LayoutData layout)
        {
            if (string.IsNullOrEmpty(layout.Canvas.BackgroundImage)) return;

            // TODO: Load and draw background image
        }

        public void DrawRulers(Canvas horizontalRuler, Canvas verticalRuler, 
            LayoutData layout, double scale, double scrollX, double scrollY)
        {
            horizontalRuler.Children.Clear();
            verticalRuler.Children.Clear();

            var rulerBrush = new SolidColorBrush(Colors.Black);
            var interval = GetRulerInterval(scale);

            // Horizontal ruler
            for (double x = 0; x < layout.Canvas.Width; x += interval)
            {
                var screenX = x * scale - scrollX;
                if (screenX < 0 || screenX > horizontalRuler.ActualWidth) continue;

                var tick = new Line
                {
                    X1 = screenX, Y1 = 15,
                    X2 = screenX, Y2 = 20,
                    Stroke = rulerBrush,
                    StrokeThickness = 1
                };
                horizontalRuler.Children.Add(tick);

                var label = new TextBlock
                {
                    Text = ((int)x).ToString(),
                    FontSize = 9,
                    Foreground = rulerBrush
                };
                Canvas.SetLeft(label, screenX + 2);
                Canvas.SetTop(label, 2);
                horizontalRuler.Children.Add(label);
            }

            // Vertical ruler
            for (double y = 0; y < layout.Canvas.Height; y += interval)
            {
                var screenY = y * scale - scrollY;
                if (screenY < 0 || screenY > verticalRuler.ActualHeight) continue;

                var tick = new Line
                {
                    X1 = 15, Y1 = screenY,
                    X2 = 20, Y2 = screenY,
                    Stroke = rulerBrush,
                    StrokeThickness = 1
                };
                verticalRuler.Children.Add(tick);

                var label = new TextBlock
                {
                    Text = ((int)y).ToString(),
                    FontSize = 9,
                    Foreground = rulerBrush,
                    RenderTransform = new RotateTransform(-90)
                };
                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, screenY + 2);
                verticalRuler.Children.Add(label);
            }
        }

        private double GetRulerInterval(double scale)
        {
            if (scale >= 2) return 25;
            if (scale >= 1) return 50;
            if (scale >= 0.5) return 100;
            return 200;
        }
    }
}
