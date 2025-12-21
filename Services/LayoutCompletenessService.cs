using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for checking layout completeness and data integrity
    /// Reports missing connections and validation issues
    /// </summary>
    public class LayoutCompletenessService
    {
        /// <summary>
        /// Check layout completeness and return report
        /// </summary>
        public CompletenessReport CheckCompleteness(LayoutData layout)
        {
            if (layout == null)
                return new CompletenessReport { IsComplete = false };

            var report = new CompletenessReport
            {
                LayoutId = layout.Id,
                CheckTime = DateTime.UtcNow
            };

            // Check for orphaned nodes (no connections)
            CheckOrphanedNodes(layout, report);

            // Check for broken connections (references to non-existent nodes)
            CheckBrokenConnections(layout, report);

            // Check for missing AGV return paths
            CheckMissingReturnPaths(layout, report);

            // Check for incomplete crane coverage
            CheckCraneCoverage(layout, report);

            // Check for missing zone assignments
            CheckZoneAssignments(layout, report);

            // Determine overall completeness
            report.IsComplete = report.Issues.Count == 0;
            report.TotalIssues = report.Issues.Count;

            return report;
        }

        /// <summary>
        /// Check for nodes with no connections
        /// </summary>
        private void CheckOrphanedNodes(LayoutData layout, CompletenessReport report)
        {
            var connectedNodeIds = new HashSet<string>();

            // Collect all connected node IDs from paths
            foreach (var path in layout.Paths)
            {
                connectedNodeIds.Add(path.From);
                connectedNodeIds.Add(path.To);
            }

            // Collect all connected node IDs from conveyors
            foreach (var conveyor in layout.Conveyors)
            {
                connectedNodeIds.Add(conveyor.FromNodeId);
                connectedNodeIds.Add(conveyor.ToNodeId);
            }

            // Find orphaned nodes (workstations that should be connected)
            var workstationTypes = new[] { "Machine", "Station", "Workstation", "Assembly" };
            foreach (var node in layout.Nodes.Where(n => workstationTypes.Contains(n.Type)))
            {
                if (!connectedNodeIds.Contains(node.Id))
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Connectivity",
                        ElementId = node.Id,
                        Description = $"Node '{node.Id}' ({node.Type}) has no connections"
                    });
                }
            }
        }

        /// <summary>
        /// Check for broken connection references
        /// </summary>
        private void CheckBrokenConnections(LayoutData layout, CompletenessReport report)
        {
            var nodeIds = new HashSet<string>(layout.Nodes.Select(n => n.Id));

            // Check path references
            foreach (var path in layout.Paths)
            {
                if (!nodeIds.Contains(path.From))
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = "Broken Reference",
                        ElementId = path.Id,
                        Description = $"Path '{path.Id}' references non-existent From node '{path.From}'"
                    });
                }

                if (!nodeIds.Contains(path.To))
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = "Broken Reference",
                        ElementId = path.Id,
                        Description = $"Path '{path.Id}' references non-existent To node '{path.To}'"
                    });
                }
            }

            // Check conveyor references
            foreach (var conveyor in layout.Conveyors)
            {
                if (!nodeIds.Contains(conveyor.FromNodeId))
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = "Broken Reference",
                        ElementId = conveyor.Id,
                        Description = $"Conveyor '{conveyor.Id}' references non-existent From node '{conveyor.FromNodeId}'"
                    });
                }

                if (!nodeIds.Contains(conveyor.ToNodeId))
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = "Broken Reference",
                        ElementId = conveyor.Id,
                        Description = $"Conveyor '{conveyor.Id}' references non-existent To node '{conveyor.ToNodeId}'"
                    });
                }
            }
        }

        /// <summary>
        /// Check for missing return paths in AGV network
        /// </summary>
        private void CheckMissingReturnPaths(LayoutData layout, CompletenessReport report)
        {
            var agvPaths = layout.Paths.Where(p => p.ConnectionType == ConnectionTypes.AGVTrack).ToList();

            // Build adjacency list
            var forwardPaths = new Dictionary<string, List<string>>();
            var reversePaths = new Dictionary<string, List<string>>();

            foreach (var path in agvPaths)
            {
                if (!forwardPaths.ContainsKey(path.From))
                    forwardPaths[path.From] = new List<string>();
                forwardPaths[path.From].Add(path.To);

                if (!reversePaths.ContainsKey(path.To))
                    reversePaths[path.To] = new List<string>();
                reversePaths[path.To].Add(path.From);
            }

            // Check for one-way paths (no return route)
            foreach (var path in agvPaths)
            {
                bool hasReturn = reversePaths.ContainsKey(path.From) &&
                                reversePaths[path.From].Contains(path.To);

                if (!hasReturn)
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Missing Return Path",
                        ElementId = path.Id,
                        Description = $"AGV path '{path.From}' → '{path.To}' has no return path"
                    });
                }
            }
        }

        /// <summary>
        /// Check crane coverage completeness
        /// </summary>
        private void CheckCraneCoverage(LayoutData layout, CompletenessReport report)
        {
            var cranes = layout.Nodes.Where(n => n.Type == "EOTCrane" || n.Type == "Crane").ToList();

            if (cranes.Count == 0)
                return; // No cranes to check

            // Check for adjacent cranes without handoff zones
            for (int i = 0; i < cranes.Count; i++)
            {
                for (int j = i + 1; j < cranes.Count; j++)
                {
                    var crane1 = cranes[i];
                    var crane2 = cranes[j];

                    var distance = Math.Sqrt(
                        Math.Pow(crane2.Visual.X - crane1.Visual.X, 2) +
                        Math.Pow(crane2.Visual.Y - crane1.Visual.Y, 2)
                    );

                    // If cranes are close (within 500 units), they should have a handoff
                    if (distance < 500)
                    {
                        // Check if there's a layer connection between them
                        var hasHandoff = layout.LayerConnections.Any(lc =>
                            (lc.FromElementId == crane1.Id && lc.ToElementId == crane2.Id) ||
                            (lc.FromElementId == crane2.Id && lc.ToElementId == crane1.Id));

                        if (!hasHandoff)
                        {
                            report.Issues.Add(new CompletenessIssue
                            {
                                Severity = IssueSeverity.Info,
                                Category = "Missing Handoff",
                                ElementId = $"{crane1.Id},{crane2.Id}",
                                Description = $"Adjacent cranes '{crane1.Id}' and '{crane2.Id}' may need a handoff zone"
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check for elements without zone assignments
        /// </summary>
        private void CheckZoneAssignments(LayoutData layout, CompletenessReport report)
        {
            if (layout.Zones.Count == 0)
                return; // No zones defined

            var workstationTypes = new[] { "Machine", "Station", "Workstation", "Assembly" };
            foreach (var node in layout.Nodes.Where(n => workstationTypes.Contains(n.Type)))
            {
                // Check if node is in any zone
                bool inZone = layout.Zones.Any(z =>
                    node.Visual.X >= z.X && node.Visual.X <= z.X + z.Width &&
                    node.Visual.Y >= z.Y && node.Visual.Y <= z.Y + z.Height
                );

                if (!inZone)
                {
                    report.Issues.Add(new CompletenessIssue
                    {
                        Severity = IssueSeverity.Info,
                        Category = "Missing Zone",
                        ElementId = node.Id,
                        Description = $"Node '{node.Id}' is not assigned to any zone"
                    });
                }
            }
        }

        /// <summary>
        /// Get summary statistics for a layout
        /// </summary>
        public LayoutStatistics GetStatistics(LayoutData layout)
        {
            if (layout == null)
                return new LayoutStatistics();

            var stats = new LayoutStatistics
            {
                LayoutId = layout.Id,
                TotalNodes = layout.Nodes.Count,
                TotalPaths = layout.Paths.Count,
                TotalConveyors = layout.Conveyors.Count,
                TotalWalls = layout.Walls.Count,
                TotalZones = layout.Zones.Count
            };

            // Count by type
            stats.WorkstationCount = layout.Nodes.Count(n => new[] { "Machine", "Station", "Workstation", "Assembly" }.Contains(n.Type));
            stats.AGVPathCount = layout.Paths.Count(p => p.ConnectionType == ConnectionTypes.AGVTrack);
            stats.CraneCount = layout.Nodes.Count(n => n.Type == "EOTCrane" || n.Type == "Crane");

            return stats;
        }
    }

    /// <summary>
    /// Report of layout completeness check
    /// </summary>
    public class CompletenessReport
    {
        public string LayoutId { get; set; }
        public DateTime CheckTime { get; set; }
        public bool IsComplete { get; set; }
        public int TotalIssues { get; set; }
        public List<CompletenessIssue> Issues { get; set; } = new List<CompletenessIssue>();

        public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);
        public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
        public int InfoCount => Issues.Count(i => i.Severity == IssueSeverity.Info);
    }

    /// <summary>
    /// Individual completeness issue
    /// </summary>
    public class CompletenessIssue
    {
        public IssueSeverity Severity { get; set; }
        public string Category { get; set; }
        public string ElementId { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Issue severity levels
    /// </summary>
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Layout statistics summary
    /// </summary>
    public class LayoutStatistics
    {
        public string LayoutId { get; set; }
        public int TotalNodes { get; set; }
        public int TotalPaths { get; set; }
        public int TotalConveyors { get; set; }
        public int TotalWalls { get; set; }
        public int TotalZones { get; set; }
        public int WorkstationCount { get; set; }
        public int AGVPathCount { get; set; }
        public int CraneCount { get; set; }
    }
}
