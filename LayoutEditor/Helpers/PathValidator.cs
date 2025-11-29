using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Validates path properties
    /// </summary>
    public static class PathValidator
    {
        public static void Validate(LayoutData layout, List<ValidationIssue> issues)
        {
            var pathIds = new HashSet<string>();
            var nodeIds = layout.Nodes.Select(n => n.Id).ToHashSet();

            foreach (var path in layout.Paths)
            {
                ValidatePathId(path, pathIds, issues);
                ValidatePathEndpoints(path, nodeIds, issues);
                ValidateSelfLoop(path, issues);
                ValidatePathSimulation(path, issues);
            }

            ValidateDuplicatePaths(layout, issues);
        }

        private static void ValidatePathId(PathData path, HashSet<string> pathIds,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(path.Id))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "EMPTY_PATH_ID",
                    Severity = "error",
                    Message = "Path has empty ID"
                });
                return;
            }

            if (!pathIds.Add(path.Id))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "DUPLICATE_PATH_ID",
                    Severity = "error",
                    Message = $"Duplicate path ID: {path.Id}",
                    PathId = path.Id
                });
            }
        }

        private static void ValidatePathEndpoints(PathData path, HashSet<string> nodeIds,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(path.From))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "EMPTY_PATH_FROM",
                    Severity = "error",
                    Message = $"Path {path.Id} has no source node",
                    PathId = path.Id
                });
            }
            else if (!nodeIds.Contains(path.From))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_PATH_FROM",
                    Severity = "error",
                    Message = $"Path {path.Id} references non-existent source: {path.From}",
                    PathId = path.Id
                });
            }

            if (string.IsNullOrWhiteSpace(path.To))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "EMPTY_PATH_TO",
                    Severity = "error",
                    Message = $"Path {path.Id} has no destination node",
                    PathId = path.Id
                });
            }
            else if (!nodeIds.Contains(path.To))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_PATH_TO",
                    Severity = "error",
                    Message = $"Path {path.Id} references non-existent destination: {path.To}",
                    PathId = path.Id
                });
            }
        }

        private static void ValidateSelfLoop(PathData path, List<ValidationIssue> issues)
        {
            if (path.From == path.To && !string.IsNullOrEmpty(path.From))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "SELF_LOOP",
                    Severity = "warning",
                    Message = $"Path {path.Id} is a self-loop",
                    PathId = path.Id
                });
            }
        }

        private static void ValidatePathSimulation(PathData path, List<ValidationIssue> issues)
        {
            if (path.Simulation.Speed <= 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_PATH_SPEED",
                    Severity = "warning",
                    Message = $"Path {path.Id} has non-positive speed",
                    PathId = path.Id
                });
            }

            if (path.Simulation.Capacity <= 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_PATH_CAPACITY",
                    Severity = "warning",
                    Message = $"Path {path.Id} has non-positive capacity",
                    PathId = path.Id
                });
            }
        }

        private static void ValidateDuplicatePaths(LayoutData layout, List<ValidationIssue> issues)
        {
            var pathPairs = new HashSet<string>();

            foreach (var path in layout.Paths)
            {
                var key = $"{path.From}->{path.To}";
                if (!pathPairs.Add(key))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "DUPLICATE_PATH",
                        Severity = "warning",
                        Message = $"Duplicate path from {path.From} to {path.To}",
                        PathId = path.Id
                    });
                }
            }
        }
    }
}
