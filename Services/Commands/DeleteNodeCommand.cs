using System;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Commands
{
    /// <summary>
    /// Command for deleting a node from the layout
    /// </summary>
    public class DeleteNodeCommand : ICommand
    {
        private readonly LayoutData _layout;
        private readonly NodeData _node;
        private int _originalIndex;

        public string Id { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        public DeleteNodeCommand(LayoutData layout, NodeData node)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;

            _layout = layout;
            _node = node;
            _originalIndex = layout.Nodes.IndexOf(node);

            Description = $"Delete {node.Type ?? "Node"} ({node.Id})";
        }

        public void Execute()
        {
            _originalIndex = _layout.Nodes.IndexOf(_node);
            _layout.Nodes.Remove(_node);
        }

        public void Undo()
        {
            // Restore at original index if possible
            if (_originalIndex >= 0 && _originalIndex <= _layout.Nodes.Count)
            {
                _layout.Nodes.Insert(_originalIndex, _node);
            }
            else
            {
                _layout.Nodes.Add(_node);
            }
        }

        public bool CanMergeWith(ICommand other)
        {
            // Delete commands cannot be merged
            return false;
        }

        public void MergeWith(ICommand other)
        {
            throw new NotSupportedException("Delete commands cannot be merged");
        }
    }
}
