using System;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for wall drawing operations
    /// </summary>
    public partial class MainWindow
    {
        #region Wall Drawing State

        private bool _isDrawingWall = false;
        private WallData? _currentWall = null;
        private string _currentWallType = WallTypes.Standard;

        #endregion

        #region Wall Tool Selection

        private void WallTool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "wall";
            _currentWallType = WallTypes.Standard;
            UpdateToolbarState();
            StatusText.Text = "Wall tool: Click to start, click again to end wall";
        }

        private void WallTypeStandard_Click(object sender, RoutedEventArgs e)
        {
            _currentWallType = WallTypes.Standard;
            _currentTool = "wall";
            StatusText.Text = "Drawing standard walls";
        }

        private void WallTypeExterior_Click(object sender, RoutedEventArgs e)
        {
            _currentWallType = WallTypes.Exterior;
            _currentTool = "wall";
            StatusText.Text = "Drawing exterior walls (thicker)";
        }

        private void WallTypePartition_Click(object sender, RoutedEventArgs e)
        {
            _currentWallType = WallTypes.Partition;
            _currentTool = "wall";
            StatusText.Text = "Drawing partition walls (thinner)";
        }

        private void WallTypeGlass_Click(object sender, RoutedEventArgs e)
        {
            _currentWallType = WallTypes.Glass;
            _currentTool = "wall";
            StatusText.Text = "Drawing glass walls";
        }

        private void WallTypeSafety_Click(object sender, RoutedEventArgs e)
        {
            _currentWallType = WallTypes.Safety;
            _currentTool = "wall";
            StatusText.Text = "Drawing safety barriers";
        }

        #endregion

        #region Wall Drawing

        private void HandleWallClick(Point pos)
        {
            if (!_isDrawingWall)
            {
                // Start new wall
                SaveUndoState();
                var snapped = SnapToGridPoint(pos);
                _currentWall = new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = snapped.X,
                    Y1 = snapped.Y,
                    X2 = snapped.X,
                    Y2 = snapped.Y,
                    WallType = _currentWallType,
                    Thickness = GetWallThickness(_currentWallType)
                };
                _layout.Walls.Add(_currentWall);
                _isDrawingWall = true;
                StatusText.Text = "Click to place wall end point (Shift for horizontal/vertical, Esc to cancel)";
            }
            else
            {
                // End wall
                if (_currentWall != null)
                {
                    var endPos = GetConstrainedEndPoint(
                        new Point(_currentWall.X1, _currentWall.Y1),
                        pos,
                        Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                    
                    var snapped = SnapToGridPoint(endPos);
                    _currentWall.X2 = snapped.X;
                    _currentWall.Y2 = snapped.Y;

                    // Remove if too short
                    if (_currentWall.Length < 10)
                    {
                        _layout.Walls.Remove(_currentWall);
                        StatusText.Text = "Wall too short, removed";
                    }
                    else
                    {
                        MarkDirty();
                        StatusText.Text = $"Wall placed ({_currentWall.Length:F0}px)";
                    }
                }
                
                _isDrawingWall = false;
                _currentWall = null;
                Redraw();
            }
        }

        private void UpdateWallPreview(Point pos)
        {
            if (_isDrawingWall && _currentWall != null)
            {
                var endPos = GetConstrainedEndPoint(
                    new Point(_currentWall.X1, _currentWall.Y1),
                    pos,
                    Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                
                var snapped = SnapToGridPoint(endPos);
                _currentWall.X2 = snapped.X;
                _currentWall.Y2 = snapped.Y;
                Redraw();
            }
        }

        private double GetWallThickness(string wallType)
        {
            return wallType switch
            {
                WallTypes.Exterior => 10,
                WallTypes.Standard => 6,
                WallTypes.Partition => 4,
                WallTypes.Glass => 3,
                WallTypes.Safety => 4,
                _ => 6
            };
        }

        private void CancelWallDrawing()
        {
            if (_isDrawingWall && _currentWall != null)
            {
                _layout.Walls.Remove(_currentWall);
                _currentWall = null;
                _isDrawingWall = false;
                Redraw();
                StatusText.Text = "Wall drawing cancelled";
            }
        }

        #endregion

        #region Drawing Helpers

        /// <summary>
        /// Snap a point to the grid if grid snapping is enabled
        /// </summary>
        private Point SnapToGridPoint(Point point)
        {
            if (_layout?.Canvas?.SnapToGrid != true) return point;
            var gridSize = _layout.Canvas.GridSize;
            return new Point(
                Math.Round(point.X / gridSize) * gridSize,
                Math.Round(point.Y / gridSize) * gridSize);
        }

        /// <summary>
        /// Constrain end point to horizontal, vertical, or 45-degree angles
        /// </summary>
        private Point GetConstrainedEndPoint(Point start, Point end, bool constrain)
        {
            if (!constrain) return end;

            var dx = Math.Abs(end.X - start.X);
            var dy = Math.Abs(end.Y - start.Y);

            // Snap to horizontal, vertical, or 45-degree
            if (dx > dy * 2)
            {
                // Horizontal
                return new Point(end.X, start.Y);
            }
            else if (dy > dx * 2)
            {
                // Vertical
                return new Point(start.X, end.Y);
            }
            else
            {
                // 45 degree
                var dist = Math.Max(dx, dy);
                var signX = end.X > start.X ? 1 : -1;
                var signY = end.Y > start.Y ? 1 : -1;
                return new Point(start.X + dist * signX, start.Y + dist * signY);
            }
        }

        #endregion
    }
}
