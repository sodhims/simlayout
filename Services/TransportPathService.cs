using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for auto-connecting transport stations into loops
    /// Works with existing TransportNetworkData and TransportStationData
    /// </summary>
    public class TransportPathService
    {
        private readonly TransportNetworkData _network;

        public TransportPathService(TransportNetworkData network)
        {
            _network = network;
        }

        #region Loop Creation

        /// <summary>
        /// Create a loop connecting all stations in a group
        /// </summary>
        public List<TrackSegmentData> CreateLoopForGroup(string groupName)
        {
            var segments = new List<TrackSegmentData>();

            var stations = _network.Stations
                .Where(s => s.GroupName == groupName)
                .ToList();

            if (stations.Count == 0) return segments;

            if (stations.Count == 1)
            {
                segments.AddRange(CreateSingleStationLoop(stations[0]));
            }
            else
            {
                segments.AddRange(CreateMultiStationLoop(stations));
            }

            return segments;
        }

        /// <summary>
        /// Create dummy loop for single station: Station → WP1 → WP2 → Station
        /// </summary>
        private List<TrackSegmentData> CreateSingleStationLoop(TransportStationData station)
        {
            var segments = new List<TrackSegmentData>();
            var center = station.GetCenter();

            // Create two waypoints offset from station
            var wp1 = new WaypointData
            {
                Name = $"{station.Name}_WP1",
                NetworkId = _network.Id,
                X = center.X + 120,
                Y = center.Y - 80
            };

            var wp2 = new WaypointData
            {
                Name = $"{station.Name}_WP2",
                NetworkId = _network.Id,
                X = center.X + 120,
                Y = center.Y + 80
            };

            _network.Waypoints.Add(wp1);
            _network.Waypoints.Add(wp2);

            // Create loop segments
            segments.Add(CreateSegment(station.Id, wp1.Id));
            segments.Add(CreateSegment(wp1.Id, wp2.Id));
            segments.Add(CreateSegment(wp2.Id, station.Id));

            foreach (var seg in segments)
                _network.Segments.Add(seg);

            return segments;
        }

        /// <summary>
        /// Create loop connecting multiple stations, sorted by angle
        /// </summary>
        private List<TrackSegmentData> CreateMultiStationLoop(List<TransportStationData> stations)
        {
            var segments = new List<TrackSegmentData>();
            var sorted = SortByAngle(stations);

            for (int i = 0; i < sorted.Count; i++)
            {
                var from = sorted[i];
                var to = sorted[(i + 1) % sorted.Count];

                var segment = CreateSegment(from.Id, to.Id);
                segments.Add(segment);
                _network.Segments.Add(segment);
            }

            return segments;
        }

        /// <summary>
        /// Sort stations by angle from centroid for clean loop shape
        /// </summary>
        private List<TransportStationData> SortByAngle(List<TransportStationData> stations)
        {
            if (stations.Count <= 2) return stations.ToList();

            var centers = stations.Select(s => s.GetCenter()).ToList();
            var centroidX = centers.Average(c => c.X);
            var centroidY = centers.Average(c => c.Y);

            return stations
                .OrderBy(s =>
                {
                    var center = s.GetCenter();
                    return Math.Atan2(center.Y - centroidY, center.X - centroidX);
                })
                .ToList();
        }

        #endregion

        #region Path Operations

        /// <summary>
        /// Add a blind path (spur) from a station or waypoint
        /// </summary>
        public (WaypointData waypoint, TrackSegmentData segment) AddBlindPath(
            string fromId, double offsetX = 100, double offsetY = 0)
        {
            var (x, y, name) = GetPointInfo(fromId);

            var waypoint = new WaypointData
            {
                Name = $"{name}_Spur",
                NetworkId = _network.Id,
                X = x + offsetX,
                Y = y + offsetY
            };
            _network.Waypoints.Add(waypoint);

            var segment = CreateSegment(fromId, waypoint.Id);
            _network.Segments.Add(segment);

            return (waypoint, segment);
        }

        /// <summary>
        /// Connect a station to the nearest existing point
        /// </summary>
        public TrackSegmentData ConnectToNearest(string stationId)
        {
            var station = _network.Stations.FirstOrDefault(s => s.Id == stationId);
            if (station == null)
                throw new ArgumentException("Station not found");

            var center = station.GetCenter();
            var nearest = FindNearestPoint(center.X, center.Y, stationId);
            if (nearest == null)
                throw new InvalidOperationException("No points to connect to");

            var segment = CreateSegment(stationId, nearest.Value.id);
            _network.Segments.Add(segment);
            return segment;
        }

        /// <summary>
        /// Recreate loop for a group (remove old, create new)
        /// </summary>
        public void RecreateLoop(string groupName)
        {
            var stationIds = _network.Stations
                .Where(s => s.GroupName == groupName)
                .Select(s => s.Id)
                .ToHashSet();

            // Remove existing segments connected to these stations
            var toRemove = _network.Segments
                .Where(seg => stationIds.Contains(seg.From) || stationIds.Contains(seg.To))
                .ToList();

            foreach (var seg in toRemove)
                _network.Segments.Remove(seg);

            // Recreate
            CreateLoopForGroup(groupName);
        }

        /// <summary>
        /// Insert waypoint in middle of a segment
        /// </summary>
        public WaypointData InsertWaypoint(string segmentId)
        {
            var segment = _network.Segments.FirstOrDefault(s => s.Id == segmentId);
            if (segment == null)
                throw new ArgumentException("Segment not found");

            var (fromX, fromY, _) = GetPointInfo(segment.From);
            var (toX, toY, _) = GetPointInfo(segment.To);

            var waypoint = new WaypointData
            {
                Name = $"WP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                NetworkId = _network.Id,
                X = (fromX + toX) / 2,
                Y = (fromY + toY) / 2
            };
            _network.Waypoints.Add(waypoint);

            // Remove old segment, add two new
            _network.Segments.Remove(segment);
            _network.Segments.Add(CreateSegment(segment.From, waypoint.Id));
            _network.Segments.Add(CreateSegment(waypoint.Id, segment.To));

            return waypoint;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Find nearest point to coordinates
        /// </summary>
        public (string id, double x, double y)? FindNearestPoint(double x, double y, string? excludeId = null)
        {
            var points = new List<(string id, double x, double y)>();

            foreach (var s in _network.Stations.Where(s => s.Id != excludeId))
            {
                var center = s.GetCenter();
                points.Add((s.Id, center.X, center.Y));
            }

            foreach (var w in _network.Waypoints.Where(w => w.Id != excludeId))
                points.Add((w.Id, w.X, w.Y));

            if (points.Count == 0) return null;

            return points
                .OrderBy(p => Math.Sqrt(Math.Pow(p.x - x, 2) + Math.Pow(p.y - y, 2)))
                .First();
        }

        private (double x, double y, string name) GetPointInfo(string id)
        {
            var station = _network.Stations.FirstOrDefault(s => s.Id == id);
            if (station != null)
            {
                var center = station.GetCenter();
                return (center.X, center.Y, station.Name);
            }

            var waypoint = _network.Waypoints.FirstOrDefault(w => w.Id == id);
            if (waypoint != null)
                return (waypoint.X, waypoint.Y, waypoint.Name);

            throw new ArgumentException($"Point {id} not found");
        }

        private TrackSegmentData CreateSegment(string fromId, string toId)
        {
            var (fromX, fromY, _) = GetPointInfo(fromId);
            var (toX, toY, _) = GetPointInfo(toId);
            var distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));

            return new TrackSegmentData
            {
                NetworkId = _network.Id,
                From = fromId,
                To = toId,
                Bidirectional = true,
                SpeedLimit = 2.0,
                Distance = distance
            };
        }

        /// <summary>
        /// Get all unique group names in the network
        /// </summary>
        public List<string> GetGroupNames()
        {
            return _network.Stations
                .Where(s => !string.IsNullOrEmpty(s.GroupName))
                .Select(s => s.GroupName)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Get stations in a group
        /// </summary>
        public List<TransportStationData> GetStationsInGroup(string groupName)
        {
            return _network.Stations
                .Where(s => s.GroupName == groupName)
                .ToList();
        }

        #endregion
    }
}
