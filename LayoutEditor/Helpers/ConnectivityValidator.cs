using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Validates layout connectivity
    /// </summary>
    public static class ConnectivityValidator
    {
        public static void Validate(LayoutData layout, List<ValidationIssue> issues)
        {
            if (layout.Nodes.Count == 0) return;

            ValidateDisconnectedNodes(layout, issues);
            ValidateSourceReachability(layout, issues);
            ValidateSinkReachability(layout, issues);
        }

        private static void ValidateDisconnectedNodes(LayoutData layout,
            List<ValidationIssue> issues)
        {
            var connectedNodes = new HashSet<string>();

            foreach (var path in layout.Paths)
            {
                connectedNodes.Add(path.From);
                connectedNodes.Add(path.To);
            }

            foreach (var node in layout.Nodes)
            {
                if (!connectedNodes.Contains(node.Id))
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "DISCONNECTED_NODE",
                        Severity = "warning",
                        Message = $"Node {node.Name} has no connections",
                        NodeId = node.Id
                    });
                }
            }
        }

        private static void ValidateSourceReachability(LayoutData layout,
            List<ValidationIssue> issues)
        {
            var sources = layout.Nodes.Where(n => n.Type == NodeTypes.Source).ToList();
            var adjacency = BuildAdjacencyList(layout);

            foreach (var source in sources)
            {
                var reachable = GetReachableNodes(source.Id, adjacency);

                // Check if any sink is reachable
                var canReachSink = layout.Nodes
                    .Where(n => n.Type == NodeTypes.Sink)
                    .Any(sink => reachable.Contains(sink.Id));

                if (!canReachSink)
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "SOURCE_NO_SINK_PATH",
                        Severity = "error",
                        Message = $"Source {source.Name} cannot reach any sink",
                        NodeId = source.Id
                    });
                }
            }
        }

        private static void ValidateSinkReachability(LayoutData layout,
            List<ValidationIssue> issues)
        {
            var sinks = layout.Nodes.Where(n => n.Type == NodeTypes.Sink).ToList();
            var reverseAdjacency = BuildReverseAdjacencyList(layout);

            foreach (var sink in sinks)
            {
                var canReachFrom = GetReachableNodes(sink.Id, reverseAdjacency);

                // Check if reachable from any source
                var reachableFromSource = layout.Nodes
                    .Where(n => n.Type == NodeTypes.Source)
                    .Any(source => canReachFrom.Contains(source.Id));

                if (!reachableFromSource)
                {
                    issues.Add(new ValidationIssue
                    {
                        Code = "SINK_UNREACHABLE",
                        Severity = "error",
                        Message = $"Sink {sink.Name} is not reachable from any source",
                        NodeId = sink.Id
                    });
                }
            }
        }

        private static Dictionary<string, List<string>> BuildAdjacencyList(LayoutData layout)
        {
            var adjacency = new Dictionary<string, List<string>>();

            foreach (var node in layout.Nodes)
            {
                adjacency[node.Id] = new List<string>();
            }

            foreach (var path in layout.Paths)
            {
                if (adjacency.ContainsKey(path.From))
                {
                    adjacency[path.From].Add(path.To);
                }
            }

            return adjacency;
        }

        private static Dictionary<string, List<string>> BuildReverseAdjacencyList(LayoutData layout)
        {
            var adjacency = new Dictionary<string, List<string>>();

            foreach (var node in layout.Nodes)
            {
                adjacency[node.Id] = new List<string>();
            }

            foreach (var path in layout.Paths)
            {
                if (adjacency.ContainsKey(path.To))
                {
                    adjacency[path.To].Add(path.From);
                }
            }

            return adjacency;
        }

        private static HashSet<string> GetReachableNodes(string startId,
            Dictionary<string, List<string>> adjacency)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<string>();

            queue.Enqueue(startId);
            visited.Add(startId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (adjacency.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (visited.Add(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return visited;
        }
    }
}
