using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private EOTCraneData? _draggingCrane;
        private bool _isDraggingCrane;

        /// <summary>
        /// Handle click on EOT crane - animate in F mode, drag runway in D mode
        /// </summary>
        private void HandleEOTCraneClick(EOTCraneData crane, MouseButtonEventArgs e)
        {
            if (_layout == null) return;

            // Check if layer is locked
            if (_transportArchitectureLayerManager != null && _transportArchitectureLayerManager.IsLocked(crane.ArchitectureLayer))
            {
                StatusText.Text = $"EOT crane '{crane.Name}' is on a locked layer";
                e.Handled = true;
                return;
            }

            // In design mode, start dragging the runway (bay movement)
            if (_layout.DesignMode)
            {
                // Find the crane's runway and start dragging it
                var runway = _layout.Runways?.FirstOrDefault(r => r.Id == crane.RunwayId);
                if (runway != null)
                {
                    var clickPos = e.GetPosition(EditorCanvas);
                    _draggingRunway = runway;
                    _isDraggingRunway = true;
                    _runwayDragStart = clickPos;
                    EditorCanvas.CaptureMouse();
                    SaveUndoState();

                    StatusText.Text = $"Design Mode: Dragging EOT bay '{crane.Name}' (runway '{runway.Name}')";
                    e.Handled = true;
                    return;
                }
                StatusText.Text = $"Design Mode: EOT crane '{crane.Name}' has no runway";
                e.Handled = true;
                return;
            }

            // In frictionless mode, toggle animation (click to start/stop)
            if (_layout.FrictionlessMode)
            {
                DebugLogger.Log($"[EOTCrane] HandleClick in F mode, calling animation service");
                if (_animationService != null)
                {
                    _animationService.ToggleEOTCraneAnimation(crane);
                }
                else
                {
                    DebugLogger.Log($"[EOTCrane] _animationService is null!");
                }
                e.Handled = true;
                return;
            }

            // Outside both modes, just show info
            StatusText.Text = $"EOT crane '{crane.Name}' - Press F to animate, D to reposition bay";
            e.Handled = true;
        }

        /// <summary>
        /// Continue dragging crane (constrained movement along runway in frictionless mode)
        /// </summary>
        private void DragEOTCrane(Point currentPos)
        {
            if (!_isDraggingCrane || _draggingCrane == null || _layout == null)
            {
                DebugLogger.Log($"[EOTCrane] DragEOTCrane early return: _isDraggingCrane={_isDraggingCrane}, _draggingCrane={_draggingCrane != null}, _layout={_layout != null}");
                return;
            }

            var runway = _layout.Runways?.FirstOrDefault(r => r.Id == _draggingCrane.RunwayId);
            if (runway == null)
            {
                DebugLogger.Log($"[EOTCrane] DragEOTCrane: runway not found for crane '{_draggingCrane.Name}', RunwayId='{_draggingCrane.RunwayId}'");
                return;
            }

            DebugLogger.Log($"[EOTCrane] DragEOTCrane at ({currentPos.X:F1}, {currentPos.Y:F1}), crane='{_draggingCrane.Name}', current BridgePosition={_draggingCrane.BridgePosition:F3}");

            // Frictionless mode: Constrained movement along runway
            var constrainedDragService = new ConstrainedDragService(_layout);

            // Project mouse position onto the runway constraint
            var (constrainedPos, parameter) = constrainedDragService.ProjectToConstraint(_draggingCrane, currentPos);
            DebugLogger.Log($"[EOTCrane] Projected to ({constrainedPos.X:F1}, {constrainedPos.Y:F1}), parameter={parameter:F3}");

            // Update crane position (automatically clamped to zone)
            bool updated = constrainedDragService.UpdateEntityPosition(_draggingCrane, currentPos);
            DebugLogger.Log($"[EOTCrane] UpdateEntityPosition returned {updated}, new BridgePosition={_draggingCrane.BridgePosition:F3}");

            // Show snap indicator at constrained position
            ShowConstraintSnapIndicator(constrainedPos);

            // Update status with position info
            StatusText.Text = $"Crane position: {(_draggingCrane.BridgePosition * 100):F1}% along runway";

            Redraw();
        }

        /// <summary>
        /// Finish crane drag operation
        /// </summary>
        private void FinishEOTCraneDrag()
        {
            if (_isDraggingCrane && _draggingCrane != null)
            {
                _isDraggingCrane = false;
                HideConstraintSnapIndicator();
                MarkDirty();

                StatusText.Text = $"EOT crane '{_draggingCrane.Name}' positioned at {(_draggingCrane.BridgePosition * 100):F1}%";
                _draggingCrane = null;
            }
        }
    }
}
