using System.Collections.Generic;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Validates simulation parameters
    /// </summary>
    public static class SimulationValidator
    {
        public static void Validate(LayoutData layout, List<ValidationIssue> issues)
        {
            foreach (var node in layout.Nodes)
            {
                ValidateNodeSimulation(node, issues);
            }
        }

        private static void ValidateNodeSimulation(NodeData node, List<ValidationIssue> issues)
        {
            var sim = node.Simulation;
            if (sim == null) return;

            if (sim.Capacity < 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_CAPACITY",
                    Severity = "error",
                    Message = $"Node {node.Name} has negative capacity",
                    NodeId = node.Id
                });
            }

            if (sim.ProcessTime != null)
                ValidateDistribution(sim.ProcessTime, node, "process time", issues);

            if (node.Type == NodeTypes.Source && sim.InterarrivalTime != null)
                ValidateDistribution(sim.InterarrivalTime, node, "interarrival time", issues);

            if (sim.SetupTime != null)
                ValidateDistribution(sim.SetupTime, node, "setup time", issues);

            if (sim.BatchSize < 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_BATCH_SIZE",
                    Severity = "warning",
                    Message = $"Node {node.Name} has negative batch size",
                    NodeId = node.Id
                });
            }
        }

        private static void ValidateDistribution(DistributionData dist, NodeData node,
            string paramName, List<ValidationIssue> issues)
        {
            if (dist == null) return;

            var validTypes = new HashSet<string>
            {
                "constant", "exponential", "normal", "uniform", "triangular", "weibull"
            };

            if (!validTypes.Contains(dist.Distribution))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_DISTRIBUTION_TYPE",
                    Severity = "error",
                    Message = $"Node {node.Name} has invalid {paramName} distribution: {dist.Distribution}",
                    NodeId = node.Id
                });
                return;
            }

            switch (dist.Distribution)
            {
                case "constant" when dist.Value < 0:
                    issues.Add(CreateIssue(node, paramName, "negative value"));
                    break;

                case "exponential" when dist.Mean <= 0:
                    issues.Add(CreateIssue(node, paramName, "non-positive mean"));
                    break;

                case "normal" when dist.StdDev < 0:
                    issues.Add(CreateIssue(node, paramName, "negative std dev"));
                    break;

                case "uniform" when dist.Min > dist.Max:
                    issues.Add(CreateIssue(node, paramName, "min > max"));
                    break;

                case "triangular" when dist.Min > dist.Mode || dist.Mode > dist.Max:
                    issues.Add(CreateIssue(node, paramName, "invalid triangular params"));
                    break;
            }
        }

        private static ValidationIssue CreateIssue(NodeData node, string param, string problem)
        {
            return new ValidationIssue
            {
                Code = "INVALID_DISTRIBUTION_PARAM",
                Severity = "warning",
                Message = $"Node {node.Name} {param}: {problem}",
                NodeId = node.Id
            };
        }
    }
}
