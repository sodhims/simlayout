using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Manages selection state for nodes, paths, walls, and groups
    /// </summary>
    public class SelectionService
    {
        private readonly HashSet<string> _selectedNodeIds = new();
        private readonly HashSet<string> _selectedGroupIds = new();
        private readonly HashSet<string> _selectedWallIds = new();
        private readonly HashSet<string> _selectedPathIds = new();
        private string? _selectedPathId;
        private string? _selectedGroupId;
        private string? _editingCellId;

        public event EventHandler? SelectionChanged;

        public IReadOnlySet<string> SelectedNodeIds => _selectedNodeIds;
        public IReadOnlySet<string> SelectedGroupIds => _selectedGroupIds;
        public IReadOnlySet<string> SelectedWallIds => _selectedWallIds;
        public IReadOnlySet<string> SelectedPathIds => _selectedPathIds;
        public string? SelectedPathId => _selectedPathId;
        public string? SelectedGroupId => _selectedGroupId;
        public string? EditingCellId => _editingCellId;
        public bool IsEditingCell => _editingCellId != null;

        public int SelectedCount => _selectedNodeIds.Count + _selectedWallIds.Count + _selectedPathIds.Count;
        public bool HasSelection => _selectedNodeIds.Count > 0 || _selectedPathId != null || 
                                    _selectedWallIds.Count > 0 || _selectedPathIds.Count > 0;
        public bool HasMultipleSelection => SelectedCount > 1;
        public bool HasSingleNodeSelection => _selectedNodeIds.Count == 1 && _selectedWallIds.Count == 0;
        public bool HasMultipleGroups => _selectedGroupIds.Count > 1;

        public void SelectNode(string nodeId, bool addToSelection = false)
        {
            if (!addToSelection) ClearAllSelections();
            _selectedNodeIds.Add(nodeId);
            OnSelectionChanged();
        }

        public void SelectNodes(IEnumerable<string> nodeIds, bool addToSelection = false)
        {
            if (!addToSelection) ClearAllSelections();
            foreach (var id in nodeIds)
                _selectedNodeIds.Add(id);
            OnSelectionChanged();
        }

        public void SelectWall(string wallId, bool addToSelection = false)
        {
            if (!addToSelection) ClearAllSelections();
            _selectedWallIds.Add(wallId);
            OnSelectionChanged();
        }

        public void SelectWalls(IEnumerable<string> wallIds, bool addToSelection = false)
        {
            if (!addToSelection) ClearAllSelections();
            foreach (var id in wallIds)
                _selectedWallIds.Add(id);
            OnSelectionChanged();
        }

        public void SelectPath(string pathId)
        {
            ClearAllSelections();
            _selectedPathId = pathId;
            _selectedPathIds.Add(pathId);
            OnSelectionChanged();
        }

        public void SelectMultiple(IEnumerable<string> nodeIds, IEnumerable<string> wallIds, IEnumerable<string> pathIds)
        {
            ClearAllSelections();
            foreach (var id in nodeIds) _selectedNodeIds.Add(id);
            foreach (var id in wallIds) _selectedWallIds.Add(id);
            foreach (var id in pathIds) _selectedPathIds.Add(id);
            if (_selectedPathIds.Count == 1) _selectedPathId = _selectedPathIds.First();
            OnSelectionChanged();
        }

        public void SelectGroup(string groupId, IEnumerable<string> memberIds, bool addToSelection = false)
        {
            if (!addToSelection) ClearAllSelections();
            _selectedGroupId = groupId;
            _selectedGroupIds.Add(groupId);
            foreach (var id in memberIds)
                _selectedNodeIds.Add(id);
            OnSelectionChanged();
        }

        /// <summary>
        /// Select a group/cell including its internal paths
        /// </summary>
        public void SelectGroupWithPaths(string groupId, IEnumerable<string> memberIds, IEnumerable<string> internalPathIds, bool addToSelection = false)
        {
            if (!addToSelection) ClearAllSelections();
            _selectedGroupId = groupId;
            _selectedGroupIds.Add(groupId);
            foreach (var id in memberIds)
                _selectedNodeIds.Add(id);
            foreach (var pathId in internalPathIds)
                _selectedPathIds.Add(pathId);
            OnSelectionChanged();
        }

        public void EnterCellEditMode(string cellId)
        {
            _editingCellId = cellId;
            OnSelectionChanged();
        }

        public void ExitCellEditMode()
        {
            _editingCellId = null;
            ClearSelection();
        }

        public void ToggleNodeSelection(string nodeId)
        {
            if (_selectedNodeIds.Contains(nodeId))
                _selectedNodeIds.Remove(nodeId);
            else
                _selectedNodeIds.Add(nodeId);
            OnSelectionChanged();
        }

        public void ToggleWallSelection(string wallId)
        {
            if (_selectedWallIds.Contains(wallId))
                _selectedWallIds.Remove(wallId);
            else
                _selectedWallIds.Add(wallId);
            OnSelectionChanged();
        }

        private void ClearAllSelections()
        {
            _selectedNodeIds.Clear();
            _selectedPathId = null;
            _selectedGroupId = null;
            _selectedGroupIds.Clear();
            _selectedWallIds.Clear();
            _selectedPathIds.Clear();
        }

        public void ClearSelection()
        {
            ClearAllSelections();
            OnSelectionChanged();
        }

        public bool IsNodeSelected(string nodeId) => _selectedNodeIds.Contains(nodeId);
        public bool IsPathSelected(string pathId) => _selectedPathId == pathId || _selectedPathIds.Contains(pathId);
        public bool IsGroupSelected(string groupId) => _selectedGroupIds.Contains(groupId);
        public bool IsWallSelected(string wallId) => _selectedWallIds.Contains(wallId);

        public string? GetSingleSelectedNodeId() =>
            _selectedNodeIds.Count == 1 ? _selectedNodeIds.First() : null;

        public NodeData? GetSelectedNode(LayoutData? layout)
        {
            if (layout == null) return null;
            var id = GetSingleSelectedNodeId();
            return id != null ? layout.Nodes.FirstOrDefault(n => n.Id == id) : null;
        }

        public PathData? GetSelectedPath(LayoutData? layout)
        {
            if (layout == null) return null;
            return _selectedPathId != null 
                ? layout.Paths.FirstOrDefault(p => p.Id == _selectedPathId) : null;
        }

        public List<NodeData> GetSelectedNodes(LayoutData? layout)
        {
            if (layout == null) return new List<NodeData>();
            return layout.Nodes.Where(n => _selectedNodeIds.Contains(n.Id)).ToList();
        }

        public List<WallData> GetSelectedWalls(LayoutData? layout)
        {
            if (layout == null) return new List<WallData>();
            return layout.Walls.Where(w => _selectedWallIds.Contains(w.Id)).ToList();
        }

        public List<PathData> GetSelectedPaths(LayoutData? layout)
        {
            if (layout == null) return new List<PathData>();
            return layout.Paths.Where(p => _selectedPathIds.Contains(p.Id)).ToList();
        }

        public List<GroupData> GetSelectedGroups(LayoutData? layout)
        {
            if (layout == null) return new List<GroupData>();
            return layout.Groups.Where(g => _selectedGroupIds.Contains(g.Id)).ToList();
        }

        protected virtual void OnSelectionChanged() =>
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
