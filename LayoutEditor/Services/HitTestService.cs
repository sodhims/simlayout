using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    public class HitTestResult
    {
        public HitType Type { get; set; }
        public string? Id { get; set; }
        public NodeData? Node { get; set; }
        public PathData? Path { get; set; }
        public GroupData? Group { get; set; }
    }

    public enum HitType
    {
        None,
        Node,
        Path,
        GroupBorder,
        CellInterior,
        Zone,
        Canvas
    }

    public class HitTestService
    {
        private const double NodeHitMargin = 4;
        private const double PathHitMargin = 6;
        private const double GroupBorderThickness = 12;  // Wider hit area for cell borders

        public HitTestResult HitTest(LayoutData layout, Point point)
        {
            // Check nodes first (topmost layer)
            var node = HitTestNodes(layout, point);
            if (node != null)
                return new HitTestResult { Type = HitType.Node, Id = node.Id, Node = node };

            // Check group borders
            var group = HitTestGroupBorders(layout, point);
            if (group != null)
                return new HitTestResult { Type = HitType.GroupBorder, Id = group.Id, Group = group };

            // Check cell interiors (for path drawing)
            var cell = HitTestCellInterior(layout, point);
            if (cell != null)
                return new HitTestResult { Type = HitType.CellInterior, Id = cell.Id, Group = cell };

            // Check paths
            var path = HitTestPaths(layout, point);
            if (path != null)
                return new HitTestResult { Type = HitType.Path, Id = path.Id, Path = path };

            return new HitTestResult { Type = HitType.Canvas };
        }

        private NodeData? HitTestNodes(LayoutData layout, Point point)
        {
            foreach (var node in layout.Nodes.Reverse<NodeData>())
            {
                var rect = new Rect(
                    node.Visual.X - NodeHitMargin, node.Visual.Y - NodeHitMargin,
                    node.Visual.Width + NodeHitMargin * 2, node.Visual.Height + NodeHitMargin * 2);
                if (rect.Contains(point)) return node;
            }
            return null;
        }

        private PathData? HitTestPaths(LayoutData layout, Point point)
        {
            foreach (var path in layout.Paths)
            {
                var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = layout.Nodes.FirstOrDefault(n => n.Id == path.To);
                if (fromNode == null || toNode == null) continue;
                if (DistanceToLine(point, GetNodeCenter(fromNode), GetNodeCenter(toNode)) < PathHitMargin)
                    return path;
            }
            return null;
        }

        private GroupData? HitTestGroupBorders(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups)
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds == null) continue;

                var rect = bounds.Value;
                var outerRect = new Rect(rect.X - GroupBorderThickness, rect.Y - GroupBorderThickness,
                    rect.Width + GroupBorderThickness * 2, rect.Height + GroupBorderThickness * 2);
                var innerRect = new Rect(rect.X + GroupBorderThickness, rect.Y + GroupBorderThickness,
                    rect.Width - GroupBorderThickness * 2, rect.Height - GroupBorderThickness * 2);

                if (outerRect.Contains(point) && !innerRect.Contains(point))
                    return group;
            }
            return null;
        }

        private GroupData? HitTestCellInterior(LayoutData layout, Point point)
        {
            // Check all groups - cells have priority (check first)
            foreach (var group in layout.Groups.OrderByDescending(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds?.Contains(point) == true) return group;
            }
            return null;
        }

        /// <summary>
        /// Public method to check if a point is inside any cell (for Shift+click multi-select)
        /// </summary>
        public GroupData? HitTestCellAtPoint(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups.Where(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds?.Contains(point) == true) return group;
            }
            return null;
        }

        public Rect? GetGroupBounds(LayoutData layout, GroupData group)
        {
            var memberNodes = group.Members
                .Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null).ToList();
            if (memberNodes.Count == 0) return null;

            double padding = 15.0;
            var minX = memberNodes.Min(n => n!.Visual.X) - padding;
            var minY = memberNodes.Min(n => n!.Visual.Y) - padding;
            var maxX = memberNodes.Max(n => n!.Visual.X + n!.Visual.Width) + padding;
            var maxY = memberNodes.Max(n => n!.Visual.Y + n!.Visual.Height) + padding;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private Point GetNodeCenter(NodeData node) =>
            new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y + node.Visual.Height / 2);

        private double DistanceToLine(Point p, Point a, Point b)
        {
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;
            if (lengthSq == 0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
            var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            var projX = a.X + t * dx; var projY = a.Y + t * dy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }
    }
}
