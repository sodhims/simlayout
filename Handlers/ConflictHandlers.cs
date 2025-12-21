using System;
using System.Collections.Generic;
using System.Windows;
using LayoutEditor.Controls;
using LayoutEditor.Models;
using LayoutEditor.Renderers;
using LayoutEditor.Services.Conflicts;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for conflict detection and visualization
    /// </summary>
    public partial class MainWindow
    {
        private ConflictChecker? _conflictChecker;
        private ConflictRenderer? _conflictRenderer;
        private ConflictPanel? _conflictPanel;
        private List<Conflict> _currentConflicts = new List<Conflict>();
        private bool _autoCheckConflicts = false;

        /// <summary>
        /// Initializes conflict detection services
        /// </summary>
        private void InitializeConflictDetection()
        {
            _conflictChecker = new ConflictChecker();
            _conflictRenderer = new ConflictRenderer();
        }

        /// <summary>
        /// Check for conflicts button click
        /// </summary>
        private void CheckConflicts_Click(object sender, RoutedEventArgs e)
        {
            CheckForConflicts();
            ShowConflictPanel();
        }

        /// <summary>
        /// Performs conflict detection
        /// </summary>
        private void CheckForConflicts()
        {
            if (_conflictChecker == null || _layout == null)
                return;

            _currentConflicts = _conflictChecker.CheckAll(_layout);

            // Update status
            var errorCount = _conflictChecker.GetErrors(_currentConflicts).Count;
            var warningCount = _conflictChecker.GetWarnings(_currentConflicts).Count;

            if (_currentConflicts.Count == 0)
            {
                StatusText.Text = "✓ No conflicts detected";
            }
            else
            {
                StatusText.Text = $"Found {_currentConflicts.Count} conflict(s): {errorCount} errors, {warningCount} warnings";
            }

            // Render conflicts on canvas
            RenderConflicts();
        }

        /// <summary>
        /// Renders conflict highlights on canvas
        /// </summary>
        private void RenderConflicts()
        {
            if (_conflictRenderer == null)
                return;

            // Clear existing conflict highlights
            _conflictRenderer.ClearConflicts(EditorCanvas);

            // Render new conflicts
            _conflictRenderer.RenderConflicts(EditorCanvas, _currentConflicts);
        }

        /// <summary>
        /// Shows the conflict panel
        /// </summary>
        private void ShowConflictPanel()
        {
            if (_conflictPanel == null)
            {
                _conflictPanel = new ConflictPanel();
                _conflictPanel.ConflictSelected += OnConflictSelected;
                _conflictPanel.Owner = this;
            }

            _conflictPanel.LoadConflicts(_currentConflicts);
            _conflictPanel.Show();
        }

        /// <summary>
        /// Handles conflict selection from panel - zooms to conflict location
        /// </summary>
        private void OnConflictSelected(object? sender, Conflict conflict)
        {
            if (conflict == null)
                return;

            // Center view on conflict location
            // This is a simplified version - full implementation would handle zoom and pan
            StatusText.Text = $"Conflict: {conflict.Description}";

            // Flash the conflict highlight
            FlashConflictHighlight(conflict);
        }

        /// <summary>
        /// Flashes a conflict highlight to draw attention
        /// </summary>
        private void FlashConflictHighlight(Conflict conflict)
        {
            // Simple feedback - redraw conflicts to ensure visibility
            RenderConflicts();
        }

        /// <summary>
        /// Auto-check conflicts after edits (if enabled)
        /// </summary>
        private void AutoCheckConflicts()
        {
            if (_autoCheckConflicts)
            {
                CheckForConflicts();
            }
        }

        /// <summary>
        /// Toggles auto-check conflicts
        /// </summary>
        private void ToggleAutoCheckConflicts_Click(object sender, RoutedEventArgs e)
        {
            _autoCheckConflicts = !_autoCheckConflicts;
            StatusText.Text = _autoCheckConflicts
                ? "Auto-check conflicts: ON"
                : "Auto-check conflicts: OFF";

            if (_autoCheckConflicts)
            {
                CheckForConflicts();
            }
        }

        /// <summary>
        /// Clears all conflict visualizations
        /// </summary>
        private void ClearConflictVisualizations()
        {
            _conflictRenderer?.ClearConflicts(EditorCanvas);
            _currentConflicts.Clear();
        }
    }
}
