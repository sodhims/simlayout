using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private const double AutoConnectThreshold = 40.0;
        
        private void InitializeToolbox()
        {
            if (NodeToolbox != null)
            {
                NodeToolbox.NodeTypeSelected += (sender, nodeType) =>
                {
                    _pendingNodeType = nodeType;
                    EditorCanvas.Cursor = Cursors.Cross;
                    StatusText.Text = $"Click on canvas to place {nodeType}";
                    if (ModeText != null) ModeText.Text = $"Mode: Place {nodeType}";
                };
            }
        }
        
        private void TryAutoConnectNode(NodeData newNode)
        {
            bool hasInput = HasInputTerminal(newNode);
            bool hasOutput = HasOutputTerminal(newNode);
            if (!hasInput && !hasOutput) return;
            
            NodeData? connectFromNode = null;
            NodeData? connectToNode = null;
            double bestInputDist = AutoConnectThreshold;
            double bestOutputDist = AutoConnectThreshold;
            
            Point? newInputPos = hasInput ? GetTerminalWorldPosition(newNode, isInput: true) : null;
            Point? newOutputPos = hasOutput ? GetTerminalWorldPosition(newNode, isInput: false) : null;
            
            foreach (var existingNode in _layout.Nodes)
            {
                if (existingNode.Id == newNode.Id) continue;
                
                if (newInputPos.HasValue && HasOutputTerminal(existingNode))
                {
                    var existingOutputPos = GetTerminalWorldPosition(existingNode, isInput: false);
                    var distToInput = TerminalDistance(existingOutputPos, newInputPos.Value);
                    if (distToInput < bestInputDist && !AutoConnectPathExists(existingNode.Id, newNode.Id))
                    {
                        bestInputDist = distToInput;
                        connectFromNode = existingNode;
                    }
                }
                
                if (newOutputPos.HasValue && HasInputTerminal(existingNode))
                {
                    var existingInputPos = GetTerminalWorldPosition(existingNode, isInput: true);
                    var distToOutput = TerminalDistance(existingInputPos, newOutputPos.Value);
                    if (distToOutput < bestOutputDist && !AutoConnectPathExists(newNode.Id, existingNode.Id))
                    {
                        bestOutputDist = distToOutput;
                        connectToNode = existingNode;
                    }
                }
            }
            
            int pathsCreated = 0;
            if (connectFromNode != null) { CreatePath(connectFromNode.Id, newNode.Id); pathsCreated++; }
            if (connectToNode != null) { CreatePath(newNode.Id, connectToNode.Id); pathsCreated++; }
            
            if (pathsCreated > 0)
            {
                Redraw();
                StatusText.Text = $"Added {newNode.Name} and created {pathsCreated} path(s)";
            }
        }
        
        private bool HasInputTerminal(NodeData node)
        {
            var type = node.Type?.ToLower() ?? "";
            return type != "source" && type != "src";
        }
        
        private bool HasOutputTerminal(NodeData node)
        {
            var type = node.Type?.ToLower() ?? "";
            return type != "sink" && type != "snk";
        }
        
        private Point GetTerminalWorldPosition(NodeData node, bool isInput)
        {
            var terminalPos = isInput ? node.Visual.InputTerminalPosition : node.Visual.OutputTerminalPosition;
            double x = node.Visual.X, y = node.Visual.Y, w = node.Visual.Width, h = node.Visual.Height;
            double stickOut = 10;
            
            return (terminalPos?.ToLower()) switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => isInput ? new Point(x - stickOut, y + h / 2) : new Point(x + w + stickOut, y + h / 2)
            };
        }
        
        private static double TerminalDistance(Point a, Point b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        private bool AutoConnectPathExists(string fromId, string toId)
            => _layout.Paths.Any(p => p.From == fromId && p.To == toId);
    }
}