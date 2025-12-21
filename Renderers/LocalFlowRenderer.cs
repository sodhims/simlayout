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
    /// Renders Local Flow layer (Layer 3): cells, conveyors, direct paths
    /// </summary>
    public class LocalFlowRenderer : ILayerRenderer
    {
        private readonly SelectionService _selection;

        public LocalFlowRenderer(SelectionService selection)
        {
            _selection = selection;
        }

        public LayerType Layer => LayerType.LocalFlow;

        public int ZOrderBase => 300; // 300-399 range for LocalFlow

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            // Render in order: cells → conveyors → direct paths
            RenderCellBoundaries(canvas, layout);
            RenderConveyors(canvas, layout, registerElement);
            RenderDirectPaths(canvas, layout, registerElement);
        }

        #region Cell Boundary Rendering

        private void RenderCellBoundaries(Canvas canvas, LayoutData layout)
        {
            var cells = layout.Groups.Where(g => g.IsCell).ToList();

            foreach (var cell in cells)
            {
                if (cell.Members.Count == 0) continue;

                // Get all member nodes
                var memberNodes = cell.Members
                    .Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                    .Where(n => n != null)
                    .ToList();

                if (memberNodes.Count == 0) continue;

                // Calculate bounding box with clearance buffer
                var bounds = CalculateCellBoundary(memberNodes, 30); // 30 unit buffer

                // Render cell boundary
                var border = new Rectangle
                {
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 100, 149, 237)), // Cornflower blue
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 8, 4 },
                    Fill = new SolidColorBrush(Color.FromArgb(10, 100, 149, 237))
                };

                Canvas.SetLeft(border, bounds.Left);
                Canvas.SetTop(border, bounds.Top);
                Panel.SetZIndex(border, ZOrderBase);
                canvas.Children.Add(border);

                // Add cell name label
                var label = new TextBlock
                {
                    Text = cell.Name,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 100, 149, 237)),
                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    Padding = new Thickness(4, 2, 4, 2)
                };

                Canvas.SetLeft(label, bounds.Left + 8);
                Canvas.SetTop(label, bounds.Top + 4);
                Panel.SetZIndex(label, ZOrderBase + 1);
                canvas.Children.Add(label);
            }
        }

        private Rect CalculateCellBoundary(System.Collections.Generic.List<NodeData?> memberNodes, double buffer)
        {
            var minX = memberNodes.Min(n => n!.Visual.X - n.Visual.OperationalClearanceLeft) - buffer;
            var minY = memberNodes.Min(n => n!.Visual.Y - n.Visual.OperationalClearanceTop) - buffer;
            var maxX = memberNodes.Max(n => n!.Visual.X + n.Visual.Width + n.Visual.OperationalClearanceRight) + buffer;
            var maxY = memberNodes.Max(n => n!.Visual.Y + n.Visual.Height + n.Visual.OperationalClearanceBottom) + buffer;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion

        #region Conveyor Rendering

        private void RenderConveyors(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var conveyor in layout.Conveyors)
            {
                if (conveyor.Path.Count < 2) continue;

                var color = (Color)ColorConverter.ConvertFromString(conveyor.Color);
                var brush = new SolidColorBrush(color);

                // Render conveyor as wide polyline
                var polyline = new Polyline
                {
                    Stroke = brush,
                    StrokeThickness = conveyor.Width,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Opacity = 0.6
                };

                foreach (var point in conveyor.Path)
                {
                    polyline.Points.Add(new Point(point.X, point.Y));
                }

                Panel.SetZIndex(polyline, ZOrderBase + 10);
                canvas.Children.Add(polyline);
                registerElement($"conveyor:{conveyor.Id}", polyline);

                // Add direction chevrons
                if (conveyor.Direction != ConveyorDirections.Bidirectional)
                {
                    RenderConveyorChevrons(canvas, conveyor);
                }

                // Add conveyor label at midpoint
                if (conveyor.Path.Count >= 2)
                {
                    var midIndex = conveyor.Path.Count / 2;
                    var midPoint = conveyor.Path[midIndex];

                    var label = new TextBlock
                    {
                        Text = conveyor.Name,
                        FontSize = 8,
                        Foreground = Brushes.Black,
                        Background = Brushes.White,
                        Padding = new Thickness(2)
                    };

                    Canvas.SetLeft(label, midPoint.X + conveyor.Width / 2 + 4);
                    Canvas.SetTop(label, midPoint.Y - 8);
                    Panel.SetZIndex(label, ZOrderBase + 20);
                    canvas.Children.Add(label);
                }
            }
        }

        private void RenderConveyorChevrons(Canvas canvas, ConveyorData conveyor)
        {
            const double chevronSpacing = 80; // Space between chevrons
            const double chevronSize = 12;

            // Calculate total path length and add chevrons
            for (int i = 0; i < conveyor.Path.Count - 1; i++)
            {
                var p1 = conveyor.Path[i];
                var p2 = conveyor.Path[i + 1];

                var dx = p2.X - p1.X;
                var dy = p2.Y - p1.Y;
                var segmentLength = Math.Sqrt(dx * dx + dy * dy);

                if (segmentLength < chevronSpacing) continue;

                // Number of chevrons in this segment
                int chevronCount = (int)(segmentLength / chevronSpacing);

                for (int c = 1; c <= chevronCount; c++)
                {
                    double t = (c * chevronSpacing) / segmentLength;
                    double cx = p1.X + dx * t;
                    double cy = p1.Y + dy * t;

                    // Calculate perpendicular direction
                    double angle = Math.Atan2(dy, dx);
                    double perpAngle = angle + Math.PI / 2;

                    // Chevron is a V shape pointing in direction of flow
                    var chevron = new Polyline
                    {
                        Stroke = Brushes.White,
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(
                                cx - Math.Cos(angle - Math.PI / 4) * chevronSize,
                                cy - Math.Sin(angle - Math.PI / 4) * chevronSize),
                            new Point(cx, cy),
                            new Point(
                                cx - Math.Cos(angle + Math.PI / 4) * chevronSize,
                                cy - Math.Sin(angle + Math.PI / 4) * chevronSize)
                        }
                    };

                    if (conveyor.Direction == ConveyorDirections.Reverse)
                    {
                        // Flip chevron for reverse direction
                        chevron.Points.Clear();
                        chevron.Points.Add(new Point(
                            cx + Math.Cos(angle - Math.PI / 4) * chevronSize,
                            cy + Math.Sin(angle - Math.PI / 4) * chevronSize));
                        chevron.Points.Add(new Point(cx, cy));
                        chevron.Points.Add(new Point(
                            cx + Math.Cos(angle + Math.PI / 4) * chevronSize,
                            cy + Math.Sin(angle + Math.PI / 4) * chevronSize));
                    }

                    Panel.SetZIndex(chevron, ZOrderBase + 15);
                    canvas.Children.Add(chevron);
                }
            }
        }

        #endregion

        #region Direct Path Rendering

        private void RenderDirectPaths(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var directPath in layout.DirectPaths)
            {
                var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == directPath.FromNodeId);
                var toNode = layout.Nodes.FirstOrDefault(n => n.Id == directPath.ToNodeId);

                if (fromNode == null || toNode == null) continue;

                // Calculate center points
                var fromCenter = new Point(
                    fromNode.Visual.X + fromNode.Visual.Width / 2,
                    fromNode.Visual.Y + fromNode.Visual.Height / 2);

                var toCenter = new Point(
                    toNode.Visual.X + toNode.Visual.Width / 2,
                    toNode.Visual.Y + toNode.Visual.Height / 2);

                var color = (Color)ColorConverter.ConvertFromString(directPath.Color);

                // Render as thin arrow
                var line = new Line
                {
                    X1 = fromCenter.X,
                    Y1 = fromCenter.Y,
                    X2 = toCenter.X,
                    Y2 = toCenter.Y,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };

                Panel.SetZIndex(line, ZOrderBase + 5);
                canvas.Children.Add(line);
                registerElement($"directpath:{directPath.Id}", line);

                // Add arrowhead
                RenderArrowhead(canvas, fromCenter, toCenter, color);

                // Add transfer type label at midpoint
                var midX = (fromCenter.X + toCenter.X) / 2;
                var midY = (fromCenter.Y + toCenter.Y) / 2;

                var label = new TextBlock
                {
                    Text = $"{directPath.TransferType} ({directPath.TransferTime}s)",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(color),
                    Background = Brushes.White,
                    Padding = new Thickness(2, 1, 2, 1)
                };

                Canvas.SetLeft(label, midX + 4);
                Canvas.SetTop(label, midY - 10);
                Panel.SetZIndex(label, ZOrderBase + 6);
                canvas.Children.Add(label);
            }
        }

        private void RenderArrowhead(Canvas canvas, Point from, Point to, Color color)
        {
            const double arrowSize = 8;

            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var angle = Math.Atan2(dy, dx);

            var arrowhead = new Polygon
            {
                Fill = new SolidColorBrush(color),
                Points = new PointCollection
                {
                    new Point(to.X, to.Y),
                    new Point(
                        to.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                        to.Y - arrowSize * Math.Sin(angle - Math.PI / 6)),
                    new Point(
                        to.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                        to.Y - arrowSize * Math.Sin(angle + Math.PI / 6))
                }
            };

            Panel.SetZIndex(arrowhead, ZOrderBase + 5);
            canvas.Children.Add(arrowhead);
        }

        #endregion
    }
}
