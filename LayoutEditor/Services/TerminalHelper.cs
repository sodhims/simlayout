using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Centralized terminal position calculations used by NodeRenderer, PathRenderer, HitTestService, and GroupRenderer
    /// </summary>
    public static class TerminalHelper
    {
        /// <summary>
        /// Get input terminal position for a node (left side, at vertical center)
        /// </summary>
        public static Point GetNodeInputTerminal(NodeData node)
        {
            var centerY = node.Visual.Height / 2.0;
            return new Point(
                node.Visual.X - RenderConstants.NodeTerminalStickOut,
                node.Visual.Y + centerY);
        }
        
        /// <summary>
        /// Get output terminal position for a node (right side, at vertical center)
        /// </summary>
        public static Point GetNodeOutputTerminal(NodeData node)
        {
            var centerY = node.Visual.Height / 2.0;
            return new Point(
                node.Visual.X + node.Visual.Width + RenderConstants.NodeTerminalStickOut,
                node.Visual.Y + centerY);
        }
        
        /// <summary>
        /// Get input terminal position within a node's local canvas (for rendering)
        /// </summary>
        public static Point GetLocalInputTerminal(double nodeHeight)
        {
            var centerY = nodeHeight / 2.0;
            return new Point(0, centerY);
        }
        
        /// <summary>
        /// Get output terminal position within a node's local canvas (for rendering)
        /// </summary>
        public static Point GetLocalOutputTerminal(double nodeWidth, double nodeHeight)
        {
            var centerY = nodeHeight / 2.0;
            return new Point(RenderConstants.NodeTerminalStickOut * 2 + nodeWidth, centerY);
        }
        
        /// <summary>
        /// Get the edge point (where stem meets node border) for input terminal
        /// </summary>
        public static Point GetLocalInputEdge(double nodeHeight)
        {
            var centerY = nodeHeight / 2.0;
            return new Point(RenderConstants.NodeTerminalStickOut, centerY);
        }
        
        /// <summary>
        /// Get the edge point (where stem meets node border) for output terminal
        /// </summary>
        public static Point GetLocalOutputEdge(double nodeWidth, double nodeHeight)
        {
            var centerY = nodeHeight / 2.0;
            return new Point(RenderConstants.NodeTerminalStickOut + nodeWidth, centerY);
        }
        
        /// <summary>
        /// Check if a point is within hit range of a node's input terminal
        /// </summary>
        public static bool HitTestInputTerminal(NodeData node, Point point)
        {
            var terminalPos = GetNodeInputTerminal(node);
            return Distance(point, terminalPos) < RenderConstants.TerminalHitRadius;
        }
        
        /// <summary>
        /// Check if a point is within hit range of a node's output terminal
        /// </summary>
        public static bool HitTestOutputTerminal(NodeData node, Point point)
        {
            var terminalPos = GetNodeOutputTerminal(node);
            return Distance(point, terminalPos) < RenderConstants.TerminalHitRadius;
        }
        
        /// <summary>
        /// Determine if a node type has an input terminal
        /// </summary>
        public static bool HasInputTerminal(string? nodeType)
        {
            return nodeType?.ToLower() != "source";
        }
        
        /// <summary>
        /// Determine if a node type has an output terminal
        /// </summary>
        public static bool HasOutputTerminal(string? nodeType)
        {
            return nodeType?.ToLower() != "sink";
        }
        
        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
