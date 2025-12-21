using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    public class PathRenderer
    {
        private readonly SelectionService _selection;
        private const double ArrowSize = 10;
        
        public bool IsEditMode { get; set; } = false;

        public PathRenderer(SelectionService selection) => _selection = selection;

        public void DrawPaths(Canvas canvas, LayoutData layout, Action<string, UIElement> register)
        {
            foreach (var path in layout.Paths)
            {
                var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = layout.Nodes.FirstOrDefault(n => n.Id == path.To);
                if (fromNode == null || toNode == null) continue;

                var element = CreatePathElement(path, fromNode, toNode, layout);
                canvas.Children.Add(element);
                register($"path:{path.Id}", element);
            }
        }

        public UIElement CreatePathElement(PathData path, NodeData fromNode, NodeData toNode, LayoutData layout)
        {
            var isSelected = _selection.IsPathSelected(path.Id);
            var showHandles = isSelected || IsEditMode;
            var brush = isSelected ? new SolidColorBrush(Colors.DodgerBlue) : GetPathBrush(path);
            var settings = EditorSettings.Instance;
            var thickness = isSelected ? settings.PathThickness + 0.5 : settings.PathThickness;

            // Check if nodes are in cells - route to cell terminals if crossing cell boundary
            var fromCell = layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(fromNode.Id));
            var toCell = layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(toNode.Id));

            Point startPoint, endPoint;
            Rect? fromCellBounds = null, toCellBounds = null;

            // If from node is in a cell and to node is NOT in same cell -> use cell output terminal
            if (fromCell != null && fromCell != toCell)
            {
                fromCellBounds = GetCellBounds(layout, fromCell);
                startPoint = GetTerminalPoint(fromCellBounds.Value, fromCell.OutputTerminalPosition);
            }
            else
            {
                // FROM node always uses OUTPUT terminal (right side)
                startPoint = TerminalHelper.GetNodeOutputTerminal(fromNode);
            }

            // If to node is in a cell and from node is NOT in same cell -> use cell input terminal
            if (toCell != null && toCell != fromCell)
            {
                toCellBounds = GetCellBounds(layout, toCell);
                endPoint = GetTerminalPoint(toCellBounds.Value, toCell.InputTerminalPosition);
            }
            else
            {
                // TO node always uses INPUT terminal (left side)
                endPoint = TerminalHelper.GetNodeInputTerminal(toNode);
            }

            // Determine terminal radius for arrow offset
            double terminalRadius = (toCell != null && toCell != fromCell) 
                ? RenderConstants.CellTerminalRadius 
                : RenderConstants.NodeTerminalRadius;

            var container = new Canvas { Tag = $"path:{path.Id}" };
            var pathGeometry = CreatePathGeometry(path, startPoint, endPoint, fromCellBounds, toCellBounds);
            container.Children.Add(new Path { Data = pathGeometry, Stroke = brush, StrokeThickness = thickness });

            // Arrow at final segment end - offset by terminal radius so tip stops at edge
            var arrowEnd = GetLastSegmentEnd(pathGeometry);
            var arrowStart = GetLastSegmentStart(pathGeometry);
            var offsetArrowEnd = OffsetPointTowardStart(arrowEnd, arrowStart, terminalRadius);
            container.Children.Add(CreateArrowHead(arrowStart, offsetArrowEnd, brush));

            // Add waypoint handles when selected or in edit mode
            if (showHandles)
            {
                // Get all points from the path geometry for proper handle placement
                var pathPoints = GetPathPoints(pathGeometry);
                AddWaypointHandles(container, path, pathPoints, isSelected);
            }

            return container;
        }

        private List<Point> GetPathPoints(PathGeometry geometry)
        {
            var points = new List<Point>();
            if (geometry.Figures.Count == 0) return points;

            var figure = geometry.Figures[0];
            points.Add(figure.StartPoint);

            foreach (var segment in figure.Segments)
            {
                if (segment is LineSegment line)
                {
                    points.Add(line.Point);
                }
                else if (segment is BezierSegment bezier)
                {
                    // For curves, just add the endpoint
                    points.Add(bezier.Point3);
                }
            }

            return points;
        }

        private void AddWaypointHandles(Canvas container, PathData path, List<Point> pathPoints, bool isSelected)
        {
            if (pathPoints.Count < 2) return;
            
            const double handleSize = 10;
            const double addHandleSize = 10;
            
            // Add handles for existing waypoints (white circles)
            for (int i = 0; i < path.Visual.Waypoints.Count; i++)
            {
                var wp = path.Visual.Waypoints[i];
                var handle = new Ellipse
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = new SolidColorBrush(Colors.White),
                    Stroke = new SolidColorBrush(Colors.DodgerBlue),
                    StrokeThickness = 2,
                    Tag = $"waypoint:{path.Id}:{i}",
                    Cursor = System.Windows.Input.Cursors.SizeAll,
                    ToolTip = "Drag to move waypoint"
                };
                Canvas.SetLeft(handle, wp.X - handleSize / 2);
                Canvas.SetTop(handle, wp.Y - handleSize / 2);
                container.Children.Add(handle);
            }

            // Add midpoint handles on EVERY segment of the actual rendered path
            // This includes Manhattan routing segments, not just start-end
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                var midPoint = new Point(
                    (pathPoints[i].X + pathPoints[i + 1].X) / 2,
                    (pathPoints[i].Y + pathPoints[i + 1].Y) / 2);

                var addHandle = new Ellipse
                {
                    Width = addHandleSize,
                    Height = addHandleSize,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 255, 165, 0)),
                    Stroke = new SolidColorBrush(Colors.DarkOrange),
                    StrokeThickness = 1.5,
                    Tag = $"addwaypoint:{path.Id}:{i}",
                    Cursor = System.Windows.Input.Cursors.Cross,
                    ToolTip = "Drag to move waypoint"
                };
                Canvas.SetLeft(addHandle, midPoint.X - addHandleSize / 2);
                Canvas.SetTop(addHandle, midPoint.Y - addHandleSize / 2);
                container.Children.Add(addHandle);
            }
        }

        private Rect GetCellBounds(LayoutData layout, GroupData cell)
        {
            var nodes = cell.Members.Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null).ToList();
            if (nodes.Count == 0) return new Rect();
            double pad = 15;
            double minX = nodes.Min(n => n!.Visual.X) - pad;
            double minY = nodes.Min(n => n!.Visual.Y) - pad;
            double maxX = nodes.Max(n => n!.Visual.X + n!.Visual.Width) + pad;
            double maxY = nodes.Max(n => n!.Visual.Y + n!.Visual.Height) + pad;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private Point GetTerminalPoint(Rect bounds, string? position)
        {
            const double offset = 12;
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.Left + bounds.Width / 2, bounds.Top - offset),
                "bottom" => new Point(bounds.Left + bounds.Width / 2, bounds.Bottom + offset),
                "left" => new Point(bounds.Left - offset, bounds.Top + bounds.Height / 2),
                _ => new Point(bounds.Right + offset, bounds.Top + bounds.Height / 2)
            };
        }

        private PathGeometry CreatePathGeometry(PathData path, Point start, Point end, Rect? fromCell, Rect? toCell)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = start };

            // If we have waypoints, route through them
            if (path.Visual.Waypoints != null && path.Visual.Waypoints.Count > 0)
            {
                var currentPoint = start;
                foreach (var wp in path.Visual.Waypoints)
                {
                    var wpPoint = new Point(wp.X, wp.Y);
                    AddRoutingSegments(figure, currentPoint, wpPoint, path.RoutingMode, null, null);
                    currentPoint = wpPoint;
                }
                // Final segment to end
                AddRoutingSegments(figure, currentPoint, end, path.RoutingMode, null, toCell);
            }
            else
            {
                // No waypoints - direct routing based on mode
                AddRoutingSegments(figure, start, end, path.RoutingMode, fromCell, toCell);
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        private void AddRoutingSegments(PathFigure figure, Point start, Point end, string? routingMode, Rect? fromCell, Rect? toCell)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;

            if (routingMode == RoutingModes.Direct)
            {
                figure.Segments.Add(new LineSegment(end, true));
                return;
            }

            // Manhattan routing - use orthogonal segments
            const double margin = 20;

            // Determine exit direction based on cell bounds or path direction
            bool exitRight = fromCell.HasValue && start.X >= fromCell.Value.Right - 1;
            bool exitLeft = fromCell.HasValue && start.X <= fromCell.Value.Left + 1;
            bool exitBottom = fromCell.HasValue && start.Y >= fromCell.Value.Bottom - 1;
            bool exitTop = fromCell.HasValue && start.Y <= fromCell.Value.Top + 1;

            bool enterLeft = toCell.HasValue && end.X <= toCell.Value.Left + 1;
            bool enterRight = toCell.HasValue && end.X >= toCell.Value.Right - 1;
            bool enterTop = toCell.HasValue && end.Y <= toCell.Value.Top + 1;
            bool enterBottom = toCell.HasValue && end.Y >= toCell.Value.Bottom + 1;

            // Special routing for cell-to-cell connections
            if (exitRight && enterLeft)
            {
                // Right to left - horizontal-vertical-horizontal
                double midX = (start.X + end.X) / 2;
                figure.Segments.Add(new LineSegment(new Point(midX, start.Y), true));
                figure.Segments.Add(new LineSegment(new Point(midX, end.Y), true));
            }
            else if (exitBottom && enterTop)
            {
                // Bottom to top - vertical-horizontal-vertical
                double midY = (start.Y + end.Y) / 2;
                figure.Segments.Add(new LineSegment(new Point(start.X, midY), true));
                figure.Segments.Add(new LineSegment(new Point(end.X, midY), true));
            }
            else if (exitRight || exitLeft)
            {
                // Horizontal exit - go horizontal first, then vertical
                double extendX = exitRight ? start.X + margin : start.X - margin;
                figure.Segments.Add(new LineSegment(new Point(extendX, start.Y), true));
                figure.Segments.Add(new LineSegment(new Point(extendX, end.Y), true));
            }
            else if (exitBottom || exitTop)
            {
                // Vertical exit - go vertical first, then horizontal  
                double extendY = exitBottom ? start.Y + margin : start.Y - margin;
                figure.Segments.Add(new LineSegment(new Point(start.X, extendY), true));
                figure.Segments.Add(new LineSegment(new Point(end.X, extendY), true));
            }
            // Standard Manhattan for non-cell paths
            else if (Math.Abs(dx) > Math.Abs(dy))
            {
                // More horizontal - L-shape, horizontal then vertical
                figure.Segments.Add(new LineSegment(new Point(end.X, start.Y), true));
            }
            else
            {
                // More vertical - L-shape, vertical then horizontal
                figure.Segments.Add(new LineSegment(new Point(start.X, end.Y), true));
            }
            
            figure.Segments.Add(new LineSegment(end, true));
        }

        /// <summary>
        /// Get the output terminal position for a node (where paths start FROM)
        /// </summary>
        public static Point GetOutputTerminal(NodeData node) => TerminalHelper.GetNodeOutputTerminal(node);
        
        /// <summary>
        /// Get the input terminal position for a node (where paths go TO)
        /// </summary>
        public static Point GetInputTerminal(NodeData node) => TerminalHelper.GetNodeInputTerminal(node);

        private Point GetLastSegmentEnd(PathGeometry geom)
        {
            if (geom.Figures.Count == 0) return new Point();
            var fig = geom.Figures[0];
            if (fig.Segments.Count == 0) return fig.StartPoint;
            var last = fig.Segments[fig.Segments.Count - 1];
            if (last is LineSegment ls) return ls.Point;
            if (last is BezierSegment bs) return bs.Point3;
            return fig.StartPoint;
        }

        private Point GetLastSegmentStart(PathGeometry geom)
        {
            if (geom.Figures.Count == 0) return new Point();
            var fig = geom.Figures[0];
            if (fig.Segments.Count <= 1) return fig.StartPoint;
            var prev = fig.Segments[fig.Segments.Count - 2];
            if (prev is LineSegment ls) return ls.Point;
            if (prev is BezierSegment bs) return bs.Point3;
            return fig.StartPoint;
        }

        /// <summary>
        /// Move a point back toward another point by a specified distance.
        /// Used to offset arrow tip so it stops at terminal edge.
        /// </summary>
        private Point OffsetPointTowardStart(Point end, Point start, double offset)
        {
            var dx = start.X - end.X;
            var dy = start.Y - end.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001) return end;  // Points are same, can't determine direction
            
            // Normalize and scale by offset
            var nx = dx / length;
            var ny = dy / length;
            return new Point(end.X + nx * offset, end.Y + ny * offset);
        }

        private Polygon CreateArrowHead(Point start, Point end, Brush brush)
        {
            var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            var sin = Math.Sin(angle); var cos = Math.Cos(angle);
            return new Polygon
            {
                Points = new PointCollection {
                    end,
                    new Point(end.X - ArrowSize*cos + ArrowSize*0.5*sin, end.Y - ArrowSize*sin - ArrowSize*0.5*cos),
                    new Point(end.X - ArrowSize*cos - ArrowSize*0.5*sin, end.Y - ArrowSize*sin + ArrowSize*0.5*cos)
                },
                Fill = brush
            };
        }

        private Brush GetPathBrush(PathData path)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(path.Visual.Color)); }
            catch { return Brushes.Gray; }
        }

        public void DrawTempPath(Canvas canvas, Point start, Point end)
        {
            canvas.Children.Add(new Line {
                X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y,
                Stroke = new SolidColorBrush(Colors.DodgerBlue), StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }, Tag = "temp_path"
            });
        }

        public void ClearTempPath(Canvas canvas)
        {
            foreach (var l in canvas.Children.OfType<Line>().Where(l => l.Tag?.ToString() == "temp_path").ToList())
                canvas.Children.Remove(l);
        }
        private PathConnectionRenderer _connectionRenderer = new();
    }
}
