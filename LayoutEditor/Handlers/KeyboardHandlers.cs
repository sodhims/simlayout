using System.Windows;
using System.Linq;
using System.Windows.Input;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Keyboard Shortcuts

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl modifier
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                HandleCtrlShortcuts(e);
            }
            else
            {
                HandleSimpleShortcuts(e);
            }
        }

        private void HandleCtrlShortcuts(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.N:
                    New_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.O:
                    Open_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.S:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        SaveAs_Click(this, new RoutedEventArgs());
                    else
                        Save_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Z:
                    Undo_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Y:
                    Redo_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.X:
                    Cut_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.C:
                    Copy_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.V:
                    Paste_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.A:
                    SelectAll_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.T:
                    ToggleToolboxPanel();
                    e.Handled = true;
                    break;

                case Key.D1:
                    TogglePropertiesPanel();
                    e.Handled = true;
                    break;

                case Key.D2:
                    ToggleExplorerPanel();
                    e.Handled = true;
                    break;

                case Key.D3:
                    ToggleLayoutsPanel();
                    e.Handled = true;
                    break;

                // case Key.T:
                //     ToggleToolboxPanel();
                //     e.Handled = true;
                //     break;

                case Key.D:
                    Duplicate_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.G:
                    DefineCell_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                // Alignment shortcuts
                case Key.Left:
                    AlignLeft_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Right:
                    AlignRight_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Up:
                    AlignTop_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Down:
                    AlignBottom_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }

        private void HandleSimpleShortcuts(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    Delete_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Escape:
                    CancelCurrentOperation();
                    e.Handled = true;
                    break;

                case Key.F5:
                    Validate_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                // Tool shortcuts
                case Key.V:
                    SetTool("select");
                    e.Handled = true;
                    break;

                case Key.A:
                    SetTool("area");
                    e.Handled = true;
                    break;

                case Key.P:
                    SetTool("path");
                    e.Handled = true;
                    break;

                case Key.H:
                    SetTool("pan");
                    e.Handled = true;
                    break;

                case Key.W:
                    SetTool("wall");
                    e.Handled = true;
                    break;

                case Key.R:
                    SetTool("measure");
                    e.Handled = true;
                    break;

                case Key.E:
                    // Toggle path edit mode
                    if (PathEditMode != null) PathEditMode.IsChecked = !PathEditMode.IsChecked;
                    e.Handled = true;
                    break;

                // Arrow keys for nudge
                case Key.Left:
                    NudgeSelection(-10, 0);
                    e.Handled = true;
                    break;

                case Key.Right:
                    NudgeSelection(10, 0);
                    e.Handled = true;
                    break;

                case Key.Up:
                    NudgeSelection(0, -10);
                    e.Handled = true;
                    break;

                case Key.Down:
                    NudgeSelection(0, 10);
                    e.Handled = true;
                    break;
            }
        }

        private void CancelCurrentOperation()
        {
            if (_pendingNodeType != null)
            {
                CancelNodePlacement();
            }
            else if (_isDrawingPath)
            {
                CancelPathDrawing();
            }
            else if (_isDrawingWall)
            {
                CancelWallDrawing();
            }
            else if (_isDrawingMeasurement)
            {
                CancelMeasurement();
            }
            else if (_isPathEditMode)
            {
                if (PathEditMode != null) PathEditMode.IsChecked = false;
            }
            else if (_selectionService.IsEditingCell)
            {
                _selectionService.ExitCellEditMode();
                StatusText.Text = "Exited cell edit mode";
                Redraw();
            }
            else
            {
                _selectionService.ClearSelection();
                UpdateSelectionVisuals();
                UpdatePropertyPanel();
            }
        }

        private void NudgeSelection(double dx, double dy)
        {
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count == 0) return;

            SaveUndoState();

            foreach (var node in nodes)
            {
                node.Visual.X += dx;
                node.Visual.Y += dy;
            }

            MarkDirty();
            Redraw();
        }

        #endregion
    }
}
