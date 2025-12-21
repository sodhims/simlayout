using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    /// <summary>
    /// Canvas click handling logic
    /// </summary>
    public partial class MainWindow
    {
        #region Node Click Handling

        private void HandleNodeClick(string nodeId, MouseButtonEventArgs e)
        {
            // Clear wall selection when selecting nodes
            ClearWallSelection();
            
            if (_currentTool == "path" || _isDrawingPath)
            {
                HandlePathDrawing(nodeId);
                return;
            }

            var cell = _layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(nodeId));

            // If node is in a cell and we're not editing that cell -> select cell
            if (cell != null && _selectionService.EditingCellId != cell.Id)
            {
                SelectCellWithPaths(cell);
                StatusText.Text = $"Selected cell: {cell.Name} (double-click to edit)";
            }
            else
            {
                HandleNormalNodeSelection(nodeId);
            }

            _isDragging = true;
            EditorCanvas.CaptureMouse();
            UpdateSelectionVisuals();
            UpdatePropertyPanel();
        }

        private void HandleNormalNodeSelection(string nodeId)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                _selectionService.ToggleNodeSelection(nodeId);
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                _selectionService.SelectNode(nodeId, addToSelection: true);
            else
                _selectionService.SelectNode(nodeId);
        }

        #endregion

        #region Path Drawing

        private void HandlePathDrawing(string nodeId)
        {
            if (_pathStartNodeId == null)
            {
                _pathStartNodeId = nodeId;
                _isDrawingPath = true;
                StatusText.Text = "Click destination node or cell to complete path";
            }
            else if (_pathStartNodeId != nodeId)
            {
                CreatePath(_pathStartNodeId, nodeId);
                _pathStartNodeId = null;
                _isDrawingPath = false;
            }
        }

        private void HandleCellPathClick(GroupData cell)
        {
            var targetNode = cell.EntryPoints.FirstOrDefault() ?? cell.Members.FirstOrDefault();
            if (targetNode == null) return;

            if (_pathStartNodeId == null)
            {
                var exitNode = cell.ExitPoints.FirstOrDefault() ?? cell.Members.LastOrDefault();
                if (exitNode == null) return;
                _pathStartNodeId = exitNode;
                _isDrawingPath = true;
                StatusText.Text = $"Click destination node or cell (from {cell.Name})";
            }
            else
            {
                CreatePath(_pathStartNodeId, targetNode);
                _pathStartNodeId = null;
                _isDrawingPath = false;
            }
        }

        private void HandleTerminalClick(Services.HitTestResult hitResult)
        {
            if (hitResult.Node == null) return;
            
            var nodeId = hitResult.Node.Id;
            var terminalType = hitResult.TerminalType; // "input" or "output"
            
            if (_pathStartNodeId == null)
            {
                // Starting a new path - should start from an OUTPUT terminal
                if (terminalType == "output")
                {
                    _pathStartNodeId = nodeId;
                    _isDrawingPath = true;
                    StartRubberbandPath(hitResult.Node, fromOutput: true);
                }
                else
                {
                    // Clicked on input terminal - can't start path from input
                    StatusText.Text = "Click on an OUTPUT terminal (red) to start a path";
                }
            }
            else
            {
                // Completing a path - should end at an INPUT terminal
                if (terminalType == "input" && _pathStartNodeId != nodeId)
                {
                    CreatePath(_pathStartNodeId, nodeId);
                    _pathStartNodeId = null;
                    _isDrawingPath = false;
                    EndRubberbandPath(completed: true);
                }
                else if (terminalType == "output")
                {
                    StatusText.Text = "Click on an INPUT terminal (green) to complete the path";
                }
                else if (_pathStartNodeId == nodeId)
                {
                    StatusText.Text = "Cannot connect node to itself";
                }
            }
        }

        private void HandleCellTerminalClick(Services.HitTestResult hitResult)
        {
            if (hitResult.Group == null || !hitResult.Group.IsCell) return;
            
            var cell = hitResult.Group;
            var terminalType = hitResult.TerminalType; // "input" or "output"
            
            // For cells, use entry/exit points
            if (_pathStartNodeId == null)
            {
                // Starting path from cell's output
                if (terminalType == "output")
                {
                    var exitNode = cell.ExitPoints.FirstOrDefault() ?? cell.Members.LastOrDefault();
                    if (exitNode != null)
                    {
                        _pathStartNodeId = exitNode;
                        _isDrawingPath = true;
                        StatusText.Text = $"Path started from {cell.Name} - click destination terminal";
                    }
                }
                else
                {
                    StatusText.Text = "Click on an OUTPUT terminal (red) to start a path";
                }
            }
            else
            {
                // Completing path to cell's input
                if (terminalType == "input")
                {
                    var entryNode = cell.EntryPoints.FirstOrDefault() ?? cell.Members.FirstOrDefault();
                    if (entryNode != null && _pathStartNodeId != entryNode)
                    {
                        CreatePath(_pathStartNodeId, entryNode);
                        _pathStartNodeId = null;
                        _isDrawingPath = false;
                    }
                }
                else
                {
                    StatusText.Text = "Click on an INPUT terminal (green) to complete the path";
                }
            }
        }

        #endregion

        #region Group/Cell Click Handling

        private void HandleGroupBorderClick(GroupData group)
        {
            bool addToSelection = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            
            // For cells, include internal paths in selection
            if (group.IsCell)
            {
                SelectCellWithPaths(group, addToSelection);
            }
            else
            {
                _selectionService.SelectGroup(group.Id, group.Members, addToSelection);
            }
            
            UpdateSelectionVisuals();
            UpdatePropertyPanel();
            
            if (_selectionService.HasMultipleGroups)
            {
                StatusText.Text = $"Selected {_selectionService.SelectedGroupIds.Count} groups - use alignment tools";
            }
            else
            {
                var hint = group.IsCell ? " (double-click to edit, Shift+click to multi-select)" : "";
                StatusText.Text = $"Selected: {group.Name}{hint}";
            }
        }

        #endregion

        #region Path Click Handling

        private void HandlePathClick(string pathId)
        {
            var path = _layout.Paths.FirstOrDefault(p => p.Id == pathId);
            if (path == null) return;

            var pos = _lastMousePos;
            
            // In path edit mode OR if path is already selected - clicking adds a waypoint
            if (_isPathEditMode || _selectionService.IsPathSelected(pathId))
            {
                if (!_selectionService.IsPathSelected(pathId))
                {
                    _selectionService.SelectPath(pathId);
                    UpdateSelectionVisuals();
                }
                
                var segmentIndex = FindPathSegmentIndex(path, pos);
                SaveUndoState();
                path.Visual.Waypoints.Insert(segmentIndex, new PointData(pos.X, pos.Y));
                _draggingPathId = pathId;
                _draggingWaypointIndex = segmentIndex;
                _isDraggingWaypoint = true;
                EditorCanvas.CaptureMouse();
                MarkDirty();
                Redraw();
                StatusText.Text = "Drag to position waypoint - release to place";
                return;
            }

            // First click - just select the path
            _selectionService.SelectPath(pathId);
            UpdateSelectionVisuals();
            UpdatePropertyPanel();
            StatusText.Text = "Path selected - click again to add waypoint, or use ✎ Edit mode";
        }

        private int FindPathSegmentIndex(PathData path, Point clickPos)
        {
            var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
            var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);
            if (fromNode == null || toNode == null) return 0;

            var start = new Point(
                fromNode.Visual.X + fromNode.Visual.Width / 2,
                fromNode.Visual.Y + fromNode.Visual.Height / 2);
            var end = new Point(
                toNode.Visual.X + toNode.Visual.Width / 2,
                toNode.Visual.Y + toNode.Visual.Height / 2);

            var points = new List<Point> { start };
            points.AddRange(path.Visual.Waypoints.Select(wp => new Point(wp.X, wp.Y)));
            points.Add(end);

            double minDist = double.MaxValue;
            int closestSegment = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var dist = PointToSegmentDistance(clickPos, points[i], points[i + 1]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestSegment = i;
                }
            }

            return closestSegment;
        }

        private double PointToSegmentDistance(Point p, Point a, Point b)
        {
            var ab = new Vector(b.X - a.X, b.Y - a.Y);
            var ap = new Vector(p.X - a.X, p.Y - a.Y);
            
            var lengthSq = ab.LengthSquared;
            if (lengthSq == 0) return ap.Length;

            var t = Math.Max(0, Math.Min(1, (ap.X * ab.X + ap.Y * ab.Y) / lengthSq));
            var projection = new Point(a.X + t * ab.X, a.Y + t * ab.Y);
            
            return (p - projection).Length;
        }

        #endregion

        #region Canvas Click Handling

        private void HandleCanvasClick(Point pos, MouseButtonEventArgs e)
        {
            // Handle AGV path drawing
            if (_isDrawingAGVPath)
            {
                HandleAGVPathClick(pos, e);
                return;
            }

            // Handle AGV station placement
            if (_isPlacingAGVStation)
            {
                HandleAGVStationClick(pos);
                return;
            }

            // Handle traffic zone drawing
            if (_isDrawingTrafficZone)
            {
                HandleTrafficZoneClick(pos);
                return;
            }

            // Handle conveyor drawing
            if (_isDrawingConveyor)
            {
                HandleConveyorDrawingClick(pos, e);
                return;
            }

            // Handle opening placement tool
            if (_isPlacingOpening)
            {
                HandleOpeningPlacementClick(pos);
                return;
            }

            // Handle wall tool
            if (_currentTool == "wall")
            {
                HandleWallClick(pos);
                return;
            }

            // Handle column tool
            if (_currentTool == "column")
            {
                HandleColumnClick(pos);
                return;
            }

            // Handle measurement tool
            if (_currentTool == "measure")
            {
                HandleMeasurementClick(pos);
                return;
            }

            // Handle walkway tool
            if (_currentTool == "walkway")
            {
                HandleWalkwayClick(pos);
                return;
            }

            // Handle crossing tool
            if (_currentTool == "crossing")
            {
                HandleCrossingClick(pos);
                return;
            }

            // Handle safety zone tool
            if (_currentTool == "safetyzone")
            {
                HandleSafetyZoneClick(pos);
                return;
            }

            if (_isDrawingPath)
            {
                _pathStartNodeId = null;
                _isDrawingPath = false;
                EndRubberbandPath(completed: false);
                return;
            }

            // Handle template placement
            if (!string.IsNullOrEmpty(_pendingTemplateId))
            {
                var template = _layout.Templates.FirstOrDefault(t => t.Id == _pendingTemplateId);
                if (template != null)
                {
                    PlaceTemplateAt(template, pos);
                    return;
                }
            }

            // Exit cell edit mode if clicking outside
            if (_selectionService.IsEditingCell)
            {
                _selectionService.ExitCellEditMode();
                StatusText.Text = "Exited cell edit mode";
                Redraw();
            }

            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                _selectionService.ClearSelection();
                ClearWallSelection();  // Also clear wall selection
                UpdateSelectionVisuals();
                UpdatePropertyPanel();
                Redraw();  // Redraw to show deselected walls
            }

            _isDragging = true;
            _isDrawingSelectionRect = true;  // Starting area selection
            EditorCanvas.CaptureMouse();
        }

        #endregion
    }
}
