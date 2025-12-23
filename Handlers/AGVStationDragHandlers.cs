using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private AGVStationData? _draggingAGVStation;
        private bool _isDraggingAGVStation;

        /// <summary>
        /// Handle click on AGV station - start dragging in design mode
        /// </summary>
        private void HandleAGVStationClick(AGVStationData station, MouseButtonEventArgs e)
        {
            System.Console.WriteLine($"[DEBUG] AGV Station clicked: {station.Name} at ({station.X}, {station.Y})");

            if (_layout == null)
            {
                System.Console.WriteLine("[DEBUG] Layout is null!");
                return;
            }

            System.Console.WriteLine($"[DEBUG] DesignMode: {_layout.DesignMode}");

            // Check if layer is locked
            if (_transportArchitectureLayerManager != null && _transportArchitectureLayerManager.IsLocked(station.ArchitectureLayer))
            {
                System.Console.WriteLine($"[DEBUG] Layer {station.ArchitectureLayer} is locked");
                StatusText.Text = $"AGV station '{station.Name}' is on a locked layer";
                e.Handled = true;
                return;
            }

            // Only allow dragging in design mode
            if (_layout.DesignMode)
            {
                System.Console.WriteLine($"[DEBUG] Starting AGV station drag for {station.Name}");
                _draggingAGVStation = station;
                _isDraggingAGVStation = true;
                EditorCanvas.CaptureMouse();
                SaveUndoState();

                StatusText.Text = $"Design Mode: Dragging AGV station '{station.Name}' - linked waypoint will follow";
                e.Handled = true;
            }
            else
            {
                System.Console.WriteLine("[DEBUG] Not in design mode");
                StatusText.Text = $"AGV station '{station.Name}' selected - Enable design mode (D) to drag";
                e.Handled = true;
            }
        }

        /// <summary>
        /// Continue dragging AGV station (free movement in design mode)
        /// </summary>
        private void DragAGVStation(Point currentPos)
        {
            if (!_isDraggingAGVStation || _draggingAGVStation == null || _layout == null) return;

            System.Console.WriteLine($"[DEBUG] Dragging AGV station to ({currentPos.X:F1}, {currentPos.Y:F1})");

            // Update station position
            _draggingAGVStation.X = currentPos.X;
            _draggingAGVStation.Y = currentPos.Y;

            // Update linked waypoint if it exists
            if (!string.IsNullOrEmpty(_draggingAGVStation.LinkedWaypointId))
            {
                var waypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == _draggingAGVStation.LinkedWaypointId);
                if (waypoint != null)
                {
                    System.Console.WriteLine($"[DEBUG] Updating linked waypoint {waypoint.Name}");
                    waypoint.X = currentPos.X;
                    waypoint.Y = currentPos.Y;
                }
            }

            StatusText.Text = $"Design Mode: AGV station at ({currentPos.X:F1}, {currentPos.Y:F1})";
            Redraw();
        }

        /// <summary>
        /// Finish AGV station drag operation
        /// </summary>
        private void FinishAGVStationDrag()
        {
            if (_isDraggingAGVStation && _draggingAGVStation != null)
            {
                _isDraggingAGVStation = false;
                MarkDirty();

                StatusText.Text = $"AGV station '{_draggingAGVStation.Name}' repositioned";
                _draggingAGVStation = null;
            }
        }
    }
}
