using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for querying layout elements by type, location, and relationships
    /// Provides APIs for MES systems and planning applications
    /// </summary>
    public class LayoutQueryService
    {
        /// <summary>
        /// Get all nodes of a specific type
        /// </summary>
        public List<NodeData> GetNodesByType(LayoutData layout, string nodeType)
        {
            if (layout == null)
                return new List<NodeData>();

            return layout.Nodes
                .Where(n => n.Type == nodeType)
                .ToList();
        }

        /// <summary>
        /// Get all workstation nodes (machines, stations, etc.)
        /// </summary>
        public List<NodeData> GetWorkstations(LayoutData layout)
        {
            if (layout == null)
                return new List<NodeData>();

            // Workstations include machines, stations, and processing nodes
            var workstationTypes = new[] { "Machine", "Station", "Workstation", "Assembly" };
            return layout.Nodes
                .Where(n => workstationTypes.Contains(n.Type))
                .ToList();
        }

        /// <summary>
        /// Get all AGV paths
        /// </summary>
        public List<PathData> GetAGVPaths(LayoutData layout)
        {
            if (layout == null)
                return new List<PathData>();

            return layout.Paths
                .Where(p => p.ConnectionType == ConnectionTypes.AGVTrack)
                .ToList();
        }

        /// <summary>
        /// Get all cranes (EOT cranes)
        /// </summary>
        public List<NodeData> GetCranes(LayoutData layout)
        {
            if (layout == null)
                return new List<NodeData>();

            return layout.Nodes
                .Where(n => n.Type == "EOTCrane" || n.Type == "Crane")
                .ToList();
        }

        /// <summary>
        /// Get elements in a named zone
        /// </summary>
        public List<NodeData> GetElementsInZone(LayoutData layout, string zoneName)
        {
            if (layout == null || string.IsNullOrEmpty(zoneName))
                return new List<NodeData>();

            // Find the zone
            var zone = layout.Zones.FirstOrDefault(z => z.Name == zoneName || z.Id == zoneName);
            if (zone == null)
                return new List<NodeData>();

            // Return nodes within zone bounds
            var zoneMinX = zone.X;
            var zoneMaxX = zone.X + zone.Width;
            var zoneMinY = zone.Y;
            var zoneMaxY = zone.Y + zone.Height;

            return layout.Nodes
                .Where(n => n.Visual.X >= zoneMinX && n.Visual.X <= zoneMaxX &&
                           n.Visual.Y >= zoneMinY && n.Visual.Y <= zoneMaxY)
                .ToList();
        }

        /// <summary>
        /// Get elements at a specific point (within tolerance)
        /// </summary>
        public List<NodeData> GetElementsAtPoint(LayoutData layout, double x, double y, double tolerance = 10)
        {
            if (layout == null)
                return new List<NodeData>();

            return layout.Nodes
                .Where(n =>
                {
                    var nodeX = n.Visual.X;
                    var nodeY = n.Visual.Y;
                    var nodeWidth = n.Visual.Width;
                    var nodeHeight = n.Visual.Height;

                    return x >= nodeX - tolerance && x <= nodeX + nodeWidth + tolerance &&
                           y >= nodeY - tolerance && y <= nodeY + nodeHeight + tolerance;
                })
                .ToList();
        }

        /// <summary>
        /// Get elements in a bounding box region
        /// </summary>
        public List<NodeData> GetElementsInRegion(LayoutData layout, double minX, double minY, double maxX, double maxY)
        {
            if (layout == null)
                return new List<NodeData>();

            return layout.Nodes
                .Where(n =>
                {
                    var nodeX = n.Visual.X;
                    var nodeY = n.Visual.Y;
                    var nodeWidth = n.Visual.Width;
                    var nodeHeight = n.Visual.Height;

                    // Check if node overlaps with region
                    return !(nodeX + nodeWidth < minX || nodeX > maxX ||
                            nodeY + nodeHeight < minY || nodeY > maxY);
                })
                .ToList();
        }

        /// <summary>
        /// Get nearest workstation to a point
        /// </summary>
        public NodeData GetNearestWorkstation(LayoutData layout, double x, double y)
        {
            var workstations = GetWorkstations(layout);
            if (workstations.Count == 0)
                return null;

            return workstations
                .OrderBy(n => GetDistance(x, y, n.Visual.X + n.Visual.Width / 2, n.Visual.Y + n.Visual.Height / 2))
                .FirstOrDefault();
        }

        /// <summary>
        /// Get nearest AGV path to a point
        /// </summary>
        public PathData GetNearestAGVPath(LayoutData layout, double x, double y)
        {
            var agvPaths = GetAGVPaths(layout);
            if (agvPaths.Count == 0)
                return null;

            // For simplicity, use the "from" node position
            // A more sophisticated implementation would calculate distance to path line
            return agvPaths
                .OrderBy(p =>
                {
                    var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == p.From);
                    if (fromNode == null) return double.MaxValue;
                    return GetDistance(x, y, fromNode.Visual.X, fromNode.Visual.Y);
                })
                .FirstOrDefault();
        }

        /// <summary>
        /// Get transport elements serving a station
        /// </summary>
        public List<object> GetTransportServingStation(LayoutData layout, string stationId)
        {
            if (layout == null || string.IsNullOrEmpty(stationId))
                return new List<object>();

            var results = new List<object>();

            // Find paths connected to this station
            var connectedPaths = layout.Paths
                .Where(p => p.From == stationId || p.To == stationId)
                .ToList();

            results.AddRange(connectedPaths);

            // Find conveyors serving this station
            var connectedConveyors = layout.Conveyors
                .Where(c => c.FromNodeId == stationId || c.ToNodeId == stationId)
                .ToList();

            results.AddRange(connectedConveyors);

            return results;
        }

        /// <summary>
        /// Get crane serving an area
        /// </summary>
        public NodeData GetCraneServingArea(LayoutData layout, double x, double y)
        {
            var cranes = GetCranes(layout);
            if (cranes.Count == 0)
                return null;

            // Find crane whose coverage area includes this point
            return cranes.FirstOrDefault(crane =>
            {
                // Simplified: assume crane coverage is its position ± large radius
                var craneX = crane.Visual.X;
                var craneY = crane.Visual.Y;
                var coverageRadius = 200; // Default coverage

                var distance = GetDistance(x, y, craneX, craneY);
                return distance <= coverageRadius;
            });
        }

        /// <summary>
        /// Get material flow path between two stations
        /// </summary>
        public List<PathData> GetMaterialFlowPath(LayoutData layout, string fromStationId, string toStationId)
        {
            if (layout == null || string.IsNullOrEmpty(fromStationId) || string.IsNullOrEmpty(toStationId))
                return new List<PathData>();

            // Direct connection
            var directPath = layout.Paths
                .FirstOrDefault(p => p.From == fromStationId && p.To == toStationId &&
                                    p.ConnectionType == ConnectionTypes.PartFlow);

            if (directPath != null)
                return new List<PathData> { directPath };

            // For more complex routing, could implement A* or BFS pathfinding
            // For now, return empty if no direct path
            return new List<PathData>();
        }

        /// <summary>
        /// Get all transport elements connected to an element
        /// </summary>
        public List<object> GetConnectedTransport(LayoutData layout, string elementId)
        {
            if (layout == null || string.IsNullOrEmpty(elementId))
                return new List<object>();

            var results = new List<object>();

            // Find connected paths
            var connectedPaths = layout.Paths
                .Where(p => p.From == elementId || p.To == elementId)
                .ToList();

            results.AddRange(connectedPaths);

            // Find connected conveyors
            var connectedConveyors = layout.Conveyors
                .Where(c => c.FromNodeId == elementId || c.ToNodeId == elementId)
                .ToList();

            results.AddRange(connectedConveyors);

            return results;
        }

        /// <summary>
        /// Calculate Euclidean distance between two points
        /// </summary>
        private double GetDistance(double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
