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
        
            if (HandleCraneKeyDown(e.Key)) return;
            
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
                case Key.F:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Ctrl+Shift+F: Regenerate Forklift Aisles
                        RegenerateForkliftAisles_Click(this, new RoutedEventArgs());
                    }
                    else
                    {
                        FlipSelectedNodeTerminals();
                    }
                    e.Handled = true;
                    break;
                case Key.N:
                    New_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.O:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Ctrl+Shift+O: Auto-Link Openings
                        AutoLinkOpenings_Click(this, new RoutedEventArgs());
                    }
                    else
                    {
                        Open_Click(this, new RoutedEventArgs());
                    }
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
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Ctrl+Shift+C: Regenerate Crane Coverage
                        RegenerateCraneCoverage_Click(this, new RoutedEventArgs());
                    }
                    else
                    {
                        Copy_Click(this, new RoutedEventArgs());
                    }
                    e.Handled = true;
                    break;

                case Key.V:
                    Paste_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.D:
                    Duplicate_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.A:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Ctrl+Shift+A: Regenerate AGV Network
                        RegenerateAGVNetwork_Click(this, new RoutedEventArgs());
                    }
                    else
                    {
                        SelectAll_Click(this, new RoutedEventArgs());
                    }
                    e.Handled = true;
                    break;

                case Key.G:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        Ungroup_Click(this, new RoutedEventArgs());
                    else
                        Group_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.P:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Ctrl+Shift+P: Regenerate Pedestrian Mesh
                        RegeneratePedestrianMesh_Click(this, new RoutedEventArgs());
                    }
                    e.Handled = true;
                    break;

                case Key.R:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Ctrl+Shift+R: Regenerate All Derived
                        RegenerateAll_Click(this, new RoutedEventArgs());
                    }
                    e.Handled = true;
                    break;

                case Key.OemPlus:
                case Key.Add:
                    ZoomIn_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.OemMinus:
                case Key.Subtract:
                    ZoomOut_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.D0:
                case Key.NumPad0:
                    ZoomFit_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                // Ctrl+Arrow for alignment (when Shift is also held)
                case Key.Left:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        AlignLeft_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Right:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        AlignRight_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        AlignTop_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Down:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
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
                    // Close properties panel before deleting
                    _panelManager?.Properties?.Hide();

                    // Delete openings if selected
                    if (_selectedOpeningId != null)
                        DeleteSelectedOpening();
                    // Delete walls if selected, otherwise delete nodes/paths
                    else if (HasWallSelection)
                        DeleteSelectedWalls();
                    else
                        Delete_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.Escape:
                    // Cancel pedestrian tools if active
                    if (_isDrawingWalkway)
                        CancelWalkway();
                    else if (_isDrawingCrossing)
                        CancelCrossing();
                    else if (_isDrawingSafetyZone)
                        CancelSafetyZone();
                    // Cancel AGV tools if active
                    else if (_isDrawingAGVPath)
                        CancelDrawingAGVPath();
                    else if (_isPlacingAGVStation)
                        CancelPlacingAGVStation();
                    else if (_isDrawingTrafficZone)
                        CancelDrawingTrafficZone();
                    // Cancel conveyor drawing if active
                    else if (_isDrawingConveyor)
                        CancelDrawingConveyor();
                    // Cancel opening placement if active
                    else if (_isPlacingOpening)
                        CancelPlacingOpening();
                    else
                        CancelCurrentOperation();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    // Finish pedestrian tools if active
                    if (_isDrawingWalkway)
                        FinishWalkway();
                    else if (_isDrawingCrossing)
                        FinishCrossing();
                    else if (_isDrawingSafetyZone)
                        FinishSafetyZone();
                    e.Handled = true;
                    break;

                case Key.F2:
                    ToggleLeftPanel_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F3:
                    ToggleRightPanel_Click(this, new RoutedEventArgs());
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

                case Key.M:
                    SetTool("move");
                    e.Handled = true;
                    break;

                case Key.W:
                    SetTool("wall");
                    e.Handled = true;
                    break;

                case Key.F:
                    // Toggle frictionless mode
                    if (FrictionlessModeToggle != null)
                    {
                        FrictionlessModeToggle.IsChecked = !FrictionlessModeToggle.IsChecked;
                        ToggleFrictionlessMode_Click(FrictionlessModeToggle, new RoutedEventArgs());
                    }
                    e.Handled = true;
                    break;

                case Key.C:
                    SetTool("corridor");
                    e.Handled = true;
                    break;

                case Key.Z:
                    SetTool("zone");
                    e.Handled = true;
                    break;

                case Key.R:
                    SetTool("measure");
                    e.Handled = true;
                    break;

                case Key.T:
                    SetTool("track");
                    e.Handled = true;
                    break;

                // Navigation with arrow keys
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    HandleArrowNavigation(e);
                    break;
            }
        }

        private void HandleArrowNavigation(KeyEventArgs e)
        {
            var selected = _selectionService.GetSelectedNodes(_layout);
            if (!selected.Any()) return;

            double delta = IsSnapGridEnabled() ? _layout.Canvas.GridSize : 1;

            SaveUndoState();

            foreach (var node in selected)
            {
                switch (e.Key)
                {
                    case Key.Left: node.Visual.X -= delta; break;
                    case Key.Right: node.Visual.X += delta; break;
                    case Key.Up: node.Visual.Y -= delta; break;
                    case Key.Down: node.Visual.Y += delta; break;
                }
            }

            MarkDirty();
            Redraw();
            e.Handled = true;
        }

        private void CancelCurrentOperation()
        {
            // Cancel any in-progress drawing
            _isDrawingPath = false;
            _isDrawingWall = false;
            
            // Cancel rubberband path if drawing
            EndRubberbandPath(completed: false);
            
            // Cancel track drawing if active
            if (_isDrawingTrack)
            {
                FinishTrackDrawing();
            }

            // Clear selection
            _selectionService.ClearSelection();
            ClearWallSelection();

            // Reset tool
            SetTool("select");

            // Hide any temp visuals
            Redraw();

            StatusText.Text = "Ready";
        }

        /// <summary>
        /// Called when Delete is pressed while Properties panel has focus
        /// </summary>
        public void DeleteSelectedFromPanel()
        {
            // Delete walls if selected, otherwise delete nodes/paths
            if (HasWallSelection)
                DeleteSelectedWalls();
            else
                Delete_Click(this, new RoutedEventArgs());
        }

        #endregion
    }
}
