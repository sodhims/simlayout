using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Validates node properties
    /// </summary>
    public static class NodeValidator
    {
        private static readonly HashSet<string> ValidNodeTypes = new()
        {
            NodeTypes.Source, NodeTypes.Sink, NodeTypes.Machine,
            NodeTypes.Buffer, NodeTypes.Storage, NodeTypes.Workstation,
            NodeTypes.Inspection, NodeTypes.Conveyor, NodeTypes.Junction,
            NodeTypes.AgvStation
        };

        public static void Validate(LayoutData layout, List<ValidationIssue> issues)
        {
            var nodeIds = new HashSet<string>();

            foreach (var node in layout.Nodes)
            {
                ValidateNodeId(node, nodeIds, issues);
                ValidateNodeName(node, issues);
                ValidateNodeType(node, issues);
                ValidateNodeVisual(node, issues);
                ValidateNodeBounds(node, layout, issues);
            }

            ValidateSourceSinkCount(layout, issues);
        }

        private static void ValidateNodeId(NodeData node, HashSet<string> nodeIds, 
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "EMPTY_NODE_ID",
                    Severity = "error",
                    Message = "Node has empty ID"
                });
                return;
            }

            if (!nodeIds.Add(node.Id))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "DUPLICATE_NODE_ID",
                    Severity = "error",
                    Message = $"Duplicate node ID: {node.Id}",
                    NodeId = node.Id
                });
            }
        }

        private static void ValidateNodeName(NodeData node, List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(node.Name))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "EMPTY_NODE_NAME",
                    Severity = "warning",
                    Message = $"Node {node.Id} has no name",
                    NodeId = node.Id
                });
            }
        }

        private static void ValidateNodeType(NodeData node, List<ValidationIssue> issues)
        {
            if (!ValidNodeTypes.Contains(node.Type))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_NODE_TYPE",
                    Severity = "error",
                    Message = $"Node {node.Id} has invalid type: {node.Type}",
                    NodeId = node.Id
                });
            }
        }

        private static void ValidateNodeVisual(NodeData node, List<ValidationIssue> issues)
        {
            if (node.Visual.Width <= 0 || node.Visual.Height <= 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_NODE_SIZE",
                    Severity = "error",
                    Message = $"Node {node.Name} has invalid size",
                    NodeId = node.Id
                });
            }
        }

        private static void ValidateNodeBounds(NodeData node, LayoutData layout,
            List<ValidationIssue> issues)
        {
            if (node.Visual.X < 0 || node.Visual.Y < 0 ||
                node.Visual.X + node.Visual.Width > layout.Canvas.Width ||
                node.Visual.Y + node.Visual.Height > layout.Canvas.Height)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "NODE_OUT_OF_BOUNDS",
                    Severity = "warning",
                    Message = $"Node {node.Name} is outside canvas bounds",
                    NodeId = node.Id
                });
            }
        }

        private static void ValidateSourceSinkCount(LayoutData layout, 
            List<ValidationIssue> issues)
        {
            var sources = layout.Nodes.Count(n => n.Type == NodeTypes.Source);
            var sinks = layout.Nodes.Count(n => n.Type == NodeTypes.Sink);

            if (sources == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "NO_SOURCE",
                    Severity = "warning",
                    Message = "Layout has no source nodes"
                });
            }

            if (sinks == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "NO_SINK",
                    Severity = "warning",
                    Message = "Layout has no sink nodes"
                });
            }
        }
    }
}
