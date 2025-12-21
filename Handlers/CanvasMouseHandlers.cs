using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    /// <summary>
    /// Core canvas mouse event handlers
    /// </summary>
    public partial class MainWindow
    {
        #region Mouse State

        private Point _lastMousePos;
        private Point _dragStart;
        private bool _isDragging;
        private bool _isDragStarted;
        private bool _isPanning;
        private bool _isDrawingPath;
        private bool _isDraggingWaypoint;
        private bool _isDrawingSelectionRect;  // True when drawing area selection
        private string? _draggingPathId;
        private int _draggingWaypointIndex = -1;
        private const double DragThreshold = 5.0;

        #endregion

        #region Mouse Down

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(EditorCanvas);
            _lastMousePos = pos;
            _dragStart = pos;
            _isDragStarted = false;
            _isDrawingSelectionRect = false;  // Reset at start

            // Handle crane drawing modes (runway, jib placement)
            if (HandleCraneMouseDown(pos, e)) return;

            if (_pendingNodeType != null)
            {
                // Get node count before placement
                int countBefore = _layout.Nodes.Count;
                
                PlaceNodeAtPosition(_pendingNodeType, pos);
                
                // If a node was added, try to auto-connect it
                if (_layout.Nodes.Count > countBefore)
                {
                    var newNode = _layout.Nodes.Last();
                    TryAutoConnectNode(newNode);
                }
                
                CancelNodePlacement();
                e.Handled = true;
                return;
            }

            if (_currentTool == "pan")
            {
                _isPanning = true;
                EditorCanvas.Cursor = Cursors.Hand;
                EditorCanvas.CaptureMouse();
                return;
            }

            // Area select tool - always draw selection rectangle, ignore nodes
            if (_currentTool == "area")
            {
                _selectionService.ClearSelection();
                UpdateSelectionVisuals();
                _isDragging = true;
                _isDrawingSelectionRect = true;
                EditorCanvas.CaptureMouse();
                return;
            }

            // Check for waypoint handle clicks first
            if (CheckWaypointHandleClick(pos, e))
            {
                e.Handled = true;
                return;
            }

            var hitResult = _hitTestService.HitTest(_layout, pos);
            
            // When Shift is held and we have groups selected, prioritize cell selection
            bool shiftHeld = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            if (shiftHeld && _selectionService.SelectedGroupIds.Count > 0)
            {
                var cellHit = _hitTestService.HitTestCellAtPoint(_layout, pos);
                if (cellHit != null)
                {
                    HandleGroupBorderClick(cellHit);
                    _isDragging = true;
                    EditorCanvas.CaptureMouse();
                    return;
                }
            }

            // Check for wall endpoint click (for stretching)
            if (HasWallSelection)
            {
                var endpoint = HitTestWallEndpoint(pos);
                if (endpoint.HasValue)
                {
                    StartEndpointDrag(endpoint.Value.wall!, endpoint.Value.isStart, pos);
                    EditorCanvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }

            // Check for wall click before canvas click
            if (hitResult.Type == HitType.Canvas)
            {
                var wall = HitTestWall(pos);
                if (wall != null)
                {
                    // If wall already selected, start dragging
                    if (_selectedWallIds.Contains(wall.Id))
                    {
                        StartWallDrag(pos);
                        EditorCanvas.CaptureMouse();
                        e.Handled = true;
                        return;
                    }
                    
                    // Select the wall
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        AddWallToSelection(wall.Id);
                    else
                        SelectWall(wall.Id);
                    e.Handled = true;
                    return;
                }
            }

            switch (hitResult.Type)
            {
                case HitType.NodeTerminal:
                    // Clicking on a terminal - use for path drawing
                    HandleTerminalClick(hitResult);
                    break;
                case HitType.CellTerminal:
                    // Clicking on a cell terminal - use for path drawing
                    HandleCellTerminalClick(hitResult);
                    break;
                case HitType.Node:
                    if (shiftHeld && hitResult.Node != null)
                    {
                        var nodeCell = _layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(hitResult.Node.Id));
                        if (nodeCell != null)
                        {
                            HandleGroupBorderClick(nodeCell);
                            _isDragging = true;
                            EditorCanvas.CaptureMouse();
                            return;
                        }
                    }
                    HandleNodeClick(hitResult.Id!, e);
                    break;
                case HitType.GroupBorder:
                case HitType.CellInterior:
                    if ((_currentTool == "path" || _isDrawingPath) && hitResult.Group?.IsCell == true)
                        HandleCellPathClick(hitResult.Group);
                    else
                    {
                        HandleGroupBorderClick(hitResult.Group!);
                        _isDragging = true;
                        EditorCanvas.CaptureMouse();
                    }
                    break;
                case HitType.Path:
                    HandlePathClick(hitResult.Id!);
                    break;
                case HitType.Canvas:
                    HandleCanvasClick(pos, e);
                    break;
            }
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            var pos = e.GetPosition(EditorCanvas);

            // Handle conveyor double-click to finish
            if (_isDrawingConveyor)
            {
                HandleConveyorDoubleClick(pos);
                e.Handled = true;
                return;
            }

            // Handle AGV path double-click to finish
            if (_isDrawingAGVPath)
            {
                FinishAGVPathDrawing();
                e.Handled = true;
                return;
            }

            // Handle traffic zone double-click to finish
            if (_isDrawingTrafficZone)
            {
                FinishTrafficZone();
                e.Handled = true;
                return;
            }

            var hitResult = _hitTestService.HitTest(_layout, pos);

            if (hitResult.Type == HitType.Node && hitResult.Id != null)
            {
                var cell = _layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(hitResult.Id));
                if (cell != null)
                {
                    _selectionService.EnterCellEditMode(cell.Id);
                    StatusText.Text = $"Editing cell: {cell.Name} (Esc to exit)";
                    Redraw();
                    e.Handled = true;
                }
            }
            else if (hitResult.Type == HitType.GroupBorder && hitResult.Group?.IsCell == true)
            {
                _selectionService.EnterCellEditMode(hitResult.Group.Id);
                StatusText.Text = $"Editing cell: {hitResult.Group.Name} (Esc to exit)";
                Redraw();
                e.Handled = true;
            }
        }

        #endregion

        #region Mouse Move

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(EditorCanvas);
            UpdateMousePosition(pos);

            // Handle crane drawing preview (runway, jib)
            if (HandleCraneMouseMove(pos)) return;

            // Handle wall endpoint dragging (stretching)
            if (_isDraggingWallEndpoint)
            {
                DragEndpoint(pos);
                return;
            }

            // Handle wall dragging
            if (_isDraggingWalls)
            {
                DragWalls(pos);
                return;
            }

            // Update wall drawing preview
            if (_isDrawingWall)
            {
                UpdateWallPreview(pos);
                return;
            }

            // Update measurement preview
            if (_isDrawingMeasurement)
            {
                UpdateMeasurementPreview(pos);
                return;
            }

            if (_isPanning)
            {
                var delta = pos - _lastMousePos;
                CanvasScroller.ScrollToHorizontalOffset(CanvasScroller.HorizontalOffset - delta.X);
                CanvasScroller.ScrollToVerticalOffset(CanvasScroller.VerticalOffset - delta.Y);
                _lastMousePos = pos;
                return;
            }

            // Handle waypoint dragging
            if (_isDraggingWaypoint && _draggingPathId != null && _draggingWaypointIndex >= 0)
            {
                var path = _layout.Paths.FirstOrDefault(p => p.Id == _draggingPathId);
                if (path != null && _draggingWaypointIndex < path.Visual.Waypoints.Count)
                {
                    path.Visual.Waypoints[_draggingWaypointIndex].X = pos.X;
                    path.Visual.Waypoints[_draggingWaypointIndex].Y = pos.Y;
                    Redraw();
                }
                _lastMousePos = pos;
                return;
            }

            if (_isDragging)
            {
                if (!_isDragStarted)
                {
                    if ((pos - _dragStart).Length < DragThreshold) return;
                    _isDragStarted = true;
                    _lastMousePos = _dragStart;
                    SaveUndoState();
                }

                // If drawing selection rectangle OR no selection, draw rectangle
                if (_isDrawingSelectionRect || !_selectionService.HasSelection)
                    UpdateSelectionRectangle(pos);
                else
                    DragSelectedNodes(pos);
            }

            if (_isDrawingPath && _pathStartNodeId != null)
                UpdateTempPath(pos);
            
            // Update rubberband path line if drawing
            if (_isDrawingRubberbandPath)
                UpdateRubberbandPath(pos);

            _lastMousePos = pos;
        }

        #endregion

        #region Mouse Up / Wheel / Right Click

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(EditorCanvas);

            // Finish crane drawing (runway, jib)
            if (HandleCraneMouseUp(pos, e)) 
            { 
                EditorCanvas.ReleaseMouseCapture(); 
                return; 
            }

            if (_isPanning)
            {
                _isPanning = false;
                EditorCanvas.Cursor = Cursors.Arrow;
            }
            if (_isDraggingWallEndpoint)
            {
                FinishEndpointDrag();
            }
            if (_isDraggingWalls)
            {
                FinishWallDrag();
            }
            if (_isDraggingWaypoint)
            {
                _isDraggingWaypoint = false;
                _draggingPathId = null;
                _draggingWaypointIndex = -1;
                MarkDirty();
            }
            if (_isDragging) 
            {
                FinishDrag();
                // Auto-connect terminals that are now touching after drag
                RunAutoPathDetection();
            }
            EditorCanvas.ReleaseMouseCapture();
        }

        private void Canvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var delta = e.Delta > 0 ? 0.1 : -0.1;
                SetZoom(Math.Clamp(_currentZoom + delta, 0.1, 4.0));
                e.Handled = true;
            }
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _contextMenuPosition = e.GetPosition(EditorCanvas);
            
            if (CheckWaypointRightClick(_contextMenuPosition))
            {
                e.Handled = true;
                return;
            }
            
            // Check for wall right-click
            var wall = HitTestWall(_contextMenuPosition);
            if (wall != null)
            {
                SelectWall(wall.Id);
                ShowWallContextMenu(wall, _contextMenuPosition);
                e.Handled = true;
                return;
            }
            
            ShowContextMenu(_contextMenuPosition);
        }

        #endregion
    }
}
