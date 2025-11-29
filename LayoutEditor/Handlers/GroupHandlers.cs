using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Group/Cell Operations

        private void DefineCell_Click(object sender, RoutedEventArgs e)
        {
            var selectedIds = _selectionService.SelectedNodeIds.ToList();
            if (selectedIds.Count < 2)
            {
                MessageBox.Show("Select at least 2 nodes to create a cell.", "Create Cell",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveUndoState();

            // Generate cell name
            var cellNumber = _layout.Groups.Count(g => g.IsCell) + 1;
            var cellName = $"Cell_{cellNumber}";

            var group = new GroupData
            {
                Id = Guid.NewGuid().ToString(),
                Name = cellName,
                IsCell = true,
                CellIndex = cellNumber,
                Members = selectedIds
            };

            // Analyze paths and auto-wire the cell
            AutoWireCell(group);

            _layout.Groups.Add(group);

            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Created cell '{cellName}' with {selectedIds.Count} nodes, Entry: {group.EntryPoints.Count}, Exit: {group.ExitPoints.Count}";
        }

        /// <summary>
        /// Analyzes paths to determine entry/exit points and builds internal routing
        /// </summary>
        private void AutoWireCell(GroupData cell)
        {
            var memberSet = new HashSet<string>(cell.Members);
            
            // Find entry points: nodes that have incoming paths from OUTSIDE the cell
            // Find exit points: nodes that have outgoing paths to OUTSIDE the cell
            var entryPoints = new HashSet<string>();
            var exitPoints = new HashSet<string>();
            var internalPaths = new List<string>();

            foreach (var path in _layout.Paths)
            {
                bool fromInCell = memberSet.Contains(path.From);
                bool toInCell = memberSet.Contains(path.To);

                if (fromInCell && toInCell)
                {
                    // Internal path - both endpoints are in the cell
                    internalPaths.Add(path.Id);
                }
                else if (!fromInCell && toInCell)
                {
                    // Incoming path from outside - 'To' node is an entry point
                    entryPoints.Add(path.To);
                }
                else if (fromInCell && !toInCell)
                {
                    // Outgoing path to outside - 'From' node is an exit point
                    exitPoints.Add(path.From);
                }
            }

            // If no external paths found, try to infer from internal topology
            if (entryPoints.Count == 0 || exitPoints.Count == 0)
            {
                InferEntryExitFromTopology(cell, memberSet, entryPoints, exitPoints);
            }

            cell.EntryPoints = entryPoints.ToList();
            cell.ExitPoints = exitPoints.ToList();
            cell.InternalPaths = internalPaths;

            // Build internal routing table
            cell.BuildInternalRouting(_layout.Paths);
        }

        /// <summary>
        /// Infer entry/exit points from internal path topology when no external paths exist
        /// Entry = nodes with no internal predecessors
        /// Exit = nodes with no internal successors
        /// </summary>
        private void InferEntryExitFromTopology(GroupData cell, HashSet<string> memberSet, 
            HashSet<string> entryPoints, HashSet<string> exitPoints)
        {
            var hasIncoming = new HashSet<string>();
            var hasOutgoing = new HashSet<string>();

            foreach (var path in _layout.Paths)
            {
                if (memberSet.Contains(path.From) && memberSet.Contains(path.To))
                {
                    hasOutgoing.Add(path.From);
                    hasIncoming.Add(path.To);
                }
            }

            // Entry points = members with no internal incoming paths
            foreach (var member in memberSet)
            {
                if (!hasIncoming.Contains(member))
                    entryPoints.Add(member);
            }

            // Exit points = members with no internal outgoing paths
            foreach (var member in memberSet)
            {
                if (!hasOutgoing.Contains(member))
                    exitPoints.Add(member);
            }
        }

        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            var selectedIds = _selectionService.SelectedNodeIds.ToList();
            if (selectedIds.Count < 2)
            {
                MessageBox.Show("Select at least 2 nodes to create a group.", "Create Group",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveUndoState();

            var groupNumber = _layout.Groups.Count + 1;
            var groupName = $"Group_{groupNumber}";

            var group = new GroupData
            {
                Id = Guid.NewGuid().ToString(),
                Name = groupName,
                IsCell = false,
                Members = selectedIds
            };

            _layout.Groups.Add(group);

            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Created group '{groupName}' with {selectedIds.Count} nodes";
        }

        private void Ungroup_Click(object sender, RoutedEventArgs e)
        {
            var groupId = _selectionService.SelectedGroupId;
            if (groupId == null) return;

            var group = _layout.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null) return;

            SaveUndoState();

            _layout.Groups.Remove(group);
            _selectionService.ClearSelection();

            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Ungrouped '{group.Name}'";
        }

        private void AddToGroup_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show dialog to select group
        }

        private void RemoveFromGroup_Click(object sender, RoutedEventArgs e)
        {
            var nodeId = _selectionService.GetSingleSelectedNodeId();
            if (nodeId == null) return;

            var group = _layout.Groups.FirstOrDefault(g => g.Members.Contains(nodeId));
            if (group == null)
            {
                StatusText.Text = "Node is not in any group";
                return;
            }

            SaveUndoState();

            group.Members.Remove(nodeId);

            // Remove group if only one member left
            if (group.Members.Count < 2)
            {
                _layout.Groups.Remove(group);
            }

            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Removed node from '{group.Name}'";
        }

        private void UngroupCell_Click(object sender, RoutedEventArgs e)
        {
            var groupId = _selectionService.SelectedGroupId;
            if (groupId == null)
            {
                StatusText.Text = "No cell selected";
                return;
            }

            var group = _layout.Groups.FirstOrDefault(g => g.Id == groupId && g.IsCell);
            if (group == null)
            {
                StatusText.Text = "Selected item is not a cell";
                return;
            }

            SaveUndoState();

            _layout.Groups.Remove(group);
            _selectionService.ClearSelection();

            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Removed cell '{group.Name}'";
        }

        private void RefreshCellEntryExit_Click(object sender, RoutedEventArgs e)
        {
            var groupId = _selectionService.SelectedGroupId;
            GroupData? cell = null;
            
            if (groupId != null)
            {
                cell = _layout.Groups.FirstOrDefault(g => g.Id == groupId && g.IsCell);
            }

            if (cell == null)
            {
                // Refresh all cells
                int refreshed = 0;
                foreach (var c in _layout.Groups.Where(g => g.IsCell))
                {
                    AutoWireCell(c);
                    refreshed++;
                }
                
                if (refreshed > 0)
                {
                    MarkDirty();
                    RefreshAll();
                    StatusText.Text = $"Refreshed entry/exit points for {refreshed} cells";
                }
                else
                {
                    StatusText.Text = "No cells to refresh";
                }
                return;
            }

            SaveUndoState();
            AutoWireCell(cell);
            
            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Refreshed '{cell.Name}': Entry={cell.EntryPoints.Count}, Exit={cell.ExitPoints.Count}";
        }

        #endregion
    }
}
