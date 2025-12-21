using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Detects when node terminals are touching and automatically creates paths.
    /// Call DetectAndConnect() after node drag operations complete.
    /// </summary>
    public class AutoPathDetector
    {
        private readonly LayoutData _layout;
        
        /// <summary>
        /// Maximum distance between terminals to trigger auto-connect (pixels)
        /// </summary>
        public double TouchThreshold { get; set; } = 25.0;
        
        public AutoPathDetector(LayoutData layout)
        {
            _layout = layout;
        }
        
        /// <summary>
        /// Check moved nodes for touching terminals and create paths automatically.
        /// Returns number of paths created.
        /// </summary>
        public int DetectAndConnect(IEnumerable<NodeData> movedNodes)
        {
            int pathsCreated = 0;
            var movedIds = movedNodes.Select(n => n.Id).ToHashSet();
            
            foreach (var movedNode in movedNodes)
            {
                foreach (var otherNode in _layout.Nodes)
                {
                    // Skip self
                    if (otherNode.Id == movedNode.Id) continue;
                    
                    // Skip if both were moved together (they maintain relative position)
                    if (movedIds.Contains(otherNode.Id)) continue;
                    
                    // Check: movedNode OUTPUT → otherNode INPUT
                    if (CanConnect(movedNode, otherNode) && AreTouching(movedNode, otherNode))
                    {
                        CreatePath(movedNode.Id, otherNode.Id);
                        pathsCreated++;
                    }
                    
                    // Check: otherNode OUTPUT → movedNode INPUT
                    if (CanConnect(otherNode, movedNode) && AreTouching(otherNode, movedNode))
                    {
                        CreatePath(otherNode.Id, movedNode.Id);
                        pathsCreated++;
                    }
                }
            }
            
            return pathsCreated;
        }
        
        /// <summary>
        /// Check a single node after placement
        /// </summary>
        public int DetectAndConnectSingle(NodeData newNode)
        {
            return DetectAndConnect(new[] { newNode });
        }
        
        /// <summary>
        /// Check if fromNode can connect to toNode (has terminals, no existing path)
        /// </summary>
        private bool CanConnect(NodeData fromNode, NodeData toNode)
        {
            // From node must have output terminal
            if (!HasOutputTerminal(fromNode)) return false;
            
            // To node must have input terminal
            if (!HasInputTerminal(toNode)) return false;
            
            // Path must not already exist
            if (_layout.Paths.Any(p => p.From == fromNode.Id && p.To == toNode.Id))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Check if fromNode's OUTPUT terminal is touching toNode's INPUT terminal
        /// </summary>
        private bool AreTouching(NodeData fromNode, NodeData toNode)
        {
            var outputPos = GetOutputTerminal(fromNode);
            var inputPos = GetInputTerminal(toNode);
            
            var distance = Math.Sqrt(
                Math.Pow(outputPos.X - inputPos.X, 2) + 
                Math.Pow(outputPos.Y - inputPos.Y, 2));
            
            return distance <= TouchThreshold;
        }
        
        /// <summary>
        /// Create a path between two nodes
        /// </summary>
        private void CreatePath(string fromId, string toId)
        {
            var path = new PathData
            {
                Id = Guid.NewGuid().ToString(),
                From = fromId,
                To = toId
            };
            _layout.Paths.Add(path);
        }
        
        #region Terminal Helpers
        
        private Point GetOutputTerminal(NodeData node)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            const double stickOut = 10;
            
            var pos = node.Visual.OutputTerminalPosition?.ToLower() ?? "right";
            
            return pos switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => new Point(x + w + stickOut, y + h / 2)
            };
        }
        
        private Point GetInputTerminal(NodeData node)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            const double stickOut = 10;
            
            var pos = node.Visual.InputTerminalPosition?.ToLower() ?? "left";
            
            return pos switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => new Point(x - stickOut, y + h / 2)
            };
        }
        
        private bool HasOutputTerminal(NodeData node)
        {
            var type = node.Type?.ToLower() ?? "";
            // Sink nodes have no output
            return !type.Contains("sink") && type != "snk";
        }
        
        private bool HasInputTerminal(NodeData node)
        {
            var type = node.Type?.ToLower() ?? "";
            // Source nodes have no input
            return !type.Contains("source") && type != "src";
        }
        
        #endregion
    }
}
