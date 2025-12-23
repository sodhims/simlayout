using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LayoutEditor.Models;

namespace EquipmentManager
{
    public partial class MainWindow : Window
    {
        private LayoutData? _layout;
        private string? _currentFilePath;
        private bool _isDirty;
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        #region File Operations

        private void OpenLayout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Layout Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Layout File"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    _layout = JsonSerializer.Deserialize<LayoutData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (_layout != null)
                    {
                        _currentFilePath = dialog.FileName;
                        RefreshAllGrids();
                        FilePathLabel.Text = Path.GetFileName(dialog.FileName);
                        StatusText.Text = $"Loaded: {dialog.FileName}";
                        _isDirty = false;
                        UpdateTitle();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveLayoutAs_Click(sender, e);
                return;
            }

            SaveToFile(_currentFilePath);
        }

        private void SaveLayoutAs_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Layout Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Layout File",
                FileName = Path.GetFileName(_currentFilePath ?? "layout.json")
            };

            if (dialog.ShowDialog() == true)
            {
                SaveToFile(dialog.FileName);
                _currentFilePath = dialog.FileName;
                FilePathLabel.Text = Path.GetFileName(dialog.FileName);
            }
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                string json = JsonSerializer.Serialize(_layout, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
                _isDirty = false;
                UpdateTitle();
                StatusText.Text = $"Saved: {filePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Save before exiting?",
                    "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    SaveLayout_Click(sender, e);
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            Close();
        }

        #endregion

        #region Export Operations

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Export to CSV"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    // Export based on current tab
                    var currentTab = MainTabControl.SelectedItem as TabItem;
                    string tabName = currentTab?.Header?.ToString() ?? "";

                    switch (tabName)
                    {
                        case "EOT Cranes":
                            sb.AppendLine("Name,BayWidth,ReachLeft,ReachRight,ZoneMin,ZoneMax,SpeedBridge,SpeedTrolley,SpeedHoist,Color");
                            foreach (var crane in _layout.EOTCranes)
                                sb.AppendLine($"\"{crane.Name}\",{crane.BayWidth},{crane.ReachLeft},{crane.ReachRight},{crane.ZoneMin},{crane.ZoneMax},{crane.SpeedBridge},{crane.SpeedTrolley},{crane.SpeedHoist},\"{crane.Color}\"");
                            break;

                        case "Zones":
                            sb.AppendLine("Name,Type,X,Y,Width,Height,Capacity,MaxOccupancy,IsRestricted");
                            foreach (var zone in _layout.Zones)
                                sb.AppendLine($"\"{zone.Name}\",\"{zone.Type}\",{zone.X},{zone.Y},{zone.Width},{zone.Height},{zone.Capacity},{zone.MaxOccupancy},{zone.IsRestricted}");
                            break;

                        case "AGV Stations":
                            sb.AppendLine("Name,StationType,X,Y,Rotation,ServiceTime,DwellTime,QueueCapacity,IsHoming");
                            foreach (var station in _layout.AGVStations)
                                sb.AppendLine($"\"{station.Name}\",\"{station.StationType}\",{station.X},{station.Y},{station.Rotation},{station.ServiceTime},{station.DwellTime},{station.QueueCapacity},{station.IsHoming}");
                            break;

                        case "Nodes":
                            sb.AppendLine("Name,Type,X,Y,Width,Height,Servers,Capacity,ProcessTime");
                            foreach (var node in _layout.Nodes)
                                sb.AppendLine($"\"{node.Name}\",\"{node.Type}\",{node.Visual?.X},{node.Visual?.Y},{node.Visual?.Width},{node.Visual?.Height},{node.Simulation?.Servers},{node.Simulation?.Capacity},{node.Simulation?.ProcessTime}");
                            break;

                        default:
                            MessageBox.Show("CSV export not implemented for this tab.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString());
                    StatusText.Text = $"Exported {tabName} to CSV: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportJSON_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Export to JSON"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var currentTab = MainTabControl.SelectedItem as TabItem;
                    string tabName = currentTab?.Header?.ToString() ?? "";
                    object? dataToExport = tabName switch
                    {
                        "EOT Cranes" => _layout.EOTCranes,
                        "Jib Cranes" => _layout.JibCranes,
                        "Zones" => _layout.Zones,
                        "AGV Stations" => _layout.AGVStations,
                        "Conveyors" => _layout.Conveyors,
                        "Openings" => _layout.Openings,
                        "Nodes" => _layout.Nodes,
                        "Runways" => _layout.Runways,
                        _ => null
                    };

                    if (dataToExport != null)
                    {
                        string json = JsonSerializer.Serialize(dataToExport, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(dialog.FileName, json);
                        StatusText.Text = $"Exported {tabName} to JSON: {dialog.FileName}";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportSQL_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql",
                Title = "Export SQL Insert Statements"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("-- Equipment Manager SQL Export");
                    sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();

                    // EOT Cranes
                    if (_layout.EOTCranes.Count > 0)
                    {
                        sb.AppendLine("-- EOT Cranes");
                        foreach (var crane in _layout.EOTCranes)
                        {
                            sb.AppendLine($"INSERT INTO eot_cranes (id, name, runway_id, bay_width, reach_left, reach_right, zone_min, zone_max, speed_bridge, speed_trolley, speed_hoist, color) VALUES ('{crane.Id}', '{EscapeSql(crane.Name)}', '{crane.RunwayId}', {crane.BayWidth}, {crane.ReachLeft}, {crane.ReachRight}, {crane.ZoneMin}, {crane.ZoneMax}, {crane.SpeedBridge}, {crane.SpeedTrolley}, {crane.SpeedHoist}, '{crane.Color}');");
                        }
                        sb.AppendLine();
                    }

                    // Zones
                    if (_layout.Zones.Count > 0)
                    {
                        sb.AppendLine("-- Zones");
                        foreach (var zone in _layout.Zones)
                        {
                            sb.AppendLine($"INSERT INTO zones (id, name, type, x, y, width, height, capacity, max_occupancy, is_restricted) VALUES ('{zone.Id}', '{EscapeSql(zone.Name)}', '{zone.Type}', {zone.X}, {zone.Y}, {zone.Width}, {zone.Height}, {zone.Capacity}, {zone.MaxOccupancy}, {(zone.IsRestricted == true ? "true" : "false")});");
                        }
                        sb.AppendLine();
                    }

                    // AGV Stations
                    if (_layout.AGVStations.Count > 0)
                    {
                        sb.AppendLine("-- AGV Stations");
                        foreach (var station in _layout.AGVStations)
                        {
                            sb.AppendLine($"INSERT INTO agv_stations (id, name, station_type, x, y, rotation, service_time, dwell_time, queue_capacity, is_homing) VALUES ('{station.Id}', '{EscapeSql(station.Name)}', '{station.StationType}', {station.X}, {station.Y}, {station.Rotation}, {station.ServiceTime}, {station.DwellTime ?? 5}, {station.QueueCapacity ?? 3}, {(station.IsHoming ? "true" : "false")});");
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString());
                    StatusText.Text = $"Exported SQL to: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string EscapeSql(string? value) => value?.Replace("'", "''") ?? "";

        private void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("CSV Import is not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Edit Operations

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count == 0) return;

            // Save current state to redo
            if (_layout != null)
            {
                _redoStack.Push(JsonSerializer.Serialize(_layout));
            }

            // Restore previous state
            string previousState = _undoStack.Pop();
            _layout = JsonSerializer.Deserialize<LayoutData>(previousState);
            RefreshAllGrids();
            StatusText.Text = "Undo performed";
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count == 0) return;

            // Save current state to undo
            if (_layout != null)
            {
                _undoStack.Push(JsonSerializer.Serialize(_layout));
            }

            // Restore next state
            string nextState = _redoStack.Pop();
            _layout = JsonSerializer.Deserialize<LayoutData>(nextState);
            RefreshAllGrids();
            StatusText.Text = "Redo performed";
        }

        private void SaveUndoState()
        {
            if (_layout != null)
            {
                _undoStack.Push(JsonSerializer.Serialize(_layout));
                _redoStack.Clear();
            }
        }

        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                _layout = new LayoutData();
            }

            SaveUndoState();

            var currentTab = MainTabControl.SelectedItem as TabItem;
            string tabName = currentTab?.Header?.ToString() ?? "";

            switch (tabName)
            {
                case "EOT Cranes":
                    _layout.EOTCranes.Add(new EOTCraneData { Name = $"EOT Crane {_layout.EOTCranes.Count + 1}" });
                    break;
                case "Jib Cranes":
                    _layout.JibCranes.Add(new JibCraneData { Name = $"Jib Crane {_layout.JibCranes.Count + 1}" });
                    break;
                case "Zones":
                    _layout.Zones.Add(new ZoneData { Name = $"Zone {_layout.Zones.Count + 1}" });
                    break;
                case "AGV Stations":
                    _layout.AGVStations.Add(new AGVStationData { Name = $"AGV Station {_layout.AGVStations.Count + 1}" });
                    break;
                case "Conveyors":
                    _layout.Conveyors.Add(new ConveyorData { Name = $"Conveyor {_layout.Conveyors.Count + 1}" });
                    break;
                case "Openings":
                    _layout.Openings.Add(new OpeningData { Name = $"Opening {_layout.Openings.Count + 1}" });
                    break;
                case "Nodes":
                    _layout.Nodes.Add(new NodeData { Name = $"Node {_layout.Nodes.Count + 1}" });
                    break;
                case "Runways":
                    _layout.Runways.Add(new RunwayData { Name = $"Runway {_layout.Runways.Count + 1}" });
                    break;
            }

            RefreshAllGrids();
            MarkDirty();
            StatusText.Text = $"Added new {tabName.TrimEnd('s')}";
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            var currentTab = MainTabControl.SelectedItem as TabItem;
            string tabName = currentTab?.Header?.ToString() ?? "";
            DataGrid? grid = GetCurrentGrid();

            if (grid?.SelectedItem == null) return;

            SaveUndoState();

            switch (tabName)
            {
                case "EOT Cranes":
                    if (grid.SelectedItem is EOTCraneData crane)
                        _layout.EOTCranes.Remove(crane);
                    break;
                case "Jib Cranes":
                    if (grid.SelectedItem is JibCraneData jib)
                        _layout.JibCranes.Remove(jib);
                    break;
                case "Zones":
                    if (grid.SelectedItem is ZoneData zone)
                        _layout.Zones.Remove(zone);
                    break;
                case "AGV Stations":
                    if (grid.SelectedItem is AGVStationData station)
                        _layout.AGVStations.Remove(station);
                    break;
                case "Conveyors":
                    if (grid.SelectedItem is ConveyorData conveyor)
                        _layout.Conveyors.Remove(conveyor);
                    break;
                case "Openings":
                    if (grid.SelectedItem is OpeningData opening)
                        _layout.Openings.Remove(opening);
                    break;
                case "Nodes":
                    if (grid.SelectedItem is NodeData node)
                        _layout.Nodes.Remove(node);
                    break;
                case "Runways":
                    if (grid.SelectedItem is RunwayData runway)
                        _layout.Runways.Remove(runway);
                    break;
            }

            RefreshAllGrids();
            MarkDirty();
            StatusText.Text = "Item deleted";
        }

        private void DuplicateSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            var currentTab = MainTabControl.SelectedItem as TabItem;
            string tabName = currentTab?.Header?.ToString() ?? "";
            DataGrid? grid = GetCurrentGrid();

            if (grid?.SelectedItem == null) return;

            SaveUndoState();

            // Serialize and deserialize to create a deep copy
            string json = JsonSerializer.Serialize(grid.SelectedItem);

            switch (tabName)
            {
                case "EOT Cranes":
                    var crane = JsonSerializer.Deserialize<EOTCraneData>(json);
                    if (crane != null)
                    {
                        crane.Id = Guid.NewGuid().ToString();
                        crane.Name += " (Copy)";
                        _layout.EOTCranes.Add(crane);
                    }
                    break;
                case "Zones":
                    var zone = JsonSerializer.Deserialize<ZoneData>(json);
                    if (zone != null)
                    {
                        zone.Id = Guid.NewGuid().ToString();
                        zone.Name += " (Copy)";
                        _layout.Zones.Add(zone);
                    }
                    break;
                // Add other types as needed
            }

            RefreshAllGrids();
            MarkDirty();
            StatusText.Text = "Item duplicated";
        }

        #endregion

        #region View Operations

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllGrids();
            StatusText.Text = "Refreshed";
        }

        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                MessageBox.Show("No layout loaded.", "Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Layout Statistics");
            sb.AppendLine("=================");
            sb.AppendLine($"EOT Cranes: {_layout.EOTCranes.Count}");
            sb.AppendLine($"Jib Cranes: {_layout.JibCranes.Count}");
            sb.AppendLine($"Runways: {_layout.Runways.Count}");
            sb.AppendLine($"Zones: {_layout.Zones.Count}");
            sb.AppendLine($"AGV Stations: {_layout.AGVStations.Count}");
            sb.AppendLine($"Conveyors: {_layout.Conveyors.Count}");
            sb.AppendLine($"Openings: {_layout.Openings.Count}");
            sb.AppendLine($"Nodes: {_layout.Nodes.Count}");
            sb.AppendLine($"Paths: {_layout.Paths.Count}");
            sb.AppendLine($"Walls: {_layout.Walls.Count}");
            sb.AppendLine();
            sb.AppendLine($"Total Items: {GetTotalCount()}");

            MessageBox.Show(sb.ToString(), "Layout Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Equipment Manager\n" +
                "Layout Editor Utility\n\n" +
                "A standalone tool for viewing, editing, and exporting\n" +
                "equipment definitions from Layout Editor files.\n\n" +
                "Version 1.0",
                "About Equipment Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region UI Helpers

        private void RefreshAllGrids()
        {
            if (_layout == null) return;

            EOTCraneGrid.ItemsSource = null;
            EOTCraneGrid.ItemsSource = _layout.EOTCranes;
            EOTCraneCount.Text = $"({_layout.EOTCranes.Count})";

            JibCraneGrid.ItemsSource = null;
            JibCraneGrid.ItemsSource = _layout.JibCranes;
            JibCraneCount.Text = $"({_layout.JibCranes.Count})";

            ZoneGrid.ItemsSource = null;
            ZoneGrid.ItemsSource = _layout.Zones;
            ZoneCount.Text = $"({_layout.Zones.Count})";

            AGVStationGrid.ItemsSource = null;
            AGVStationGrid.ItemsSource = _layout.AGVStations;
            AGVStationCount.Text = $"({_layout.AGVStations.Count})";

            ConveyorGrid.ItemsSource = null;
            ConveyorGrid.ItemsSource = _layout.Conveyors;
            ConveyorCount.Text = $"({_layout.Conveyors.Count})";

            OpeningGrid.ItemsSource = null;
            OpeningGrid.ItemsSource = _layout.Openings;
            OpeningCount.Text = $"({_layout.Openings.Count})";

            NodeGrid.ItemsSource = null;
            NodeGrid.ItemsSource = _layout.Nodes;
            NodeCount.Text = $"({_layout.Nodes.Count})";

            RunwayGrid.ItemsSource = null;
            RunwayGrid.ItemsSource = _layout.Runways;
            RunwayCount.Text = $"({_layout.Runways.Count})";

            TotalItemsLabel.Text = $"Total: {GetTotalCount()} items";
        }

        private int GetTotalCount()
        {
            if (_layout == null) return 0;
            return _layout.EOTCranes.Count + _layout.JibCranes.Count + _layout.Zones.Count +
                   _layout.AGVStations.Count + _layout.Conveyors.Count + _layout.Openings.Count +
                   _layout.Nodes.Count + _layout.Runways.Count;
        }

        private DataGrid? GetCurrentGrid()
        {
            var currentTab = MainTabControl.SelectedItem as TabItem;
            string tabName = currentTab?.Header?.ToString() ?? "";

            return tabName switch
            {
                "EOT Cranes" => EOTCraneGrid,
                "Jib Cranes" => JibCraneGrid,
                "Zones" => ZoneGrid,
                "AGV Stations" => AGVStationGrid,
                "Conveyors" => ConveyorGrid,
                "Openings" => OpeningGrid,
                "Nodes" => NodeGrid,
                "Runways" => RunwayGrid,
                _ => null
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_layout == null) return;

            string filter = SearchBox.Text.ToLower();
            if (string.IsNullOrEmpty(filter))
            {
                RefreshAllGrids();
                return;
            }

            // Filter current tab's data
            var currentTab = MainTabControl.SelectedItem as TabItem;
            string tabName = currentTab?.Header?.ToString() ?? "";

            switch (tabName)
            {
                case "EOT Cranes":
                    EOTCraneGrid.ItemsSource = _layout.EOTCranes.Where(c => c.Name.ToLower().Contains(filter));
                    break;
                case "Zones":
                    ZoneGrid.ItemsSource = _layout.Zones.Where(z => z.Name.ToLower().Contains(filter) || z.Type.ToLower().Contains(filter));
                    break;
                case "AGV Stations":
                    AGVStationGrid.ItemsSource = _layout.AGVStations.Where(s => s.Name.ToLower().Contains(filter));
                    break;
                case "Nodes":
                    NodeGrid.ItemsSource = _layout.Nodes.Where(n => n.Name.ToLower().Contains(filter) || (n.Type?.ToLower().Contains(filter) ?? false));
                    break;
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem != null)
            {
                SelectionStatus.Text = $"Selected: 1 item";
            }
            else
            {
                SelectionStatus.Text = "";
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath) ? "Untitled" : Path.GetFileName(_currentFilePath);
            Title = $"Equipment Manager - {fileName}{(_isDirty ? " *" : "")}";
        }

        #endregion
    }
}
