using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using LayoutEditor.Helpers;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    /// <summary>
    /// Handles importing JSON layout files onto existing CAD floor plans.
    /// Places nodes, paths, groups over imported walls.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>Import JSON layout and merge with current layout (keeps walls)</summary>
        public void ImportJsonLayout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Layout Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Import Layout (Nodes/Paths) onto Floor Plan"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var json = File.ReadAllText(dialog.FileName);
                var imported = JsonHelper.Deserialize<LayoutData>(json);
                if (imported == null)
                {
                    MessageBox.Show("Failed to parse layout file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ShowLayoutImportDialog(imported, dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowLayoutImportDialog(LayoutData imported, string fileName)
        {
            var dlg = new Window
            {
                Title = "Import Layout onto Floor Plan",
                Width = 450,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(15) };
            var stack = new StackPanel();

            // File info
            stack.Children.Add(new TextBlock { Text = "Importing:", FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = Path.GetFileName(fileName), Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 10) });

            // Summary
            var summary = new Border { Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)), BorderBrush = Brushes.LightBlue, BorderThickness = new Thickness(1), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            var sumStack = new StackPanel();
            sumStack.Children.Add(new TextBlock { Text = "Layout Contents:", FontWeight = FontWeights.SemiBold });
            sumStack.Children.Add(new TextBlock { Text = $"  Nodes: {imported.Nodes.Count}", Margin = new Thickness(10, 2, 0, 0) });
            sumStack.Children.Add(new TextBlock { Text = $"  Paths: {imported.Paths.Count}", Margin = new Thickness(10, 2, 0, 0) });
            sumStack.Children.Add(new TextBlock { Text = $"  Groups/Cells: {imported.Groups.Count}", Margin = new Thickness(10, 2, 0, 0) });
            sumStack.Children.Add(new TextBlock { Text = $"  Walls: {imported.Walls.Count}", Margin = new Thickness(10, 2, 0, 0) });
            summary.Child = sumStack;
            stack.Children.Add(summary);

            // Current layout info
            var current = new Border { Background = new SolidColorBrush(Color.FromRgb(255, 250, 240)), BorderBrush = Brushes.Orange, BorderThickness = new Thickness(1), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            var curStack = new StackPanel();
            curStack.Children.Add(new TextBlock { Text = "Current Layout:", FontWeight = FontWeights.SemiBold });
            curStack.Children.Add(new TextBlock { Text = $"  Nodes: {_layout.Nodes.Count}", Margin = new Thickness(10, 2, 0, 0) });
            curStack.Children.Add(new TextBlock { Text = $"  Paths: {_layout.Paths.Count}", Margin = new Thickness(10, 2, 0, 0) });
            curStack.Children.Add(new TextBlock { Text = $"  Walls: {_layout.Walls.Count}", Margin = new Thickness(10, 2, 0, 0) });
            current.Child = curStack;
            stack.Children.Add(current);

            // Import options
            stack.Children.Add(new TextBlock { Text = "What to Import:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var chkNodes = new CheckBox { Content = $"Nodes ({imported.Nodes.Count})", IsChecked = true, Margin = new Thickness(10, 2, 0, 0) };
            var chkPaths = new CheckBox { Content = $"Paths ({imported.Paths.Count})", IsChecked = true, Margin = new Thickness(10, 2, 0, 0) };
            var chkGroups = new CheckBox { Content = $"Groups/Cells ({imported.Groups.Count})", IsChecked = true, Margin = new Thickness(10, 2, 0, 0) };
            var chkWalls = new CheckBox { Content = $"Walls ({imported.Walls.Count})", IsChecked = false, Margin = new Thickness(10, 2, 0, 0) };
            chkWalls.ToolTip = "Usually unchecked - your CAD walls are already imported";
            stack.Children.Add(chkNodes);
            stack.Children.Add(chkPaths);
            stack.Children.Add(chkGroups);
            stack.Children.Add(chkWalls);

            // Position offset
            stack.Children.Add(new TextBlock { Text = "Position Offset:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5) });
            var offsetPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };
            offsetPanel.Children.Add(new TextBlock { Text = "X:", VerticalAlignment = VerticalAlignment.Center, Width = 20 });
            var offsetX = new TextBox { Text = "0", Width = 60 };
            offsetPanel.Children.Add(offsetX);
            offsetPanel.Children.Add(new TextBlock { Text = "  Y:", VerticalAlignment = VerticalAlignment.Center });
            var offsetY = new TextBox { Text = "0", Width = 60 };
            offsetPanel.Children.Add(offsetY);
            offsetPanel.Children.Add(new TextBlock { Text = " px", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray });
            stack.Children.Add(offsetPanel);

            // Scale
            stack.Children.Add(new TextBlock { Text = "Scale:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5) });
            var scalePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };
            var scaleBox = new TextBox { Text = "1.0", Width = 60 };
            scalePanel.Children.Add(scaleBox);
            scalePanel.Children.Add(new TextBlock { Text = " (1.0 = no change)", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray });
            stack.Children.Add(scalePanel);

            // Clear existing
            stack.Children.Add(new TextBlock { Text = "Existing Elements:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5) });
            var chkClearNodes = new CheckBox { Content = "Clear existing nodes before import", IsChecked = false, Margin = new Thickness(10, 2, 0, 0) };
            var chkClearPaths = new CheckBox { Content = "Clear existing paths before import", IsChecked = false, Margin = new Thickness(10, 2, 0, 0) };
            stack.Children.Add(chkClearNodes);
            stack.Children.Add(chkClearPaths);

            // Auto-routing option
            stack.Children.Add(new TextBlock { Text = "Routing:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5) });
            var chkAutoRoute = new CheckBox { Content = "Auto-route paths around walls after import", IsChecked = true, Margin = new Thickness(10, 2, 0, 0) };
            chkAutoRoute.ToolTip = "Recalculate path waypoints to go through doorways and avoid walls";
            stack.Children.Add(chkAutoRoute);

            scroll.Content = stack;
            Grid.SetRow(scroll, 0);
            mainGrid.Children.Add(scroll);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(15) };
            var btnImport = new Button { Content = "Import", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            var btnCancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };

            btnImport.Click += (s, ev) =>
            {
                double.TryParse(offsetX.Text, out var ox);
                double.TryParse(offsetY.Text, out var oy);
                double.TryParse(scaleBox.Text, out var scale);
                if (scale <= 0) scale = 1.0;

                var options = new LayoutImportOptions
                {
                    ImportNodes = chkNodes.IsChecked == true,
                    ImportPaths = chkPaths.IsChecked == true,
                    ImportGroups = chkGroups.IsChecked == true,
                    ImportWalls = chkWalls.IsChecked == true,
                    ClearExistingNodes = chkClearNodes.IsChecked == true,
                    ClearExistingPaths = chkClearPaths.IsChecked == true,
                    OffsetX = ox,
                    OffsetY = oy,
                    Scale = scale,
                    AutoRouteAfterImport = chkAutoRoute.IsChecked == true
                };

                var result = ImportLayoutData(imported, options);
                dlg.DialogResult = true;

                StatusText.Text = $"Imported: {result.NodesImported} nodes, {result.PathsImported} paths, {result.GroupsImported} groups" +
                                  (result.PathsRerouted > 0 ? $", {result.PathsRerouted} paths rerouted" : "");
            };

            btnCancel.Click += (s, ev) => dlg.DialogResult = false;

            btnPanel.Children.Add(btnImport);
            btnPanel.Children.Add(btnCancel);
            Grid.SetRow(btnPanel, 1);
            mainGrid.Children.Add(btnPanel);

            dlg.Content = mainGrid;
            dlg.ShowDialog();
        }

        private class LayoutImportOptions
        {
            public bool ImportNodes { get; set; } = true;
            public bool ImportPaths { get; set; } = true;
            public bool ImportGroups { get; set; } = true;
            public bool ImportWalls { get; set; } = false;
            public bool ClearExistingNodes { get; set; } = false;
            public bool ClearExistingPaths { get; set; } = false;
            public double OffsetX { get; set; } = 0;
            public double OffsetY { get; set; } = 0;
            public double Scale { get; set; } = 1.0;
            public bool AutoRouteAfterImport { get; set; } = true;
        }

        private class LayoutImportResult
        {
            public int NodesImported { get; set; }
            public int PathsImported { get; set; }
            public int GroupsImported { get; set; }
            public int WallsImported { get; set; }
            public int PathsRerouted { get; set; }
        }

        private LayoutImportResult ImportLayoutData(LayoutData source, LayoutImportOptions options)
        {
            SaveUndoState();
            var result = new LayoutImportResult();

            // Build ID mapping for imported nodes (old ID -> new ID)
            var nodeIdMap = new Dictionary<string, string>();

            // Clear existing if requested
            if (options.ClearExistingNodes)
            {
                _layout.Nodes.Clear();
                _layout.Groups.Clear();
            }
            if (options.ClearExistingPaths)
                _layout.Paths.Clear();

            // Import nodes
            if (options.ImportNodes)
            {
                foreach (var srcNode in source.Nodes)
                {
                    var newNode = CloneNode(srcNode);
                    newNode.Id = Guid.NewGuid().ToString();
                    nodeIdMap[srcNode.Id] = newNode.Id;

                    // Apply offset and scale
                    newNode.Visual.X = (srcNode.Visual.X * options.Scale) + options.OffsetX;
                    newNode.Visual.Y = (srcNode.Visual.Y * options.Scale) + options.OffsetY;
                    newNode.Visual.Width = srcNode.Visual.Width * options.Scale;
                    newNode.Visual.Height = srcNode.Visual.Height * options.Scale;

                    _layout.Nodes.Add(newNode);
                    result.NodesImported++;
                }
            }

            // Import paths (with updated node references)
            if (options.ImportPaths)
            {
                foreach (var srcPath in source.Paths)
                {
                    // Map old node IDs to new ones
                    var fromId = nodeIdMap.TryGetValue(srcPath.From, out var newFrom) ? newFrom : srcPath.From;
                    var toId = nodeIdMap.TryGetValue(srcPath.To, out var newTo) ? newTo : srcPath.To;

                    // Skip if nodes don't exist
                    if (!_layout.Nodes.Any(n => n.Id == fromId) || !_layout.Nodes.Any(n => n.Id == toId))
                        continue;

                    var newPath = ClonePath(srcPath);
                    newPath.Id = Guid.NewGuid().ToString();
                    newPath.From = fromId;
                    newPath.To = toId;

                    // Clear waypoints - will be recalculated if auto-route enabled
                    newPath.Visual.Waypoints.Clear();

                    _layout.Paths.Add(newPath);
                    result.PathsImported++;
                }
            }

            // Import groups (with updated member references)
            if (options.ImportGroups)
            {
                foreach (var srcGroup in source.Groups)
                {
                    var newGroup = CloneGroup(srcGroup);
                    newGroup.Id = Guid.NewGuid().ToString();

                    // Update member IDs
                    newGroup.Members.Clear();
                    foreach (var memberId in srcGroup.Members)
                    {
                        if (nodeIdMap.TryGetValue(memberId, out var newMemberId))
                            newGroup.Members.Add(newMemberId);
                    }

                    // Update internal path IDs
                    newGroup.InternalPaths.Clear();
                    foreach (var pathId in srcGroup.InternalPaths)
                    {
                        var matchingPath = _layout.Paths.FirstOrDefault(p =>
                            source.Paths.Any(sp => sp.Id == pathId && 
                                nodeIdMap.ContainsKey(sp.From) && nodeIdMap.ContainsKey(sp.To) &&
                                p.From == nodeIdMap[sp.From] && p.To == nodeIdMap[sp.To]));
                        if (matchingPath != null)
                            newGroup.InternalPaths.Add(matchingPath.Id);
                    }

                    if (newGroup.Members.Count > 0)
                    {
                        _layout.Groups.Add(newGroup);
                        result.GroupsImported++;
                    }
                }
            }

            // Import walls (usually not needed if CAD already imported)
            if (options.ImportWalls)
            {
                foreach (var srcWall in source.Walls)
                {
                    var newWall = new WallData
                    {
                        Id = Guid.NewGuid().ToString(),
                        X1 = (srcWall.X1 * options.Scale) + options.OffsetX,
                        Y1 = (srcWall.Y1 * options.Scale) + options.OffsetY,
                        X2 = (srcWall.X2 * options.Scale) + options.OffsetX,
                        Y2 = (srcWall.Y2 * options.Scale) + options.OffsetY,
                        Thickness = srcWall.Thickness,
                        WallType = srcWall.WallType,
                        Color = srcWall.Color,
                        DashPattern = srcWall.DashPattern,
                        Layer = srcWall.Layer
                    };
                    _layout.Walls.Add(newWall);
                    result.WallsImported++;
                }
            }

            // Auto-route paths around walls
            if (options.AutoRouteAfterImport && _layout.Walls.Count > 0)
            {
                var router = new AutoRouterService(_layout);
                result.PathsRerouted = router.RerouteAllPaths();
            }

            MarkDirty();
            RefreshAll();
            return result;
        }

        private NodeData CloneNode(NodeData src)
        {
            return new NodeData
            {
                Id = src.Id,
                Type = src.Type,
                Name = src.Name,
                Label = src.Label,
                Visual = new NodeVisual
                {
                    X = src.Visual.X,
                    Y = src.Visual.Y,
                    Width = src.Visual.Width,
                    Height = src.Visual.Height,
                    Rotation = src.Visual.Rotation,
                    Icon = src.Visual.Icon,
                    Color = src.Visual.Color,
                    InputTerminalPosition = src.Visual.InputTerminalPosition,
                    OutputTerminalPosition = src.Visual.OutputTerminalPosition
                },
                Simulation = new SimulationParams
                {
                    ProcessTime = src.Simulation.ProcessTime,
                    Capacity = src.Simulation.Capacity,
                    Servers = src.Simulation.Servers,
                    SetupTime = src.Simulation.SetupTime,
                    BatchSize = src.Simulation.BatchSize
                }
            };
        }

        private PathData ClonePath(PathData src)
        {
            return new PathData
            {
                Id = src.Id,
                From = src.From,
                To = src.To,
                FromTerminal = src.FromTerminal,
                ToTerminal = src.ToTerminal,
                PathType = src.PathType,
                RoutingMode = src.RoutingMode,
                Visual = new PathVisual
                {
                    Color = src.Visual.Color,
                    Thickness = src.Visual.Thickness
                },
                Simulation = new PathSimulation
                {
                    Distance = src.Simulation.Distance,
                    TransportType = src.Simulation.TransportType,
                    Speed = src.Simulation.Speed,
                    Capacity = src.Simulation.Capacity
                }
            };
        }

        private GroupData CloneGroup(GroupData src)
        {
            return new GroupData
            {
                Id = src.Id,
                Name = src.Name,
                CellType = src.CellType,
                CellIndex = src.CellIndex,
                Color = src.Color,
                IsCell = src.IsCell,
                InputTerminalPosition = src.InputTerminalPosition,
                OutputTerminalPosition = src.OutputTerminalPosition
            };
        }
    }
}
