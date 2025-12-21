using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Transport;
using LayoutEditor.Transport.AGV;
using LayoutEditor.Transport.AGV;
using LayoutEditor.Transport.AGV;

namespace LayoutEditor.Transport.AGV
{
    /// <summary>
    /// UI handlers for AGV operations
    /// </summary>
    public class AgvHandlers
    {
        private readonly AgvNetwork _network;
        private readonly AgvPathService _pathService;
        private readonly AgvRenderer _renderer;
        private readonly Action _refreshCallback;
        private readonly Action<string> _statusCallback;

        public AgvHandlers(AgvNetwork network, Canvas canvas, Action refreshCallback, Action<string> statusCallback)
        {
            _network = network;
            _pathService = new AgvPathService(network);
            _renderer = new AgvRenderer(network);
            _refreshCallback = refreshCallback;
            _statusCallback = statusCallback;
        }

        #region Station Operations

        /// <summary>
        /// Add a new AGV station
        /// </summary>
        public AgvStation AddStation(double x, double y, StationType type = StationType.Pickup)
        {
            var count = _network.Stations.Count + 1;
            var station = new AgvStation
            {
                Name = $"AGV_Station_{count}",
                NetworkId = _network.Id,
                X = x,
                Y = y,
                StationType = type,
                Color = "#9B59B6"
            };

            _network.Stations.Add(station);
            _refreshCallback();
            _statusCallback($"Added AGV station: {station.Name}");
            return station;
        }

        /// <summary>
        /// Delete selected station
        /// </summary>
        public void DeleteStation(AgvStation station)
        {
            // Remove connected tracks
            var tracksToRemove = _network.Tracks
                .Where(t => t.From == station.Id || t.To == station.Id)
                .ToList();

            foreach (var track in tracksToRemove)
                _network.Tracks.Remove(track);

            _network.Stations.Remove(station);
            _refreshCallback();
            _statusCallback($"Deleted station: {station.Name}");
        }

        #endregion

        #region Loop Operations

        /// <summary>
        /// Auto-connect all stations in selected station's group
        /// </summary>
        public void AutoConnectGroup(AgvStation station)
        {
            if (string.IsNullOrEmpty(station.GroupName))
            {
                var groupName = PromptForGroupName(station.Name + "_Loop");
                if (string.IsNullOrEmpty(groupName)) return;
                station.GroupName = groupName;
            }

            try
            {
                var tracks = _pathService.CreateLoopForGroup(station.GroupName);
                _refreshCallback();
                _statusCallback($"Created loop with {tracks.Count} tracks for '{station.GroupName}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Assign selected stations to a group
        /// </summary>
        public void AssignToGroup(AgvStation[] stations, string? groupName = null)
        {
            if (stations.Length == 0) return;

            groupName ??= PromptForGroupName();
            if (string.IsNullOrEmpty(groupName)) return;

            foreach (var station in stations)
                station.GroupName = groupName;

            _refreshCallback();
            _statusCallback($"Assigned {stations.Length} station(s) to '{groupName}'");
        }

        /// <summary>
        /// Recreate loop for a group
        /// </summary>
        public void RecreateLoop(string groupName)
        {
            var result = MessageBox.Show(
                $"Remove all tracks for '{groupName}' and recreate?\n\nThis cannot be undone.",
                "Recreate Loop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _pathService.RecreateLoop(groupName);
                _refreshCallback();
                _statusCallback($"Recreated loop for '{groupName}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Path Operations

        /// <summary>
        /// Add blind path from a point
        /// </summary>
        public void AddBlindPath(string fromId, double offsetX = 100, double offsetY = 0)
        {
            try
            {
                var (waypoint, track) = _pathService.AddBlindPath(fromId, offsetX, offsetY);
                _refreshCallback();
                _statusCallback($"Added blind path: {waypoint.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Connect station to nearest point
        /// </summary>
        public void ConnectToNearest(AgvStation station)
        {
            try
            {
                var track = _pathService.ConnectToNearest(station.Id);
                _refreshCallback();
                _statusCallback($"Connected {station.Name} to nearest point");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Insert waypoint in track
        /// </summary>
        public void InsertWaypoint(AgvTrack track)
        {
            try
            {
                var waypoint = _pathService.InsertWaypoint(track.Id);
                _refreshCallback();
                _statusCallback($"Inserted waypoint: {waypoint.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Vehicle Operations

        /// <summary>
        /// Add a new AGV vehicle
        /// </summary>
        public AgvVehicle AddVehicle(string? homeStationId = null)
        {
            var count = _network.Vehicles.Count + 1;
            var vehicle = new AgvVehicle
            {
                Name = $"AGV_{count}",
                NetworkId = _network.Id,
                HomeStationId = homeStationId ?? "",
                VehicleType = "unit_load"
            };

            // Position at home station if specified
            if (!string.IsNullOrEmpty(homeStationId))
            {
                var station = _network.Stations.FirstOrDefault(s => s.Id == homeStationId);
                if (station != null)
                {
                    vehicle.CurrentX = station.X + station.Width / 2;
                    vehicle.CurrentY = station.Y + station.Height / 2;
                    vehicle.CurrentLocationId = homeStationId;
                }
            }

            _network.Vehicles.Add(vehicle);
            _refreshCallback();
            _statusCallback($"Added AGV: {vehicle.Name}");
            return vehicle;
        }

        #endregion

        #region Rendering

        public void Render(Canvas canvas)
        {
            _renderer.Clear(canvas);
            _renderer.Render(canvas);
        }

        public object? HitTest(Point position)
        {
            return _renderer.HitTest(position);
        }

        #endregion

        #region Helpers

        private string? PromptForGroupName(string defaultName = "Loop1")
        {
            var dialog = new Window
            {
                Title = "Group Name",
                Width = 280,
                Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(12) };
            var textBox = new TextBox { Text = defaultName, Margin = new Thickness(0, 0, 0, 12) };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "Cancel", Width = 70, IsCancel = true };

            ok.Click += (s, e) => dialog.DialogResult = true;
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(new TextBlock { Text = "Enter group name:" });
            panel.Children.Add(textBox);
            panel.Children.Add(buttons);
            dialog.Content = panel;

            textBox.SelectAll();
            textBox.Focus();

            return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text)
                ? textBox.Text.Trim()
                : null;
        }

        public AgvStation? GetSelectedStation()
        {
            return _network.Stations.FirstOrDefault(s => s.IsSelected);
        }

        public AgvStation[] GetSelectedStations()
        {
            return _network.Stations.Where(s => s.IsSelected).ToArray();
        }

        #endregion
    }
}
