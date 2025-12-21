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
    /// Renders Infrastructure layer (Layer 0): walls, columns, openings, crane runways
    /// </summary>
    public class InfrastructureRenderer : ILayerRenderer
    {
        private readonly SelectionService _selection;

        public InfrastructureRenderer(SelectionService selection)
        {
            _selection = selection;
        }

        public LayerType Layer => LayerType.Infrastructure;

        public int ZOrderBase => 0; // 0-99 range for Infrastructure

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: runways → walls → columns → openings
            RenderRunways(canvas, layout, registerElement);
            RenderWalls(canvas, layout, registerElement);
            RenderColumns(canvas, layout, registerElement);
            RenderOpenings(canvas, layout, registerElement);
            RenderLegacyDoors(canvas, layout, registerElement); // Backward compatibility
        }

        private void RenderRunways(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var runway in layout.Runways)
            {
                var line = new Line
                {
                    X1 = runway.StartX,
                    Y1 = runway.StartY,
                    X2 = runway.EndX,
                    Y2 = runway.EndY,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(runway.Color)),
                    StrokeThickness = 4,
                    StrokeDashArray = new DoubleCollection { 8, 4 }
                };

                canvas.Children.Add(line);
                registerElement($"runway:{runway.Id}", line);
            }
        }

        private void RenderWalls(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var wall in layout.Walls)
            {
                var isSelected = _selection.IsWallSelected(wall.Id);
                var color = ColorConverter.ConvertFromString(wall.Color);
                var brush = new SolidColorBrush((Color)color);

                var line = new Line
                {
                    X1 = wall.X1,
                    Y1 = wall.Y1,
                    X2 = wall.X2,
                    Y2 = wall.Y2,
                    Stroke = isSelected ? Brushes.Orange : brush,
                    StrokeThickness = isSelected ? wall.Thickness + 2 : wall.Thickness
                };

                // Apply line style
                if (!string.IsNullOrEmpty(wall.DashPattern))
                {
                    var parts = wall.DashPattern.Split(',');
                    var dashArray = new DoubleCollection();
                    foreach (var part in parts)
                    {
                        if (double.TryParse(part.Trim(), out var val))
                            dashArray.Add(val);
                    }
                    if (dashArray.Count > 0)
                        line.StrokeDashArray = dashArray;
                }

                canvas.Children.Add(line);
                registerElement($"wall:{wall.Id}", line);
            }
        }

        private void RenderColumns(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var column in layout.Columns)
            {
                UIElement element;

                if (column.Shape == "round")
                {
                    var ellipse = new Ellipse
                    {
                        Width = column.Width,
                        Height = column.Height,
                        Fill = Brushes.Gray,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(ellipse, column.X - column.Width / 2);
                    Canvas.SetTop(ellipse, column.Y - column.Height / 2);
                    element = ellipse;
                }
                else // square
                {
                    var rect = new Rectangle
                    {
                        Width = column.Width,
                        Height = column.Height,
                        Fill = Brushes.Gray,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(rect, column.X - column.Width / 2);
                    Canvas.SetTop(rect, column.Y - column.Height / 2);
                    element = rect;
                }

                canvas.Children.Add(element);
                registerElement($"column:{column.Id}", element);
            }
        }

        private void RenderOpenings(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var opening in layout.Openings)
            {
                var element = CreateOpeningElement(opening);
                if (element != null)
                {
                    canvas.Children.Add(element);
                    registerElement($"opening:{opening.Id}", element);
                }
            }
        }

        private UIElement? CreateOpeningElement(OpeningData opening)
        {
            // State color coding
            var stateColor = opening.State switch
            {
                OpeningStates.Open => Colors.Green,
                OpeningStates.Closed => Colors.Red,
                OpeningStates.Locked => Colors.Gray,
                OpeningStates.Emergency => Colors.Yellow,
                _ => Colors.Green
            };

            var stateBrush = new SolidColorBrush(stateColor) { Opacity = 0.6 };

            // Create visual based on opening type
            if (opening is DoorOpening door)
            {
                return CreateDoorVisual(door, stateBrush);
            }
            else if (opening is HatchOpening hatch)
            {
                return CreateHatchVisual(hatch, stateBrush);
            }
            else if (opening is UnconstrainedOpening aisle)
            {
                return CreateAisleVisual(aisle, stateBrush);
            }
            else if (opening is GateOpening gate)
            {
                return CreateGateVisual(gate, stateBrush);
            }
            else if (opening is ConstrainedOpening constrained)
            {
                return CreateGenericOpeningVisual(constrained, stateBrush);
            }

            return null;
        }

        private UIElement CreateDoorVisual(DoorOpening door, Brush stateBrush)
        {
            var group = new Canvas();

            // Door rectangle
            var rect = new Rectangle
            {
                Width = door.Width,
                Height = door.Height,
                Fill = stateBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            // Swing arc if not sliding
            if (door.SwingDirection != SwingDirections.Sliding)
            {
                var arc = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };

                // Create arc geometry (simplified)
                var figure = new PathFigure { StartPoint = new Point(0, 0) };
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(door.Width, 0),
                    Size = new Size(door.Width, door.Width),
                    SweepDirection = SweepDirection.Clockwise
                });

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                arc.Data = geometry;

                group.Children.Add(arc);
            }

            group.Children.Add(rect);

            // Apply rotation and position
            var transform = new TransformGroup();
            transform.Children.Add(new RotateTransform(door.Rotation));
            transform.Children.Add(new TranslateTransform(door.X, door.Y));
            group.RenderTransform = transform;

            return group;
        }

        private UIElement CreateHatchVisual(HatchOpening hatch, Brush stateBrush)
        {
            var group = new Canvas();

            // Hatch square
            var rect = new Rectangle
            {
                Width = hatch.Width,
                Height = hatch.Height,
                Fill = stateBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            group.Children.Add(rect);

            // Ladder symbol (simple lines)
            var ladder = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Data = Geometry.Parse($"M {hatch.Width / 4},0 L {hatch.Width / 4},{hatch.Height} M {3 * hatch.Width / 4},0 L {3 * hatch.Width / 4},{hatch.Height}")
            };
            group.Children.Add(ladder);

            Canvas.SetLeft(group, hatch.X);
            Canvas.SetTop(group, hatch.Y);

            return group;
        }

        private UIElement CreateAisleVisual(UnconstrainedOpening aisle, Brush stateBrush)
        {
            // Aisle is just a wide gap - render as a transparent rectangle
            var rect = new Rectangle
            {
                Width = aisle.Width,
                Height = 20, // Arbitrary height for visual representation
                Fill = Brushes.Transparent,
                Stroke = stateBrush,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 }
            };

            Canvas.SetLeft(rect, aisle.X);
            Canvas.SetTop(rect, aisle.Y);

            return rect;
        }

        private UIElement CreateGateVisual(GateOpening gate, Brush stateBrush)
        {
            // Gate similar to door but larger
            var rect = new Rectangle
            {
                Width = gate.Width,
                Height = gate.Height,
                Fill = stateBrush,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 3
            };

            Canvas.SetLeft(rect, gate.X);
            Canvas.SetTop(rect, gate.Y);

            return rect;
        }

        private UIElement CreateGenericOpeningVisual(ConstrainedOpening opening, Brush stateBrush)
        {
            var rect = new Rectangle
            {
                Width = opening.Width,
                Height = opening.Height,
                Fill = stateBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Canvas.SetLeft(rect, opening.X);
            Canvas.SetTop(rect, opening.Y);

            return rect;
        }

        private void RenderLegacyDoors(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render old DoorData for backward compatibility
            foreach (var door in layout.Doors)
            {
                var wall = layout.Walls.FirstOrDefault(w => w.Id == door.WallId);
                if (wall != null)
                {
                    // Calculate door position along wall
                    var dx = wall.X2 - wall.X1;
                    var dy = wall.Y2 - wall.Y1;
                    var doorX = wall.X1 + dx * door.Position;
                    var doorY = wall.Y1 + dy * door.Position;

                    var rect = new Rectangle
                    {
                        Width = door.Width,
                        Height = 10,
                        Fill = Brushes.LightBlue,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(rect, doorX - door.Width / 2);
                    Canvas.SetTop(rect, doorY - 5);

                    canvas.Children.Add(rect);
                    registerElement($"door:{door.Id}", rect);
                }
            }
        }
    }
}
