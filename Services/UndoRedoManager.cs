using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Services.Commands;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Manages undo/redo history for the application
    /// Supports command merging and grouped operations
    /// </summary>
    public class UndoRedoManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        private int _maxHistorySize = 100;
        private bool _enableMerging = true;

        /// <summary>
        /// Event raised when the undo/redo state changes
        /// </summary>
        public event EventHandler HistoryChanged;

        /// <summary>
        /// Maximum number of commands to keep in history
        /// </summary>
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set => _maxHistorySize = Math.Max(1, value);
        }

        /// <summary>
        /// Enable/disable automatic command merging
        /// </summary>
        public bool EnableMerging
        {
            get => _enableMerging;
            set => _enableMerging = value;
        }

        /// <summary>
        /// Whether undo is available
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Whether redo is available
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Number of commands in undo history
        /// </summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>
        /// Number of commands in redo history
        /// </summary>
        public int RedoCount => _redoStack.Count;

        /// <summary>
        /// Get the undo history (most recent first)
        /// </summary>
        public IEnumerable<ICommand> UndoHistory => _undoStack;

        /// <summary>
        /// Get the redo history (most recent first)
        /// </summary>
        public IEnumerable<ICommand> RedoHistory => _redoStack;

        /// <summary>
        /// Execute a command and add it to the undo history
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Execute the command
            command.Execute();

            // Try to merge with the last command if enabled
            if (_enableMerging && _undoStack.Count > 0)
            {
                var lastCommand = _undoStack.Peek();
                if (lastCommand.CanMergeWith(command))
                {
                    lastCommand.MergeWith(command);
                    OnHistoryChanged();
                    return;
                }
            }

            // Add to undo stack
            _undoStack.Push(command);

            // Clear redo stack (can't redo after new command)
            _redoStack.Clear();

            // Trim history if needed
            TrimHistory();

            OnHistoryChanged();
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            OnHistoryChanged();
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            OnHistoryChanged();
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged();
        }

        /// <summary>
        /// Begin a command group for multi-element operations
        /// Returns a CommandGroup that can be populated with commands
        /// </summary>
        public CommandGroup BeginGroup(string description)
        {
            return new CommandGroup(description);
        }

        /// <summary>
        /// End a command group and add it to history
        /// </summary>
        public void EndGroup(CommandGroup group)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (group.Count > 0)
            {
                ExecuteCommand(group);
            }
        }

        /// <summary>
        /// Trim history to max size
        /// </summary>
        private void TrimHistory()
        {
            while (_undoStack.Count > _maxHistorySize)
            {
                // Remove oldest command (from bottom of stack)
                var temp = new Stack<ICommand>();
                while (_undoStack.Count > 1)
                {
                    temp.Push(_undoStack.Pop());
                }
                _undoStack.Pop(); // Remove oldest

                // Restore stack
                while (temp.Count > 0)
                {
                    _undoStack.Push(temp.Pop());
                }
            }
        }

        /// <summary>
        /// Raise the HistoryChanged event
        /// </summary>
        private void OnHistoryChanged()
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get a description of the next undo command
        /// </summary>
        public string GetUndoDescription()
        {
            return CanUndo ? _undoStack.Peek().Description : null;
        }

        /// <summary>
        /// Get a description of the next redo command
        /// </summary>
        public string GetRedoDescription()
        {
            return CanRedo ? _redoStack.Peek().Description : null;
        }
    }
}
