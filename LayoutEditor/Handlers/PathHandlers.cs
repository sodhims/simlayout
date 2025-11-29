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

            // Use Manhattan routing if checkbox is checked
            var routingMode = ManhattanCheck.IsChecked == true 
                ? RoutingModes.Manhattan 
                : RoutingModes.Direct;

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

            var startPoint = new Point(
                startNode.Visual.X + startNode.Visual.Width / 2,
                startNode.Visual.Y + startNode.Visual.Height / 2);

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
            ModeText.Text = "Mode: Draw Path";
        }

        #endregion
    }
}
