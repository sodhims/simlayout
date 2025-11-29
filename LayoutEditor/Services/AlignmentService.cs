using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Provides alignment and distribution operations for nodes and groups
    /// </summary>
    public class AlignmentService
    {
        /// <summary>
        /// Represents an alignable unit - either a single node or a group of nodes
        /// </summary>
        private class AlignUnit
        {
            public List<NodeData> Nodes { get; } = new();
            public double X => Nodes.Min(n => n.Visual.X);
            public double Y => Nodes.Min(n => n.Visual.Y);
            public double Right => Nodes.Max(n => n.Visual.X + n.Visual.Width);
            public double Bottom => Nodes.Max(n => n.Visual.Y + n.Visual.Height);
            public double Width => Right - X;
            public double Height => Bottom - Y;
            public double CenterX => X + Width / 2;
            public double CenterY => Y + Height / 2;

            public void MoveBy(double dx, double dy)
            {
                foreach (var node in Nodes)
                {
                    node.Visual.X += dx;
                    node.Visual.Y += dy;
                }
            }

            public void MoveTo(double x, double y)
            {
                var dx = x - X;
                var dy = y - Y;
                MoveBy(dx, dy);
            }
        }

        private List<AlignUnit> GetAlignUnits(List<NodeData> nodes, LayoutData layout)
        {
            var units = new List<AlignUnit>();
            var processedIds = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (processedIds.Contains(node.Id)) continue;

                // Check if node is in a group
                var group = layout.Groups.FirstOrDefault(g => g.Members.Contains(node.Id));
                
                if (group != null)
                {
                    // Create unit for entire group
                    var unit = new AlignUnit();
                    foreach (var memberId in group.Members)
                    {
                        var memberNode = nodes.FirstOrDefault(n => n.Id == memberId);
                        if (memberNode != null)
                        {
                            unit.Nodes.Add(memberNode);
                            processedIds.Add(memberId);
                        }
                    }
                    if (unit.Nodes.Count > 0)
                        units.Add(unit);
                }
                else
                {
                    // Single node unit
                    var unit = new AlignUnit();
                    unit.Nodes.Add(node);
                    processedIds.Add(node.Id);
                    units.Add(unit);
                }
            }

            return units;
        }

        public void AlignLeft(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 2) return;
            
            if (layout == null)
            {
                // Simple mode - no group awareness
                var minX = nodes.Min(n => n.Visual.X);
                foreach (var node in nodes)
                    node.Visual.X = minX;
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 2) return;
                var minX = units.Min(u => u.X);
                foreach (var unit in units)
                    unit.MoveTo(minX, unit.Y);
            }
        }

        public void AlignRight(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 2) return;
            
            if (layout == null)
            {
                var maxRight = nodes.Max(n => n.Visual.X + n.Visual.Width);
                foreach (var node in nodes)
                    node.Visual.X = maxRight - node.Visual.Width;
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 2) return;
                var maxRight = units.Max(u => u.Right);
                foreach (var unit in units)
                    unit.MoveTo(maxRight - unit.Width, unit.Y);
            }
        }

        public void AlignTop(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 2) return;
            
            if (layout == null)
            {
                var minY = nodes.Min(n => n.Visual.Y);
                foreach (var node in nodes)
                    node.Visual.Y = minY;
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 2) return;
                var minY = units.Min(u => u.Y);
                foreach (var unit in units)
                    unit.MoveTo(unit.X, minY);
            }
        }

        public void AlignBottom(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 2) return;
            
            if (layout == null)
            {
                var maxBottom = nodes.Max(n => n.Visual.Y + n.Visual.Height);
                foreach (var node in nodes)
                    node.Visual.Y = maxBottom - node.Visual.Height;
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 2) return;
                var maxBottom = units.Max(u => u.Bottom);
                foreach (var unit in units)
                    unit.MoveTo(unit.X, maxBottom - unit.Height);
            }
        }

        public void AlignCenterHorizontal(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 2) return;
            
            if (layout == null)
            {
                var avgCenterX = nodes.Average(n => n.Visual.X + n.Visual.Width / 2);
                foreach (var node in nodes)
                    node.Visual.X = avgCenterX - node.Visual.Width / 2;
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 2) return;
                var avgCenterX = units.Average(u => u.CenterX);
                foreach (var unit in units)
                    unit.MoveTo(avgCenterX - unit.Width / 2, unit.Y);
            }
        }

        public void AlignCenterVertical(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 2) return;
            
            if (layout == null)
            {
                var avgCenterY = nodes.Average(n => n.Visual.Y + n.Visual.Height / 2);
                foreach (var node in nodes)
                    node.Visual.Y = avgCenterY - node.Visual.Height / 2;
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 2) return;
                var avgCenterY = units.Average(u => u.CenterY);
                foreach (var unit in units)
                    unit.MoveTo(unit.X, avgCenterY - unit.Height / 2);
            }
        }

        public void DistributeHorizontally(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 3) return;

            if (layout == null)
            {
                var sorted = nodes.OrderBy(n => n.Visual.X).ToList();
                var totalWidth = sorted.Sum(n => n.Visual.Width);
                var minX = sorted.First().Visual.X;
                var maxRight = sorted.Last().Visual.X + sorted.Last().Visual.Width;
                var totalSpace = maxRight - minX - totalWidth;
                var spacing = totalSpace / (nodes.Count - 1);

                var currentX = minX;
                foreach (var node in sorted)
                {
                    node.Visual.X = currentX;
                    currentX += node.Visual.Width + spacing;
                }
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 3) return;
                
                var sorted = units.OrderBy(u => u.X).ToList();
                var totalWidth = sorted.Sum(u => u.Width);
                var minX = sorted.First().X;
                var maxRight = sorted.Last().Right;
                var totalSpace = maxRight - minX - totalWidth;
                var spacing = totalSpace / (units.Count - 1);

                var currentX = minX;
                foreach (var unit in sorted)
                {
                    unit.MoveTo(currentX, unit.Y);
                    currentX += unit.Width + spacing;
                }
            }
        }

        public void DistributeVertically(List<NodeData> nodes, LayoutData? layout = null)
        {
            if (nodes.Count < 3) return;

            if (layout == null)
            {
                var sorted = nodes.OrderBy(n => n.Visual.Y).ToList();
                var totalHeight = sorted.Sum(n => n.Visual.Height);
                var minY = sorted.First().Visual.Y;
                var maxBottom = sorted.Last().Visual.Y + sorted.Last().Visual.Height;
                var totalSpace = maxBottom - minY - totalHeight;
                var spacing = totalSpace / (nodes.Count - 1);

                var currentY = minY;
                foreach (var node in sorted)
                {
                    node.Visual.Y = currentY;
                    currentY += node.Visual.Height + spacing;
                }
            }
            else
            {
                var units = GetAlignUnits(nodes, layout);
                if (units.Count < 3) return;
                
                var sorted = units.OrderBy(u => u.Y).ToList();
                var totalHeight = sorted.Sum(u => u.Height);
                var minY = sorted.First().Y;
                var maxBottom = sorted.Last().Bottom;
                var totalSpace = maxBottom - minY - totalHeight;
                var spacing = totalSpace / (units.Count - 1);

                var currentY = minY;
                foreach (var unit in sorted)
                {
                    unit.MoveTo(unit.X, currentY);
                    currentY += unit.Height + spacing;
                }
            }
        }

        public void MatchWidth(List<NodeData> nodes)
        {
            if (nodes.Count < 2) return;
            var targetWidth = nodes.First().Visual.Width;
            foreach (var node in nodes.Skip(1))
                node.Visual.Width = targetWidth;
        }

        public void MatchHeight(List<NodeData> nodes)
        {
            if (nodes.Count < 2) return;
            var targetHeight = nodes.First().Visual.Height;
            foreach (var node in nodes.Skip(1))
                node.Visual.Height = targetHeight;
        }

        public void MatchSize(List<NodeData> nodes)
        {
            if (nodes.Count < 2) return;
            var first = nodes.First();
            foreach (var node in nodes.Skip(1))
            {
                node.Visual.Width = first.Visual.Width;
                node.Visual.Height = first.Visual.Height;
            }
        }
    }
}
