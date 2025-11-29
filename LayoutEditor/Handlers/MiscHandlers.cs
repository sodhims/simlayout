using System.Windows;
using System.Linq;
using System.Windows.Controls;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Group/Cell Handlers

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            CreateGroup_Click(sender, e);
        }

        #endregion

        #region View Toggle Handlers

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0);
        }

        private void ToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;
            _layout.Canvas.ShowGrid = !_layout.Canvas.ShowGrid;
            Redraw();
        }

        private void ToggleRulers_Click(object sender, RoutedEventArgs e)
        {
            if (HorizontalRuler == null || VerticalRuler == null) return;
            var show = !HorizontalRuler.IsVisible;
            HorizontalRuler.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            VerticalRuler.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleMinimap_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Toggle minimap visibility
            if (StatusText != null) StatusText.Text = "Minimap toggled";
        }

        private void ToggleLabels_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;
            _layout.Display.Layers.Labels = !_layout.Display.Layers.Labels;
            Redraw();
        }

        #endregion

        #region Tools Menu Handlers

        private void ApplyCanonicalNames_Click(object sender, RoutedEventArgs e)
        {
            ApplyCanonicalNamesNodes_Click(sender, e);
            ApplyCanonicalNamesCells_Click(sender, e);
        }

        private void ApplyCanonicalNamesNodes_Click(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            int counter = 1;
            foreach (var node in _layout.Nodes)
            {
                var prefix = node.Type switch
                {
                    Models.NodeTypes.Source => "SRC",
                    Models.NodeTypes.Sink => "SNK",
                    Models.NodeTypes.Machine => "M",
                    Models.NodeTypes.Buffer => "Q",
                    Models.NodeTypes.Workstation => "W",
                    Models.NodeTypes.Inspection => "I",
                    _ => "N"
                };
                node.Name = $"{prefix}{counter++}";
            }
            MarkDirty();
            RefreshAll();
            StatusText.Text = "Applied canonical names to nodes";
        }

        private void ApplyCanonicalNamesCells_Click(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            int counter = 1;
            foreach (var group in _layout.Groups)
            {
                if (group.IsCell)
                {
                    group.Name = $"Cell_{counter++}";
                }
            }
            MarkDirty();
            RefreshAll();
            StatusText.Text = "Applied canonical names to cells";
        }

        private void ResetNames_Click(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            foreach (var node in _layout.Nodes)
            {
                node.Name = $"{node.Type}_{node.Id.Substring(0, 4)}";
            }
            MarkDirty();
            RefreshAll();
            StatusText.Text = "Reset all names";
        }

        private void AutoGeneratePaths_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Auto-generate paths based on node layout
            StatusText.Text = "Auto-generate paths not yet implemented";
        }

        private void ClearPaths_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Paths.Count == 0) return;

            var result = MessageBox.Show(
                $"Delete all {_layout.Paths.Count} paths?",
                "Clear Paths",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                SaveUndoState();
                _layout.Paths.Clear();
                MarkDirty();
                RefreshAll();
                StatusText.Text = "All paths cleared";
            }
        }

        private void ResourcePools_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open resource pools dialog
            StatusText.Text = "Resource pools dialog not yet implemented";
        }

        private void TemplatesManager_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open templates manager dialog
            StatusText.Text = "Templates manager not yet implemented";
        }

        private void LayoutProperties_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open layout properties dialog
            StatusText.Text = "Layout properties dialog not yet implemented";
        }

        private void ToggleValidation_Click(object sender, RoutedEventArgs e)
        {
            Validate_Click(sender, e);
        }

        #endregion

        #region Help Menu Handlers

        private void Shortcuts_Click(object sender, RoutedEventArgs e)
        {
            var shortcuts = @"Keyboard Shortcuts:

Ctrl+N - New layout
Ctrl+O - Open layout
Ctrl+S - Save layout
Ctrl+Z - Undo
Ctrl+Y - Redo
Ctrl+C - Copy
Ctrl+V - Paste
Ctrl+X - Cut
Ctrl+A - Select all
Ctrl+D - Duplicate
Ctrl+G - Create cell
Delete - Delete selected

S - Select tool
P - Path tool
H - Pan tool

Arrow keys - Nudge selection
Ctrl+Arrows - Align selection";

            MessageBox.Show(shortcuts, "Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UserGuide_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open user guide
            StatusText.Text = "User guide not yet implemented";
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Simulation Layout Editor\nVersion 2.1\n\nA visual editor for creating factory simulation layouts.",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Tool Selection Handlers

        private void Tool_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectionService == null) return;  // Not initialized yet
            
            if (sender is RadioButton rb && rb.Tag is string tool)
            {
                SetTool(tool);
            }
        }

        #endregion

        #region Path Drawing Handlers

        private void StartPath_Click(object sender, RoutedEventArgs e)
        {
            StartPathDrawing_Click(sender, e);
        }

        private void EndPath_Click(object sender, RoutedEventArgs e)
        {
            // Complete path if in progress
            if (_isDrawingPath && _pathStartNodeId != null)
            {
                StatusText.Text = "Click a destination node to complete path";
            }
        }

        private void CancelPath_Click(object sender, RoutedEventArgs e)
        {
            CancelPathDrawing();
        }

        #endregion
    }
}
