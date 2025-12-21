using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    public partial class ConflictPanel : Window
    {
        private List<Conflict> _allConflicts = new List<Conflict>();
        public event EventHandler<Conflict>? ConflictSelected;

        public ConflictPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loads conflicts into the panel
        /// </summary>
        public void LoadConflicts(List<Conflict> conflicts)
        {
            _allConflicts = conflicts ?? new List<Conflict>();
            RefreshConflictList();
        }

        /// <summary>
        /// Refreshes the conflict list display
        /// </summary>
        private void RefreshConflictList()
        {
            var conflicts = _allConflicts;

            // Filter acknowledged if checkbox is checked
            if (HideResolvedCheckbox.IsChecked == true)
            {
                conflicts = conflicts.Where(c => !c.IsAcknowledged).ToList();
            }

            // Convert to display models
            var displayItems = conflicts.Select(c => new ConflictDisplayItem(c)).ToList();

            ConflictListBox.ItemsSource = displayItems;

            // Update count
            var errorCount = conflicts.Count(c => c.Severity == ConflictSeverity.Error);
            var warningCount = conflicts.Count(c => c.Severity == ConflictSeverity.Warning);
            ConflictCountText.Text = $"{conflicts.Count} conflict{(conflicts.Count != 1 ? "s" : "")} found ({errorCount} errors, {warningCount} warnings)";
        }

        private void ConflictListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConflictListBox.SelectedItem is ConflictDisplayItem item)
            {
                ConflictSelected?.Invoke(this, item.Conflict);
            }
        }

        private void HideResolved_Changed(object sender, RoutedEventArgs e)
        {
            RefreshConflictList();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Display wrapper for conflicts in the list
        /// </summary>
        private class ConflictDisplayItem
        {
            public Conflict Conflict { get; }

            public ConflictDisplayItem(Conflict conflict)
            {
                Conflict = conflict;
            }

            public string SeverityIcon => Conflict.Severity == ConflictSeverity.Error ? "⚠" : "⚡";
            public string SeverityColor => Conflict.Severity == ConflictSeverity.Error ? "#E74C3C" : "#F39C12";
            public string TypeDisplay => Conflict.Type.ToString().Replace("_", " ");
            public string Description => Conflict.Description;
            public string SuggestedFix => $"💡 {Conflict.SuggestedFix}";
        }
    }
}
