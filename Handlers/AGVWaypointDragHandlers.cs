using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private AGVWaypointData? _draggingAGVWaypoint;
        private bool _isDraggingAGVWaypoint;

        /// <summary>
        /// Handle click on AGV waypoint - start selection/dragging
        /// </summary>
        private void HandleAGVWaypointClick(AGVWaypointData waypoint, MouseButtonEventArgs e)
        {
            if (_layout == null) return;

            // In Design Mode, allow free movement (reconfigure network)
            if (_layout.DesignMode)
            {
                _draggingAGVWaypoint = waypoint;
                _isDraggingAGVWaypoint = true;
                EditorCanvas.CaptureMouse();
                SaveUndoState();
                StatusText.Text = $"Design Mode: Dragging AGV waypoint '{waypoint.Name}' (free movement)";
                e.Handled = true;
            }
            // In frictionless mode, waypoints are FIXED (use test vehicles to validate tracks)
            else if (_layout.FrictionlessMode)
            {
                StatusText.Text = $"Frictionless Mode: AGV waypoint '{waypoint.Name}' is fixed - use Test Vehicle to navigate tracks";
                e.Handled = true;
            }
            else
            {
                // Outside design/frictionless mode, just select it
                StatusText.Text = $"AGV waypoint '{waypoint.Name}' selected - Enable Design (D) mode to reposition";
                e.Handled = true;
            }
        }

        /// <summary>
        /// Continue dragging AGV waypoint - free in Design Mode, constrained in Frictionless Mode
        /// </summary>
        private void DragAGVWaypoint(Point currentPos)
        {
            if (!_isDraggingAGVWaypoint || _draggingAGVWaypoint == null || _layout == null) return;

            // Design Mode: Free movement (allows reconfiguring the AGV network)
            if (_layout.DesignMode)
            {
                _draggingAGVWaypoint.X = currentPos.X;
                _draggingAGVWaypoint.Y = currentPos.Y;
                StatusText.Text = $"Design Mode: Waypoint '{_draggingAGVWaypoint.Name}' at ({currentPos.X:F1}, {currentPos.Y:F1})";
                Redraw();
                return;
            }

            // Frictionless Mode: Constrained movement along connected paths
            // Find all paths connected to this waypoint
            var connectedPaths = _layout.AGVPaths
                .Where(p => p.FromWaypointId == _draggingAGVWaypoint.Id || p.ToWaypointId == _draggingAGVWaypoint.Id)
                .ToList();

            if (connectedPaths.Count == 0)
            {
                // No constraint - free movement
                _draggingAGVWaypoint.X = currentPos.X;
                _draggingAGVWaypoint.Y = currentPos.Y;
                StatusText.Text = $"Waypoint position: ({currentPos.X:F1}, {currentPos.Y:F1}) - No connected paths (free movement)";
                Redraw();
                return;
            }

            // Find closest point on any connected path
            Point closestPoint = currentPos;
            double minDistance = double.MaxValue;
            AGVPathData? closestPath = null;

            foreach (var path in connectedPaths)
            {
                // Get the other waypoint (the one we're not dragging)
                var otherWaypointId = path.FromWaypointId == _draggingAGVWaypoint.Id
                    ? path.ToWaypointId
                    : path.FromWaypointId;

                var otherWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == otherWaypointId);
                if (otherWaypoint == null) continue;

                // Project mouse position onto this path line
                var otherPoint = new Point(otherWaypoint.X, otherWaypoint.Y);
                var projectedPoint = ProjectPointOntoLine(currentPos,
                    new Point(_draggingAGVWaypoint.X, _draggingAGVWaypoint.Y),
                    otherPoint);

                var distance = (currentPos - projectedPoint).Length;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = projectedPoint;
                    closestPath = path;
                }
            }

            // Update waypoint position to closest constrained point
            _draggingAGVWaypoint.X = closestPoint.X;
            _draggingAGVWaypoint.Y = closestPoint.Y;

            // Show snap indicator at constrained position
            ShowConstraintSnapIndicator(closestPoint);

            // Update status with position info
            StatusText.Text = $"Waypoint position: ({closestPoint.X:F1}, {closestPoint.Y:F1}) - Constrained to {connectedPaths.Count} path(s)";

            Redraw();
        }

        /// <summary>
        /// Finish AGV waypoint drag operation
        /// </summary>
        private void FinishAGVWaypointDrag()
        {
            if (_isDraggingAGVWaypoint && _draggingAGVWaypoint != null)
            {
                _isDraggingAGVWaypoint = false;
                HideConstraintSnapIndicator();
                MarkDirty();

                StatusText.Text = $"AGV waypoint '{_draggingAGVWaypoint.Name}' positioned at ({_draggingAGVWaypoint.X:F1}, {_draggingAGVWaypoint.Y:F1})";
                _draggingAGVWaypoint = null;
            }
        }

        /// <summary>
        /// Project a point onto a line segment defined by two other points
        /// </summary>
        private Point ProjectPointOntoLine(Point p, Point lineStart, Point lineEnd)
        {
            var lineVec = lineEnd - lineStart;
            var lineLength = lineVec.Length;

            if (lineLength < 0.001) // Degenerate line
                return lineStart;

            var lineDir = new Vector(lineVec.X / lineLength, lineVec.Y / lineLength);
            var pointVec = p - lineStart;

            // Project onto line (dot product)
            var projection = pointVec.X * lineDir.X + pointVec.Y * lineDir.Y;

            // Clamp to line segment
            projection = Math.Max(0, Math.Min(lineLength, projection));

            return lineStart + lineDir * projection;
        }
    }
}
