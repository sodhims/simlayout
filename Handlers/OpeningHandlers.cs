using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handles opening placement, selection, and editing
    /// </summary>
    public partial class MainWindow
    {
        // Opening placement state
        private bool _isPlacingOpening = false;
        private string _placementOpeningType = "Door"; // Door, Hatch, Gate, Manhole, Aisle

        // Opening selection state
        private string? _selectedOpeningId;

        #region Opening Placement

        /// <summary>
        /// Start placing an opening of the specified type
        /// </summary>
        public void StartPlacingOpening(string openingType)
        {
            _isPlacingOpening = true;
            _placementOpeningType = openingType;
            StatusText.Text = $"Click on a wall to place {openingType}, or click between zones for freestanding opening. Press Esc to cancel.";
            Mouse.OverrideCursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancel opening placement mode
        /// </summary>
        public void CancelPlacingOpening()
        {
            _isPlacingOpening = false;
            Mouse.OverrideCursor = null;
            StatusText.Text = "Opening placement canceled";
        }

        /// <summary>
        /// Handle click to place opening
        /// </summary>
        private void HandleOpeningPlacementClick(Point canvasPoint)
        {
            if (!_isPlacingOpening)
                return;

            SaveUndoState();

            // Check if clicked on a wall
            var wall = FindWallNearPoint(canvasPoint, 10.0);

            OpeningData opening;

            if (wall != null)
            {
                // Place opening on wall
                opening = CreateOpeningOnWall(wall, canvasPoint);
            }
            else
            {
                // Place freestanding opening
                opening = CreateFreestandingOpening(canvasPoint);
            }

            // Try to auto-link zones
            AutoLinkOpeningToZones(opening, canvasPoint);

            _layout.Openings.Add(opening);

            MarkDirty();
            Redraw();

            StatusText.Text = $"{_placementOpeningType} placed. Click for another or press Esc to finish.";
        }

        private OpeningData CreateOpeningOnWall(WallData wall, Point clickPoint)
        {
            // Calculate position along wall
            var wallStart = new Point(wall.X1, wall.Y1);
            var wallEnd = new Point(wall.X2, wall.Y2);

            // Project click point onto wall line
            var wallVector = new Point(wallEnd.X - wallStart.X, wallEnd.Y - wallStart.Y);
            var wallLength = Math.Sqrt(wallVector.X * wallVector.X + wallVector.Y * wallVector.Y);
            var clickVector = new Point(clickPoint.X - wallStart.X, clickPoint.Y - wallStart.Y);
            var projection = (clickVector.X * wallVector.X + clickVector.Y * wallVector.Y) / (wallLength * wallLength);
            projection = Math.Clamp(projection, 0, 1);

            var openingPosition = new Point(
                wallStart.X + wallVector.X * projection,
                wallStart.Y + wallVector.Y * projection);

            // Calculate rotation from wall angle
            var angle = Math.Atan2(wallVector.Y, wallVector.X) * 180 / Math.PI;

            return CreateOpeningInstance(_placementOpeningType, openingPosition.X, openingPosition.Y, angle);
        }

        private OpeningData CreateFreestandingOpening(Point position)
        {
            return CreateOpeningInstance(_placementOpeningType, position.X, position.Y, 0);
        }

        private OpeningData CreateOpeningInstance(string type, double x, double y, double rotation)
        {
            return type.ToLower() switch
            {
                "door" => new DoorOpening
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Door",
                    X = x,
                    Y = y,
                    Rotation = rotation,
                    Capacity = 1
                },
                "hatch" => new HatchOpening
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Hatch",
                    X = x,
                    Y = y,
                    Rotation = rotation,
                    Capacity = 1
                },
                "gate" => new GateOpening
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Gate",
                    X = x,
                    Y = y,
                    Rotation = rotation,
                    Capacity = 2
                },
                "manhole" => new ManholeOpening
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Manhole",
                    X = x,
                    Y = y,
                    Rotation = rotation,
                    Capacity = 1
                },
                "aisle" => new UnconstrainedOpening
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Aisle",
                    X = x,
                    Y = y,
                    Rotation = rotation,
                    Capacity = 0
                },
                _ => new DoorOpening
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Door",
                    X = x,
                    Y = y,
                    Rotation = rotation,
                    Capacity = 1
                }
            };
        }

        private void AutoLinkOpeningToZones(OpeningData opening, Point position)
        {
            // Find zones near this opening
            var nearbyZones = _layout.Zones
                .Where(z => IsPointNearZone(position, z, 50))
                .OrderBy(z => DistanceToZoneEdge(position, z))
                .Take(2)
                .ToList();

            if (nearbyZones.Count >= 1)
                opening.FromZoneId = nearbyZones[0].Id;

            if (nearbyZones.Count >= 2)
                opening.ToZoneId = nearbyZones[1].Id;
        }

        private bool IsPointNearZone(Point point, ZoneData zone, double maxDistance)
        {
            var rect = new Rect(zone.X, zone.Y, zone.Width, zone.Height);

            // Check if point is inside zone
            if (rect.Contains(point))
                return true;

            // Check if point is near zone edge
            return DistanceToZoneEdge(point, zone) < maxDistance;
        }

        private double DistanceToZoneEdge(Point point, ZoneData zone)
        {
            var rect = new Rect(zone.X, zone.Y, zone.Width, zone.Height);

            if (rect.Contains(point))
                return 0;

            // Calculate distance to nearest edge
            double dx = Math.Max(rect.Left - point.X, Math.Max(0, point.X - rect.Right));
            double dy = Math.Max(rect.Top - point.Y, Math.Max(0, point.Y - rect.Bottom));

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private WallData? FindWallNearPoint(Point point, double tolerance)
        {
            foreach (var wall in _layout.Walls)
            {
                var p1 = new Point(wall.X1, wall.Y1);
                var p2 = new Point(wall.X2, wall.Y2);

                var distance = DistanceToLineSegment(point, p1, p2);
                if (distance < tolerance)
                    return wall;
            }
            return null;
        }

        private double DistanceToLineSegment(Point p, Point a, Point b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));

            var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            var projX = a.X + t * dx;
            var projY = a.Y + t * dy;

            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        #endregion

        #region Opening Selection

        /// <summary>
        /// Select an opening by ID
        /// </summary>
        public void SelectOpening(string openingId)
        {
            _selectedOpeningId = openingId;
            _selectionService.ClearSelection();
            ClearWallSelection();

            var opening = _layout.Openings.FirstOrDefault(o => o.Id == openingId);
            if (opening != null)
            {
                StatusText.Text = $"Opening selected: {opening.Name} ({opening.GetType().Name}) - Del to delete";
                // Future: Show opening properties in panel
            }

            Redraw();
        }

        /// <summary>
        /// Clear opening selection
        /// </summary>
        public void ClearOpeningSelection()
        {
            _selectedOpeningId = null;
        }

        /// <summary>
        /// Delete selected opening
        /// </summary>
        public void DeleteSelectedOpening()
        {
            if (_selectedOpeningId == null)
                return;

            var opening = _layout.Openings.FirstOrDefault(o => o.Id == _selectedOpeningId);
            if (opening != null)
            {
                SaveUndoState();
                _layout.Openings.Remove(opening);
                _selectedOpeningId = null;
                MarkDirty();
                Redraw();
                StatusText.Text = "Opening deleted";
            }
        }

        #endregion

        #region Menu Handlers

        /// <summary>
        /// Menu item click to start placing a door
        /// </summary>
        private void PlaceDoor_Click(object sender, RoutedEventArgs e)
        {
            StartPlacingOpening("Door");
        }

        /// <summary>
        /// Menu item click to start placing a hatch
        /// </summary>
        private void PlaceHatch_Click(object sender, RoutedEventArgs e)
        {
            StartPlacingOpening("Hatch");
        }

        /// <summary>
        /// Menu item click to start placing a gate
        /// </summary>
        private void PlaceGate_Click(object sender, RoutedEventArgs e)
        {
            StartPlacingOpening("Gate");
        }

        /// <summary>
        /// Menu item click to start placing a manhole
        /// </summary>
        private void PlaceManhole_Click(object sender, RoutedEventArgs e)
        {
            StartPlacingOpening("Manhole");
        }

        /// <summary>
        /// Menu item click to start placing an aisle
        /// </summary>
        private void PlaceAisle_Click(object sender, RoutedEventArgs e)
        {
            StartPlacingOpening("Aisle");
        }

        #endregion
    }
}
