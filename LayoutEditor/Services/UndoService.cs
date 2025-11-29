using System;
using System.Collections.Generic;
using LayoutEditor.Helpers;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Manages undo/redo state for layout changes
    /// </summary>
    public class UndoService
    {
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private const int MaxUndoLevels = 50;

        public event EventHandler? StateChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public void SaveState(LayoutData layout)
        {
            var json = JsonHelper.Serialize(layout);
            _undoStack.Push(json);
            _redoStack.Clear();

            // Limit stack size
            while (_undoStack.Count > MaxUndoLevels)
            {
                var temp = new Stack<string>();
                while (_undoStack.Count > 1)
                    temp.Push(_undoStack.Pop());
                _undoStack.Clear();
                while (temp.Count > 0)
                    _undoStack.Push(temp.Pop());
            }

            OnStateChanged();
        }

        public LayoutData? Undo(LayoutData currentLayout)
        {
            if (!CanUndo) return null;

            // Save current state to redo stack
            var currentJson = JsonHelper.Serialize(currentLayout);
            _redoStack.Push(currentJson);

            // Restore previous state
            var previousJson = _undoStack.Pop();
            var restoredLayout = JsonHelper.Deserialize<LayoutData>(previousJson);

            OnStateChanged();
            return restoredLayout;
        }

        public LayoutData? Redo(LayoutData currentLayout)
        {
            if (!CanRedo) return null;

            // Save current state to undo stack
            var currentJson = JsonHelper.Serialize(currentLayout);
            _undoStack.Push(currentJson);

            // Restore redo state
            var redoJson = _redoStack.Pop();
            var restoredLayout = JsonHelper.Deserialize<LayoutData>(redoJson);

            OnStateChanged();
            return restoredLayout;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnStateChanged();
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
