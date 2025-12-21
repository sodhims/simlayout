using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Transport;
using LayoutEditor.Transport.AGV;

namespace LayoutEditor.Transport.AGV
{
    /// <summary>
    /// Service for AGV path creation and routing
    /// </summary>
    public class AgvPathService
    {
        private readonly AgvNetwork _network;

        public AgvPathService(AgvNetwork network)
        {
            _network = network;
        }

        #region Loop Creation

        /// <summary>
        /// Create a loop connecting all stations in a group
        /// </summary>
        public List<AgvTrack> CreateLoopForGroup(string groupName)
        {
            var tracks = new List<AgvTrack>();
            
            var stations = _network.Stations
                .Where(s => s.GroupName == groupName)
                .ToList();

            if (stations.Count == 0) return tracks;

            if (stations.Count == 1)
            {
                tracks.AddRange(CreateSingleStationLoop(stations[0]));
            }
            else
            {
                tracks.AddRange(CreateMultiStationLoop(stations));
            }

            return tracks;
        }

        /// <summary>
        /// Create dummy loop for single station
        /// </summary>
        private List<AgvTrack> CreateSingleStationLoop(AgvStation station)
        {
            var tracks = new List<AgvTrack>();
            
            // Create two waypoints offset from station
            var wp1 = new AgvWaypoint
            {
                Name = $"{station.Name}_WP1",
                NetworkId = _network.Id,
                X = station.X + 120,
                Y = station.Y - 80
            };
            
            var wp2 = new AgvWaypoint
            {
                Name = $"{station.Name}_WP2",
                NetworkId = _network.Id,
                X = station.X + 120,
                Y = station.Y + station.Height + 80
            };
            
            _network.Waypoints.Add(wp1);
            _network.Waypoints.Add(wp2);
            
            // Create loop
            tracks.Add(CreateTrack(station.Id, wp1.Id));
            tracks.Add(CreateTrack(wp1.Id, wp2.Id));
            tracks.Add(CreateTrack(wp2.Id, station.Id));
            
            foreach (var track in tracks)
                _network.Tracks.Add(track);
            
            return tracks;
        }

        /// <summary>
        /// Create loop connecting multiple stations
        /// </summary>
        private List<AgvTrack> CreateMultiStationLoop(List<AgvStation> stations)
        {
            var tracks = new List<AgvTrack>();
            var sorted = SortByAngle(stations);
            
            for (int i = 0; i < sorted.Count; i++)
            {
                var from = sorted[i];
                var to = sorted[(i + 1) % sorted.Count];
                
                var track = CreateTrack(from.Id, to.Id);
                tracks.Add(track);
                _network.Tracks.Add(track);
            }
            
            return tracks;
        }

        /// <summary>
        /// Sort stations by angle from centroid
        /// </summary>
        private List<AgvStation> SortByAngle(List<AgvStation> stations)
        {
            if (stations.Count <= 2) return stations.ToList();

            var centerX = stations.Average(s => s.X + s.Width / 2);
            var centerY = stations.Average(s => s.Y + s.Height / 2);

            return stations
                .OrderBy(s => Math.Atan2(
                    s.Y + s.Height / 2 - centerY, 
                    s.X + s.Width / 2 - centerX))
                .ToList();
        }

        #endregion

        #region Path Operations

        /// <summary>
        /// Add blind/spur path from a point
        /// </summary>
        public (AgvWaypoint waypoint, AgvTrack track) AddBlindPath(string fromId, double offsetX = 100, double offsetY = 0)
        {
            var (x, y, name) = GetPointInfo(fromId);
            
            var waypoint = new AgvWaypoint
            {
                Name = $"{name}_Spur",
                NetworkId = _network.Id,
                X = x + offsetX,
                Y = y + offsetY
            };
            _network.Waypoints.Add(waypoint);

            var track = CreateTrack(fromId, waypoint.Id);
            _network.Tracks.Add(track);

            return (waypoint, track);
        }

        /// <summary>
        /// Connect station to nearest existing point
        /// </summary>
        public AgvTrack ConnectToNearest(string stationId)
        {
            var station = _network.Stations.FirstOrDefault(s => s.Id == stationId);
            if (station == null)
                throw new ArgumentException("Station not found");

            var nearest = FindNearestPoint(station.X + station.Width / 2, station.Y + station.Height / 2, stationId);
            if (nearest == null)
                throw new InvalidOperationException("No points to connect to");

            var track = CreateTrack(stationId, nearest.Value.id);
            _network.Tracks.Add(track);
            return track;
        }

