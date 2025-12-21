using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Validates AGV waypoints, paths, stations, and traffic zones
    /// </summary>
    public static class AGVValidator
    {
        public static void Validate(LayoutData layout, List<ValidationIssue> issues)
        {
            var waypointIds = layout.AGVWaypoints.Select(w => w.Id).ToHashSet();
            var equipmentIds = layout.Nodes.Select(n => n.Id).ToHashSet();

            // Validate waypoints
            ValidateWaypoints(layout, issues);

            // Validate paths
            ValidateAGVPaths(layout, waypointIds, issues);

            // Validate stations
            ValidateAGVStations(layout, waypointIds, equipmentIds, issues);

            // Validate traffic zones
            ValidateTrafficZones(layout, issues);

            // Validate network connectivity
            ValidateNetworkConnectivity(layout, issues);
        }

        private static void ValidateWaypoints(LayoutData layout, List<ValidationIssue> issues)
        {
            var waypointIds = new HashSet<string>();

            foreach (var waypoint in layout.AGVWaypoints)
            {
                // Check for duplicate IDs
                if (!waypointIds.Add(waypoint.Id))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "DUPLICATE_WAYPOINT_ID",
                        Severity = "error",
                        Message = $"Duplicate AGV waypoint ID: {waypoint.Name ?? waypoint.Id}"
                    });
                }

                // Check for empty names
                if (string.IsNullOrWhiteSpace(waypoint.Name))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "EMPTY_WAYPOINT_NAME",
                        Severity = "warning",
                        Message = $"AGV waypoint {waypoint.Id} has no name"
                    });
                }
            }
        }

        private static void ValidateAGVPaths(LayoutData layout, HashSet<string> waypointIds,
            List<ValidationIssue> issues)
        {
            foreach (var path in layout.AGVPaths)
            {
                // Check if path endpoints exist
                if (!waypointIds.Contains(path.FromWaypointId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "INVALID_PATH_FROM",
                        Severity = "error",
                        Message = $"AGV path '{path.Name}' has invalid FromWaypoint: {path.FromWaypointId}"
                    });
                }

                if (!waypointIds.Contains(path.ToWaypointId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "INVALID_PATH_TO",
                        Severity = "error",
                        Message = $"AGV path '{path.Name}' has invalid ToWaypoint: {path.ToWaypointId}"
                    });
                }

                // Check for self-loops
                if (path.FromWaypointId == path.ToWaypointId)
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "AGV_PATH_SELF_LOOP",
                        Severity = "warning",
                        Message = $"AGV path '{path.Name}' connects waypoint to itself"
                    });
                }
            }

            // Check for overlapping paths (same from/to waypoints)
            ValidateOverlappingPaths(layout, issues);
        }

        private static void ValidateOverlappingPaths(LayoutData layout, List<ValidationIssue> issues)
        {
            var pathPairs = new HashSet<string>();

            foreach (var path in layout.AGVPaths)
            {
                var key = $"{path.FromWaypointId}->{path.ToWaypointId}";

                if (!pathPairs.Add(key))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "OVERLAPPING_AGV_PATH",
                        Severity = "warning",
                        Message = $"Multiple AGV paths between same waypoints: {path.Name}"
                    });
                }
            }
        }

        private static void ValidateAGVStations(LayoutData layout, HashSet<string> waypointIds,
            HashSet<string> equipmentIds, List<ValidationIssue> issues)
        {
            foreach (var station in layout.AGVStations)
            {
                // Check if station has linked waypoint
                if (string.IsNullOrWhiteSpace(station.LinkedWaypointId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "STATION_NO_WAYPOINT",
                        Severity = "warning",
                        Message = $"AGV station '{station.Name}' has no linked waypoint"
                    });
                }
                else if (!waypointIds.Contains(station.LinkedWaypointId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "STATION_INVALID_WAYPOINT",
                        Severity = "error",
                        Message = $"AGV station '{station.Name}' links to non-existent waypoint: {station.LinkedWaypointId}"
                    });
                }

                // Check if station has linked equipment (optional, so only warn if specified but invalid)
                if (!string.IsNullOrWhiteSpace(station.LinkedEquipmentId) &&
                    !equipmentIds.Contains(station.LinkedEquipmentId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "STATION_INVALID_EQUIPMENT",
                        Severity = "warning",
                        Message = $"AGV station '{station.Name}' links to non-existent equipment: {station.LinkedEquipmentId}"
                    });
                }
            }
        }

        private static void ValidateTrafficZones(LayoutData layout, List<ValidationIssue> issues)
        {
            foreach (var zone in layout.TrafficZones)
            {
                // Check if zone has enough points
                if (zone.Boundary.Count < 3)
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "INVALID_ZONE_BOUNDARY",
                        Severity = "error",
                        Message = $"Traffic zone '{zone.Name}' has less than 3 boundary points"
                    });
                }

                // Check if max vehicles is valid
                if (zone.MaxVehicles < 1)
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "INVALID_ZONE_CAPACITY",
                        Severity = "warning",
                        Message = $"Traffic zone '{zone.Name}' has invalid max vehicles: {zone.MaxVehicles}"
                    });
                }
            }
        }

        private static void ValidateNetworkConnectivity(LayoutData layout, List<ValidationIssue> issues)
        {
            if (layout.AGVWaypoints.Count == 0)
                return; // No waypoints, nothing to validate

            // Build adjacency list
            var adjacency = new Dictionary<string, List<string>>();
            foreach (var waypoint in layout.AGVWaypoints)
            {
                adjacency[waypoint.Id] = new List<string>();
            }

            foreach (var path in layout.AGVPaths)
            {
                if (adjacency.ContainsKey(path.FromWaypointId))
                    adjacency[path.FromWaypointId].Add(path.ToWaypointId);

                // If bidirectional, add reverse connection
                if (path.Direction == PathDirections.Bidirectional &&
                    adjacency.ContainsKey(path.ToWaypointId))
                {
                    adjacency[path.ToWaypointId].Add(path.FromWaypointId);
                }
            }

            // Find disconnected waypoints (no outgoing or incoming connections)
            var disconnectedWaypoints = new List<AGVWaypointData>();
            foreach (var waypoint in layout.AGVWaypoints)
            {
                bool hasOutgoing = adjacency[waypoint.Id].Count > 0;
                bool hasIncoming = layout.AGVPaths.Any(p =>
                    p.ToWaypointId == waypoint.Id ||
                    (p.Direction == PathDirections.Bidirectional && p.FromWaypointId == waypoint.Id));

                if (!hasOutgoing && !hasIncoming)
                {
                    disconnectedWaypoints.Add(waypoint);
                }
            }

            // Report disconnected waypoints
            foreach (var waypoint in disconnectedWaypoints)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "DISCONNECTED_WAYPOINT",
                    Severity = "warning",
                    Message = $"AGV waypoint '{waypoint.Name}' is not connected to any paths"
                });
            }

            // Check if network has multiple disconnected components
            if (layout.AGVWaypoints.Count > 1 && layout.AGVPaths.Count > 0)
            {
                var visited = new HashSet<string>();
                var components = 0;

                foreach (var waypoint in layout.AGVWaypoints)
                {
                    if (!visited.Contains(waypoint.Id))
                    {
                        components++;
                        VisitComponent(waypoint.Id, adjacency, visited, layout);
                    }
                }

                if (components > 1)
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "DISCONNECTED_NETWORK",
                        Severity = "warning",
                        Message = $"AGV network has {components} disconnected components"
                    });
                }
            }
        }

        private static void VisitComponent(string waypointId,
            Dictionary<string, List<string>> adjacency,
            HashSet<string> visited,
            LayoutData layout)
        {
            var stack = new Stack<string>();
            stack.Push(waypointId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (visited.Contains(current))
                    continue;

                visited.Add(current);

                // Add neighbors (both outgoing and incoming)
                if (adjacency.ContainsKey(current))
                {
                    foreach (var neighbor in adjacency[current])
                    {
                        if (!visited.Contains(neighbor))
                            stack.Push(neighbor);
                    }
                }

                // Also check incoming connections (for unidirectional paths)
                foreach (var path in layout.AGVPaths)
                {
                    if (path.ToWaypointId == current && !visited.Contains(path.FromWaypointId))
                    {
                        stack.Push(path.FromWaypointId);
                    }
                }
            }
        }
    }
}
