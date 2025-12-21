using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handles AGV waypoint, path, station, and traffic zone tools
    /// </summary>
    public partial class MainWindow
    {
        // AGV Path drawing state
        private bool _isDrawingAGVPath = false;
        private string? _lastAGVWaypointId = null;
        private const double WaypointSnapThreshold = 20.0; // pixels

        // AGV Station placement state
        private bool _isPlacingAGVStation = false;

        // Traffic Zone drawing state
        private bool _isDrawingTrafficZone = false;
        private List<PointData> _trafficZonePoints = new();

        #region AGV Path Tool

        /// <summary>
        /// Start AGV path drawing tool
        /// </summary>
        public void StartDrawingAGVPath()
        {
            _isDrawingAGVPath = true;
            _lastAGVWaypointId = null;
            StatusText.Text = "AGV Path Tool: Click to create waypoints. Shift+Click for bidirectional. Double-click or Esc to finish.";
            Mouse.OverrideCursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancel AGV path drawing
        /// </summary>
        public void CancelDrawingAGVPath()
        {
            _isDrawingAGVPath = false;
            _lastAGVWaypointId = null;
            Mouse.OverrideCursor = null;
            StatusText.Text = "AGV path drawing canceled";
            Redraw();
        }

        /// <summary>
        /// Handle click while drawing AGV path
        /// </summary>
        private void HandleAGVPathClick(Point canvasPoint, MouseButtonEventArgs e)
        {
            if (!_isDrawingAGVPath) return;

            // Check if clicking near an existing waypoint (snap)
            var nearbyWaypoint = FindNearbyWaypoint(canvasPoint);

            string waypointId;
            if (nearbyWaypoint != null)
            {
                // Snap to existing waypoint
                waypointId = nearbyWaypoint.Id;
                StatusText.Text = $"Snapped to waypoint: {nearbyWaypoint.Name}";
            }
            else
            {
                // Create new waypoint at click position
                SaveUndoState();
                var waypoint = new AGVWaypointData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"WP{_layout.AGVWaypoints.Count + 1}",
                    X = canvasPoint.X,
                    Y = canvasPoint.Y,
                    WaypointType = WaypointTypes.Through
                };
                _layout.AGVWaypoints.Add(waypoint);
                waypointId = waypoint.Id;
                MarkDirty();
            }

            // If this is not the first waypoint, create a path from last to current
            if (_lastAGVWaypointId != null && _lastAGVWaypointId != waypointId)
            {
                var direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                    ? PathDirections.Bidirectional
                    : PathDirections.Unidirectional;

                var path = new AGVPathData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Path{_layout.AGVPaths.Count + 1}",
                    FromWaypointId = _lastAGVWaypointId,
                    ToWaypointId = waypointId,
                    Direction = direction
                };
                _layout.AGVPaths.Add(path);
                StatusText.Text = $"Created {direction} path. Click next waypoint or double-click to finish.";
            }

            _lastAGVWaypointId = waypointId;
            Redraw();
        }

        /// <summary>
        /// Find waypoint near click position
        /// </summary>
        private AGVWaypointData? FindNearbyWaypoint(Point clickPos)
        {
            foreach (var waypoint in _layout.AGVWaypoints)
            {
                var dx = waypoint.X - clickPos.X;
                var dy = waypoint.Y - clickPos.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < WaypointSnapThreshold)
                    return waypoint;
            }
            return null;
        }

        /// <summary>
        /// Finish AGV path drawing on double-click
        /// </summary>
        private void FinishAGVPathDrawing()
        {
            _isDrawingAGVPath = false;
            _lastAGVWaypointId = null;
            Mouse.OverrideCursor = null;
            StatusText.Text = "AGV path drawing complete";
            Redraw();
        }

        /// <summary>
        /// Menu handler for AGV path tool
        /// </summary>
        private void DrawAGVPath_Click(object sender, RoutedEventArgs e)
        {
            StartDrawingAGVPath();
        }

        #endregion

        #region AGV Station Tool

        /// <summary>
        /// Start AGV station placement
        /// </summary>
        public void StartPlacingAGVStation()
        {
            _isPlacingAGVStation = true;
            StatusText.Text = "AGV Station Tool: Click to place station. It will auto-link to nearest waypoint.";
            Mouse.OverrideCursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancel AGV station placement
        /// </summary>
        public void CancelPlacingAGVStation()
        {
            _isPlacingAGVStation = false;
            Mouse.OverrideCursor = null;
            StatusText.Text = "AGV station placement canceled";
        }

        /// <summary>
        /// Handle click while placing AGV station
        /// </summary>
        private void HandleAGVStationClick(Point canvasPoint)
        {
            if (!_isPlacingAGVStation) return;

            SaveUndoState();

            // Create station
            var station = new AGVStationData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Station{_layout.AGVStations.Count + 1}",
                X = canvasPoint.X,
                Y = canvasPoint.Y,
                StationType = AGVStationTypes.LoadUnload
            };

            // Auto-link to nearest waypoint
            var nearestWaypoint = FindNearestWaypoint(canvasPoint);
            if (nearestWaypoint != null)
            {
                station.LinkedWaypointId = nearestWaypoint.Id;
                StatusText.Text = $"Station placed and linked to {nearestWaypoint.Name}";
            }
            else
            {
                StatusText.Text = "Station placed (no waypoints nearby to link)";
            }

            _layout.AGVStations.Add(station);
            MarkDirty();
            Redraw();

            // Continue placing mode
            StatusText.Text += ". Click to place another or Esc to finish.";
        }

        /// <summary>
        /// Find nearest waypoint to a position
        /// </summary>
        private AGVWaypointData? FindNearestWaypoint(Point pos)
        {
            AGVWaypointData? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var waypoint in _layout.AGVWaypoints)
            {
                var dx = waypoint.X - pos.X;
                var dy = waypoint.Y - pos.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = waypoint;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Menu handler for AGV station tool
        /// </summary>
        private void PlaceAGVStation_Click(object sender, RoutedEventArgs e)
        {
            StartPlacingAGVStation();
        }

        #endregion

        #region Traffic Zone Tool

        /// <summary>
        /// Start traffic zone drawing
        /// </summary>
        public void StartDrawingTrafficZone()
        {
            _isDrawingTrafficZone = true;
            _trafficZonePoints.Clear();
            StatusText.Text = "Traffic Zone Tool: Click to add polygon points. Double-click or close polygon to finish.";
            Mouse.OverrideCursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancel traffic zone drawing
        /// </summary>
        public void CancelDrawingTrafficZone()
        {
            _isDrawingTrafficZone = false;
            _trafficZonePoints.Clear();
            Mouse.OverrideCursor = null;
            StatusText.Text = "Traffic zone drawing canceled";
            Redraw();
        }

        /// <summary>
        /// Handle click while drawing traffic zone
        /// </summary>
        private void HandleTrafficZoneClick(Point canvasPoint)
        {
            if (!_isDrawingTrafficZone) return;

            // Check if clicking near first point to close polygon
            if (_trafficZonePoints.Count >= 3)
            {
                var firstPoint = _trafficZonePoints[0];
                var dx = firstPoint.X - canvasPoint.X;
                var dy = firstPoint.Y - canvasPoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 20) // Close threshold
                {
                    FinishTrafficZone();
                    return;
                }
            }

            // Add point to polygon
            _trafficZonePoints.Add(new PointData(canvasPoint.X, canvasPoint.Y));
            Redraw();
            DrawTrafficZonePreview();

            StatusText.Text = $"Traffic zone: {_trafficZonePoints.Count} points. Click to add more or double-click to finish.";
        }

        /// <summary>
        /// Finish traffic zone on double-click
        /// </summary>
        private void FinishTrafficZone()
        {
            if (_trafficZonePoints.Count < 3)
            {
                StatusText.Text = "Traffic zone needs at least 3 points";
                return;
            }

            SaveUndoState();

            var zone = new TrafficZoneData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Zone{_layout.TrafficZones.Count + 1}",
                Boundary = new List<PointData>(_trafficZonePoints),
                MaxVehicles = 1,
                ZoneType = TrafficZoneTypes.Exclusive
            };

            _layout.TrafficZones.Add(zone);

            _isDrawingTrafficZone = false;
            _trafficZonePoints.Clear();
            Mouse.OverrideCursor = null;
            MarkDirty();
            Redraw();

            StatusText.Text = $"Traffic zone created with {zone.Boundary.Count} points";
        }

        /// <summary>
        /// Draw preview of traffic zone being drawn
        /// </summary>
        private void DrawTrafficZonePreview()
        {
            if (_trafficZonePoints.Count < 2) return;

            // Draw temporary preview polygon
            var polygon = new System.Windows.Shapes.Polygon
            {
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) { Opacity = 0.1 }
            };

            foreach (var point in _trafficZonePoints)
            {
                polygon.Points.Add(new Point(point.X, point.Y));
            }

            EditorCanvas.Children.Add(polygon);

            // Draw point markers
            foreach (var point in _trafficZonePoints)
            {
                var marker = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = System.Windows.Media.Brushes.Red,
                    Stroke = System.Windows.Media.Brushes.DarkRed,
                    StrokeThickness = 1
                };
                System.Windows.Controls.Canvas.SetLeft(marker, point.X - 4);
                System.Windows.Controls.Canvas.SetTop(marker, point.Y - 4);
                EditorCanvas.Children.Add(marker);
            }
        }

        /// <summary>
        /// Menu handler for traffic zone tool
        /// </summary>
        private void DrawTrafficZone_Click(object sender, RoutedEventArgs e)
        {
            StartDrawingTrafficZone();
        }

        #endregion
    }
}
