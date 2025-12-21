using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for snapping positions to grid or other elements
    /// </summary>
    public class SnappingService
    {
        private double _gridSize = 10.0;
        private double _snapThreshold = 8.0; // pixels
        private bool _gridSnapEnabled = true;
        private bool _elementSnapEnabled = true;

        /// <summary>
        /// Grid size for snapping
        /// </summary>
        public double GridSize
        {
            get => _gridSize;
            set => _gridSize = Math.Max(1, value);
        }

        /// <summary>
        /// Distance threshold for element snapping
        /// </summary>
        public double SnapThreshold
        {
            get => _snapThreshold;
            set => _snapThreshold = Math.Max(1, value);
        }

        /// <summary>
        /// Enable/disable grid snapping
        /// </summary>
        public bool GridSnapEnabled
        {
            get => _gridSnapEnabled;
            set => _gridSnapEnabled = value;
        }

        /// <summary>
        /// Enable/disable element snapping
        /// </summary>
        public bool ElementSnapEnabled
        {
            get => _elementSnapEnabled;
            set => _elementSnapEnabled = value;
        }

        /// <summary>
        /// Snaps a point to the grid
        /// </summary>
        public Point SnapToGrid(Point point)
        {
            if (!_gridSnapEnabled)
                return point;

            return new Point(
                Math.Round(point.X / _gridSize) * _gridSize,
                Math.Round(point.Y / _gridSize) * _gridSize
            );
        }

        /// <summary>
        /// Snaps a value to the grid
        /// </summary>
        public double SnapToGrid(double value)
        {
            if (!_gridSnapEnabled)
                return value;

            return Math.Round(value / _gridSize) * _gridSize;
        }

        /// <summary>
        /// Snaps a point to nearby elements
        /// Returns snapped point and list of guide lines
        /// </summary>
        public (Point snappedPoint, List<GuideLine> guides) SnapToElements(
            Point point,
            LayoutData layout,
            string excludeNodeId = null)
        {
            if (!_elementSnapEnabled || layout == null)
                return (point, new List<GuideLine>());

            var guides = new List<GuideLine>();
            var snappedX = point.X;
            var snappedY = point.Y;
            var snappedToX = false;
            var snappedToY = false;

            // Check all nodes for snapping opportunities
            foreach (var node in layout.Nodes)
            {
                if (node.Id == excludeNodeId)
                    continue;

                var nodeLeft = node.Visual.X;
                var nodeRight = node.Visual.X + node.Visual.Width;
                var nodeTop = node.Visual.Y;
                var nodeBottom = node.Visual.Y + node.Visual.Height;
                var nodeCenterX = node.Visual.X + node.Visual.Width / 2;
                var nodeCenterY = node.Visual.Y + node.Visual.Height / 2;

                // Snap to left edge
                if (!snappedToX && Math.Abs(point.X - nodeLeft) < _snapThreshold)
                {
                    snappedX = nodeLeft;
                    snappedToX = true;
                    guides.Add(new GuideLine { X = nodeLeft, IsVertical = true });
                }

                // Snap to right edge
                if (!snappedToX && Math.Abs(point.X - nodeRight) < _snapThreshold)
                {
                    snappedX = nodeRight;
                    snappedToX = true;
                    guides.Add(new GuideLine { X = nodeRight, IsVertical = true });
                }

                // Snap to center X
                if (!snappedToX && Math.Abs(point.X - nodeCenterX) < _snapThreshold)
                {
                    snappedX = nodeCenterX;
                    snappedToX = true;
                    guides.Add(new GuideLine { X = nodeCenterX, IsVertical = true });
                }

                // Snap to top edge
                if (!snappedToY && Math.Abs(point.Y - nodeTop) < _snapThreshold)
                {
                    snappedY = nodeTop;
                    snappedToY = true;
                    guides.Add(new GuideLine { Y = nodeTop, IsVertical = false });
                }

                // Snap to bottom edge
                if (!snappedToY && Math.Abs(point.Y - nodeBottom) < _snapThreshold)
                {
                    snappedY = nodeBottom;
                    snappedToY = true;
                    guides.Add(new GuideLine { Y = nodeBottom, IsVertical = false });
                }

                // Snap to center Y
                if (!snappedToY && Math.Abs(point.Y - nodeCenterY) < _snapThreshold)
                {
                    snappedY = nodeCenterY;
                    snappedToY = true;
                    guides.Add(new GuideLine { Y = nodeCenterY, IsVertical = false });
                }

                if (snappedToX && snappedToY)
                    break;
            }

            return (new Point(snappedX, snappedY), guides);
        }

        /// <summary>
        /// Snaps with combined grid and element snapping
        /// </summary>
        public (Point snappedPoint, List<GuideLine> guides) Snap(
            Point point,
            LayoutData layout,
            string excludeNodeId = null)
        {
            // Try element snapping first (higher priority)
            var (elementSnapped, guides) = SnapToElements(point, layout, excludeNodeId);

            // If no element snap occurred, try grid snap
            if (guides.Count == 0)
            {
                return (SnapToGrid(point), guides);
            }

            return (elementSnapped, guides);
        }

        /// <summary>
        /// Calculates spacing between elements
        /// </summary>
        public List<SpacingGuide> CalculateSpacing(LayoutData layout, Rect bounds)
        {
            var spacingGuides = new List<SpacingGuide>();

            if (layout == null)
                return spacingGuides;

            // Find horizontal spacing patterns
            var nodesSortedByX = layout.Nodes
                .OrderBy(n => n.Visual.X)
                .ToList();

            for (int i = 0; i < nodesSortedByX.Count - 1; i++)
            {
                var node1 = nodesSortedByX[i];
                var node2 = nodesSortedByX[i + 1];

                var gap = node2.Visual.X - (node1.Visual.X + node1.Visual.Width);
                if (gap > 0)
                {
                    spacingGuides.Add(new SpacingGuide
                    {
                        Start = node1.Visual.X + node1.Visual.Width,
                        End = node2.Visual.X,
                        IsHorizontal = true,
                        Spacing = gap
                    });
                }
            }

            return spacingGuides;
        }
    }

    /// <summary>
    /// Represents a guide line for visual feedback
    /// </summary>
    public class GuideLine
    {
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsVertical { get; set; }
    }

    /// <summary>
    /// Represents a spacing guide
    /// </summary>
    public class SpacingGuide
    {
        public double Start { get; set; }
        public double End { get; set; }
        public bool IsHorizontal { get; set; }
        public double Spacing { get; set; }
    }
}
