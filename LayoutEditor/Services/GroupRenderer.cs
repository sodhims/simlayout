using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders groups, cells, and zones on the canvas
    /// </summary>
    public class GroupRenderer
    {
        private readonly SelectionService _selection;
        private const double TerminalSize = 12;

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
            {
                DrawZone(canvas, zone);
            }
        }

        private void DrawGroup(Canvas canvas, LayoutData layout, GroupData group)
        {
            var memberNodes = group.Members
                .Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null)
                .ToList();

            if (memberNodes.Count == 0) return;

            var bounds = CalculateBounds(memberNodes!);
            var isSelected = _selection.IsGroupSelected(group.Id);
            var isEditing = _selection.EditingCellId == group.Id;
            var isMultiSelected = _selection.SelectedGroupIds.Count > 1 && isSelected;

            var rect = new Rectangle
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Stroke = group.IsCell 
                    ? new SolidColorBrush(Colors.DarkOrange)
                    : new SolidColorBrush(Colors.DarkGray),
                StrokeThickness = isSelected || isEditing ? 3 : 1,
                Fill = isSelected 
                    ? new SolidColorBrush(Color.FromArgb(40, 30, 144, 255))  // More visible blue tint when selected
                    : new SolidColorBrush(Color.FromArgb(15, 128, 128, 128)),
                RadiusX = 8,
                RadiusY = 8,
                Tag = $"group:{group.Id}"
            };

            if (isEditing)
            {
                rect.Stroke = new SolidColorBrush(Colors.Green);
                rect.StrokeDashArray = null; // Solid line when editing
            }
            else if (isSelected)
            {
                rect.Stroke = new SolidColorBrush(Colors.DodgerBlue);
                rect.StrokeDashArray = null; // Solid line when selected (more visible)
                rect.StrokeThickness = 3;
            }
            else
            {
                rect.StrokeDashArray = new DoubleCollection { 6, 3 };
            }

            Canvas.SetLeft(rect, bounds.X);
            Canvas.SetTop(rect, bounds.Y);
            canvas.Children.Add(rect);

            // Add corner handles for selected cells (visual indicator for multi-select)
            if (isSelected && group.IsCell)
            {
                DrawSelectionHandles(canvas, bounds);
            }

            // Group label
            var labelText = group.IsCell ? $"⬤ {group.Name}" : group.Name;
            if (isEditing) labelText = $"✏ {group.Name} (editing)";
            if (isMultiSelected) labelText = $"☑ {group.Name}";
            
            var label = new TextBlock
            {
                Text = labelText,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = isEditing 
                    ? new SolidColorBrush(Colors.Green)
                    : (group.IsCell 
                        ? new SolidColorBrush(Colors.DarkOrange)
                        : new SolidColorBrush(Colors.DarkGray))
            };
            Canvas.SetLeft(label, bounds.X + 4);
            Canvas.SetTop(label, bounds.Y - 18);
            canvas.Children.Add(label);

            // Draw I/O terminals for cells only
            if (group.IsCell)
            {
                DrawCellTerminals(canvas, bounds, group, layout);
            }
        }

        private void DrawCellTerminals(Canvas canvas, Rect bounds, GroupData group, LayoutData layout)
        {
            // Input terminal - green
            var inPos = GetTerminalPosition(bounds, group.InputTerminalPosition);
            AddTerminal(canvas, inPos.X - TerminalSize/2, inPos.Y - TerminalSize/2, Colors.ForestGreen, "IN");
            
            // Output terminal - red
            var outPos = GetTerminalPosition(bounds, group.OutputTerminalPosition);
            AddTerminal(canvas, outPos.X - TerminalSize/2, outPos.Y - TerminalSize/2, Colors.Crimson, "OUT");

            // Draw connection lines from terminal to entry/exit nodes
            DrawTerminalConnections(canvas, layout, group, inPos, outPos);
        }

        private void DrawTerminalConnections(Canvas canvas, LayoutData layout, GroupData group, Point inPos, Point outPos)
        {
            var dashArray = new DoubleCollection { 4, 2 };

            // Draw line from input terminal to each entry point
            foreach (var entryId in group.EntryPoints)
            {
                var node = layout.Nodes.FirstOrDefault(n => n.Id == entryId);
                if (node == null) continue;

                var nodeCenter = new Point(
                    node.Visual.X + node.Visual.Width / 2,
                    node.Visual.Y + node.Visual.Height / 2);

                var line = new Line
                {
                    X1 = inPos.X,
                    Y1 = inPos.Y,
                    X2 = nodeCenter.X,
                    Y2 = nodeCenter.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 34, 139, 34)), // ForestGreen with alpha
                    StrokeThickness = 1.5,
                    StrokeDashArray = dashArray
                };
                canvas.Children.Add(line);
            }

            // Draw line from each exit point to output terminal
            foreach (var exitId in group.ExitPoints)
            {
                var node = layout.Nodes.FirstOrDefault(n => n.Id == exitId);
                if (node == null) continue;

                var nodeCenter = new Point(
                    node.Visual.X + node.Visual.Width / 2,
                    node.Visual.Y + node.Visual.Height / 2);

                var line = new Line
                {
                    X1 = nodeCenter.X,
                    Y1 = nodeCenter.Y,
                    X2 = outPos.X,
                    Y2 = outPos.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 220, 20, 60)), // Crimson with alpha
                    StrokeThickness = 1.5,
                    StrokeDashArray = dashArray
                };
                canvas.Children.Add(line);
            }
        }

        private Point GetTerminalPosition(Rect bounds, string position)
        {
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.X + bounds.Width / 2, bounds.Y),
                "bottom" => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height),
                "right" => new Point(bounds.X + bounds.Width, bounds.Y + bounds.Height / 2),
                _ => new Point(bounds.X, bounds.Y + bounds.Height / 2) // left default
            };
        }

        private void AddTerminal(Canvas canvas, double x, double y, Color color, string label)
        {
            var terminal = new Ellipse
            {
                Width = TerminalSize,
                Height = TerminalSize,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };
            Canvas.SetLeft(terminal, x);
            Canvas.SetTop(terminal, y);
            canvas.Children.Add(terminal);

            // Small label
            var text = new TextBlock
            {
                Text = label,
                FontSize = 7,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            Canvas.SetLeft(text, x + (label == "IN" ? 1 : 0));
            Canvas.SetTop(text, y + 2);
            canvas.Children.Add(text);
        }

        private void DrawZone(Canvas canvas, ZoneData zone)
        {
            var rect = new Rectangle
            {
                Width = zone.Width,
                Height = zone.Height,
                Fill = GetZoneBrush(zone),
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                RadiusX = 4,
                RadiusY = 4,
                Tag = $"zone:{zone.Id}"
            };

            Canvas.SetLeft(rect, zone.X);
            Canvas.SetTop(rect, zone.Y);
            canvas.Children.Add(rect);

            if (!string.IsNullOrEmpty(zone.Name))
            {
                var label = new TextBlock
                {
                    Text = zone.Name,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.DimGray)
                };
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
            var handleBrush = new SolidColorBrush(Colors.DodgerBlue);
            var handleFill = new SolidColorBrush(Colors.White);

            // Corner positions
            var corners = new[]
            {
                new Point(bounds.Left, bounds.Top),
                new Point(bounds.Right, bounds.Top),
                new Point(bounds.Left, bounds.Bottom),
                new Point(bounds.Right, bounds.Bottom)
            };

            foreach (var corner in corners)
            {
                var handle = new Rectangle
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = handleFill,
                    Stroke = handleBrush,
                    StrokeThickness = 2
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
                .Where(n => n != null)
                .ToList();

            if (memberNodes.Count == 0) return null;
            return CalculateBounds(memberNodes!);
        }
    }
}
