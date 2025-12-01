using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
            Redraw();  // Force redraw
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
            Redraw();  // Force redraw
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
            
            // Apply panel visibility
            _panelManager?.ApplyVisibility(settings);
            
            // Refresh canvas to apply line thickness settings
            Redraw();
            
            StatusText.Text = "Editor settings applied";
        }

        #endregion

        #region Node Palette

        private Window? _nodePaletteWindow = null;

        private void ShowNodePalette_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // If already open, bring to front
                if (_nodePaletteWindow != null && _nodePaletteWindow.IsVisible)
                {
                    _nodePaletteWindow.Activate();
                    return;
                }

                _nodePaletteWindow = new Window
                {
                    Title = "Node Palette - Drag to Canvas",
                    Width = 420,
                    Height = 520,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.CanResize,
                    ShowInTaskbar = false
                };

                var mainStack = new StackPanel { Margin = new Thickness(8) };
                
                // Instructions
                var instructions = new TextBlock
                {
                    Text = "Drag icons to canvas to add nodes. Double-click for quick add.",
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                mainStack.Children.Add(instructions);

                var scroll = new ScrollViewer 
                { 
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Height = 440
                };
                var categoriesStack = new StackPanel();

            // Get all categories from IconLibrary
            var categories = Models.IconLibrary.GetCategories();

            foreach (var category in categories)
            {
                var icons = Models.IconLibrary.GetByCategory(category).ToList();
                if (icons.Count == 0) continue;

                // Category header
                var header = new TextBlock
                {
                    Text = category,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.DarkSlateGray),
                    Margin = new Thickness(0, 10, 0, 4)
                };
                categoriesStack.Children.Add(header);

                // Icons wrap panel
                var wrap = new WrapPanel();
                foreach (var kvp in icons)
                {
                    var iconKey = kvp.Key;
                    var iconDef = kvp.Value;
                    
                    var iconButton = CreatePaletteIconButton(iconKey, iconDef);
                    wrap.Children.Add(iconButton);
                }
                categoriesStack.Children.Add(wrap);
            }

            scroll.Content = categoriesStack;
            mainStack.Children.Add(scroll);
            _nodePaletteWindow.Content = mainStack;

            _nodePaletteWindow.Closed += (s, args) => _nodePaletteWindow = null;
            _nodePaletteWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening palette: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreatePaletteIconButton(string iconKey, Models.IconDefinition iconDef)
        {
            var color = (Color)ColorConverter.ConvertFromString(iconDef.DefaultColor);
            var brush = new SolidColorBrush(color);

            // Create the icon path
            var iconPath = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(iconDef.Path),
                Stroke = iconDef.IsFilled ? null : brush,
                Fill = iconDef.IsFilled ? brush : null,
                StrokeThickness = 1.5,
                Width = 28,
                Height = 28,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(4)
            };

            // Label under icon
            var label = new TextBlock
            {
                Text = iconDef.Name,
                FontSize = 8,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 60,
                Foreground = Brushes.DimGray
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(iconPath);
            stack.Children.Add(label);

            var border = new Border
            {
                Width = 70,
                Height = 58,
                Margin = new Thickness(2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4),
                Child = stack,
                Tag = iconKey,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = $"{iconDef.Name}\nDrag to canvas or double-click"
            };

            // Hover effect
            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(240, 248, 255));
                border.BorderBrush = brush;
                border.BorderThickness = new Thickness(2);
            };
            border.MouseLeave += (s, e) =>
            {
                border.Background = Brushes.White;
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                border.BorderThickness = new Thickness(1);
            };

            // Double-click to add at center
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    // Double-click - add at canvas center
                    var nodeType = GetNodeTypeFromIcon(iconKey);
                    AddNodeAtCanvasCenter(nodeType, iconKey, iconDef.DefaultColor);
                    e.Handled = true;
                }
                else
                {
                    // Start drag
                    var data = new DataObject();
                    data.SetData("NodeIcon", iconKey);
                    data.SetData("NodeColor", iconDef.DefaultColor);
                    data.SetData("NodeType", GetNodeTypeFromIcon(iconKey));
                    DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
                }
            };

            return border;
        }

        private string GetNodeTypeFromIcon(string iconKey)
        {
            // Map icon key to node type
            if (iconKey.Contains("source")) return Models.NodeTypes.Source;
            if (iconKey.Contains("sink") || iconKey.Contains("exit")) return Models.NodeTypes.Sink;
            if (iconKey.Contains("buffer") || iconKey.Contains("queue") || iconKey.Contains("fifo")) return Models.NodeTypes.Buffer;
            if (iconKey.Contains("conveyor") || iconKey.Contains("belt") || iconKey.Contains("roller")) return Models.NodeTypes.Conveyor;
            if (iconKey.Contains("agv") || iconKey.Contains("amr")) return Models.NodeTypes.AgvStation;
            if (iconKey.Contains("robot") || iconKey.Contains("arm")) return Models.NodeTypes.Robot;
            if (iconKey.Contains("inspect") || iconKey.Contains("camera") || iconKey.Contains("gauge")) return Models.NodeTypes.Inspection;
            if (iconKey.Contains("shelf") || iconKey.Contains("storage") || iconKey.Contains("warehouse")) return Models.NodeTypes.Storage;
            if (iconKey.Contains("work") || iconKey.Contains("manual") || iconKey.Contains("operator") || iconKey.Contains("person")) return Models.NodeTypes.Workstation;
            if (iconKey.Contains("assembly") || iconKey.Contains("torque") || iconKey.Contains("press_fit")) return Models.NodeTypes.Assembly;
            if (iconKey.Contains("junction") || iconKey.Contains("diverter") || iconKey.Contains("merge")) return Models.NodeTypes.Junction;
            return Models.NodeTypes.Machine;
        }

        private void AddNodeAtCanvasCenter(string nodeType, string iconKey, string color)
        {
            if (_layout == null || EditorCanvas == null) return;

            // Get visible canvas center
            var scrollViewer = CanvasScroller;
            double centerX = scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth / 2;
            double centerY = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight / 2;

            // Adjust for zoom
            centerX /= CanvasScale.ScaleX;
            centerY /= CanvasScale.ScaleY;

            // Snap to grid
            centerX = Math.Round(centerX / _layout.Canvas.GridSize) * _layout.Canvas.GridSize;
            centerY = Math.Round(centerY / _layout.Canvas.GridSize) * _layout.Canvas.GridSize;

            SaveUndoState();

            var node = new Models.NodeData
            {
                Id = Guid.NewGuid().ToString(),
                Type = nodeType,
                Name = $"{nodeType}_{_layout.Nodes.Count + 1}",
                Visual = new Models.NodeVisual
                {
                    X = centerX - 25,
                    Y = centerY - 30,
                    Width = 50,
                    Height = 60,
                    Icon = iconKey,
                    Color = color
                }
            };

            _layout.Nodes.Add(node);
            MarkDirty();
            RefreshAll();
            _selectionService?.SelectNode(node.Id);
            StatusText.Text = $"Added {nodeType}: {node.Name}";
        }

        #endregion

        #region Floor Handlers

        private void Floor_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Guard against initialization-time calls
            if (_layout == null || StatusText == null) return;
            
            if (FloorCombo?.SelectedItem is ComboBoxItem item)
            {
                var floorTag = item.Tag?.ToString() ?? "all";
                // TODO: Filter elements by floor
                StatusText.Text = floorTag == "all" ? "Showing all floors" : $"Showing floor: {item.Content}";
                Redraw();
            }
        }

        private void ManageFloors_Click(object sender, RoutedEventArgs e)
        {
            // Simple floor management dialog
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
