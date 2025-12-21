using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    public static class TerminalHelper
    {
        // INCREASED from 10 to 18 for easier clicking
        private const double HitTestRadius = 18;

        public static Point GetNodeInputTerminal(NodeData node)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            var stickOut = RenderConstants.NodeTerminalStickOut;
            
            return node.Visual.InputTerminalPosition?.ToLower() switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => new Point(x - stickOut, y + h / 2)
            };
        }
        
        public static Point GetNodeOutputTerminal(NodeData node)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            var stickOut = RenderConstants.NodeTerminalStickOut;
            
            return node.Visual.OutputTerminalPosition?.ToLower() switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => new Point(x + w + stickOut, y + h / 2)
            };
        }

        public static bool HasInputTerminal(NodeData node) => node.Type?.ToLower() != "source";
        public static bool HasInputTerminal(string? nodeType) => nodeType?.ToLower() != "source";
        public static bool HasOutputTerminal(NodeData node) => node.Type?.ToLower() != "sink";
        public static bool HasOutputTerminal(string? nodeType) => nodeType?.ToLower() != "sink";

        public static bool HitTestInputTerminal(NodeData node, Point point)
        {
            if (!HasInputTerminal(node)) return false;
            var terminalPos = GetNodeInputTerminal(node);
            return Distance(point, terminalPos) <= HitTestRadius;
        }

        public static bool HitTestOutputTerminal(NodeData node, Point point)
        {
            if (!HasOutputTerminal(node)) return false;
            var terminalPos = GetNodeOutputTerminal(node);
            return Distance(point, terminalPos) <= HitTestRadius;
        }

        public static Point GetTerminalAtPosition(NodeData node, string position)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            var stickOut = RenderConstants.NodeTerminalStickOut;
            
            return position?.ToLower() switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                "center" => new Point(x + w / 2, y + h / 2),
                _ => new Point(x + w / 2, y + h / 2)
            };
        }

        public static Point GetNodeCenter(NodeData node) =>
            new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y + node.Visual.Height / 2);

        public static Point GetNodeEdge(NodeData node, string position)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            
            return position?.ToLower() switch
            {
                "left" => new Point(x, y + h / 2),
                "right" => new Point(x + w, y + h / 2),
                "top" => new Point(x + w / 2, y),
                "bottom" => new Point(x + w / 2, y + h),
                _ => new Point(x + w / 2, y + h / 2)
            };
        }

        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}