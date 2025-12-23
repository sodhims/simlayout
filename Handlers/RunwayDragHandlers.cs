using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private RunwayData? _draggingRunway;
        private bool _isDraggingRunway;
        private Point _runwayDragStart;

        /// <summary>
        /// Handle click on runway - start dragging in design mode
        /// </summary>
        private void HandleRunwayClick(RunwayData runway, Point clickPos, MouseButtonEventArgs e)
        {
            if (_layout == null) return;

            // Check if layer is locked
            if (_transportArchitectureLayerManager != null && _transportArchitectureLayerManager.IsLocked(runway.ArchitectureLayer))
            {
                StatusText.Text = $"Runway '{runway.Name}' is on a locked layer";
                e.Handled = true;
                return;
            }

            // Only allow dragging in design mode
            if (_layout.DesignMode)
            {
                _draggingRunway = runway;
                _isDraggingRunway = true;
                _runwayDragStart = clickPos;
                EditorCanvas.CaptureMouse();
                SaveUndoState();

                StatusText.Text = $"Design Mode: Dragging runway '{runway.Name}'";
                e.Handled = true;
            }
            else
            {
                StatusText.Text = $"Runway '{runway.Name}' selected - Enable design mode (D) to drag";
                e.Handled = true;
            }
        }

        /// <summary>
        /// Continue dragging runway (free movement in design mode)
        /// </summary>
        private void DragRunway(Point currentPos)
        {
            if (!_isDraggingRunway || _draggingRunway == null || _layout == null) return;

            // Calculate delta from drag start
            double deltaX = currentPos.X - _runwayDragStart.X;
            double deltaY = currentPos.Y - _runwayDragStart.Y;

            // Move both start and end points
            _draggingRunway.StartX += deltaX;
            _draggingRunway.StartY += deltaY;
            _draggingRunway.EndX += deltaX;
            _draggingRunway.EndY += deltaY;

            // Update drag start for next move
            _runwayDragStart = currentPos;

            StatusText.Text = $"Design Mode: Moving runway '{_draggingRunway.Name}'";
            Redraw();
        }

        /// <summary>
        /// Finish runway drag operation
        /// </summary>
        private void FinishRunwayDrag()
        {
            if (_isDraggingRunway && _draggingRunway != null)
            {
                _isDraggingRunway = false;
                MarkDirty();

                StatusText.Text = $"Runway '{_draggingRunway.Name}' repositioned";
                _draggingRunway = null;
            }
        }
    }
}
