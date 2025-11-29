using System.Collections.Generic;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Validates visual overlaps between nodes
    /// </summary>
    public static class OverlapValidator
    {
        public static void Validate(LayoutData layout, List<ValidationIssue> issues)
        {
            var nodes = layout.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (NodesOverlap(nodes[i], nodes[j]))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Code = "NODE_OVERLAP",
                            Severity = "warning",
                            Message = $"Nodes {nodes[i].Name} and {nodes[j].Name} overlap",
                            NodeId = nodes[i].Id
                        });
                    }
                }
            }
        }

        private static bool NodesOverlap(NodeData a, NodeData b)
        {
            // Simple AABB overlap test
            return !(a.Visual.X + a.Visual.Width < b.Visual.X ||
                     b.Visual.X + b.Visual.Width < a.Visual.X ||
                     a.Visual.Y + a.Visual.Height < b.Visual.Y ||
                     b.Visual.Y + b.Visual.Height < a.Visual.Y);
        }
    }
}
