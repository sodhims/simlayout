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
        public string? TerminalType { get; set; }  // "input" or "output"
    }

    public enum HitType
    {
        None,
        Node,
        NodeTerminal,
        Path,
        GroupBorder,
        CellTerminal,
        CellInterior,
        Zone,
        Canvas
    }

    public class HitTestService
    {
        public HitTestResult HitTest(LayoutData layout, Point point)
        {
            // Check node terminals first (highest priority for path connections)
            var terminalHit = HitTestNodeTerminals(layout, point);
            if (terminalHit != null)
                return terminalHit;

            // Check cell terminals
            var cellTerminalHit = HitTestCellTerminals(layout, point);
            if (cellTerminalHit != null)
                return cellTerminalHit;

            // Check nodes
            var node = HitTestNodes(layout, point);
            if (node != null)
                return new HitTestResult { Type = HitType.Node, Id = node.Id, Node = node };

            // Check group borders
            var group = HitTestGroupBorders(layout, point);
            if (group != null)
                return new HitTestResult { Type = HitType.GroupBorder, Id = group.Id, Group = group };

            // Check cell interiors
            var cell = HitTestCellInterior(layout, point);
            if (cell != null)
                return new HitTestResult { Type = HitType.CellInterior, Id = cell.Id, Group = cell };

            // Check paths
            var path = HitTestPaths(layout, point);
            if (path != null)
                return new HitTestResult { Type = HitType.Path, Id = path.Id, Path = path };

            return new HitTestResult { Type = HitType.Canvas };
        }

        private HitTestResult? HitTestNodeTerminals(LayoutData layout, Point point)
        {
            foreach (var node in layout.Nodes.Reverse<NodeData>())
            {
                // Use TerminalHelper for hit testing
                if (TerminalHelper.HasInputTerminal(node.Type) && TerminalHelper.HitTestInputTerminal(node, point))
                {
                    return new HitTestResult 
                    { 
                        Type = HitType.NodeTerminal, 
                        Id = node.Id, 
                        Node = node,
                        TerminalType = "input"
                    };
                }

                if (TerminalHelper.HasOutputTerminal(node.Type) && TerminalHelper.HitTestOutputTerminal(node, point))
                {
                    return new HitTestResult 
                    { 
                        Type = HitType.NodeTerminal, 
                        Id = node.Id, 
                        Node = node,
                        TerminalType = "output"
                    };
                }
            }
            return null;
        }

        private HitTestResult? HitTestCellTerminals(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups.Where(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds == null) continue;

                var rect = bounds.Value;
                const double terminalOffset = 12;

                // Input terminal position
                var inputPos = GetTerminalPosition(rect, group.InputTerminalPosition, terminalOffset);
                if (Distance(point, inputPos) < RenderConstants.TerminalHitRadius)
                {
                    return new HitTestResult
                    {
                        Type = HitType.CellTerminal,
                        Id = group.Id,
                        Group = group,
                        TerminalType = "input"
                    };
                }

                // Output terminal position
                var outputPos = GetTerminalPosition(rect, group.OutputTerminalPosition, terminalOffset);
                if (Distance(point, outputPos) < RenderConstants.TerminalHitRadius)
                {
                    return new HitTestResult
                    {
                        Type = HitType.CellTerminal,
                        Id = group.Id,
                        Group = group,
                        TerminalType = "output"
                    };
                }
            }
            return null;
        }

        private Point GetTerminalPosition(Rect bounds, string position, double offset)
        {
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.X + bounds.Width / 2, bounds.Y - offset),
                "bottom" => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height + offset),
                "right" => new Point(bounds.X + bounds.Width + offset, bounds.Y + bounds.Height / 2),
                _ => new Point(bounds.X - offset, bounds.Y + bounds.Height / 2)
            };
        }

        private NodeData? HitTestNodes(LayoutData layout, Point point)
        {
            foreach (var node in layout.Nodes.Reverse<NodeData>())
            {
                var rect = new Rect(
                    node.Visual.X - RenderConstants.NodeHitMargin, node.Visual.Y - RenderConstants.NodeHitMargin,
                    node.Visual.Width + RenderConstants.NodeHitMargin * 2, node.Visual.Height + RenderConstants.NodeHitMargin * 2);
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
                if (DistanceToLine(point, GetNodeCenter(fromNode), GetNodeCenter(toNode)) < RenderConstants.PathHitMargin)
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
                var outerRect = new Rect(rect.X - RenderConstants.GroupBorderThickness, rect.Y - RenderConstants.GroupBorderThickness,
                    rect.Width + RenderConstants.GroupBorderThickness * 2, rect.Height + RenderConstants.GroupBorderThickness * 2);
                var innerRect = new Rect(rect.X + RenderConstants.GroupBorderThickness, rect.Y + RenderConstants.GroupBorderThickness,
                    rect.Width - RenderConstants.GroupBorderThickness * 2, rect.Height - RenderConstants.GroupBorderThickness * 2);

                if (outerRect.Contains(point) && !innerRect.Contains(point))
                    return group;
            }
            return null;
        }

        private GroupData? HitTestCellInterior(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups.OrderByDescending(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds?.Contains(point) == true) return group;
            }
            return null;
        }

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

        private double Distance(Point a, Point b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private double DistanceToLine(Point p, Point a, Point b)
        {
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;
            if (lengthSq == 0) return Distance(p, a);
            var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            var projX = a.X + t * dx; var projY = a.Y + t * dy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }
    }
}
