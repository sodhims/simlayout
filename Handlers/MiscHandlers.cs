using System;
using System.Collections.Generic;
using System.Windows;
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

        private void ToggleOperationalClearance_Click(object sender, RoutedEventArgs e)
        {
            _equipmentRenderer.ShowOperationalClearance = !_equipmentRenderer.ShowOperationalClearance;
            Redraw();
        }

        private void ToggleMaintenanceClearance_Click(object sender, RoutedEventArgs e)
        {
            _equipmentRenderer.ShowMaintenanceClearance = !_equipmentRenderer.ShowMaintenanceClearance;
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
            
            // Group nodes by type and number them
            var typeCounters = new Dictionary<string, int>();
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
                    Models.NodeTypes.Robot => "R",
                    Models.NodeTypes.Agv => "AGV",
                    Models.NodeTypes.Assembly => "A",
                    _ => "N"
                };
                
                if (!typeCounters.ContainsKey(prefix))
                    typeCounters[prefix] = 1;
                    
                node.Name = $"{prefix}{typeCounters[prefix]++}";
            }
            
            MarkDirty();
            Redraw();
            _panelManager?.RefreshAll();
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
            Redraw();
            _panelManager?.RefreshAll();
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
            if (_selectionService == null) return;
            
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

        #region Editor Settings

        private void EditorSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.EditorSettingsDialog(Models.EditorSettings.Instance);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                ApplyEditorSettings();
            }
        }

        private void ApplyEditorSettings()
        {
            var settings = Models.EditorSettings.Instance;
            _panelManager?.ApplyVisibility(settings);
            Redraw();
            StatusText.Text = "Editor settings applied";
        }

        #endregion

        #region Floor Handlers

        private void Floor_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_layout == null || StatusText == null) return;
            
            if (FloorCombo?.SelectedItem is ComboBoxItem item)
            {
                var floorTag = item.Tag?.ToString() ?? "all";
                StatusText.Text = floorTag == "all" ? "Showing all floors" : $"Showing floor: {item.Content}";
                Redraw();
            }
        }

        private void ManageFloors_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Manage Floors",
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stack = new StackPanel { Margin = new Thickness(15) };
            stack.Children.Add(new TextBlock 
            { 
                Text = "Floor Management", 
                FontWeight = FontWeights.Bold, 
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10) 
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Define floors for your layout. Elements can be assigned to specific floors.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            });

            var floorList = new ListBox { Height = 120, Margin = new Thickness(0, 0, 0, 10) };
            floorList.Items.Add("Ground (default)");
            floorList.Items.Add("Floor 2");
            floorList.Items.Add("Basement");
            stack.Children.Add(floorList);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var addBtn = new Button { Content = "Add Floor", Width = 80, Margin = new Thickness(0, 0, 5, 0) };
            var closeBtn = new Button { Content = "Close", Width = 80 };
            closeBtn.Click += (s, args) => dialog.Close();
            btnPanel.Children.Add(addBtn);
            btnPanel.Children.Add(closeBtn);
            stack.Children.Add(btnPanel);

            dialog.Content = stack;
            dialog.ShowDialog();
        }

        private void GridSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_layout == null) return;
            if (GridSizeCombo?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (int.TryParse(item.Tag.ToString(), out int gridSize))
                {
                    _layout.Canvas.GridSize = gridSize;
                    Redraw();
                    StatusText.Text = $"Grid size: {gridSize}";
                }
            }
        }

        #endregion
    }
}
