using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Manages selection state for nodes, paths, and groups
    /// </summary>
    public class SelectionService
    {
        private readonly HashSet<string> _selectedNodeIds = new();
        private readonly HashSet<string> _selectedGroupIds = new();
        private string? _selectedPathId;
        private string? _selectedGroupId;
        private string? _editingCellId;

        public event EventHandler? SelectionChanged;

        public IReadOnlySet<string> SelectedNodeIds => _selectedNodeIds;
        public IReadOnlySet<string> SelectedGroupIds => _selectedGroupIds;
        public string? SelectedPathId => _selectedPathId;
        public string? SelectedGroupId => _selectedGroupId;
        public string? EditingCellId => _editingCellId;
        public bool IsEditingCell => _editingCellId != null;

        public int SelectedCount => _selectedNodeIds.Count;
        public bool HasSelection => _selectedNodeIds.Count > 0 || _selectedPathId != null;
        public bool HasMultipleSelection => _selectedNodeIds.Count > 1;
        public bool HasSingleNodeSelection => _selectedNodeIds.Count == 1;
        public bool HasMultipleGroups => _selectedGroupIds.Count > 1;

        public void SelectNode(string nodeId, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                _selectedNodeIds.Clear();
                _selectedPathId = null;
                _selectedGroupId = null;
                _selectedGroupIds.Clear();
            }
            _selectedNodeIds.Add(nodeId);
            OnSelectionChanged();
        }

        public void SelectNodes(IEnumerable<string> nodeIds, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                _selectedNodeIds.Clear();
                _selectedPathId = null;
                _selectedGroupId = null;
                _selectedGroupIds.Clear();
            }
            foreach (var id in nodeIds)
                _selectedNodeIds.Add(id);
            OnSelectionChanged();
        }

        public void SelectPath(string pathId)
        {
            _selectedNodeIds.Clear();
            _selectedGroupId = null;
            _selectedGroupIds.Clear();
            _selectedPathId = pathId;
            OnSelectionChanged();
        }

        public void SelectGroup(string groupId, IEnumerable<string> memberIds, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                _selectedNodeIds.Clear();
                _selectedPathId = null;
                _selectedGroupId = null;
                _selectedGroupIds.Clear();
            }
            _selectedGroupId = groupId;
            _selectedGroupIds.Add(groupId);
            foreach (var id in memberIds)
                _selectedNodeIds.Add(id);
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

        public void ClearSelection()
        {
            _selectedNodeIds.Clear();
            _selectedPathId = null;
            _selectedGroupId = null;
            _selectedGroupIds.Clear();
            OnSelectionChanged();
        }

        public bool IsNodeSelected(string nodeId) => _selectedNodeIds.Contains(nodeId);
        public bool IsPathSelected(string pathId) => _selectedPathId == pathId;
        public bool IsGroupSelected(string groupId) => _selectedGroupIds.Contains(groupId);

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

        public List<GroupData> GetSelectedGroups(LayoutData? layout)
        {
            if (layout == null) return new List<GroupData>();
            return layout.Groups.Where(g => _selectedGroupIds.Contains(g.Id)).ToList();
        }

        protected virtual void OnSelectionChanged() =>
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
