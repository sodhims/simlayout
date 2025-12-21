using System;
using System.Collections.Generic;
using System.Linq;

namespace LayoutEditor.Services.Commands
{
    /// <summary>
    /// Groups multiple commands into a single undoable unit
    /// Used for multi-element operations like group move, delete multiple, etc.
    /// </summary>
    public class CommandGroup : ICommand
    {
        private readonly List<ICommand> _commands = new List<ICommand>();

        public string Id { get; }
        public string Description { get; set; }
        public DateTime Timestamp { get; }

        public CommandGroup(string description)
        {
            Id = Guid.NewGuid().ToString();
            Description = description;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Add a command to the group
        /// </summary>
        public void Add(ICommand command)
        {
            _commands.Add(command);
        }

        /// <summary>
        /// Number of commands in the group
        /// </summary>
        public int Count => _commands.Count;

        /// <summary>
        /// Execute all commands in order
        /// </summary>
        public void Execute()
        {
            foreach (var command in _commands)
            {
                command.Execute();
            }
        }

        /// <summary>
        /// Undo all commands in reverse order
        /// </summary>
        public void Undo()
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }

        /// <summary>
        /// Command groups cannot be merged
        /// </summary>
        public bool CanMergeWith(ICommand other)
        {
            return false;
        }

        /// <summary>
        /// Not implemented for command groups
        /// </summary>
        public void MergeWith(ICommand other)
        {
            throw new NotSupportedException("Command groups cannot be merged");
        }
    }
}
