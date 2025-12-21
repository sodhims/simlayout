using System;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Commands
{
    /// <summary>
    /// Command for adding a node to the layout
    /// </summary>
    public class AddNodeCommand : ICommand
    {
        private readonly LayoutData _layout;
        private readonly NodeData _node;

        public string Id { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        public AddNodeCommand(LayoutData layout, NodeData node)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;

            _layout = layout;
            _node = node;

            Description = $"Add {node.Type ?? "Node"} ({node.Id})";
        }

        public void Execute()
        {
            if (!_layout.Nodes.Contains(_node))
            {
                _layout.Nodes.Add(_node);
            }
        }

        public void Undo()
        {
            _layout.Nodes.Remove(_node);
        }

        public bool CanMergeWith(ICommand other)
        {
            // Add commands cannot be merged
            return false;
        }

        public void MergeWith(ICommand other)
        {
            throw new NotSupportedException("Add commands cannot be merged");
        }
    }
}
