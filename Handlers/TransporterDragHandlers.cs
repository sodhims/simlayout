using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Test vehicle system for validating AGV paths and forklift aisles.
    /// Use arrow keys to move test vehicles along tracks to verify layout connectivity.
    /// </summary>
    public partial class MainWindow
    {
        private TransporterData? _testVehicle;
        private bool _testVehicleActive;
        private const double MOVE_STEP = 0.05; // 5% of track per keypress

        #region Test Vehicle Management

        /// <summary>
        /// Create and place a test AGV on the track network
        /// </summary>
        public void CreateTestAGV()
        {
            if (_layout == null) return;

            // Find first AGV path to place vehicle on
            var firstPath = _layout.AGVPaths.FirstOrDefault();
            if (firstPath == null)
            {
                StatusText.Text = "No AGV paths defined - create paths first";
                return;
            }

            var fromWp = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == firstPath.FromWaypointId);
            if (fromWp == null)
            {
                StatusText.Text = "AGV path has no valid waypoint";
                return;
            }

            _testVehicle = new TransporterData
            {
                Id = "test_agv",
                Name = "Test AGV",
                TransporterType = TransporterTypes.AGV,
                CurrentX = fromWp.X,
                CurrentY = fromWp.Y,
                CurrentTrackId = firstPath.Id,
                PositionOnTrack = 0,
                Color = "#FF6B6B" // Bright red for visibility
            };

            _testVehicleActive = true;
            StatusText.Text = "Test AGV created - use Arrow keys to move along paths, Escape to remove";
            Redraw();
        }

        /// <summary>
        /// Create and place a test forklift on the aisle network
        /// </summary>
        public void CreateTestForklift()
        {
            if (_layout == null) return;

            // Find first forklift aisle to place vehicle on
            var firstAisle = _layout.ForkliftAisles.FirstOrDefault();
            if (firstAisle == null || firstAisle.Centerline == null || firstAisle.Centerline.Count < 2)
            {
                StatusText.Text = "No forklift aisles defined - create aisles first";
                return;
            }

            _testVehicle = new TransporterData
            {
                Id = "test_forklift",
                Name = "Test Forklift",
                TransporterType = TransporterTypes.Forklift,
                CurrentX = firstAisle.Centerline[0].X,
                CurrentY = firstAisle.Centerline[0].Y,
                CurrentTrackId = firstAisle.Id,
                PositionOnTrack = 0,
                Color = "#FFD93D" // Bright yellow for visibility
            };

            _testVehicleActive = true;
            StatusText.Text = "Test Forklift created - use Arrow keys to move along aisles, Escape to remove";
            Redraw();
        }

        /// <summary>
        /// Remove the test vehicle
        /// </summary>
        public void RemoveTestVehicle()
        {
            _testVehicle = null;
            _testVehicleActive = false;
            StatusText.Text = "Test vehicle removed";
            Redraw();
        }

        /// <summary>
        /// Check if a test vehicle is active
        /// </summary>
        public bool HasTestVehicle => _testVehicleActive && _testVehicle != null;

        /// <summary>
        /// Get the current test vehicle for rendering
        /// </summary>
        public TransporterData? TestVehicle => _testVehicle;

        #endregion

        #region Arrow Key Movement

        /// <summary>
        /// Handle arrow key input for test vehicle movement
        /// Returns true if the key was handled
        /// </summary>
        public bool HandleTestVehicleKey(Key key)
        {
            if (!_testVehicleActive || _testVehicle == null || _layout == null)
                return false;

            switch (key)
            {
                case Key.Right:
                case Key.Up:
                    MoveTestVehicleForward();
                    return true;

                case Key.Left:
                case Key.Down:
                    MoveTestVehicleBackward();
                    return true;

                case Key.Tab:
                    SwitchToNextTrack();
                    return true;

                case Key.Escape:
                    RemoveTestVehicle();
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Move test vehicle forward along current track
        /// </summary>
        private void MoveTestVehicleForward()
        {
            if (_testVehicle == null || _layout == null) return;

            double newPos = _testVehicle.PositionOnTrack + MOVE_STEP;

            if (newPos > 1.0)
            {
                // Try to switch to next connected track
                if (TryMoveToNextTrack())
                {
                    StatusText.Text = $"Moved to next track: {_testVehicle.CurrentTrackId}";
                }
                else
                {
                    _testVehicle.PositionOnTrack = 1.0;
                    StatusText.Text = "End of track - no connected track ahead (Tab to switch)";
                }
            }
            else
            {
                _testVehicle.PositionOnTrack = newPos;
            }

            UpdateTestVehiclePosition();
            Redraw();
        }

        /// <summary>
        /// Move test vehicle backward along current track
        /// </summary>
        private void MoveTestVehicleBackward()
        {
            if (_testVehicle == null || _layout == null) return;

            double newPos = _testVehicle.PositionOnTrack - MOVE_STEP;

            if (newPos < 0.0)
            {
                // Try to switch to previous connected track
                if (TryMoveToPreviousTrack())
                {
                    StatusText.Text = $"Moved to previous track: {_testVehicle.CurrentTrackId}";
                }
                else
                {
                    _testVehicle.PositionOnTrack = 0.0;
                    StatusText.Text = "Start of track - no connected track behind (Tab to switch)";
                }
            }
            else
            {
                _testVehicle.PositionOnTrack = newPos;
            }

            UpdateTestVehiclePosition();
            Redraw();
        }

        /// <summary>
        /// Switch to next available track at current junction
        /// </summary>
        private void SwitchToNextTrack()
        {
            if (_testVehicle == null || _layout == null) return;

            var connectedTracks = GetConnectedTracks();
            if (connectedTracks.Count <= 1)
            {
                StatusText.Text = "No alternate tracks at this location";
                return;
            }

            // Find current track in list and switch to next
            int currentIndex = connectedTracks.FindIndex(t => t == _testVehicle.CurrentTrackId);
            int nextIndex = (currentIndex + 1) % connectedTracks.Count;

            _testVehicle.CurrentTrackId = connectedTracks[nextIndex];
            _testVehicle.PositionOnTrack = 0;

            UpdateTestVehiclePosition();
            StatusText.Text = $"Switched to track: {_testVehicle.CurrentTrackId}";
            Redraw();
        }

        /// <summary>
        /// Try to move to the next connected track when reaching end
        /// </summary>
        private bool TryMoveToNextTrack()
        {
            if (_testVehicle == null || _layout == null) return false;

            if (_testVehicle.TransporterType == TransporterTypes.Forklift)
            {
                return TryMoveToNextAisle(forward: true);
            }
            else
            {
                return TryMoveToNextPath(forward: true);
            }
        }

        /// <summary>
        /// Try to move to the previous connected track when reaching start
        /// </summary>
        private bool TryMoveToPreviousTrack()
        {
            if (_testVehicle == null || _layout == null) return false;

            if (_testVehicle.TransporterType == TransporterTypes.Forklift)
            {
                return TryMoveToNextAisle(forward: false);
            }
            else
            {
                return TryMoveToNextPath(forward: false);
            }
        }

        /// <summary>
        /// Try to move AGV to next connected path
        /// </summary>
        private bool TryMoveToNextPath(bool forward)
        {
            if (_testVehicle == null || _layout == null) return false;

            var currentPath = _layout.AGVPaths.FirstOrDefault(p => p.Id == _testVehicle.CurrentTrackId);
            if (currentPath == null) return false;

            // Get waypoint at the end/start we're moving toward
            string waypointId = forward ? currentPath.ToWaypointId : currentPath.FromWaypointId;

            // Find paths connected to this waypoint
            var connectedPaths = _layout.AGVPaths
                .Where(p => p.Id != currentPath.Id &&
                           (p.FromWaypointId == waypointId || p.ToWaypointId == waypointId))
                .ToList();

            if (connectedPaths.Count == 0) return false;

            var nextPath = connectedPaths.First();
            _testVehicle.CurrentTrackId = nextPath.Id;

            // Set position based on which end we entered from
            if (nextPath.FromWaypointId == waypointId)
            {
                _testVehicle.PositionOnTrack = 0;
            }
            else
            {
                _testVehicle.PositionOnTrack = 1.0;
            }

            return true;
        }

        /// <summary>
        /// Try to move forklift to next connected aisle
        /// </summary>
        private bool TryMoveToNextAisle(bool forward)
        {
            if (_testVehicle == null || _layout == null) return false;

            var currentAisle = _layout.ForkliftAisles.FirstOrDefault(a => a.Id == _testVehicle.CurrentTrackId);
            if (currentAisle == null || currentAisle.Centerline == null || currentAisle.Centerline.Count < 2)
                return false;

            // Get endpoint we're at
            var endpoint = forward
                ? currentAisle.Centerline.Last()
                : currentAisle.Centerline.First();

            // Find aisles that share this endpoint (within tolerance)
            const double TOLERANCE = 20;
            var connectedAisles = _layout.ForkliftAisles
                .Where(a => a.Id != currentAisle.Id && a.Centerline != null && a.Centerline.Count >= 2)
                .Where(a =>
                {
                    var start = a.Centerline!.First();
                    var end = a.Centerline!.Last();
                    double distStart = Math.Sqrt(Math.Pow(start.X - endpoint.X, 2) + Math.Pow(start.Y - endpoint.Y, 2));
                    double distEnd = Math.Sqrt(Math.Pow(end.X - endpoint.X, 2) + Math.Pow(end.Y - endpoint.Y, 2));
                    return distStart < TOLERANCE || distEnd < TOLERANCE;
                })
                .ToList();

            if (connectedAisles.Count == 0) return false;

            var nextAisle = connectedAisles.First();
            _testVehicle.CurrentTrackId = nextAisle.Id;

            // Set position based on which end is closest
            var startDist = Math.Sqrt(
                Math.Pow(nextAisle.Centerline!.First().X - endpoint.X, 2) +
                Math.Pow(nextAisle.Centerline!.First().Y - endpoint.Y, 2));
            var endDist = Math.Sqrt(
                Math.Pow(nextAisle.Centerline!.Last().X - endpoint.X, 2) +
                Math.Pow(nextAisle.Centerline!.Last().Y - endpoint.Y, 2));

            _testVehicle.PositionOnTrack = startDist < endDist ? 0 : 1.0;

            return true;
        }

        /// <summary>
        /// Get list of track IDs connected at current position
        /// </summary>
        private System.Collections.Generic.List<string> GetConnectedTracks()
        {
            var tracks = new System.Collections.Generic.List<string>();
            if (_testVehicle == null || _layout == null) return tracks;

            if (_testVehicle.TransporterType == TransporterTypes.Forklift)
            {
                // Get aisles near current position
                foreach (var aisle in _layout.ForkliftAisles)
                {
                    if (aisle.Centerline == null || aisle.Centerline.Count < 2) continue;

                    foreach (var pt in aisle.Centerline)
                    {
                        double dist = Math.Sqrt(
                            Math.Pow(pt.X - _testVehicle.CurrentX, 2) +
                            Math.Pow(pt.Y - _testVehicle.CurrentY, 2));
                        if (dist < 30)
                        {
                            tracks.Add(aisle.Id);
                            break;
                        }
                    }
                }
            }
            else
            {
                // Get paths connected at current waypoint
                foreach (var path in _layout.AGVPaths)
                {
                    var fromWp = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.FromWaypointId);
                    var toWp = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.ToWaypointId);

                    if (fromWp != null)
                    {
                        double dist = Math.Sqrt(
                            Math.Pow(fromWp.X - _testVehicle.CurrentX, 2) +
                            Math.Pow(fromWp.Y - _testVehicle.CurrentY, 2));
                        if (dist < 30)
                        {
                            tracks.Add(path.Id);
                            continue;
                        }
                    }

                    if (toWp != null)
                    {
                        double dist = Math.Sqrt(
                            Math.Pow(toWp.X - _testVehicle.CurrentX, 2) +
                            Math.Pow(toWp.Y - _testVehicle.CurrentY, 2));
                        if (dist < 30)
                        {
                            tracks.Add(path.Id);
                        }
                    }
                }
            }

            return tracks;
        }

        /// <summary>
        /// Update test vehicle X,Y position based on current track and position
        /// </summary>
        private void UpdateTestVehiclePosition()
        {
            if (_testVehicle == null || _layout == null) return;

            if (_testVehicle.TransporterType == TransporterTypes.Forklift)
            {
                UpdateForkliftPosition();
            }
            else
            {
                UpdateAGVPosition();
            }

            StatusText.Text = $"{_testVehicle.TransporterType} at ({_testVehicle.CurrentX:F0}, {_testVehicle.CurrentY:F0}) - {_testVehicle.PositionOnTrack:P0} along {_testVehicle.CurrentTrackId}";
        }

        /// <summary>
        /// Calculate AGV position from track and t parameter
        /// </summary>
        private void UpdateAGVPosition()
        {
            if (_testVehicle == null || _layout == null) return;

            var path = _layout.AGVPaths.FirstOrDefault(p => p.Id == _testVehicle.CurrentTrackId);
            if (path == null) return;

            var fromWp = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.FromWaypointId);
            var toWp = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.ToWaypointId);

            if (fromWp == null || toWp == null) return;

            double t = _testVehicle.PositionOnTrack;
            _testVehicle.CurrentX = fromWp.X + (toWp.X - fromWp.X) * t;
            _testVehicle.CurrentY = fromWp.Y + (toWp.Y - fromWp.Y) * t;

            // Calculate heading
            double dx = toWp.X - fromWp.X;
            double dy = toWp.Y - fromWp.Y;
            _testVehicle.Heading = Math.Atan2(dy, dx) * 180 / Math.PI;
        }

        /// <summary>
        /// Calculate forklift position from aisle centerline and t parameter
        /// </summary>
        private void UpdateForkliftPosition()
        {
            if (_testVehicle == null || _layout == null) return;

            var aisle = _layout.ForkliftAisles.FirstOrDefault(a => a.Id == _testVehicle.CurrentTrackId);
            if (aisle == null || aisle.Centerline == null || aisle.Centerline.Count < 2) return;

            // Calculate total length and find position
            double totalLength = 0;
            var segmentLengths = new System.Collections.Generic.List<double>();

            for (int i = 0; i < aisle.Centerline.Count - 1; i++)
            {
                double len = Math.Sqrt(
                    Math.Pow(aisle.Centerline[i + 1].X - aisle.Centerline[i].X, 2) +
                    Math.Pow(aisle.Centerline[i + 1].Y - aisle.Centerline[i].Y, 2));
                segmentLengths.Add(len);
                totalLength += len;
            }

            double targetDist = _testVehicle.PositionOnTrack * totalLength;
            double accumulatedDist = 0;

            for (int i = 0; i < segmentLengths.Count; i++)
            {
                if (accumulatedDist + segmentLengths[i] >= targetDist)
                {
                    // Position is on this segment
                    double segmentT = (targetDist - accumulatedDist) / segmentLengths[i];
                    var p1 = aisle.Centerline[i];
                    var p2 = aisle.Centerline[i + 1];

                    _testVehicle.CurrentX = p1.X + (p2.X - p1.X) * segmentT;
                    _testVehicle.CurrentY = p1.Y + (p2.Y - p1.Y) * segmentT;

                    // Calculate heading
                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    _testVehicle.Heading = Math.Atan2(dy, dx) * 180 / Math.PI;
                    return;
                }
                accumulatedDist += segmentLengths[i];
            }

            // At end
            var last = aisle.Centerline.Last();
            _testVehicle.CurrentX = last.X;
            _testVehicle.CurrentY = last.Y;
        }

        #endregion
    }
}
