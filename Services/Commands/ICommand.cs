using System;

namespace LayoutEditor.Services.Commands
{
    /// <summary>
    /// Interface for invertible command pattern
    /// Each edit operation can be executed and undone
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Unique identifier for this command
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable description of the command
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Timestamp when command was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Execute the command
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo the command (reverse the operation)
        /// </summary>
        void Undo();

        /// <summary>
        /// Whether this command can be merged with another
        /// (e.g., consecutive moves of the same element)
        /// </summary>
        bool CanMergeWith(ICommand other);

        /// <summary>
        /// Merge this command with another
        /// </summary>
        void MergeWith(ICommand other);
    }
}