        /// <summary>
        /// Recreate loop for a group
        /// </summary>
        public void RecreateLoop(string groupName)
        {
            var stationIds = _network.Stations
                .Where(s => s.GroupName == groupName)
                .Select(s => s.Id)
                .ToHashSet();

            // Remove existing tracks
            var toRemove = _network.Tracks
                .Where(t => stationIds.Contains(t.From) || stationIds.Contains(t.To))
                .ToList();

            foreach (var track in toRemove)
                _network.Tracks.Remove(track);

            // Recreate
            CreateLoopForGroup(groupName);
        }

        /// <summary>
        /// Insert waypoint in middle of track
        /// </summary>
        public AgvWaypoint InsertWaypoint(string trackId)
        {
            var track = _network.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track == null)
                throw new ArgumentException("Track not found");

            var (fromX, fromY, _) = GetPointInfo(track.From);
            var (toX, toY, _) = GetPointInfo(track.To);

            var waypoint = new AgvWaypoint
            {
                Name = $"WP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                NetworkId = _network.Id,
                X = (fromX + toX) / 2,
                Y = (fromY + toY) / 2
            };
            _network.Waypoints.Add(waypoint);

            // Remove old track, add two new
            _network.Tracks.Remove(track);
            _network.Tracks.Add(CreateTrack(track.From, waypoint.Id));
            _network.Tracks.Add(CreateTrack(waypoint.Id, track.To));

            return waypoint;
        }

        #endregion

        #region Routing

        /// <summary>
        /// Find shortest path using Dijkstra
        /// </summary>
        public List<string>? FindShortestPath(string fromId, string toId)
        {
            var graph = BuildGraph();
            
            var distances = new Dictionary<string, double>();
            var previous = new Dictionary<string, string?>();
            var unvisited = new HashSet<string>(graph.Keys);

            foreach (var node in graph.Keys)
            {
                distances[node] = double.MaxValue;
                previous[node] = null;
            }
            distances[fromId] = 0;

            while (unvisited.Count > 0)
            {
                var current = unvisited.OrderBy(n => distances[n]).First();
                
                if (current == toId)
                    break;

                unvisited.Remove(current);

                if (!graph.ContainsKey(current)) continue;

                foreach (var (neighbor, weight) in graph[current])
                {
                    if (!unvisited.Contains(neighbor)) continue;
                    
                    var alt = distances[current] + weight;
                    if (alt < distances[neighbor])
                    {
                        distances[neighbor] = alt;
                        previous[neighbor] = current;
                    }
                }
            }

            // Build path
            if (previous[toId] == null && fromId != toId)
                return null;

            var path = new List<string>();
            var currentNode = toId;
            while (currentNode != null)
            {
                path.Insert(0, currentNode);
                previous.TryGetValue(currentNode, out currentNode!);
            }

            return path;
        }

        /// <summary>
        /// Build adjacency graph from tracks
        /// </summary>
        private Dictionary<string, List<(string, double)>> BuildGraph()
        {
            var graph = new Dictionary<string, List<(string, double)>>();

            foreach (var track in _network.Tracks)
            {
                var weight = track.Distance > 0 ? track.Distance : CalculateDistance(track.From, track.To);

                if (!graph.ContainsKey(track.From))
                    graph[track.From] = new List<(string, double)>();
                graph[track.From].Add((track.To, weight));

                if (track.Bidirectional)
                {
                    if (!graph.ContainsKey(track.To))
                        graph[track.To] = new List<(string, double)>();
                    graph[track.To].Add((track.From, weight));
                }
            }

            return graph;
        }

        private double CalculateDistance(string fromId, string toId)
        {
            var (x1, y1, _) = GetPointInfo(fromId);
            var (x2, y2, _) = GetPointInfo(toId);
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        #endregion

        #region Helpers

        public (string id, double x, double y)? FindNearestPoint(double x, double y, string? excludeId = null)
        {
            var points = new List<(string id, double x, double y)>();
            
            foreach (var s in _network.Stations.Where(s => s.Id != excludeId))
                points.Add((s.Id, s.X + s.Width / 2, s.Y + s.Height / 2));
            
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
                return (station.X + station.Width / 2, station.Y + station.Height / 2, station.Name);

            var waypoint = _network.Waypoints.FirstOrDefault(w => w.Id == id);
            if (waypoint != null)
                return (waypoint.X, waypoint.Y, waypoint.Name);

            throw new ArgumentException($"Point {id} not found");
        }

        private AgvTrack CreateTrack(string fromId, string toId)
        {
            return new AgvTrack
            {
                NetworkId = _network.Id,
                From = fromId,
                To = toId,
                Direction = TrackDirection.Bidirectional,
                SpeedLimit = _network.DefaultSpeedLimit,
                Distance = CalculateDistance(fromId, toId)
            };
        }

        public List<string> GetGroupNames()
        {
            return _network.Stations
                .Where(s => !string.IsNullOrEmpty(s.GroupName))
                .Select(s => s.GroupName)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        #endregion
    }
}
