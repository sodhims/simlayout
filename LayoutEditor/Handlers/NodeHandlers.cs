using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Node Operations

        private string? _pendingNodeType;
        private string? _pathStartNodeId;

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string nodeType)
            {
                _pendingNodeType = nodeType;
                EditorCanvas.Cursor = System.Windows.Input.Cursors.Cross;
                StatusText.Text = $"Click on canvas to place {nodeType}";
                ModeText.Text = $"Mode: Place {nodeType}";
            }
        }

        private void AddNodeHere_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string nodeType)
                PlaceNodeAtPosition(nodeType, _contextMenuPosition);
        }

        private void PlaceNodeAtPosition(string nodeType, Point pos)
        {
            SaveUndoState();
            var snapped = SnapToGrid(pos);
            var node = LayoutFactory.CreateNode(nodeType, snapped.X - 40, snapped.Y - 30);
            _layout.Nodes.Add(node);
            _selectionService.SelectNode(node.Id);
            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Added {node.Name}";
        }

        private void CancelNodePlacement()
        {
            _pendingNodeType = null;
            EditorCanvas.Cursor = System.Windows.Input.Cursors.Arrow;
            StatusText.Text = "Ready";
            ModeText.Text = "Mode: Select";
        }

        private void DragSelectedNodes(Point currentPos)
        {
            var delta = currentPos - _lastMousePos;
            var nodes = _selectionService.GetSelectedNodes(_layout);
            
            foreach (var node in nodes)
            {
                node.Visual.X += delta.X;
                node.Visual.Y += delta.Y;
            }

            MoveSelectedVisuals();  // Fast move, no full redraw
        }

        private void FinishDrag()
        {
            // Snap to grid on drag finish
            if (_isDragStarted && SnapToGridMenu?.IsChecked == true)
            {
                var nodes = _selectionService.GetSelectedNodes(_layout);
                foreach (var node in nodes)
                {
                    var snapped = SnapToGrid(new Point(node.Visual.X, node.Visual.Y));
                    node.Visual.X = snapped.X;
                    node.Visual.Y = snapped.Y;
                }
            }
            
            if (_isDragStarted)
            {
                MarkDirty();
                Redraw();  // Full redraw only at end
            }
            
            _isDragging = false;
            _isDragStarted = false;
            ClearSelectionRectangle();
        }

        private Point SnapToGrid(Point point)
        {
            if (SnapToGridMenu?.IsChecked != true) return point;
            int gridSize = _layout?.Canvas?.GridSize ?? 25;
            return new Point(
                Math.Round(point.X / gridSize) * gridSize,
                Math.Round(point.Y / gridSize) * gridSize);
        }

        #endregion
    }
}
