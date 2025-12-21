using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Automatic path routing that respects walls and uses passages (doorways).
    /// Use for connecting nodes placed over CAD floor plans.
    /// </summary>
    public class AutoRouterService
    {
        private readonly LayoutData _layout;
        private List<RoutingGeometry.Passage>? _cachedPassages;

        public AutoRouterService(LayoutData layout)
        {
            _layout = layout;
        }

        /// <summary>Invalidate passage cache when walls change</summary>
        public void InvalidateCache() => _cachedPassages = null;

        /// <summary>Get or detect passages</summary>
        public List<RoutingGeometry.Passage> GetPassages()
        {
            _cachedPassages ??= RoutingGeometry.DetectPassages(_layout);
            return _cachedPassages;
        }

        #region Path Creation

        /// <summary>Create a path between two nodes with automatic wall-aware routing</summary>
        public PathData CreatePath(string fromNodeId, string toNodeId)
        {
            var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == fromNodeId);
            var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == toNodeId);
            
            var path = new PathData
            {
                Id = Guid.NewGuid().ToString(),
                From = fromNodeId,
                To = toNodeId,
                FromTerminal = "output",
                ToTerminal = "input",
                RoutingMode = "direct",
                Visual = new PathVisual(),
                Simulation = new PathSimulation()
            };

            if (fromNode != null && toNode != null)
            {
                // Calculate waypoints for wall avoidance
                var from = RoutingGeometry.GetNodeOutputTerminal(fromNode);
                var to = RoutingGeometry.GetNodeInputTerminal(toNode);
                var waypoints = CalculateWaypoints(from, to);
                
                path.Visual.Waypoints = new ObservableCollection<PointData>(waypoints.Select(p => new PointData(p.X, p.Y)));
                path.RoutingMode = waypoints.Count > 0 ? "orthogonal" : "direct";
                
                // Calculate path distance
                path.Simulation.Distance = CalculatePathLength(from, to, waypoints);
            }

            return path;
        }

        /// <summary>Re-route an existing path to avoid walls</summary>
        public void ReroutePath(PathData path)
        {
            var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
            var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);
            
            if (fromNode == null || toNode == null) return;

            var from = RoutingGeometry.GetNodeOutputTerminal(fromNode);
            var to = RoutingGeometry.GetNodeInputTerminal(toNode);
            var waypoints = CalculateWaypoints(from, to);
            
            path.Visual.Waypoints = new ObservableCollection<PointData>(waypoints.Select(p => new PointData(p.X, p.Y)));
            path.Simulation.Distance = CalculatePathLength(from, to, waypoints);
        }

        /// <summary>Re-route all paths in layout</summary>
        public int RerouteAllPaths()
        {
            InvalidateCache();
            var count = 0;
            foreach (var path in _layout.Paths)
            {
                var oldWaypoints = path.Visual.Waypoints.Count;
                ReroutePath(path);
                if (path.Visual.Waypoints.Count != oldWaypoints)
                    count++;
            }
            return count;
        }

        #endregion

        #region Waypoint Calculation

        /// <summary>Calculate waypoints to route from A to B avoiding walls</summary>
        public List<Point> CalculateWaypoints(Point from, Point to)
        {
            var waypoints = new List<Point>();
            
            // Direct path clear?
            if (!RoutingGeometry.IntersectsWall(from, to, _layout))
                return waypoints;

            // Get passages
            var passages = GetPassages();
            if (passages.Count == 0)
                return waypoints; // No passages available

            // Try single passage routing first
            var singlePassage = FindBestSinglePassage(from, to, passages);
            if (singlePassage != null)
            {
                waypoints.Add(singlePassage.Center);
                return waypoints;
            }

            // Try two-passage routing for complex layouts
            var twoPassageRoute = FindTwoPassageRoute(from, to, passages);
            if (twoPassageRoute != null)
            {
                waypoints.AddRange(twoPassageRoute);
                return waypoints;
            }

            // Fallback: orthogonal routing
            return CalculateOrthogonalRoute(from, to);
        }

        private RoutingGeometry.Passage? FindBestSinglePassage(Point from, Point to, List<RoutingGeometry.Passage> passages)
        {
            RoutingGeometry.Passage? best = null;
            double bestScore = double.MaxValue;

            foreach (var passage in passages)
            {
                // Check both legs of path are clear
                if (RoutingGeometry.IntersectsWall(from, passage.Center, _layout) ||
                    RoutingGeometry.IntersectsWall(passage.Center, to, _layout))
                    continue;

                // Score: total distance with penalty for deviation
                var directDist = RoutingGeometry.Distance(from, to);
                var throughDist = RoutingGeometry.Distance(from, passage.Center) + 
                                  RoutingGeometry.Distance(passage.Center, to);
                var deviation = throughDist - directDist;
                var score = throughDist + deviation * 0.5; // Penalize large detours

                if (score < bestScore)
                {
                    bestScore = score;
                    best = passage;
                }
            }

            return best;
        }

        private List<Point>? FindTwoPassageRoute(Point from, Point to, List<RoutingGeometry.Passage> passages)
        {
            double bestScore = double.MaxValue;
            List<Point>? bestRoute = null;

            foreach (var p1 in passages)
            {
                if (RoutingGeometry.IntersectsWall(from, p1.Center, _layout))
                    continue;

                foreach (var p2 in passages)
                {
                    if (p1.Id == p2.Id) continue;
                    
                    if (RoutingGeometry.IntersectsWall(p1.Center, p2.Center, _layout) ||
                        RoutingGeometry.IntersectsWall(p2.Center, to, _layout))
                        continue;

                    var dist = RoutingGeometry.Distance(from, p1.Center) +
                               RoutingGeometry.Distance(p1.Center, p2.Center) +
                               RoutingGeometry.Distance(p2.Center, to);

                    if (dist < bestScore)
                    {
                        bestScore = dist;
                        bestRoute = new List<Point> { p1.Center, p2.Center };
                    }
                }
            }

            return bestRoute;
        }

        private List<Point> CalculateOrthogonalRoute(Point from, Point to)
        {
            var waypoints = new List<Point>();
            
            // Simple L-shaped routing
            var midX = (from.X + to.X) / 2;
            var midY = (from.Y + to.Y) / 2;

            // Try horizontal-first
            var h1 = new Point(midX, from.Y);
            var h2 = new Point(midX, to.Y);
            if (!RoutingGeometry.IntersectsWall(from, h1, _layout) &&
                !RoutingGeometry.IntersectsWall(h1, h2, _layout) &&
                !RoutingGeometry.IntersectsWall(h2, to, _layout))
            {
                waypoints.Add(h1);
                waypoints.Add(h2);
                return waypoints;
            }

            // Try vertical-first
            var v1 = new Point(from.X, midY);
            var v2 = new Point(to.X, midY);
            if (!RoutingGeometry.IntersectsWall(from, v1, _layout) &&
                !RoutingGeometry.IntersectsWall(v1, v2, _layout) &&
                !RoutingGeometry.IntersectsWall(v2, to, _layout))
            {
                waypoints.Add(v1);
                waypoints.Add(v2);
                return waypoints;
            }

            return waypoints; // Empty if no clear route found
        }

        #endregion

        #region Auto-Connect

        /// <summary>Automatically connect all unconnected output terminals to nearest input terminals</summary>
        public List<PathData> AutoConnectNodes(double maxDistance = 500)
        {
            var newPaths = new List<PathData>();
            
            // Find nodes with unconnected outputs
            var connectedOutputs = _layout.Paths.Select(p => p.From).ToHashSet();
            var nodesWithFreeOutput = _layout.Nodes
                .Where(n => n.Type != "sink" && !connectedOutputs.Contains(n.Id))
                .ToList();

            // Find nodes with unconnected inputs
            var connectedInputs = _layout.Paths.Select(p => p.To).ToHashSet();
            var nodesWithFreeInput = _layout.Nodes
                .Where(n => n.Type != "source" && !connectedInputs.Contains(n.Id))
                .ToList();

            foreach (var fromNode in nodesWithFreeOutput)
            {
                var fromPos = RoutingGeometry.GetNodeOutputTerminal(fromNode);
                
                // Find nearest unconnected input
                NodeData? nearest = null;
                double nearestDist = maxDistance;

                foreach (var toNode in nodesWithFreeInput)
                {
                    if (toNode.Id == fromNode.Id) continue;
                    if (connectedInputs.Contains(toNode.Id)) continue; // Already connected
                    
                    var toPos = RoutingGeometry.GetNodeInputTerminal(toNode);
                    var dist = RoutingGeometry.Distance(fromPos, toPos);
                    
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = toNode;
                    }
                }

                if (nearest != null)
                {
                    var path = CreatePath(fromNode.Id, nearest.Id);
                    newPaths.Add(path);
                    connectedInputs.Add(nearest.Id); // Mark as connected
                }
            }

            return newPaths;
        }

        /// <summary>Connect nodes that are visually close and aligned (for assembly lines)</summary>
        public List<PathData> AutoConnectSequential(double proximityThreshold = 150)
        {
            var newPaths = new List<PathData>();
            var connectedOutputs = _layout.Paths.Select(p => p.From).ToHashSet();
            
            // Sort nodes roughly left-to-right, top-to-bottom
            var sortedNodes = _layout.Nodes
                .Where(n => n.Type != "sink")
                .OrderBy(n => n.Visual.Y / 100) // Group by row
                .ThenBy(n => n.Visual.X)
                .ToList();

            for (int i = 0; i < sortedNodes.Count - 1; i++)
            {
                var fromNode = sortedNodes[i];
                if (connectedOutputs.Contains(fromNode.Id)) continue;

                var fromOutput = RoutingGeometry.GetNodeOutputTerminal(fromNode);
                
                // Look for nearby node to connect to
                for (int j = i + 1; j < sortedNodes.Count; j++)
                {
                    var toNode = sortedNodes[j];
                    if (toNode.Type == "source") continue;
                    
                    var toInput = RoutingGeometry.GetNodeInputTerminal(toNode);
                    var dist = RoutingGeometry.Distance(fromOutput, toInput);
                    
                    // Check proximity and rough alignment
                    if (dist < proximityThreshold)
                    {
                        var path = CreatePath(fromNode.Id, toNode.Id);
                        newPaths.Add(path);
                        connectedOutputs.Add(fromNode.Id);
                        break;
                    }
                }
            }

            return newPaths;
        }

        #endregion

        #region Utilities

        private double CalculatePathLength(Point from, Point to, List<Point> waypoints)
        {
            if (waypoints.Count == 0)
                return RoutingGeometry.Distance(from, to);

            var total = RoutingGeometry.Distance(from, waypoints[0]);
            for (int i = 0; i < waypoints.Count - 1; i++)
                total += RoutingGeometry.Distance(waypoints[i], waypoints[i + 1]);
            total += RoutingGeometry.Distance(waypoints[^1], to);
            
            return total;
        }

        /// <summary>Check if a path crosses any walls</summary>
        public bool PathCrossesWalls(PathData path)
        {
            var (from, to) = RoutingGeometry.GetPathEndpoints(_layout, path);
            var points = new List<Point> { from };
            points.AddRange(path.Visual.Waypoints.Select(w => new Point(w.X, w.Y)));
            points.Add(to);

            for (int i = 0; i < points.Count - 1; i++)
            {
                if (RoutingGeometry.IntersectsWall(points[i], points[i + 1], _layout))
                    return true;
            }
            return false;
        }

        /// <summary>Get statistics about routing in layout</summary>
        public (int total, int crossing, int routed) GetRoutingStats()
        {
            int crossing = 0, routed = 0;
            foreach (var path in _layout.Paths)
            {
                if (path.Visual.Waypoints.Count > 0) routed++;
                if (PathCrossesWalls(path)) crossing++;
            }
            return (_layout.Paths.Count, crossing, routed);
        }

        #endregion
    }
}
