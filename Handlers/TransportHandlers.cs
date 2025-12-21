using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Transport Fields

        private TransportRenderer? _transportRenderer;
        private bool _isDrawingTrack = false;
        private string? _trackStartPointId = null;
        private Point? _trackStartPosition = null;
        private TransportNetworkData? _currentNetwork = null;
        private TransporterTrackData? _currentTrack = null;  // Legacy support
        private bool _isInsertingWaypoint = false;
        private TrackSegmentData? _segmentToSplit = null;
        private Point _waypointInsertPosition;

        // Network management
        private string? _activeNetworkId = null;

        #endregion

        #region Initialize Transport

        private void InitializeTransport()
        {
            // Load existing groups from layout
            if (_layout.TransportGroups != null && _layout.TransportGroups.Any())
            {
                TransportGroupPanel.LoadGroups(_layout.TransportGroups);
            }
            
            // ═══════════════════════════════════════════════════════════════
            // SYNC PANEL CHANGES TO LAYOUT (so they save to file)
            // ═══════════════════════════════════════════════════════════════
            
            TransportGroupPanel.GroupAdded += (s, group) =>
            {
                if (!_layout.TransportGroups.Contains(group))
                    _layout.TransportGroups.Add(group);
                MarkDirty();
            };
            
            TransportGroupPanel.GroupDeleted += (s, group) =>
            {
                _layout.TransportGroups.Remove(group);
                MarkDirty();
            };
            
            TransportGroupPanel.GroupEdited += (s, group) =>
            {
                MarkDirty();
            };
            
            TransportGroupPanel.MemberAdded += (s, args) =>
            {
                MarkDirty();
            };
            
            TransportGroupPanel.MemberRemoved += (s, args) =>
            {
                MarkDirty();
            };
            
            // ═══════════════════════════════════════════════════════════════
            // OTHER PANEL EVENTS
            // ═══════════════════════════════════════════════════════════════
            
            TransportGroupPanel.ResetViewRequested += (s, e) => ResetViewToShowAllNodes();
            
            TransportGroupPanel.PickModeChanged += (s, isPickMode) =>
            {
                EditorCanvas.Cursor = isPickMode ? Cursors.Cross : Cursors.Arrow;
            };
            
            // Click member in group → highlight on canvas
            TransportGroupPanel.MemberClicked += (s, memberId) =>
            {
                // Try to find as cell first
                var cell = _layout.Groups.FirstOrDefault(g => g.IsCell && g.Id == memberId);
                if (cell != null)
                {
                    SelectCellWithPaths(cell);
                    StatusText.Text = $"Selected cell: {cell.Name}";
                }
                else
                {
                    // Try as node
                    var node = _layout.Nodes.FirstOrDefault(n => n.Id == memberId);
                    if (node != null)
                    {
                        _selectionService.SelectNode(node.Id);
                        StatusText.Text = $"Selected: {node.Name}";
                    }
                }
                UpdateSelectionVisuals();
                Redraw();
            };
            
            // ═══════════════════════════════════════════════════════════════
            // TRANSPORT RENDERER & COLLECTIONS
            // ═══════════════════════════════════════════════════════════════
            
            _transportRenderer = new TransportRenderer();
            EnsureTransportCollections();
        }

        private void EnsureTransportCollections()
        {
            if (_layout.TransportNetworks == null)
                _layout.TransportNetworks = new ObservableCollection<TransportNetworkData>();
            if (_layout.TransportStations == null)
                _layout.TransportStations = new ObservableCollection<TransportStationData>();
            if (_layout.Waypoints == null)
                _layout.Waypoints = new ObservableCollection<WaypointData>();
            if (_layout.TransporterTracks == null)
                _layout.TransporterTracks = new ObservableCollection<TransporterTrackData>();
            if (_layout.Transporters == null)
                _layout.Transporters = new ObservableCollection<TransporterData>();
        }

        #endregion

        #region Network Management

        private void AddNetwork_Click(object sender, RoutedEventArgs e)
        {
            EnsureTransportCollections();
            SaveUndoState();

            var count = _layout.TransportNetworks?.Count ?? 0;
            var network = new TransportNetworkData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Network {count + 1}",
                Visual = new TransportNetworkVisual
                {
                    Color = NetworkColors.GetNextColor(count)
                }
            };

            _layout.TransportNetworks!.Add(network);
            _activeNetworkId = network.Id;
            
            MarkDirty();
            Redraw();
            UpdateNetworkUI();

            StatusText.Text = $"Created transport network: {network.Name}";
        }

        private void SelectNetwork_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var networkId = btn?.Tag as string;
            if (networkId != null)
            {
                SetActiveNetwork(networkId);
            }
        }

        private void SetActiveNetwork(string? networkId)
        {
            _activeNetworkId = networkId;
            UpdateNetworkUI();

            var network = GetActiveNetwork();
            if (network != null)
            {
                StatusText.Text = $"Active network: {network.Name}";
            }
        }

        private TransportNetworkData? GetActiveNetwork()
        {
            if (_activeNetworkId == null) return null;
            return _layout.TransportNetworks?.FirstOrDefault(n => n.Id == _activeNetworkId);
        }

        private TransportNetworkData GetOrCreateActiveNetwork()
        {
            var network = GetActiveNetwork();
            if (network == null)
            {
                // Create default network if none exists
                EnsureTransportCollections();
                
                if (_layout.TransportNetworks!.Count == 0)
                {
                    network = new TransportNetworkData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Default Network"
                    };
                    _layout.TransportNetworks.Add(network);
                }
                else
                {
                    network = _layout.TransportNetworks.First();
                }
                
                _activeNetworkId = network.Id;
                UpdateNetworkUI();
            }
            return network;
        }

        private void UpdateNetworkUI()
        {
            // Update UI to show active network (implement based on your UI)
            // This could update a combo box, highlight buttons, etc.
        }

        private void DeleteNetwork_Click(object sender, RoutedEventArgs e)
        {
            var network = GetActiveNetwork();
            if (network == null)
            {
                MessageBox.Show("No network selected.", "Delete Network", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Delete network '{network.Name}' and all its elements?\n\n" +
                $"This will remove {network.Stations.Count} stations, " +
                $"{network.Waypoints.Count} waypoints, " +
                $"{network.Segments.Count} track segments, and " +
                $"{network.Transporters.Count} transporters.",
                "Delete Network",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                SaveUndoState();
                _layout.TransportNetworks!.Remove(network);
                _activeNetworkId = _layout.TransportNetworks.FirstOrDefault()?.Id;
                
                MarkDirty();
                Redraw();
                UpdateNetworkUI();

                StatusText.Text = $"Deleted network: {network.Name}";
            }
        }

        private void ValidateNetwork_Click(object sender, RoutedEventArgs e)
        {
            var network = GetActiveNetwork();
            if (network == null)
            {
                MessageBox.Show("No network selected.", "Validate Network", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var issues = network.ValidateNetwork();
            
            if (issues.Count == 0)
            {
                MessageBox.Show($"Network '{network.Name}' passed validation.\n\n" +
                    $"• {network.Stations.Count} stations\n" +
                    $"• {network.Waypoints.Count} waypoints\n" +
                    $"• {network.Segments.Count} track segments\n" +
                    $"• {network.Transporters.Count} transporters",
                    "Network Valid",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                var errors = issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                var warnings = issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
                
                var message = $"Network '{network.Name}' has {issues.Count} issue(s):\n\n";
                
                if (errors.Any())
                {
                    message += "ERRORS:\n";
                    foreach (var err in errors.Take(5))
                        message += $"  • {err.Message}\n";
                    if (errors.Count > 5)
                        message += $"  ... and {errors.Count - 5} more errors\n";
                    message += "\n";
                }
                
                if (warnings.Any())
                {
                    message += "WARNINGS:\n";
                    foreach (var warn in warnings.Take(5))
                        message += $"  • {warn.Message}\n";
                    if (warnings.Count > 5)
                        message += $"  ... and {warnings.Count - 5} more warnings\n";
                }

                MessageBox.Show(message, "Validation Results",
                    MessageBoxButton.OK,
                    errors.Any() ? MessageBoxImage.Error : MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Add Transport Stations

        private void AddTransportStation_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var stationType = btn?.Tag as string ?? StationTypes.Pickup;

            // Get center of visible canvas
            var scrollX = CanvasScroller.HorizontalOffset;
            var scrollY = CanvasScroller.VerticalOffset;
            var x = (scrollX + CanvasScroller.ViewportWidth / 2) / _currentZoom;
            var y = (scrollY + CanvasScroller.ViewportHeight / 2) / _currentZoom;

            AddTransportStationAt(stationType, x, y);
        }

        public TransportStationData AddTransportStationAt(string stationType, double x, double y, 
            TransportNetworkData? network = null)
        {
            EnsureTransportCollections();
            SaveUndoState();

            network ??= GetOrCreateActiveNetwork();

            var station = new TransportStationData
            {
                Id = Guid.NewGuid().ToString(),
                Name = GetDefaultStationName(stationType, network),
                Type = "transportStation",
                NetworkId = network.Id,
                Visual = new TransportStationVisual
                {
                    X = x - 25,
                    Y = y - 25,
                    Width = 50,
                    Height = 50,
                    Color = TransportRenderConstants.GetStationColorHex(stationType)
                },
                Simulation = new TransportStationSimulation
                {
                    StationType = stationType,
                    QueueCapacity = stationType == StationTypes.Buffer ? 10 : 5,
                    DwellTime = stationType == StationTypes.Home ? 60 : 10
                }
            };

            network.Stations.Add(station);
            
            // Also add to legacy collection for backward compatibility
            _layout.TransportStations!.Add(station);
            
            MarkDirty();
            Redraw();

            StatusText.Text = $"Added {stationType} station: {station.Name}";
            return station;
        }

        private string GetDefaultStationName(string stationType, TransportNetworkData? network = null)
        {
            int count;
            if (network != null)
            {
                count = network.Stations.Count(s => s.Simulation.StationType == stationType);
            }
            else
            {
                count = _layout.TransportStations?.Count(s => s.Simulation.StationType == stationType) ?? 0;
            }

            return stationType switch
            {
                StationTypes.Pickup => $"Pickup {count + 1}",
                StationTypes.Dropoff => $"Dropoff {count + 1}",
                StationTypes.Home => $"Home {count + 1}",
                StationTypes.Buffer => $"Buffer {count + 1}",
                StationTypes.Crossing => $"Crossing {count + 1}",
                StationTypes.Charging => $"Charging {count + 1}",
                StationTypes.Maintenance => $"Maintenance {count + 1}",
                _ => $"Station {count + 1}"
            };
        }

        private string GetStationColor(string stationType)
        {
            return TransportRenderConstants.GetStationColorHex(stationType);
        }

        #endregion

        #region Add Waypoint

        private void AddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            var scrollX = CanvasScroller.HorizontalOffset;
            var scrollY = CanvasScroller.VerticalOffset;
            var x = (scrollX + CanvasScroller.ViewportWidth / 2) / _currentZoom;
            var y = (scrollY + CanvasScroller.ViewportHeight / 2) / _currentZoom;

            AddWaypointAt(x, y);
        }

        public WaypointData AddWaypointAt(double x, double y, TransportNetworkData? network = null, 
            bool isJunction = false)
        {
            EnsureTransportCollections();
            SaveUndoState();

            network ??= GetOrCreateActiveNetwork();

            var count = network.Waypoints.Count;
            var wp = new WaypointData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"WP {count + 1}",
                NetworkId = network.Id,
                X = x,
                Y = y,
                Color = network.Visual.Color,
                IsJunction = isJunction
            };

            network.Waypoints.Add(wp);
            
            // Also add to legacy collection
            _layout.Waypoints!.Add(wp);
            
            MarkDirty();
            Redraw();

            StatusText.Text = $"Added waypoint: {wp.Name}";
            return wp;
        }

        #endregion

        #region Track Drawing

        private void StartTrackDrawing(string pointId, Point position)
        {
            _isDrawingTrack = true;
            _trackStartPointId = pointId;
            _trackStartPosition = position;
            _currentNetwork = GetOrCreateActiveNetwork();

            StatusText.Text = "Drawing track - click on another station/waypoint to connect, ESC to cancel";
            ModeText.Text = "TRACK";
        }

        private void ContinueTrackDrawing(Point currentPos)
        {
            if (!_isDrawingTrack || _trackStartPosition == null || _transportRenderer == null) return;

            // Check for snap target
            var (snapId, snapPos) = FindNearestTrackPoint(currentPos, TransportRenderConstants.SnapDistance);
            
            if (snapId != null && snapId != _trackStartPointId)
            {
                // Near a valid snap point
                _transportRenderer.DrawTrackPreview(EditorCanvas, _trackStartPosition.Value, snapPos, true);
                _transportRenderer.DrawSnapIndicator(EditorCanvas, snapPos);
            }
            else
            {
                // Free drawing
                _transportRenderer.DrawTrackPreview(EditorCanvas, _trackStartPosition.Value, currentPos, snapId == null);
            }
        }

        private void CompleteTrackSegment(string endPointId, Point endPosition)
        {
            if (!_isDrawingTrack || _trackStartPointId == null || _currentNetwork == null) return;

            // Don't connect to self
            if (endPointId == _trackStartPointId)
            {
                StatusText.Text = "Cannot connect point to itself";
                return;
            }

            // Check if segment already exists
            if (_currentNetwork.ArePointsConnected(_trackStartPointId, endPointId))
            {
                StatusText.Text = "These points are already connected";
                return;
            }

            SaveUndoState();

            // Calculate distance
            var distance = Math.Sqrt(
                Math.Pow(endPosition.X - _trackStartPosition!.Value.X, 2) +
                Math.Pow(endPosition.Y - _trackStartPosition!.Value.Y, 2)
            );

            // Create segment
            var segment = new TrackSegmentData
            {
                Id = Guid.NewGuid().ToString(),
                NetworkId = _currentNetwork.Id,
                From = _trackStartPointId,
                To = endPointId,
                Distance = Math.Round(distance, 1),
                Bidirectional = true,
                SpeedLimit = 2.0,
                LaneCount = 1
            };

            _currentNetwork.Segments.Add(segment);

            // Continue from this point
            _trackStartPointId = endPointId;
            _trackStartPosition = endPosition;

            MarkDirty();
            Redraw();

            StatusText.Text = $"Added track segment ({distance:F0}px) - click next point or ESC to finish";
        }

        private void FinishTrackDrawing()
        {
            _isDrawingTrack = false;
            _trackStartPointId = null;
            _trackStartPosition = null;
            _currentNetwork = null;
            _currentTrack = null;
            ModeText.Text = "";
            
            _transportRenderer?.RemovePreviewElements(EditorCanvas);
            
            StatusText.Text = "Track drawing finished";
            Redraw();
        }

        private void DrawTrackPreview(Point from, Point to)
        {
            // Remove old preview
            var oldPreview = EditorCanvas.Children.OfType<Line>()
                .FirstOrDefault(l => l.Tag as string == "trackPreview");
            if (oldPreview != null)
                EditorCanvas.Children.Remove(oldPreview);

            var preview = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(TransportRenderConstants.PreviewColor),
                StrokeThickness = 3,
                StrokeDashArray = TransportRenderConstants.PreviewDashArray,
                Tag = "trackPreview",
                IsHitTestVisible = false
            };
            EditorCanvas.Children.Add(preview);
        }

        #endregion

        #region Track Tool

        private void HandleTrackToolClick(Point position)
        {
            // First check if we're near an existing segment for waypoint insertion
            if (_isInsertingWaypoint)
            {
                CompleteWaypointInsertion(position);
                return;
            }

            // Find nearest station or waypoint
            var (pointId, pointPos) = FindNearestTrackPoint(position, TransportRenderConstants.SnapDistance);

            if (pointId == null)
            {
                // No nearby point - check if near a segment for splitting
                var (segment, projPos, dist) = _transportRenderer?.FindNearestSegment(position, _layout) 
                    ?? (null, position, double.MaxValue);

                if (segment != null && dist < TransportRenderConstants.SegmentHitDistance)
                {
                    // Offer to insert waypoint
                    StartWaypointInsertion(segment, projPos);
                    return;
                }

                // Offer to add a new waypoint
                if (MessageBox.Show("No station or waypoint nearby. Add a waypoint here?",
                    "Add Waypoint", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var wp = AddWaypointAt(position.X, position.Y);
                    if (_isDrawingTrack)
                    {
                        CompleteTrackSegment(wp.Id, new Point(wp.X, wp.Y));
                    }
                    else
                    {
                        StartTrackDrawing(wp.Id, new Point(wp.X, wp.Y));
                    }
                }
                return;
            }

            if (_isDrawingTrack)
            {
                CompleteTrackSegment(pointId, pointPos);
            }
            else
            {
                StartTrackDrawing(pointId, pointPos);
            }
        }

        private void HandleTrackToolMove(Point position)
        {
            if (_isDrawingTrack)
            {
                ContinueTrackDrawing(position);
            }
            else if (_isInsertingWaypoint && _segmentToSplit != null)
            {
                // Show waypoint insertion preview
                var fromPos = GetPointPosition(_segmentToSplit.From);
                var toPos = GetPointPosition(_segmentToSplit.To);
                if (fromPos.HasValue && toPos.HasValue)
                {
                    _transportRenderer?.DrawWaypointInsertPreview(EditorCanvas, position, fromPos.Value, toPos.Value);
                }
            }
        }

        private (string? id, Point position) FindNearestTrackPoint(Point pos, double maxDistance)
        {
            string? nearestId = null;
            Point nearestPos = pos;
            double nearestDist = maxDistance;

            // Check network stations and waypoints
            var network = GetActiveNetwork();
            if (network != null)
            {
                foreach (var station in network.Stations)
                {
                    var (cx, cy) = station.GetCenter();
                    var center = new Point(cx, cy);
                    var dist = Distance(pos, center);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestId = station.Id;
                        nearestPos = center;
                    }
                }

                foreach (var wp in network.Waypoints)
                {
                    var wpPos = new Point(wp.X, wp.Y);
                    var dist = Distance(pos, wpPos);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestId = wp.Id;
                        nearestPos = wpPos;
                    }
                }
            }

            // Also check legacy collections
            if (_layout.TransportStations != null)
            {
                foreach (var station in _layout.TransportStations)
                {
                    var (cx, cy) = station.GetCenter();
                    var center = new Point(cx, cy);
                    var dist = Distance(pos, center);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestId = station.Id;
                        nearestPos = center;
                    }
                }
            }

            if (_layout.Waypoints != null)
            {
                foreach (var wp in _layout.Waypoints)
                {
                    var wpPos = new Point(wp.X, wp.Y);
                    var dist = Distance(pos, wpPos);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestId = wp.Id;
                        nearestPos = wpPos;
                    }
                }
            }

            // Check regular nodes (can connect transport to machines)
            foreach (var node in _layout.Nodes)
            {
                var center = new Point(
                    node.Visual.X + node.Visual.Width / 2,
                    node.Visual.Y + node.Visual.Height / 2
                );
                var dist = Distance(pos, center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestId = node.Id;
                    nearestPos = center;
                }
            }

            return (nearestId, nearestPos);
        }

        private Point? GetPointPosition(string pointId)
        {
            // Check network stations
            var network = GetActiveNetwork();
            if (network != null)
            {
                var station = network.Stations.FirstOrDefault(s => s.Id == pointId);
                if (station != null)
                {
                    var (cx, cy) = station.GetCenter();
                    return new Point(cx, cy);
                }

                var wp = network.Waypoints.FirstOrDefault(w => w.Id == pointId);
                if (wp != null)
                    return new Point(wp.X, wp.Y);
            }

            // Check legacy collections
            var legacyStation = _layout.TransportStations?.FirstOrDefault(s => s.Id == pointId);
            if (legacyStation != null)
            {
                var (cx, cy) = legacyStation.GetCenter();
                return new Point(cx, cy);
            }

            var legacyWp = _layout.Waypoints?.FirstOrDefault(w => w.Id == pointId);
            if (legacyWp != null)
                return new Point(legacyWp.X, legacyWp.Y);

            // Check nodes
            var node = _layout.Nodes.FirstOrDefault(n => n.Id == pointId);
            if (node != null)
            {
                return new Point(
                    node.Visual.X + node.Visual.Width / 2,
                    node.Visual.Y + node.Visual.Height / 2
                );
            }

            return null;
        }

        private double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        #endregion

        #region Waypoint Insertion (Segment Splitting)

        private void StartWaypointInsertion(TrackSegmentData segment, Point position)
        {
            _isInsertingWaypoint = true;
            _segmentToSplit = segment;
            _waypointInsertPosition = position;
            
            StatusText.Text = "Click to insert waypoint, or ESC to cancel";
            ModeText.Text = "INSERT WP";
        }

        private void CompleteWaypointInsertion(Point position)
        {
            if (!_isInsertingWaypoint || _segmentToSplit == null) return;

            SaveUndoState();

            var network = GetActiveNetwork();
            if (network == null)
            {
                CancelWaypointInsertion();
                return;
            }

            // Create new waypoint
            var wp = new WaypointData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"WP {network.Waypoints.Count + 1}",
                NetworkId = network.Id,
                X = position.X,
                Y = position.Y,
                Color = network.Visual.Color
            };

            network.Waypoints.Add(wp);
            _layout.Waypoints!.Add(wp);

            // Calculate distances
            var fromPos = GetPointPosition(_segmentToSplit.From);
            var toPos = GetPointPosition(_segmentToSplit.To);
            
            var distFrom = fromPos.HasValue ? Distance(fromPos.Value, position) : 0;
            var distTo = toPos.HasValue ? Distance(position, toPos.Value) : 0;

            // Create two new segments
            var seg1 = new TrackSegmentData
            {
                Id = Guid.NewGuid().ToString(),
                NetworkId = network.Id,
                From = _segmentToSplit.From,
                To = wp.Id,
                Distance = Math.Round(distFrom, 1),
                Bidirectional = _segmentToSplit.Bidirectional,
                SpeedLimit = _segmentToSplit.SpeedLimit,
                LaneCount = _segmentToSplit.LaneCount
            };

            var seg2 = new TrackSegmentData
            {
                Id = Guid.NewGuid().ToString(),
                NetworkId = network.Id,
                From = wp.Id,
                To = _segmentToSplit.To,
                Distance = Math.Round(distTo, 1),
                Bidirectional = _segmentToSplit.Bidirectional,
                SpeedLimit = _segmentToSplit.SpeedLimit,
                LaneCount = _segmentToSplit.LaneCount
            };

            // Remove old segment, add new ones
            network.Segments.Remove(_segmentToSplit);
            network.Segments.Add(seg1);
            network.Segments.Add(seg2);

            CancelWaypointInsertion();
            MarkDirty();
            Redraw();

            StatusText.Text = $"Inserted waypoint, splitting segment";
        }

        private void CancelWaypointInsertion()
        {
            _isInsertingWaypoint = false;
            _segmentToSplit = null;
            ModeText.Text = "";
            _transportRenderer?.RemovePreviewElements(EditorCanvas);
        }

        #endregion

        #region Segment Properties

        private void ShowSegmentProperties(TrackSegmentData segment)
        {
            // This could open a properties panel or dialog
            // For now, we'll use a simple dialog

            var dialog = new Window
            {
                Title = "Segment Properties",
                Width = 300,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(10) };

            // Bidirectional checkbox
            var biCheck = new CheckBox
            {
                Content = "Bidirectional",
                IsChecked = segment.Bidirectional,
                Margin = new Thickness(0, 5, 0, 5)
            };
            panel.Children.Add(biCheck);

            // Speed limit
            panel.Children.Add(new TextBlock { Text = "Speed Limit (m/s):", Margin = new Thickness(0, 10, 0, 2) });
            var speedBox = new TextBox { Text = segment.SpeedLimit.ToString("F1"), Width = 100 };
            panel.Children.Add(speedBox);

            // Lane count
            panel.Children.Add(new TextBlock { Text = "Lane Count:", Margin = new Thickness(0, 10, 0, 2) });
            var laneBox = new TextBox { Text = segment.LaneCount.ToString(), Width = 100 };
            panel.Children.Add(laneBox);

            // Priority
            panel.Children.Add(new TextBlock { Text = "Priority:", Margin = new Thickness(0, 10, 0, 2) });
            var priorityBox = new TextBox { Text = segment.Priority.ToString(), Width = 100 };
            panel.Children.Add(priorityBox);

            // Blocked checkbox
            var blockedCheck = new CheckBox
            {
                Content = "Temporarily Blocked",
                IsChecked = segment.IsBlocked,
                Margin = new Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(blockedCheck);

            // Buttons
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var okBtn = new Button { Content = "OK", Width = 75, Margin = new Thickness(5, 0, 5, 0) };
            var cancelBtn = new Button { Content = "Cancel", Width = 75 };

            okBtn.Click += (s, e) =>
            {
                SaveUndoState();
                
                segment.Bidirectional = biCheck.IsChecked ?? true;
                segment.IsBlocked = blockedCheck.IsChecked ?? false;

                if (double.TryParse(speedBox.Text, out var speed))
                    segment.SpeedLimit = Math.Max(0.1, speed);
                if (int.TryParse(laneBox.Text, out var lanes))
                    segment.LaneCount = Math.Max(1, Math.Min(4, lanes));
                if (int.TryParse(priorityBox.Text, out var priority))
                    segment.Priority = priority;

                MarkDirty();
                Redraw();
                dialog.Close();
            };

            cancelBtn.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            dialog.ShowDialog();
        }

        #endregion

        #region Add Transporter

        private void AddTransporter_Click(object sender, RoutedEventArgs e)
        {
            var network = GetActiveNetwork();
            
            // Find a home station
            TransportStationData? homeStation = null;
            
            if (network != null)
            {
                homeStation = network.Stations.FirstOrDefault(s => 
                    s.Simulation.StationType == StationTypes.Home);
            }
            
            if (homeStation == null)
            {
                homeStation = _layout.TransportStations?.FirstOrDefault(s =>
                    s.Simulation.StationType == StationTypes.Home);
            }

            if (homeStation == null)
            {
                MessageBox.Show("Please add a Home station first.", "No Home Station",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveUndoState();

            var count = network?.Transporters.Count ?? _layout.Transporters?.Count ?? 0;
            var transporter = new TransporterData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"AGV {count + 1}",
                NetworkId = network?.Id ?? "",
                HomeStationId = homeStation.Id,
                Speed = 1.5,
                Capacity = 1,
                Color = "#E74C3C"
            };

            if (network != null)
            {
                network.Transporters.Add(transporter);
            }
            _layout.Transporters!.Add(transporter);
            
            MarkDirty();
            Redraw();

            StatusText.Text = $"Added transporter: {transporter.Name}";
        }

        #endregion

        #region Delete Transport Elements

        public void DeleteSelectedTransportElements()
        {
            if (_transportRenderer == null) return;

            var anyDeleted = false;

            // Delete selected stations
            var stationIds = _transportRenderer.GetSelectedStationIds().ToList();
            if (stationIds.Any())
            {
                SaveUndoState();
                anyDeleted = true;

                foreach (var stationId in stationIds)
                {
                    // Remove from networks
                    foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
                    {
                        var station = network.Stations.FirstOrDefault(s => s.Id == stationId);
                        if (station != null)
                        {
                            network.Stations.Remove(station);
                            
                            // Remove connected segments
                            var segsToRemove = network.Segments
                                .Where(s => s.From == stationId || s.To == stationId)
                                .ToList();
                            foreach (var seg in segsToRemove)
                                network.Segments.Remove(seg);
                        }
                    }

                    // Remove from legacy collection
                    var legacyStation = _layout.TransportStations?.FirstOrDefault(s => s.Id == stationId);
                    if (legacyStation != null)
                        _layout.TransportStations!.Remove(legacyStation);
                }

                StatusText.Text = $"Deleted {stationIds.Count} station(s)";
            }

            // Delete selected waypoints
            var waypointIds = _transportRenderer.GetSelectedWaypointIds().ToList();
            if (waypointIds.Any())
            {
                if (!anyDeleted) SaveUndoState();
                anyDeleted = true;

                foreach (var wpId in waypointIds)
                {
                    foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
                    {
                        var wp = network.Waypoints.FirstOrDefault(w => w.Id == wpId);
                        if (wp != null)
                        {
                            network.Waypoints.Remove(wp);
                            
                            var segsToRemove = network.Segments
                                .Where(s => s.From == wpId || s.To == wpId)
                                .ToList();
                            foreach (var seg in segsToRemove)
                                network.Segments.Remove(seg);
                        }
                    }

                    var legacyWp = _layout.Waypoints?.FirstOrDefault(w => w.Id == wpId);
                    if (legacyWp != null)
                        _layout.Waypoints!.Remove(legacyWp);
                }

                StatusText.Text = $"Deleted {waypointIds.Count} waypoint(s)";
            }

            // Delete selected segments
            var segmentIds = _transportRenderer.GetSelectedSegmentIds().ToList();
            if (segmentIds.Any())
            {
                if (!anyDeleted) SaveUndoState();
                anyDeleted = true;

                foreach (var segId in segmentIds)
                {
                    foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
                    {
                        var seg = network.Segments.FirstOrDefault(s => s.Id == segId);
                        if (seg != null)
                            network.Segments.Remove(seg);
                    }

                    // Also check legacy tracks
                    foreach (var track in _layout.TransporterTracks ?? Enumerable.Empty<TransporterTrackData>())
                    {
                        var seg = track.Segments.FirstOrDefault(s => s.Id == segId);
                        if (seg != null)
                            track.Segments.Remove(seg);
                    }
                }

                StatusText.Text = $"Deleted {segmentIds.Count} segment(s)";
            }

            if (anyDeleted)
            {
                _transportRenderer.ClearSelection();
                MarkDirty();
                Redraw();
            }
        }

        public void DeleteSegment(TrackSegmentData segment)
        {
            SaveUndoState();

            foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
            {
                if (network.Segments.Remove(segment))
                    break;
            }

            foreach (var track in _layout.TransporterTracks ?? Enumerable.Empty<TransporterTrackData>())
            {
                if (track.Segments.Remove(segment))
                    break;
            }

            MarkDirty();
            Redraw();
            StatusText.Text = "Deleted track segment";
        }

        #endregion

        #region Transport Element Selection

        private void SelectTransportStation(TransportStationData station)
        {
            _transportRenderer?.SelectStation(station.Id);
            Redraw();
            
            // Update properties panel if you have one
            StatusText.Text = $"Selected station: {station.Name}";
        }

        private void SelectWaypoint(WaypointData waypoint)
        {
            _transportRenderer?.SelectWaypoint(waypoint.Id);
            Redraw();
            
            StatusText.Text = $"Selected waypoint: {waypoint.Name}";
        }

        private void SelectSegment(TrackSegmentData segment)
        {
            _transportRenderer?.SelectSegment(segment.Id);
            Redraw();
            
            StatusText.Text = $"Selected segment: {segment.From} → {segment.To}";
        }

        #endregion

        #region Station Terminal Layout

        /// <summary>
        /// Cycle through terminal layout options for a station
        /// </summary>
        private void CycleStationTerminalLayout(TransportStationData station)
        {
            SaveUndoState();
            
            station.Visual.TerminalLayout = station.Visual.TerminalLayout switch
            {
                TerminalLayouts.LeftRight => TerminalLayouts.RightLeft,
                TerminalLayouts.RightLeft => TerminalLayouts.TopBottom,
                TerminalLayouts.TopBottom => TerminalLayouts.BottomTop,
                TerminalLayouts.BottomTop => TerminalLayouts.Left,
                TerminalLayouts.Left => TerminalLayouts.Right,
                TerminalLayouts.Right => TerminalLayouts.Top,
                TerminalLayouts.Top => TerminalLayouts.Bottom,
                TerminalLayouts.Bottom => TerminalLayouts.Center,
                TerminalLayouts.Center => TerminalLayouts.LeftRight,
                _ => TerminalLayouts.LeftRight
            };
            
            MarkDirty();
            Redraw();
            StatusText.Text = $"Terminal layout: {station.Visual.TerminalLayout}";
        }

        /// <summary>
        /// Set specific terminal layout for a station
        /// </summary>
        private void SetStationTerminalLayout(TransportStationData station, string layout)
        {
            SaveUndoState();
            station.Visual.TerminalLayout = layout;
            MarkDirty();
            Redraw();
            StatusText.Text = $"Terminal layout: {layout}";
        }

        /// <summary>
        /// Toggle terminal visibility for a station
        /// </summary>
        private void ToggleStationTerminals(TransportStationData station)
        {
            SaveUndoState();
            station.Visual.ShowTerminals = !station.Visual.ShowTerminals;
            MarkDirty();
            Redraw();
            StatusText.Text = station.Visual.ShowTerminals ? "Terminals shown" : "Terminals hidden";
        }

        #endregion

        #region Keyboard Handling for Transport

        private void HandleTransportKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_isDrawingTrack)
                {
                    FinishTrackDrawing();
                    e.Handled = true;
                }
                else if (_isInsertingWaypoint)
                {
                    CancelWaypointInsertion();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedTransportElements();
                e.Handled = true;
            }
        }

        #endregion

        #region Rendering Integration

        /// <summary>
        /// Call this from Redraw() to render transport elements
        /// </summary>
        private void DrawTransportElements()
        {
            if (_transportRenderer == null) return;
            if (!_showTracks) return;  // Check visibility flag

            EnsureTransportCollections();

            // Create adapter for RegisterElement
            Action<FrameworkElement, string, string>? registerAdapter = (element, id, type) =>
            {
                RegisterElement(id, element);
                
                // Add click handlers for selection
                if (type == "transportStation" || type == "waypoint" || type == "trackSegment")
                {
                    element.MouseLeftButtonDown += (s, e) =>
                    {
                        HandleTransportElementClick(id, type, e);
                    };
                    
                    element.MouseRightButtonDown += (s, e) =>
                    {
                        HandleTransportElementRightClick(id, type, e);
                    };
                }
            };

            // Draw transport networks (new model)
            _transportRenderer.DrawNetworks(EditorCanvas, _layout, registerAdapter);

            // Draw legacy elements for backward compatibility
            _transportRenderer.DrawTracks(EditorCanvas, _layout, registerAdapter);
            _transportRenderer.DrawWaypoints(EditorCanvas, _layout, registerAdapter);
            _transportRenderer.DrawStations(EditorCanvas, _layout, registerAdapter);
            _transportRenderer.DrawTransporters(EditorCanvas, _layout);
        }

        private void HandleTransportElementClick(string id, string type, MouseButtonEventArgs e)
        {
            if (_isDrawingTrack)
            {
                // If drawing a track, try to connect to this element
                var pos = GetPointPosition(id);
                if (pos.HasValue)
                {
                    CompleteTrackSegment(id, pos.Value);
                }
                e.Handled = true;
                return;
            }

            // Normal selection
            bool addToSelection = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            
            if (!addToSelection)
            {
                _transportRenderer?.ClearSelection();
            }

            switch (type)
            {
                case "transportStation":
                    var station = FindStationById(id);
                    if (station != null)
                    {
                        if (addToSelection)
                            _transportRenderer?.AddToSelection(id, "station");
                        else
                            SelectTransportStation(station);
                    }
                    break;
                    
                case "waypoint":
                    var wp = FindWaypointById(id);
                    if (wp != null)
                    {
                        if (addToSelection)
                            _transportRenderer?.AddToSelection(id, "waypoint");
                        else
                            SelectWaypoint(wp);
                    }
                    break;
                    
                case "trackSegment":
                    var seg = FindSegmentById(id);
                    if (seg != null)
                    {
                        if (addToSelection)
                            _transportRenderer?.AddToSelection(id, "segment");
                        else
                            SelectSegment(seg);
                    }
                    break;
            }

            e.Handled = true;
        }

        private void HandleTransportElementRightClick(string id, string type, MouseButtonEventArgs e)
        {
            // Show context menu
            var menu = new ContextMenu();

            if (type == "trackSegment")
            {
                var seg = FindSegmentById(id);
                if (seg != null)
                {
                    var propsItem = new MenuItem { Header = "Properties..." };
                    propsItem.Click += (s, args) => ShowSegmentProperties(seg);
                    menu.Items.Add(propsItem);

                    var toggleBi = new MenuItem 
                    { 
                        Header = seg.Bidirectional ? "Make Unidirectional" : "Make Bidirectional" 
                    };
                    toggleBi.Click += (s, args) =>
                    {
                        SaveUndoState();
                        seg.Bidirectional = !seg.Bidirectional;
                        MarkDirty();
                        Redraw();
                    };
                    menu.Items.Add(toggleBi);

                    menu.Items.Add(new Separator());

                    var insertWp = new MenuItem { Header = "Insert Waypoint" };
                    insertWp.Click += (s, args) =>
                    {
                        var mousePos = e.GetPosition(EditorCanvas);
                        StartWaypointInsertion(seg, mousePos);
                    };
                    menu.Items.Add(insertWp);

                    menu.Items.Add(new Separator());

                    var deleteItem = new MenuItem { Header = "Delete Segment" };
                    deleteItem.Click += (s, args) => DeleteSegment(seg);
                    menu.Items.Add(deleteItem);
                }
            }
            else if (type == "transportStation")
            {
                var station = FindStationById(id);
                if (station != null)
                {
                    // Terminal layout submenu
                    var terminalMenu = new MenuItem { Header = "Terminal Layout" };
                    
                    var layouts = new[] 
                    {
                        (TerminalLayouts.LeftRight, "← ● ● → (Left-Right)"),
                        (TerminalLayouts.RightLeft, "→ ● ● ← (Right-Left)"),
                        (TerminalLayouts.TopBottom, "↑ Top / ↓ Bottom"),
                        (TerminalLayouts.BottomTop, "↓ Bottom / ↑ Top"),
                        (TerminalLayouts.Top, "↑ Top Only"),
                        (TerminalLayouts.Bottom, "↓ Bottom Only"),
                        (TerminalLayouts.Left, "← Left Only"),
                        (TerminalLayouts.Right, "→ Right Only"),
                        (TerminalLayouts.Center, "● Center")
                    };
                    
                    foreach (var (layout, label) in layouts)
                    {
                        var item = new MenuItem 
                        { 
                            Header = label,
                            IsChecked = station.Visual.TerminalLayout == layout
                        };
                        var capturedLayout = layout;
                        item.Click += (s, args) => SetStationTerminalLayout(station, capturedLayout);
                        terminalMenu.Items.Add(item);
                    }
                    
                    menu.Items.Add(terminalMenu);

                    // Quick flip option
                    var flipItem = new MenuItem { Header = "Flip Terminals (F)" };
                    flipItem.Click += (s, args) => CycleStationTerminalLayout(station);
                    menu.Items.Add(flipItem);

                    // Toggle terminals visibility
                    var toggleTerminals = new MenuItem 
                    { 
                        Header = station.Visual.ShowTerminals ? "Hide Terminals" : "Show Terminals" 
                    };
                    toggleTerminals.Click += (s, args) => ToggleStationTerminals(station);
                    menu.Items.Add(toggleTerminals);

                    menu.Items.Add(new Separator());

                    var deleteItem = new MenuItem { Header = "Delete Station" };
                    deleteItem.Click += (s, args) =>
                    {
                        _transportRenderer?.AddToSelection(id, "station");
                        DeleteSelectedTransportElements();
                    };
                    menu.Items.Add(deleteItem);
                }
            }
            else
            {
                var deleteItem = new MenuItem { Header = $"Delete {type}" };
                deleteItem.Click += (s, args) =>
                {
                    _transportRenderer?.AddToSelection(id, type);
                    DeleteSelectedTransportElements();
                };
                menu.Items.Add(deleteItem);
            }

            menu.IsOpen = true;
            e.Handled = true;
        }

        #endregion

        #region Find Elements by ID

        private TransportStationData? FindStationById(string id)
        {
            // Check networks
            foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
            {
                var station = network.Stations.FirstOrDefault(s => s.Id == id);
                if (station != null) return station;
            }
            
            // Check legacy
            return _layout.TransportStations?.FirstOrDefault(s => s.Id == id);
        }

        private WaypointData? FindWaypointById(string id)
        {
            foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
            {
                var wp = network.Waypoints.FirstOrDefault(w => w.Id == id);
                if (wp != null) return wp;
            }
            
            return _layout.Waypoints?.FirstOrDefault(w => w.Id == id);
        }

        private TrackSegmentData? FindSegmentById(string id)
        {
            foreach (var network in _layout.TransportNetworks ?? Enumerable.Empty<TransportNetworkData>())
            {
                var seg = network.Segments.FirstOrDefault(s => s.Id == id);
                if (seg != null) return seg;
            }
            
            foreach (var track in _layout.TransporterTracks ?? Enumerable.Empty<TransporterTrackData>())
            {
                var seg = track.Segments.FirstOrDefault(s => s.Id == id);
                if (seg != null) return seg;
            }
            
            return null;
        }

        #endregion

        #region Migration Helper

        /// <summary>
        /// Migrate legacy transport elements to the new network model
        /// </summary>
        private void MigrateLegacyTransportToNetworks()
        {
            if (_layout.TransportStations?.Count == 0 && 
                _layout.Waypoints?.Count == 0 && 
                _layout.TransporterTracks?.Count == 0)
            {
                return; // Nothing to migrate
            }

            EnsureTransportCollections();

            // Create a default network for legacy elements
            var legacyNetwork = new TransportNetworkData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Migrated Network",
                Description = "Auto-migrated from legacy transport elements"
            };

            // Migrate stations
            foreach (var station in _layout.TransportStations ?? Enumerable.Empty<TransportStationData>())
            {
                station.NetworkId = legacyNetwork.Id;
                legacyNetwork.Stations.Add(station);
            }

            // Migrate waypoints
            foreach (var wp in _layout.Waypoints ?? Enumerable.Empty<WaypointData>())
            {
                wp.NetworkId = legacyNetwork.Id;
                legacyNetwork.Waypoints.Add(wp);
            }

            // Migrate track segments
            foreach (var track in _layout.TransporterTracks ?? Enumerable.Empty<TransporterTrackData>())
            {
                foreach (var segment in track.Segments)
                {
                    segment.NetworkId = legacyNetwork.Id;
                    legacyNetwork.Segments.Add(segment);
                }
            }

            // Migrate transporters
            foreach (var t in _layout.Transporters ?? Enumerable.Empty<TransporterData>())
            {
                t.NetworkId = legacyNetwork.Id;
                legacyNetwork.Transporters.Add(t);
            }

            if (legacyNetwork.Stations.Count > 0 || legacyNetwork.Waypoints.Count > 0 || 
                legacyNetwork.Segments.Count > 0)
            {
                _layout.TransportNetworks!.Add(legacyNetwork);
                _activeNetworkId = legacyNetwork.Id;
                
                StatusText.Text = $"Migrated {legacyNetwork.Stations.Count} stations, " +
                    $"{legacyNetwork.Waypoints.Count} waypoints, " +
                    $"{legacyNetwork.Segments.Count} segments to network";
            }
        }

        #endregion
    }
}
