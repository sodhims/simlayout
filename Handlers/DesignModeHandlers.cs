using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Design Mode Toggle

        /// <summary>
        /// Toggle design mode on/off with 'D' key
        /// </summary>
        private void ToggleDesignMode()
        {
            if (_layout == null) return;

            _layout.DesignMode = !_layout.DesignMode;

            // Turn off frictionless mode when entering design mode
            if (_layout.DesignMode && _layout.FrictionlessMode)
            {
                _layout.FrictionlessMode = false;
            }

            UpdateModeStatus();
            Redraw();
        }

        /// <summary>
        /// Update status text to reflect current mode
        /// </summary>
        private void UpdateModeStatus()
        {
            if (_layout == null) return;

            if (_layout.DesignMode)
            {
                StatusText.Text = "Design Mode: All entities unlocked - Press D to toggle";
            }
            else if (_layout.FrictionlessMode)
            {
                // Frictionless mode status is handled in FrictionlessHandlers.cs
                int constrainedEntityCount = _layout.EOTCranes.Count + _layout.JibCranes.Count;
                constrainedEntityCount += _layout.AGVWaypoints.Count;
                StatusText.Text = $"Frictionless Mode: {constrainedEntityCount} constrained entities - Press F to toggle";
            }
            else
            {
                StatusText.Text = "Normal Mode - Press D for Design Mode, F for Frictionless Mode";
            }
        }

        #endregion

        #region Zone Vertex Editing

        private string? _selectedZoneId;
        private int _draggingVertexIndex = -1;
        private bool _isDraggingVertex;

        /// <summary>
        /// Select a zone for vertex editing
        /// </summary>
        private void SelectZoneForEditing(string zoneId)
        {
            _selectedZoneId = zoneId;
            Redraw();
        }

        /// <summary>
        /// Check if mouse is over a zone vertex
        /// </summary>
        private (ZoneData? zone, int vertexIndex) HitTestZoneVertex(Point pos)
        {
            if (_layout == null || !_layout.DesignMode || string.IsNullOrEmpty(_selectedZoneId))
                return (null, -1);

            var zone = _layout.Zones.FirstOrDefault(z => z.Id == _selectedZoneId);
            if (zone == null || zone.Points == null)
                return (null, -1);

            const double vertexHitRadius = 8.0;

            for (int i = 0; i < zone.Points.Count; i++)
            {
                var point = zone.Points[i];
                var dx = pos.X - point.X;
                var dy = pos.Y - point.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= vertexHitRadius)
                    return (zone, i);
            }

            return (null, -1);
        }

        /// <summary>
        /// Start dragging a zone vertex
        /// </summary>
        private void StartVertexDrag(ZoneData zone, int vertexIndex)
        {
            _isDraggingVertex = true;
            _draggingVertexIndex = vertexIndex;
            SaveUndoState();
        }

        /// <summary>
        /// Update vertex position while dragging
        /// </summary>
        private void DragVertex(Point pos)
        {
            if (!_isDraggingVertex || _draggingVertexIndex < 0 || _layout == null || string.IsNullOrEmpty(_selectedZoneId))
                return;

            var zone = _layout.Zones.FirstOrDefault(z => z.Id == _selectedZoneId);
            if (zone == null || zone.Points == null || _draggingVertexIndex >= zone.Points.Count)
                return;

            zone.Points[_draggingVertexIndex].X = pos.X;
            zone.Points[_draggingVertexIndex].Y = pos.Y;

            StatusText.Text = $"Editing {zone.Name} vertex {_draggingVertexIndex + 1} at ({pos.X:F1}, {pos.Y:F1})";

            Redraw();
        }

        /// <summary>
        /// Finish vertex dragging
        /// </summary>
        private void FinishVertexDrag()
        {
            if (_isDraggingVertex)
            {
                _isDraggingVertex = false;
                _draggingVertexIndex = -1;
                MarkDirty();
                UpdateModeStatus();
            }
        }

        #endregion
    }
}
