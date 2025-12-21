using System;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Commands
{
    /// <summary>
    /// Command for moving a node to a new position
    /// </summary>
    public class MoveNodeCommand : ICommand
    {
        private readonly NodeData _node;
        private readonly double _oldX;
        private readonly double _oldY;
        private double _newX;
        private double _newY;

        public string Id { get; }
        public string Description { get; private set; }
        public DateTime Timestamp { get; }

        public MoveNodeCommand(NodeData node, double newX, double newY)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;

            _node = node;
            _oldX = node.Visual.X;
            _oldY = node.Visual.Y;
            _newX = newX;
            _newY = newY;

            UpdateDescription();
        }

        private void UpdateDescription()
        {
            var deltaX = _newX - _oldX;
            var deltaY = _newY - _oldY;
            Description = $"Move {_node.Id} by ({deltaX:F0}, {deltaY:F0})";
        }

        public void Execute()
        {
            _node.Visual.X = _newX;
            _node.Visual.Y = _newY;
        }

        public void Undo()
        {
            _node.Visual.X = _oldX;
            _node.Visual.Y = _oldY;
        }

        public bool CanMergeWith(ICommand other)
        {
            if (other is MoveNodeCommand moveCmd)
            {
                // Can merge if same node and commands are close in time (within 1 second)
                return moveCmd._node.Id == _node.Id &&
                       (Timestamp - moveCmd.Timestamp).TotalSeconds < 1.0;
            }
            return false;
        }

        public void MergeWith(ICommand other)
        {
            if (other is MoveNodeCommand moveCmd && CanMergeWith(other))
            {
                // Update the new position to the merged command's position
                _newX = moveCmd._newX;
                _newY = moveCmd._newY;
                UpdateDescription();
            }
        }
    }
}
