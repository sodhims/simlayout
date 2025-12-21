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
    /// Renders walls, doors, columns on the canvas
    /// </summary>
    public class WallRenderer
    {
        private readonly SelectionService _selection;
        private string? _selectedWallId;
        private HashSet<string> _selectedWallIds = new();

        public WallRenderer(SelectionService selection)
        {
            _selection = selection;
        }

        public string? SelectedWallId
        {
            get => _selectedWallId;
            set => _selectedWallId = value;
        }

        /// <summary>
        /// Set multiple selected wall IDs
        /// </summary>
        public void SetSelectedWalls(IEnumerable<string> wallIds)
        {
            _selectedWallIds = new HashSet<string>(wallIds);
        }

        /// <summary>
        /// Check if wall is selected (checks both local state and SelectionService)
        /// </summary>
        private bool IsWallSelected(string wallId)
        {
            return _selectedWallId == wallId || 
                   _selectedWallIds.Contains(wallId) || 
                   _selection.IsWallSelected(wallId);
        }

        /// <summary>
        /// Get all selected wall IDs (from both local state and SelectionService)
        /// </summary>
        private HashSet<string> GetAllSelectedWallIds()
        {
            var all = new HashSet<string>(_selectedWallIds);
            foreach (var id in _selection.SelectedWallIds)
                all.Add(id);
            if (_selectedWallId != null)
                all.Add(_selectedWallId);
            return all;
        }

        public void DrawWalls(Canvas canvas, LayoutData layout, Action<string, UIElement>? register = null)
        {
            // Draw walls
            foreach (var wall in layout.Walls)
            {
                var element = CreateWallElement(wall, layout);
                canvas.Children.Add(element);
                register?.Invoke($"wall:{wall.Id}", element);
            }

            // Draw columns
            foreach (var column in layout.Columns)
            {
                var element = CreateColumnElement(column);
                canvas.Children.Add(element);
                register?.Invoke($"column:{column.Id}", element);
            }

            // Draw doors (on top of walls)
            foreach (var door in layout.Doors)
            {
                var wall = layout.Walls.FirstOrDefault(w => w.Id == door.WallId);
                if (wall != null)
                {
                    var element = CreateDoorElement(door, wall);
                    canvas.Children.Add(element);
                    register?.Invoke($"door:{door.Id}", element);
                }
            }
        }

        private UIElement CreateWallElement(WallData wall, LayoutData layout)
        {
            var isSelected = IsWallSelected(wall.Id);
            var brush = GetWallBrush(wall);
            
            var line = new Line
            {
                X1 = wall.X1,
                Y1 = wall.Y1,
                X2 = wall.X2,
                Y2 = wall.Y2,
                Stroke = isSelected ? new SolidColorBrush(Colors.DodgerBlue) : brush,
                StrokeThickness = wall.Thickness,
                StrokeStartLineCap = PenLineCap.Square,
                StrokeEndLineCap = PenLineCap.Square,
                Tag = $"wall:{wall.Id}"
            };

            // Apply dash pattern from wall data if set
            if (!string.IsNullOrEmpty(wall.DashPattern))
            {
                try
                {
                    var parts = wall.DashPattern.Split(',');
                    var dashes = new DoubleCollection();
                    foreach (var part in parts)
                    {
                        if (double.TryParse(part.Trim(), out var val))
                            dashes.Add(val);
                    }
                    if (dashes.Count > 0)
                        line.StrokeDashArray = dashes;
                }
                catch { }
            }
            // Different visual styles for wall types (only if no custom dash set)
            else if (wall.WallType == WallTypes.Glass)
            {
                line.StrokeDashArray = new DoubleCollection { 10, 5 };
                line.StrokeThickness = Math.Max(2, wall.Thickness * 0.5);
            }
            else if (wall.WallType == WallTypes.Safety)
            {
                if (!isSelected) line.Stroke = new SolidColorBrush(Colors.Orange);
                line.StrokeDashArray = new DoubleCollection { 15, 10 };
            }
            else if (wall.WallType == WallTypes.Partition)
            {
                line.StrokeThickness = Math.Max(2, wall.Thickness * 0.8);
            }

            // Selection highlight effect
            if (isSelected)
            {
                var container = new Canvas { Tag = $"wall:{wall.Id}" };
                
                // Add glow/highlight behind selected wall
                var highlight = new Line
                {
                    X1 = wall.X1,
                    Y1 = wall.Y1,
                    X2 = wall.X2,
                    Y2 = wall.Y2,
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 30, 144, 255)),
                    StrokeThickness = wall.Thickness + 6,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                container.Children.Add(highlight);
                container.Children.Add(line);
                
                // Add selection handles at endpoints
                AddSelectionHandle(container, wall.X1, wall.Y1);
                AddSelectionHandle(container, wall.X2, wall.Y2);
                
                return container;
            }

            // For glass walls, add a second parallel line to show transparency
            if (wall.WallType == WallTypes.Glass && !isSelected)
            {
                var container = new Canvas { Tag = $"wall:{wall.Id}" };
                
                var dx = wall.X2 - wall.X1;
                var dy = wall.Y2 - wall.Y1;
                var len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0)
                {
                    var fill = new Line
                    {
                        X1 = wall.X1, Y1 = wall.Y1,
                        X2 = wall.X2, Y2 = wall.Y2,
                        Stroke = new SolidColorBrush(Color.FromArgb(60, 100, 180, 255)),
                        StrokeThickness = wall.Thickness * 0.6,
                        StrokeStartLineCap = PenLineCap.Square,
                        StrokeEndLineCap = PenLineCap.Square
                    };
                    container.Children.Insert(0, fill);
                }
                container.Children.Add(line);
                return container;
            }

            return line;
        }

        private void AddSelectionHandle(Canvas container, double x, double y, string handleType = "move")
        {
            var handle = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
                StrokeThickness = 2,
                Tag = handleType,
                Cursor = handleType == "endpoint" ? Cursors.Cross : Cursors.SizeAll
            };
            Canvas.SetLeft(handle, x - 4);
            Canvas.SetTop(handle, y - 4);
            container.Children.Add(handle);
        }

        /// <summary>
        /// Draw endpoint handles for selected walls (for stretching)
        /// </summary>
        public void DrawWallEndpointHandles(Canvas canvas, LayoutData layout)
        {
            foreach (var wallId in GetAllSelectedWallIds())
            {
                var wall = layout.Walls.FirstOrDefault(w => w.Id == wallId);
                if (wall == null) continue;

                // Start endpoint handle
                var startHandle = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(100, 200, 100)),
                    Stroke = new SolidColorBrush(Colors.DarkGreen),
                    StrokeThickness = 2,
                    Tag = $"wallstart:{wall.Id}",
                    Cursor = Cursors.Cross
                };
                Canvas.SetLeft(startHandle, wall.X1 - 5);
                Canvas.SetTop(startHandle, wall.Y1 - 5);
                canvas.Children.Add(startHandle);

                // End endpoint handle
                var endHandle = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(200, 100, 100)),
                    Stroke = new SolidColorBrush(Colors.DarkRed),
                    StrokeThickness = 2,
                    Tag = $"wallend:{wall.Id}",
                    Cursor = Cursors.Cross
                };
                Canvas.SetLeft(endHandle, wall.X2 - 5);
                Canvas.SetTop(endHandle, wall.Y2 - 5);
                canvas.Children.Add(endHandle);
            }
        }

        private UIElement CreateColumnElement(ColumnData column)
        {
            Shape shape;
            
            if (column.Shape == "round")
            {
                shape = new Ellipse
                {
                    Width = column.Width,
                    Height = column.Height,
                    Fill = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Stroke = new SolidColorBrush(Colors.DarkGray),
                    StrokeThickness = 1,
                    Tag = $"column:{column.Id}"
                };
            }
            else
            {
                shape = new Rectangle
                {
                    Width = column.Width,
                    Height = column.Height,
                    Fill = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Stroke = new SolidColorBrush(Colors.DarkGray),
                    StrokeThickness = 1,
                    Tag = $"column:{column.Id}"
                };
            }

            Canvas.SetLeft(shape, column.X - column.Width / 2);
            Canvas.SetTop(shape, column.Y - column.Height / 2);

            return shape;
        }

        private UIElement CreateDoorElement(DoorData door, WallData wall)
        {
            var container = new Canvas { Tag = $"door:{door.Id}" };

            var dx = wall.X2 - wall.X1;
            var dy = wall.Y2 - wall.Y1;
            var length = Math.Sqrt(dx * dx + dy * dy);
            
            var doorX = wall.X1 + dx * door.Position;
            var doorY = wall.Y1 + dy * door.Position;

            var perpX = -dy / length;
            var perpY = dx / length;

            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            
            var doorRect = new Rectangle
            {
                Width = door.Width,
                Height = wall.Thickness + 4,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                RenderTransform = new RotateTransform(angle, door.Width / 2, (wall.Thickness + 4) / 2)
            };
            Canvas.SetLeft(doorRect, doorX - door.Width / 2);
            Canvas.SetTop(doorRect, doorY - (wall.Thickness + 4) / 2);
            container.Children.Add(doorRect);

            if (door.DoorType == DoorTypes.Standard || door.DoorType == DoorTypes.Double)
            {
                var arc = new Path
                {
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };

                var arcGeometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(doorX, doorY) };
                figure.Segments.Add(new ArcSegment(
                    new Point(doorX + perpX * door.Width * 0.7, doorY + perpY * door.Width * 0.7),
                    new Size(door.Width * 0.7, door.Width * 0.7),
                    0, false, SweepDirection.Clockwise, true));
                arcGeometry.Figures.Add(figure);
                arc.Data = arcGeometry;
                container.Children.Add(arc);
            }

            return container;
        }

        private Brush GetWallBrush(WallData wall)
        {
            if (!string.IsNullOrEmpty(wall.Color))
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(wall.Color));
                }
                catch { }
            }

            return wall.WallType switch
            {
                WallTypes.Exterior => new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                WallTypes.Partition => new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                WallTypes.Glass => new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                WallTypes.Safety => new SolidColorBrush(Colors.Yellow),
                _ => new SolidColorBrush(Color.FromRgb(80, 80, 80))
            };
        }

        public void DrawMeasurements(Canvas canvas, LayoutData layout)
        {
            foreach (var measurement in layout.Measurements)
            {
                var element = CreateMeasurementElement(measurement, layout);
                canvas.Children.Add(element);
            }
        }

        private UIElement CreateMeasurementElement(MeasurementData measurement, LayoutData layout)
        {
            var container = new Canvas { Tag = $"measurement:{measurement.Id}" };

            var line = new Line
            {
                X1 = measurement.X1,
                Y1 = measurement.Y1,
                X2 = measurement.X2,
                Y2 = measurement.Y2,
                Stroke = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                StrokeThickness = 1
            };
            container.Children.Add(line);

            var dx = measurement.X2 - measurement.X1;
            var dy = measurement.Y2 - measurement.Y1;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1) return container;

            var perpX = -dy / length * 6;
            var perpY = dx / length * 6;

            // Start tick
            container.Children.Add(new Line
            {
                X1 = measurement.X1 - perpX,
                Y1 = measurement.Y1 - perpY,
                X2 = measurement.X1 + perpX,
                Y2 = measurement.Y1 + perpY,
                Stroke = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                StrokeThickness = 1
            });

            // End tick
            container.Children.Add(new Line
            {
                X1 = measurement.X2 - perpX,
                Y1 = measurement.Y2 - perpY,
                X2 = measurement.X2 + perpX,
                Y2 = measurement.Y2 + perpY,
                Stroke = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                StrokeThickness = 1
            });

            if (measurement.ShowLength || !string.IsNullOrEmpty(measurement.Label))
            {
                var labelText = measurement.ShowLength 
                    ? FormatLength(length, layout.Metadata.Units, layout.Metadata.PixelsPerUnit)
                    : measurement.Label;

                var label = new TextBlock
                {
                    Text = labelText,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                };

                var midX = (measurement.X1 + measurement.X2) / 2;
                var midY = (measurement.Y1 + measurement.Y2) / 2;
                Canvas.SetLeft(label, midX - 20);
                Canvas.SetTop(label, midY - perpY - 8);
                container.Children.Add(label);
            }

            return container;
        }

        private string FormatLength(double pixelLength, string units, double pixelsPerUnit)
        {
            var realLength = pixelLength / pixelsPerUnit;
            
            return units.ToLower() switch
            {
                "feet" or "ft" => $"{realLength:F1} ft",
                "inches" or "in" => $"{realLength:F1} in",
                "millimeters" or "mm" => $"{realLength:F0} mm",
                "centimeters" or "cm" => $"{realLength:F1} cm",
                _ => $"{realLength:F2} m"
            };
        }
    }
}
