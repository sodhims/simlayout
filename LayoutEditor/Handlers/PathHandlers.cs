using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Path Operations

        private void CreatePath(string fromNodeId, string toNodeId)
        {
            // Check if path already exists
            var existing = _layout.Paths.FirstOrDefault(p =>
                (p.From == fromNodeId && p.To == toNodeId) ||
                (p.From == toNodeId && p.To == fromNodeId));

            if (existing != null)
            {
                StatusText.Text = "Path already exists between these nodes";
                return;
            }

            SaveUndoState();

            // Use direct routing by default
            var routingMode = RoutingModes.Direct;

            var path = new PathData
            {
                Id = Guid.NewGuid().ToString(),
                From = fromNodeId,
                To = toNodeId,
                PathType = PathTypes.Single,
                RoutingMode = routingMode,
                Visual = new PathVisual { Color = "#666666" },
                Simulation = new PathSimulation
                {
                    TransportType = TransportTypes.Conveyor,
                    Speed = 1.0,
                    Capacity = 10
                }
            };

            // Calculate distance
            var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == fromNodeId);
            var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == toNodeId);

            if (fromNode != null && toNode != null)
            {
                var dx = toNode.Visual.X - fromNode.Visual.X;
                var dy = toNode.Visual.Y - fromNode.Visual.Y;
                path.Simulation.Distance = Math.Sqrt(dx * dx + dy * dy);
            }

            _layout.Paths.Add(path);

            _selectionService.SelectPath(path.Id);

            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Created path from {fromNode?.Name} to {toNode?.Name}";
        }

        private void DeletePath(string pathId)
        {
            var path = _layout.Paths.FirstOrDefault(p => p.Id == pathId);
            if (path == null) return;

            SaveUndoState();
            _layout.Paths.Remove(path);

            _selectionService.ClearSelection();

            MarkDirty();
            RefreshAll();
            StatusText.Text = "Path deleted";
        }

        private void UpdateTempPath(Point currentPos)
        {
            if (_pathStartNodeId == null) return;

            var startNode = _layout.Nodes.FirstOrDefault(n => n.Id == _pathStartNodeId);
            if (startNode == null) return;

            Point startPoint;
            
            // Check if starting node is inside a cell - if so, draw from cell's output terminal
            var cell = _layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(_pathStartNodeId));
            if (cell != null)
            {
                // Get cell bounds
                var cellNodes = cell.Members.Select(id => _layout.Nodes.FirstOrDefault(n => n.Id == id))
                    .Where(n => n != null).ToList();
                if (cellNodes.Count > 0)
                {
                    const double pad = 15;
                    const double terminalOffset = 12;
                    double minX = cellNodes.Min(n => n!.Visual.X) - pad;
                    double minY = cellNodes.Min(n => n!.Visual.Y) - pad;
                    double maxX = cellNodes.Max(n => n!.Visual.X + n!.Visual.Width) + pad;
                    double maxY = cellNodes.Max(n => n!.Visual.Y + n!.Visual.Height) + pad;
                    
                    // Get output terminal position
                    startPoint = cell.OutputTerminalPosition?.ToLower() switch
                    {
                        "top" => new Point((minX + maxX) / 2, minY - terminalOffset),
                        "bottom" => new Point((minX + maxX) / 2, maxY + terminalOffset),
                        "left" => new Point(minX - terminalOffset, (minY + maxY) / 2),
                        _ => new Point(maxX + terminalOffset, (minY + maxY) / 2) // right default
                    };
                }
                else
                {
                    // Fallback to node terminal
                    startPoint = Services.TerminalHelper.GetNodeOutputTerminal(startNode);
                }
            }
            else
            {
                // Start from node's OUTPUT terminal
                startPoint = Services.TerminalHelper.GetNodeOutputTerminal(startNode);
            }

            _pathRenderer.ClearTempPath(EditorCanvas);
            _pathRenderer.DrawTempPath(EditorCanvas, startPoint, currentPos);
        }

        private void CancelPathDrawing()
        {
            _pathStartNodeId = null;
            _isDrawingPath = false;
            _pathRenderer.ClearTempPath(EditorCanvas);
            StatusText.Text = "Path drawing cancelled";
        }

        private void StartPathDrawing_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "path";
            _isDrawingPath = true;
            StatusText.Text = "Click source node to start path";
            if (ModeText != null) ModeText.Text = "Mode: Draw Path";
        }

        #endregion
    }
}
