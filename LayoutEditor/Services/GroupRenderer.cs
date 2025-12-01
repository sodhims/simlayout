using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    public class GroupRenderer
    {
        private readonly SelectionService _selection;
        private const double TerminalRadius = 8;      // Circle radius
        private const double TerminalStickOut = 12;   // How far terminal sticks out

        public GroupRenderer(SelectionService selection)
        {
            _selection = selection;
        }

        public void DrawGroups(Canvas canvas, LayoutData layout)
        {
            foreach (var group in layout.Groups)
            {
                if (group.Members.Count == 0) continue;
                DrawGroup(canvas, layout, group);
            }
        }

        public void DrawZones(Canvas canvas, LayoutData layout)
        {
            foreach (var zone in layout.Zones)
                DrawZone(canvas, zone);
        }

        private void DrawGroup(Canvas canvas, LayoutData layout, GroupData group)
        {
            var memberNodes = group.Members
                .Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null).ToList();

            if (memberNodes.Count == 0) return;

            var bounds = CalculateBounds(memberNodes!);
            var isSelected = _selection.IsGroupSelected(group.Id);
            var isEditing = _selection.EditingCellId == group.Id;

            // Determine colors and style based on type
            Color borderColor;
            bool useDash;
            
            if (isEditing)
            {
                borderColor = Colors.Green;
                useDash = false;
            }
            else if (isSelected)
            {
                borderColor = Colors.DodgerBlue;
                useDash = false;
            }
            else if (group.IsCell)
            {
                borderColor = Colors.DarkOrange;
                useDash = false;  // SOLID for cells
            }
            else
            {
                borderColor = Colors.Gray;
                useDash = true;   // DASHED for groups
            }

            var rect = new Rectangle
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Stroke = new SolidColorBrush(borderColor),
                StrokeThickness = isSelected || isEditing ? 3 : (group.IsCell ? 2 : 1),
                Fill = isSelected 
                    ? new SolidColorBrush(Color.FromArgb(40, 30, 144, 255))
                    : new SolidColorBrush(Color.FromArgb(15, 128, 128, 128)),
                RadiusX = 8,
                RadiusY = 8,
                Tag = $"group:{group.Id}"
            };

            if (useDash)
                rect.StrokeDashArray = new DoubleCollection { 6, 3 };

            Canvas.SetLeft(rect, bounds.X);
            Canvas.SetTop(rect, bounds.Y);
            canvas.Children.Add(rect);

            // Selection handles
            if (isSelected && group.IsCell)
                DrawSelectionHandles(canvas, bounds);

            // Label
            var labelText = group.IsCell ? $"⬤ {group.Name}" : group.Name;
            if (isEditing) labelText = $"✏ {group.Name} (editing)";
            
            var label = new TextBlock
            {
                Text = labelText,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(borderColor)
            };
            Canvas.SetLeft(label, bounds.X + 4);
            Canvas.SetTop(label, bounds.Y - 18);
            canvas.Children.Add(label);

            // Draw protruding terminals for cells
            if (group.IsCell)
                DrawCellTerminals(canvas, bounds, group, layout);
        }

        private void DrawCellTerminals(Canvas canvas, Rect bounds, GroupData group, LayoutData layout)
        {
            // Input terminal (green) - sticks OUT from the cell
            var inEdge = GetEdgePoint(bounds, group.InputTerminalPosition);
            var inOuter = GetOuterPoint(bounds, group.InputTerminalPosition, TerminalStickOut);
            DrawProtrudingTerminal(canvas, inEdge, inOuter, Colors.ForestGreen, "IN", $"cell-in:{group.Id}");
            
            // Output terminal (red) - sticks OUT from the cell  
            var outEdge = GetEdgePoint(bounds, group.OutputTerminalPosition);
            var outOuter = GetOuterPoint(bounds, group.OutputTerminalPosition, TerminalStickOut);
            DrawProtrudingTerminal(canvas, outEdge, outOuter, Colors.Crimson, "OUT", $"cell-out:{group.Id}");

            // Draw connection lines to entry/exit nodes
            DrawTerminalConnections(canvas, layout, group, inOuter, outOuter);
        }

        private void DrawProtrudingTerminal(Canvas canvas, Point edgePoint, Point outerPoint, Color color, string label, string tag)
        {
            // Draw the stem line from edge to terminal
            var stem = new Line
            {
                X1 = edgePoint.X,
                Y1 = edgePoint.Y,
                X2 = outerPoint.X,
                Y2 = outerPoint.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3
            };
            canvas.Children.Add(stem);

            // Draw the terminal circle at the outer point - NO arrows, just plain circle
            var terminal = new Ellipse
            {
                Width = TerminalRadius * 2,
                Height = TerminalRadius * 2,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Tag = tag,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Canvas.SetLeft(terminal, outerPoint.X - TerminalRadius);
            Canvas.SetTop(terminal, outerPoint.Y - TerminalRadius);
            canvas.Children.Add(terminal);
        }

        private Point GetEdgePoint(Rect bounds, string position)
        {
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.X + bounds.Width / 2, bounds.Y),
                "bottom" => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height),
                "right" => new Point(bounds.X + bounds.Width, bounds.Y + bounds.Height / 2),
                _ => new Point(bounds.X, bounds.Y + bounds.Height / 2)
            };
        }

        private Point GetOuterPoint(Rect bounds, string position, double offset)
        {
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.X + bounds.Width / 2, bounds.Y - offset),
                "bottom" => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height + offset),
                "right" => new Point(bounds.X + bounds.Width + offset, bounds.Y + bounds.Height / 2),
                _ => new Point(bounds.X - offset, bounds.Y + bounds.Height / 2)
            };
        }

        private void DrawTerminalConnections(Canvas canvas, LayoutData layout, GroupData group, Point cellInPos, Point cellOutPos)
        {
            var dashArray = new DoubleCollection { 4, 2 };

            // Connect cell input terminal to entry node's INPUT terminal
            foreach (var entryId in group.EntryPoints)
            {
                var node = layout.Nodes.FirstOrDefault(n => n.Id == entryId);
                if (node == null) continue;
                
                // Node's INPUT terminal position (left side at vertical center)
                var nodeCenterY = node.Visual.Height / 2.0;
                var nodeInputTerminal = new Point(node.Visual.X - 10, node.Visual.Y + nodeCenterY);
                
                canvas.Children.Add(new Line
                {
                    X1 = cellInPos.X, Y1 = cellInPos.Y, 
                    X2 = nodeInputTerminal.X, Y2 = nodeInputTerminal.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(150, 34, 139, 34)),
                    StrokeThickness = 2, StrokeDashArray = dashArray
                });
            }

            // Connect exit node's OUTPUT terminal to cell output terminal
            foreach (var exitId in group.ExitPoints)
            {
                var node = layout.Nodes.FirstOrDefault(n => n.Id == exitId);
                if (node == null) continue;
                
                // Node's OUTPUT terminal position (right side at vertical center)
                var nodeCenterY = node.Visual.Height / 2.0;
                var nodeOutputTerminal = new Point(node.Visual.X + node.Visual.Width + 10, node.Visual.Y + nodeCenterY);
                
                canvas.Children.Add(new Line
                {
                    X1 = nodeOutputTerminal.X, Y1 = nodeOutputTerminal.Y, 
                    X2 = cellOutPos.X, Y2 = cellOutPos.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(150, 220, 20, 60)),
                    StrokeThickness = 2, StrokeDashArray = dashArray
                });
            }
        }

        private void DrawZone(Canvas canvas, ZoneData zone)
        {
            var rect = new Rectangle
            {
                Width = zone.Width, Height = zone.Height,
                Fill = GetZoneBrush(zone),
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                RadiusX = 4, RadiusY = 4,
                Tag = $"zone:{zone.Id}"
            };
            Canvas.SetLeft(rect, zone.X);
            Canvas.SetTop(rect, zone.Y);
            canvas.Children.Add(rect);

            if (!string.IsNullOrEmpty(zone.Name))
            {
                var label = new TextBlock { Text = zone.Name, FontSize = 10, Foreground = Brushes.DimGray };
                Canvas.SetLeft(label, zone.X + 4);
                Canvas.SetTop(label, zone.Y + 4);
                canvas.Children.Add(label);
            }
        }

        private Rect CalculateBounds(System.Collections.Generic.List<NodeData> nodes)
        {
            var padding = 15.0;
            var minX = nodes.Min(n => n.Visual.X) - padding;
            var minY = nodes.Min(n => n.Visual.Y) - padding;
            var maxX = nodes.Max(n => n.Visual.X + n.Visual.Width) + padding;
            var maxY = nodes.Max(n => n.Visual.Y + n.Visual.Height) + padding;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private Brush GetZoneBrush(ZoneData zone)
        {
            var color = zone.Type?.ToLower() switch
            {
                "restricted" => Color.FromArgb(30, 255, 0, 0),
                "safety" => Color.FromArgb(30, 255, 255, 0),
                "storage" => Color.FromArgb(30, 0, 0, 255),
                "maintenance" => Color.FromArgb(30, 255, 165, 0),
                _ => Color.FromArgb(20, 128, 128, 128)
            };
            return new SolidColorBrush(color);
        }

        private void DrawSelectionHandles(Canvas canvas, Rect bounds)
        {
            const double handleSize = 8;
            var corners = new[] {
                new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top),
                new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom)
            };

            foreach (var corner in corners)
            {
                var handle = new Rectangle
                {
                    Width = handleSize, Height = handleSize,
                    Fill = Brushes.White, Stroke = Brushes.DodgerBlue, StrokeThickness = 2
                };
                Canvas.SetLeft(handle, corner.X - handleSize / 2);
                Canvas.SetTop(handle, corner.Y - handleSize / 2);
                canvas.Children.Add(handle);
            }
        }

        public Rect? GetGroupBounds(LayoutData layout, string groupId)
        {
            var group = layout.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null || group.Members.Count == 0) return null;
            var memberNodes = group.Members
                .Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null).ToList();
            if (memberNodes.Count == 0) return null;
            return CalculateBounds(memberNodes!);
        }
    }
}
