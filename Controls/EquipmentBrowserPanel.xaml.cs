using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    public partial class EquipmentBrowserPanel : UserControl
    {
        private LayoutData? _layout;
        private object? _selectedItem;
        private string _currentCategory = "EOTCranes";

        public event EventHandler<object>? ItemSelected;
        public event EventHandler<object>? ItemDoubleClicked;
        public event EventHandler<object>? EditRequested;
        public event EventHandler? DataChanged;

        public EquipmentBrowserPanel()
        {
            InitializeComponent();
            CategoryCombo.SelectedIndex = 0;
        }

        public void SetLayout(LayoutData layout)
        {
            _layout = layout;
            RefreshTree();
            RefreshGrid();
        }

        public void RefreshTree()
        {
            if (_layout == null) return;

            EquipmentTree.Items.Clear();
            int totalCount = 0;
            string searchFilter = SearchBox.Text?.ToLower() ?? "";

            // EOT Cranes
            var cranesCategory = CreateCategory("🏗️ EOT Cranes", _layout.EOTCranes.Count);
            foreach (var crane in _layout.EOTCranes.Where(c => MatchesFilter(c.Name, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {crane.Name}",
                    Tag = crane,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(crane.Color ?? "#E67E22"))
                };
                cranesCategory.Items.Add(item);
                totalCount++;
            }
            if (cranesCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(cranesCategory);

            // Jib Cranes
            var jibCategory = CreateCategory("🔄 Jib Cranes", _layout.JibCranes.Count);
            foreach (var jib in _layout.JibCranes.Where(j => MatchesFilter(j.Name, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {jib.Name}",
                    Tag = jib,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(jib.Color ?? "#27AE60"))
                };
                jibCategory.Items.Add(item);
                totalCount++;
            }
            if (jibCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(jibCategory);

            // Runways
            var runwayCategory = CreateCategory("═ Runways", _layout.Runways.Count);
            foreach (var runway in _layout.Runways.Where(r => MatchesFilter(r.Name, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {runway.Name}",
                    Tag = runway
                };
                runwayCategory.Items.Add(item);
                totalCount++;
            }
            if (runwayCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(runwayCategory);

            // Zones
            var zoneCategory = CreateCategory("▢ Zones", _layout.Zones.Count);
            foreach (var zone in _layout.Zones.Where(z => MatchesFilter(z.Name, searchFilter) || MatchesFilter(z.Type, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {zone.Name} ({zone.Type})",
                    Tag = zone,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(zone.Visual?.FillColor ?? "#3498DB"))
                };
                zoneCategory.Items.Add(item);
                totalCount++;
            }
            if (zoneCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(zoneCategory);

            // AGV Stations
            var stationCategory = CreateCategory("🚏 AGV Stations", _layout.AGVStations.Count);
            foreach (var station in _layout.AGVStations.Where(s => MatchesFilter(s.Name, searchFilter) || MatchesFilter(s.StationType, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {station.Name} ({station.StationType})",
                    Tag = station
                };
                stationCategory.Items.Add(item);
                totalCount++;
            }
            if (stationCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(stationCategory);

            // Conveyors
            var conveyorCategory = CreateCategory("➜ Conveyors", _layout.Conveyors.Count);
            foreach (var conveyor in _layout.Conveyors.Where(c => MatchesFilter(c.Name, searchFilter) || MatchesFilter(c.ConveyorType, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {conveyor.Name} ({conveyor.ConveyorType})",
                    Tag = conveyor,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(conveyor.Color ?? "#FFA500"))
                };
                conveyorCategory.Items.Add(item);
                totalCount++;
            }
            if (conveyorCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(conveyorCategory);

            // Openings
            var openingCategory = CreateCategory("🚪 Openings", _layout.Openings.Count);
            foreach (var opening in _layout.Openings.Where(o => MatchesFilter(o.Name, searchFilter) || MatchesFilter(o.OpeningType, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {opening.Name} ({opening.OpeningType})",
                    Tag = opening
                };
                openingCategory.Items.Add(item);
                totalCount++;
            }
            if (openingCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(openingCategory);

            // Nodes (Equipment)
            var nodeCategory = CreateCategory("⚙️ Nodes", _layout.Nodes.Count);
            foreach (var node in _layout.Nodes.Where(n => MatchesFilter(n.Name, searchFilter) || MatchesFilter(n.Type, searchFilter)))
            {
                var item = new TreeViewItem
                {
                    Header = $"  {node.Name} ({node.Type})",
                    Tag = node,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(node.Visual?.Color ?? "#4A90D9"))
                };
                nodeCategory.Items.Add(item);
                totalCount++;
            }
            if (nodeCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(nodeCategory);

            // Paths
            var pathCategory = CreateCategory("↗ Paths", _layout.Paths.Count);
            foreach (var path in _layout.Paths.Where(p => MatchesFilter(p.PathType, searchFilter)))
            {
                var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);
                var item = new TreeViewItem
                {
                    Header = $"  {fromNode?.Name ?? "?"} → {toNode?.Name ?? "?"}",
                    Tag = path
                };
                pathCategory.Items.Add(item);
                totalCount++;
            }
            if (pathCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(pathCategory);

            // Walls
            var wallCategory = CreateCategory("▭ Walls", _layout.Walls.Count);
            foreach (var wall in _layout.Walls)
            {
                var item = new TreeViewItem
                {
                    Header = $"  Wall ({wall.WallType})",
                    Tag = wall
                };
                wallCategory.Items.Add(item);
                if (string.IsNullOrEmpty(searchFilter) || MatchesFilter(wall.WallType, searchFilter))
                    totalCount++;
            }
            if (wallCategory.Items.Count > 0 || string.IsNullOrEmpty(searchFilter))
                EquipmentTree.Items.Add(wallCategory);

            CountLabel.Text = $" ({totalCount} items)";
        }

        private TreeViewItem CreateCategory(string name, int count)
        {
            return new TreeViewItem
            {
                Header = $"{name} ({count})",
                FontWeight = FontWeights.SemiBold,
                IsExpanded = true,
                Foreground = Brushes.DarkSlateGray
            };
        }

        private bool MatchesFilter(string? value, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return value?.ToLower().Contains(filter) ?? false;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshTree();
            if (ViewTabs.SelectedIndex == 1) // Data Grid tab
            {
                RefreshGrid();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshTree();
            RefreshGrid();
        }

        private void EquipmentTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag != null)
            {
                _selectedItem = item.Tag;
                DisplayProperties(item.Tag);
                ItemSelected?.Invoke(this, item.Tag);
            }
            else
            {
                _selectedItem = null;
                ClearProperties();
            }
        }

        private void EquipmentTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedItem != null)
            {
                ItemDoubleClicked?.Invoke(this, _selectedItem);
            }
        }

        private void EditProperties_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                EditRequested?.Invoke(this, _selectedItem);
            }
        }

        private void ClearProperties()
        {
            PropertiesPanel.Children.Clear();
            PropertiesPanel.Children.Add(new TextBlock
            {
                Text = "Select an item to view properties",
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
            SelectedItemLabel.Text = "No selection";
        }

        private void DisplayProperties(object item)
        {
            PropertiesPanel.Children.Clear();

            var properties = new List<(string Name, string Value)>();

            switch (item)
            {
                case EOTCraneData crane:
                    SelectedItemLabel.Text = $"EOT Crane: {crane.Name}";
                    properties.Add(("Name", crane.Name));
                    properties.Add(("Bay Width", $"{crane.BayWidth:F1}"));
                    properties.Add(("Reach Left", $"{crane.ReachLeft:F1}"));
                    properties.Add(("Reach Right", $"{crane.ReachRight:F1}"));
                    properties.Add(("Zone Min", $"{crane.ZoneMin:F2}"));
                    properties.Add(("Zone Max", $"{crane.ZoneMax:F2}"));
                    properties.Add(("Bridge Position", $"{crane.BridgePosition:F2}"));
                    // MaxLoad not available on EOTCraneData - using runway capacity
                    properties.Add(("Bridge Speed", $"{crane.SpeedBridge:F2} m/s"));
                    properties.Add(("Trolley Speed", $"{crane.SpeedTrolley:F2} m/s"));
                    properties.Add(("Hoist Speed", $"{crane.SpeedHoist:F2} m/s"));
                    break;

                case JibCraneData jib:
                    SelectedItemLabel.Text = $"Jib Crane: {jib.Name}";
                    properties.Add(("Name", jib.Name));
                    properties.Add(("Center X", $"{jib.CenterX:F1}"));
                    properties.Add(("Center Y", $"{jib.CenterY:F1}"));
                    properties.Add(("Radius", $"{jib.Radius:F1}"));
                    properties.Add(("Arc Start", $"{jib.ArcStart}°"));
                    properties.Add(("Arc End", $"{jib.ArcEnd}°"));
                    properties.Add(("Slew Speed", $"{jib.SpeedSlew:F1}°/s"));
                    break;

                case ZoneData zone:
                    SelectedItemLabel.Text = $"Zone: {zone.Name}";
                    properties.Add(("Name", zone.Name));
                    properties.Add(("Type", zone.Type));
                    properties.Add(("X", $"{zone.X:F1}"));
                    properties.Add(("Y", $"{zone.Y:F1}"));
                    properties.Add(("Width", $"{zone.Width:F1}"));
                    properties.Add(("Height", $"{zone.Height:F1}"));
                    properties.Add(("Capacity", $"{zone.Capacity}"));
                    properties.Add(("Max Occupancy", $"{zone.MaxOccupancy}"));
                    properties.Add(("Restricted", $"{zone.IsRestricted}"));
                    break;

                case AGVStationData station:
                    SelectedItemLabel.Text = $"AGV Station: {station.Name}";
                    properties.Add(("Name", station.Name));
                    properties.Add(("Type", station.StationType ?? "pickup"));
                    properties.Add(("X", $"{station.X:F1}"));
                    properties.Add(("Y", $"{station.Y:F1}"));
                    properties.Add(("Rotation", $"{station.Rotation:F1}°"));
                    properties.Add(("Service Time", $"{station.ServiceTime:F1}s"));
                    properties.Add(("Dwell Time", $"{station.DwellTime:F1}s"));
                    properties.Add(("Queue Capacity", $"{station.QueueCapacity}"));
                    properties.Add(("Is Homing", $"{station.IsHoming}"));
                    break;

                case ConveyorData conveyor:
                    SelectedItemLabel.Text = $"Conveyor: {conveyor.Name}";
                    properties.Add(("Name", conveyor.Name));
                    properties.Add(("Type", conveyor.ConveyorType));
                    properties.Add(("Direction", conveyor.Direction));
                    properties.Add(("Width", $"{conveyor.Width:F1}"));
                    properties.Add(("Speed", $"{conveyor.Speed:F2} m/s"));
                    properties.Add(("Accumulating", $"{conveyor.IsAccumulating}"));
                    properties.Add(("Path Points", $"{conveyor.Path?.Count ?? 0}"));
                    break;

                case OpeningData opening:
                    SelectedItemLabel.Text = $"Opening: {opening.Name}";
                    properties.Add(("Name", opening.Name));
                    properties.Add(("Type", opening.OpeningType));
                    properties.Add(("State", opening.State));
                    properties.Add(("X", $"{opening.X:F1}"));
                    properties.Add(("Y", $"{opening.Y:F1}"));
                    properties.Add(("Clear Width", $"{opening.ClearWidth:F1}\""));
                    properties.Add(("Clear Height", $"{opening.ClearHeight:F1}\""));
                    properties.Add(("Capacity", $"{opening.Capacity}"));
                    properties.Add(("Direction", opening.DirectionMode));
                    properties.Add(("Traversal Time", $"{opening.TraversalTime:F1}s"));
                    break;

                case NodeData node:
                    SelectedItemLabel.Text = $"Node: {node.Name}";
                    properties.Add(("Name", node.Name));
                    properties.Add(("Type", node.Type ?? "machine"));
                    properties.Add(("X", $"{node.Visual?.X:F1}"));
                    properties.Add(("Y", $"{node.Visual?.Y:F1}"));
                    properties.Add(("Servers", $"{node.Simulation?.Servers}"));
                    properties.Add(("Capacity", $"{node.Simulation?.Capacity}"));
                    properties.Add(("Process Time", $"{node.Simulation?.ProcessTime}"));
                    break;

                case RunwayData runway:
                    SelectedItemLabel.Text = $"Runway: {runway.Name}";
                    properties.Add(("Name", runway.Name));
                    properties.Add(("Start X", $"{runway.StartX:F1}"));
                    properties.Add(("Start Y", $"{runway.StartY:F1}"));
                    properties.Add(("End X", $"{runway.EndX:F1}"));
                    properties.Add(("End Y", $"{runway.EndY:F1}"));
                    properties.Add(("Height", $"{runway.Height:F1}"));
                    double length = Math.Sqrt(Math.Pow(runway.EndX - runway.StartX, 2) + Math.Pow(runway.EndY - runway.StartY, 2));
                    properties.Add(("Length", $"{length:F1}"));
                    break;

                case PathData path:
                    var pathFromNode = _layout?.Nodes.FirstOrDefault(n => n.Id == path.From);
                    var pathToNode = _layout?.Nodes.FirstOrDefault(n => n.Id == path.To);
                    SelectedItemLabel.Text = $"Path: {pathFromNode?.Name} → {pathToNode?.Name}";
                    properties.Add(("From", pathFromNode?.Name ?? path.From));
                    properties.Add(("To", pathToNode?.Name ?? path.To));
                    properties.Add(("Type", path.PathType ?? "single"));
                    properties.Add(("Routing", path.RoutingMode ?? "direct"));
                    properties.Add(("Distance", $"{path.Simulation?.Distance:F1}"));
                    properties.Add(("Speed", $"{path.Simulation?.Speed:F2}"));
                    properties.Add(("Bidirectional", $"{path.Simulation?.Bidirectional}"));
                    break;

                case WallData wall:
                    SelectedItemLabel.Text = $"Wall: {wall.WallType}";
                    properties.Add(("Type", wall.WallType ?? "standard"));
                    properties.Add(("X1", $"{wall.X1:F1}"));
                    properties.Add(("Y1", $"{wall.Y1:F1}"));
                    properties.Add(("X2", $"{wall.X2:F1}"));
                    properties.Add(("Y2", $"{wall.Y2:F1}"));
                    properties.Add(("Thickness", $"{wall.Thickness:F1}"));
                    break;

                default:
                    SelectedItemLabel.Text = item.GetType().Name;
                    properties.Add(("Type", item.GetType().Name));
                    break;
            }

            foreach (var prop in properties)
            {
                var row = new Border { Style = (Style)Resources["PropertyRow"] };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var nameLabel = new TextBlock
                {
                    Text = prop.Name,
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameLabel, 0);
                grid.Children.Add(nameLabel);

                var valueLabel = new TextBlock
                {
                    Text = prop.Value ?? "—",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(valueLabel, 1);
                grid.Children.Add(valueLabel);

                row.Child = grid;
                PropertiesPanel.Children.Add(row);
            }
        }

        #region DataGrid Support

        private void ViewTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewTabs.SelectedIndex == 1) // Data Grid tab
            {
                CategoryCombo.Visibility = Visibility.Visible;
                RefreshGrid();
            }
            else
            {
                CategoryCombo.Visibility = Visibility.Collapsed;
            }
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _currentCategory = tag;
                RefreshGrid();
            }
        }

        public void RefreshGrid()
        {
            if (_layout == null || EquipmentGrid == null) return;

            EquipmentGrid.Columns.Clear();
            EquipmentGrid.ItemsSource = null;

            string filter = SearchBox?.Text?.ToLower() ?? "";

            switch (_currentCategory)
            {
                case "EOTCranes":
                    SetupEOTCraneColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.EOTCranes
                        : _layout.EOTCranes.Where(c => c.Name.ToLower().Contains(filter)).ToList();
                    break;

                case "JibCranes":
                    SetupJibCraneColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.JibCranes
                        : _layout.JibCranes.Where(j => j.Name.ToLower().Contains(filter)).ToList();
                    break;

                case "Runways":
                    SetupRunwayColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Runways
                        : _layout.Runways.Where(r => r.Name.ToLower().Contains(filter)).ToList();
                    break;

                case "Zones":
                    SetupZoneColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Zones
                        : _layout.Zones.Where(z => z.Name.ToLower().Contains(filter) || z.Type.ToLower().Contains(filter)).ToList();
                    break;

                case "AGVStations":
                    SetupAGVStationColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.AGVStations
                        : _layout.AGVStations.Where(s => s.Name.ToLower().Contains(filter)).ToList();
                    break;

                case "Conveyors":
                    SetupConveyorColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Conveyors
                        : _layout.Conveyors.Where(c => c.Name.ToLower().Contains(filter)).ToList();
                    break;

                case "Openings":
                    SetupOpeningColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Openings
                        : _layout.Openings.Where(o => o.Name.ToLower().Contains(filter)).ToList();
                    break;

                case "Nodes":
                    SetupNodeColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Nodes
                        : _layout.Nodes.Where(n => n.Name.ToLower().Contains(filter) || (n.Type?.ToLower().Contains(filter) ?? false)).ToList();
                    break;

                case "Paths":
                    SetupPathColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Paths
                        : _layout.Paths.Where(p => p.PathType?.ToLower().Contains(filter) ?? false).ToList();
                    break;

                case "Walls":
                    SetupWallColumns();
                    EquipmentGrid.ItemsSource = string.IsNullOrEmpty(filter)
                        ? _layout.Walls
                        : _layout.Walls.Where(w => w.WallType?.ToLower().Contains(filter) ?? false).ToList();
                    break;
            }
        }

        private void SetupEOTCraneColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Bay Width", Binding = new Binding("BayWidth") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Reach L", Binding = new Binding("ReachLeft") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Reach R", Binding = new Binding("ReachRight") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Zone Min", Binding = new Binding("ZoneMin") { StringFormat = "F2" }, Width = 65 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Zone Max", Binding = new Binding("ZoneMax") { StringFormat = "F2" }, Width = 65 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Bridge Spd", Binding = new Binding("SpeedBridge") { StringFormat = "F2" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Trolley Spd", Binding = new Binding("SpeedTrolley") { StringFormat = "F2" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Hoist Spd", Binding = new Binding("SpeedHoist") { StringFormat = "F2" }, Width = 70 });
        }

        private void SetupJibCraneColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Center X", Binding = new Binding("CenterX") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Center Y", Binding = new Binding("CenterY") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Radius", Binding = new Binding("Radius") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Arc Start", Binding = new Binding("ArcStart"), Width = 65 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Arc End", Binding = new Binding("ArcEnd"), Width = 65 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Slew Spd", Binding = new Binding("SpeedSlew") { StringFormat = "F1" }, Width = 70 });
        }

        private void SetupRunwayColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Start X", Binding = new Binding("StartX") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Start Y", Binding = new Binding("StartY") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "End X", Binding = new Binding("EndX") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "End Y", Binding = new Binding("EndY") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Height", Binding = new Binding("Height") { StringFormat = "F1" }, Width = 60 });
        }

        private void SetupZoneColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("Type"), Width = 80 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "X", Binding = new Binding("X") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Y", Binding = new Binding("Y") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Width", Binding = new Binding("Width") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Height", Binding = new Binding("Height") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Capacity", Binding = new Binding("Capacity"), Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Restricted", Binding = new Binding("IsRestricted"), Width = 70 });
        }

        private void SetupAGVStationColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("StationType"), Width = 80 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "X", Binding = new Binding("X") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Y", Binding = new Binding("Y") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Rotation", Binding = new Binding("Rotation") { StringFormat = "F0" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Service", Binding = new Binding("ServiceTime") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Queue", Binding = new Binding("QueueCapacity"), Width = 50 });
            EquipmentGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Homing", Binding = new Binding("IsHoming"), Width = 55 });
        }

        private void SetupConveyorColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("ConveyorType"), Width = 80 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Direction", Binding = new Binding("Direction"), Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Width", Binding = new Binding("Width") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Speed", Binding = new Binding("Speed") { StringFormat = "F2" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Accum", Binding = new Binding("IsAccumulating"), Width = 55 });
        }

        private void SetupOpeningColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("OpeningType"), Width = 80 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "State", Binding = new Binding("State"), Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "X", Binding = new Binding("X") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Y", Binding = new Binding("Y") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Width", Binding = new Binding("ClearWidth") { StringFormat = "F1" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Height", Binding = new Binding("ClearHeight") { StringFormat = "F1" }, Width = 60 });
        }

        private void SetupNodeColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = 100 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("Type"), Width = 80 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "X", Binding = new Binding("Visual.X") { StringFormat = "F1" }, Width = 60, IsReadOnly = true });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Y", Binding = new Binding("Visual.Y") { StringFormat = "F1" }, Width = 60, IsReadOnly = true });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Servers", Binding = new Binding("Simulation.Servers"), Width = 55 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Capacity", Binding = new Binding("Simulation.Capacity"), Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Process", Binding = new Binding("Simulation.ProcessTime") { StringFormat = "F1" }, Width = 60 });
        }

        private void SetupPathColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "From", Binding = new Binding("From"), Width = 100, IsReadOnly = true });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "To", Binding = new Binding("To"), Width = 100, IsReadOnly = true });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("PathType"), Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Routing", Binding = new Binding("RoutingMode"), Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Distance", Binding = new Binding("Simulation.Distance") { StringFormat = "F1" }, Width = 65 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Speed", Binding = new Binding("Simulation.Speed") { StringFormat = "F2" }, Width = 60 });
            EquipmentGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Bidir", Binding = new Binding("Simulation.Bidirectional"), Width = 50 });
        }

        private void SetupWallColumns()
        {
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("WallType"), Width = 80 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "X1", Binding = new Binding("X1") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Y1", Binding = new Binding("Y1") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "X2", Binding = new Binding("X2") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Y2", Binding = new Binding("Y2") { StringFormat = "F1" }, Width = 70 });
            EquipmentGrid.Columns.Add(new DataGridTextColumn { Header = "Thickness", Binding = new Binding("Thickness") { StringFormat = "F1" }, Width = 70 });
        }

        private void EquipmentGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EquipmentGrid.SelectedItem != null)
            {
                _selectedItem = EquipmentGrid.SelectedItem;
                DisplayProperties(EquipmentGrid.SelectedItem);
                ItemSelected?.Invoke(this, EquipmentGrid.SelectedItem);
            }
        }

        private void EquipmentGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedItem != null)
            {
                ItemDoubleClicked?.Invoke(this, _selectedItem);
            }
        }

        private void EquipmentGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Notify that data has changed after editing
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Use dispatcher to fire after the edit is complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DataChanged?.Invoke(this, EventArgs.Empty);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        #endregion
    }
}
