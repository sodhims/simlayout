using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for managing transport markers, paths, and links
    /// </summary>
    public class TransportMarkerService
    {
        private readonly ObservableCollection<TransportMarker> _markers;
        private readonly ObservableCollection<TrackSegmentData> _segments;
        private readonly ObservableCollection<WaypointData> _waypoints;
        private readonly ObservableCollection<TransportLink> _links;
        private readonly ObservableCollection<TransportGroup> _groups;

        private TransportMarker? _lastPlacedMarker;
        private int _markerCounter = 0;

        public bool AutoConnectMode { get; set; }

        public TransportMarkerService(
            ObservableCollection<TransportMarker> markers,
            ObservableCollection<TrackSegmentData> segments,
            ObservableCollection<WaypointData> waypoints,
            ObservableCollection<TransportLink> links,
            ObservableCollection<TransportGroup> groups)
        {
            _markers = markers;
            _segments = segments;
            _waypoints = waypoints;
            _links = links;
            _groups = groups;
        }

        #region Marker Operations

        /// <summary>
        /// Add a new marker at position
        /// </summary>
        public TransportMarker AddMarker(double x, double y, string? markerType = null)
        {
            _markerCounter++;
            var marker = new TransportMarker
            {
                Name = $"MK{_markerCounter:D2}",
                X = x,
                Y = y,
                MarkerType = markerType ?? MarkerTypes.Junction
            };

            _markers.Add(marker);

            // Auto-connect to last marker if in auto-connect mode
            if (AutoConnectMode && _lastPlacedMarker != null)
            {
                ConnectMarkers(_lastPlacedMarker.Id, marker.Id);
            }

            _lastPlacedMarker = marker;
            return marker;
        }

        /// <summary>
        /// Remove a marker and its connections
        /// </summary>
        public void RemoveMarker(string markerId)
        {
            var marker = _markers.FirstOrDefault(m => m.Id == markerId);
            if (marker == null) return;

            // Remove connected segments
            var connectedSegments = _segments
                .Where(s => s.From == markerId || s.To == markerId)
                .ToList();

            foreach (var seg in connectedSegments)
                _segments.Remove(seg);

            // Remove links to this marker
            var connectedLinks = _links
                .Where(l => l.ToMarkerId == markerId)
                .ToList();

            foreach (var link in connectedLinks)
                _links.Remove(link);

            // Remove from groups
            foreach (var group in _groups)
                group.PathMarkerIds.Remove(markerId);

            _markers.Remove(marker);

            if (_lastPlacedMarker?.Id == markerId)
                _lastPlacedMarker = null;
        }

        /// <summary>
        /// Move a marker to new position
        /// </summary>
        public void MoveMarker(string markerId, double x, double y)
        {
            var marker = _markers.FirstOrDefault(m => m.Id == markerId);
            if (marker != null)
            {
                marker.X = x;
                marker.Y = y;
            }
        }

        #endregion

        #region Path Operations

        /// <summary>
        /// Connect two markers with a segment
        /// </summary>
        public TrackSegmentData ConnectMarkers(string fromId, string toId, bool bidirectional = true)
        {
            // Check if already connected
            var existing = _segments.FirstOrDefault(s =>
                (s.From == fromId && s.To == toId) ||
                (s.From == toId && s.To == fromId));

            if (existing != null)
                return existing;

            var fromMarker = _markers.FirstOrDefault(m => m.Id == fromId);
            var toMarker = _markers.FirstOrDefault(m => m.Id == toId);

            var distance = 0.0;
            if (fromMarker != null && toMarker != null)
            {
                distance = Math.Sqrt(
                    Math.Pow(toMarker.X - fromMarker.X, 2) +
                    Math.Pow(toMarker.Y - fromMarker.Y, 2));
            }

            var segment = new TrackSegmentData
            {
                From = fromId,
                To = toId,
                Bidirectional = bidirectional,
                Distance = distance
            };

            _segments.Add(segment);
            return segment;
        }

        /// <summary>
        /// Remove segment between markers
        /// </summary>
        public void DisconnectMarkers(string fromId, string toId)
        {
            var segment = _segments.FirstOrDefault(s =>
                (s.From == fromId && s.To == toId) ||
                (s.From == toId && s.To == fromId));

            if (segment != null)
                _segments.Remove(segment);
        }

        /// <summary>
        /// Auto-connect all markers in sequence (by proximity)
        /// </summary>
        public List<TrackSegmentData> AutoConnectMarkers()
        {
            var segments = new List<TrackSegmentData>();
            if (_markers.Count < 2) return segments;

            // Sort by X then Y for left-to-right, top-to-bottom order
            var sorted = _markers
                .OrderBy(m => m.X)
                .ThenBy(m => m.Y)
                .ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var seg = ConnectMarkers(sorted[i].Id, sorted[i + 1].Id);
                segments.Add(seg);
            }

            return segments;
        }

        /// <summary>
        /// Create loop connecting markers by nearest neighbor
        /// </summary>
        public List<TrackSegmentData> CreateLoopByNearest()
        {
            var segments = new List<TrackSegmentData>();
            if (_markers.Count < 2) return segments;

            var remaining = new List<TransportMarker>(_markers);
            var current = remaining[0];
            remaining.RemoveAt(0);
            var first = current;

            while (remaining.Count > 0)
            {
                var nearest = remaining
                    .OrderBy(m => Math.Sqrt(
                        Math.Pow(m.X - current.X, 2) +
                        Math.Pow(m.Y - current.Y, 2)))
                    .First();

                var seg = ConnectMarkers(current.Id, nearest.Id);
                segments.Add(seg);

                remaining.Remove(nearest);
                current = nearest;
            }

            // Close the loop
            if (_markers.Count > 2)
            {
                var seg = ConnectMarkers(current.Id, first.Id);
                segments.Add(seg);
            }

            return segments;
        }

        /// <summary>
        /// Insert waypoint on a segment
        /// </summary>
        public WaypointData InsertWaypointOnSegment(string segmentId, double x, double y)
        {
            var segment = _segments.FirstOrDefault(s => s.Id == segmentId);
            if (segment == null)
                throw new ArgumentException("Segment not found");

            var waypoint = new WaypointData
            {
                Name = $"WP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                X = x,
                Y = y,
                NetworkId = segment.NetworkId
            };

            _waypoints.Add(waypoint);

            // Split segment
            var newSeg1 = new TrackSegmentData
            {
                From = segment.From,
                To = waypoint.Id,
                Bidirectional = segment.Bidirectional,
                NetworkId = segment.NetworkId
            };

            var newSeg2 = new TrackSegmentData
            {
                From = waypoint.Id,
                To = segment.To,
                Bidirectional = segment.Bidirectional,
                NetworkId = segment.NetworkId
            };

            _segments.Remove(segment);
            _segments.Add(newSeg1);
            _segments.Add(newSeg2);

            return waypoint;
        }

        #endregion

        #region Link Operations

        /// <summary>
        /// Link a node terminal to the nearest point on transport network
        /// </summary>
        public TransportLink LinkToNearest(string nodeId, string terminal, Point terminalPosition)
        {
            // Find nearest marker or segment point
            var nearestMarker = FindNearestMarker(terminalPosition.X, terminalPosition.Y);
            var (nearestSegment, nearestPoint) = FindNearestSegmentPoint(terminalPosition.X, terminalPosition.Y);

            var distToMarker = nearestMarker != null
                ? Math.Sqrt(Math.Pow(nearestMarker.X - terminalPosition.X, 2) +
                           Math.Pow(nearestMarker.Y - terminalPosition.Y, 2))
                : double.MaxValue;

            var distToSegment = nearestSegment != null
                ? Math.Sqrt(Math.Pow(nearestPoint.X - terminalPosition.X, 2) +
                           Math.Pow(nearestPoint.Y - terminalPosition.Y, 2))
                : double.MaxValue;

            var link = new TransportLink
            {
                FromNodeId = nodeId,
                FromTerminal = terminal,
                LinkType = terminal == "output" ? LinkTypes.Pickup : LinkTypes.Dropoff
            };

            if (distToMarker <= distToSegment && nearestMarker != null)
            {
                // Link to marker
                link.ToMarkerId = nearestMarker.Id;
            }
            else if (nearestSegment != null)
            {
                // Link to segment - create waypoint at connection point
                var waypoint = InsertWaypointOnSegment(nearestSegment.Id, nearestPoint.X, nearestPoint.Y);
                link.ToMarkerId = waypoint.Id;
                link.ConnectionPointX = nearestPoint.X;
                link.ConnectionPointY = nearestPoint.Y;
            }
            else
            {
                throw new InvalidOperationException("No transport network to link to");
            }

            _links.Add(link);
            return link;
        }

        /// <summary>
        /// Link node to specific marker
        /// </summary>
        public TransportLink LinkToMarker(string nodeId, string terminal, string markerId, string linkType = LinkTypes.Both)
        {
            var link = new TransportLink
            {
                FromNodeId = nodeId,
                FromTerminal = terminal,
                ToMarkerId = markerId,
                LinkType = linkType
            };

            _links.Add(link);
            return link;
        }

        /// <summary>
        /// Remove link
        /// </summary>
        public void RemoveLink(string linkId)
        {
            var link = _links.FirstOrDefault(l => l.Id == linkId);
            if (link != null)
                _links.Remove(link);
        }

        /// <summary>
        /// Get links for a node
        /// </summary>
        public IEnumerable<TransportLink> GetLinksForNode(string nodeId)
        {
            return _links.Where(l => l.FromNodeId == nodeId);
        }

        #endregion

        #region Group Path Assignment

        /// <summary>
        /// Assign a path (sequence of markers) to a group
        /// </summary>
        public void AssignPathToGroup(string groupId, IEnumerable<string> markerIds)
        {
            var group = _groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null) return;

            group.PathMarkerIds.Clear();
            foreach (var id in markerIds)
                group.PathMarkerIds.Add(id);

            // Update markers' passing groups
            UpdateMarkerPassingGroups();
        }

        /// <summary>
        /// Add marker to group's path
        /// </summary>
        public void AddMarkerToGroupPath(string groupId, string markerId)
        {
            var group = _groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null) return;

            if (!group.PathMarkerIds.Contains(markerId))
                group.PathMarkerIds.Add(markerId);

            UpdateMarkerPassingGroups();
        }

        /// <summary>
        /// Update which groups pass through each marker
        /// </summary>
        public void UpdateMarkerPassingGroups()
        {
            foreach (var marker in _markers)
            {
                marker.PassingGroupIds.Clear();
                foreach (var group in _groups)
                {
                    if (group.PathMarkerIds.Contains(marker.Id))
                        marker.PassingGroupIds.Add(group.Id);
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Find nearest marker to position
        /// </summary>
        public TransportMarker? FindNearestMarker(double x, double y, string? excludeId = null)
        {
            return _markers
                .Where(m => m.Id != excludeId)
                .OrderBy(m => Math.Sqrt(Math.Pow(m.X - x, 2) + Math.Pow(m.Y - y, 2)))
                .FirstOrDefault();
        }

        /// <summary>
        /// Find nearest point on any segment
        /// </summary>
        public (TrackSegmentData? segment, Point point) FindNearestSegmentPoint(double x, double y)
        {
            TrackSegmentData? nearestSegment = null;
            var nearestPoint = new Point();
            var minDist = double.MaxValue;

            foreach (var segment in _segments)
            {
                var fromMarker = _markers.FirstOrDefault(m => m.Id == segment.From);
                var toMarker = _markers.FirstOrDefault(m => m.Id == segment.To);

                // Also check waypoints
                var fromWp = _waypoints.FirstOrDefault(w => w.Id == segment.From);
                var toWp = _waypoints.FirstOrDefault(w => w.Id == segment.To);

                var fromX = fromMarker?.X ?? fromWp?.X ?? 0;
                var fromY = fromMarker?.Y ?? fromWp?.Y ?? 0;
                var toX = toMarker?.X ?? toWp?.X ?? 0;
                var toY = toMarker?.Y ?? toWp?.Y ?? 0;

                var nearest = GetNearestPointOnLine(x, y, fromX, fromY, toX, toY);
                var dist = Math.Sqrt(Math.Pow(nearest.X - x, 2) + Math.Pow(nearest.Y - y, 2));

                if (dist < minDist)
                {
                    minDist = dist;
                    nearestSegment = segment;
                    nearestPoint = nearest;
                }
            }

            return (nearestSegment, nearestPoint);
        }

        private Point GetNearestPointOnLine(double px, double py, double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return new Point(x1, y1);

            var t = Math.Max(0, Math.Min(1,
                ((px - x1) * dx + (py - y1) * dy) / lengthSq));

            return new Point(x1 + t * dx, y1 + t * dy);
        }

        /// <summary>
        /// Get point position (marker or waypoint)
        /// </summary>
        public Point? GetPointPosition(string pointId)
        {
            var marker = _markers.FirstOrDefault(m => m.Id == pointId);
            if (marker != null)
                return new Point(marker.X, marker.Y);

            var waypoint = _waypoints.FirstOrDefault(w => w.Id == pointId);
            if (waypoint != null)
                return new Point(waypoint.X, waypoint.Y);

            return null;
        }

        /// <summary>
        /// Clear last placed marker (reset auto-connect chain)
        /// </summary>
        public void ResetAutoConnect()
        {
            _lastPlacedMarker = null;
        }

        #endregion
    }
}
