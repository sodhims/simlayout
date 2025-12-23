using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private JibCraneData? _draggingJibCrane;
        private bool _isDraggingJibCrane;

        /// <summary>
        /// Handle click on jib crane - animate in F mode, drag in D mode
        /// </summary>
        private void HandleJibCraneClick(JibCraneData jib, MouseButtonEventArgs e)
        {
            if (_layout == null) return;

            // Check if layer is locked
            if (_transportArchitectureLayerManager != null && _transportArchitectureLayerManager.IsLocked(jib.ArchitectureLayer))
            {
                StatusText.Text = $"Jib crane '{jib.Name}' is on a locked layer";
                e.Handled = true;
                return;
            }

            // In frictionless mode, toggle animation (click to start/stop rotation)
            if (_layout.FrictionlessMode)
            {
                if (_animationService != null)
                {
                    _animationService.ToggleJibCraneAnimation(jib);
                }
                e.Handled = true;
                return;
            }

            // In design mode, allow dragging to reposition
            if (_layout.DesignMode)
            {
                _draggingJibCrane = jib;
                _isDraggingJibCrane = true;
                EditorCanvas.CaptureMouse();
                SaveUndoState();

                StatusText.Text = $"Design Mode: Dragging jib crane '{jib.Name}' - free repositioning";
                e.Handled = true;
                return;
            }

            // Outside both modes, just show info
            StatusText.Text = $"Jib crane '{jib.Name}' - Press F to animate, D to reposition";
            e.Handled = true;
        }

        /// <summary>
        /// Continue dragging jib crane (free movement in design mode)
        /// </summary>
        private void DragJibCrane(Point currentPos)
        {
            if (!_isDraggingJibCrane || _draggingJibCrane == null || _layout == null) return;

            // Update jib crane center position
            _draggingJibCrane.CenterX = currentPos.X;
            _draggingJibCrane.CenterY = currentPos.Y;

            StatusText.Text = $"Design Mode: Jib crane at ({currentPos.X:F1}, {currentPos.Y:F1})";
            Redraw();
        }

        /// <summary>
        /// Finish jib crane drag operation
        /// </summary>
        private void FinishJibCraneDrag()
        {
            if (_isDraggingJibCrane && _draggingJibCrane != null)
            {
                _isDraggingJibCrane = false;
                MarkDirty();

                StatusText.Text = $"Jib crane '{_draggingJibCrane.Name}' repositioned";
                _draggingJibCrane = null;
            }
        }
    }
}
