using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Helpers;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Edit Operations

        // Clipboard data structure for cells
        private class ClipboardData
        {
            public List<NodeData> Nodes { get; set; } = new();
            public List<PathData> Paths { get; set; } = new();
            public List<GroupData> Groups { get; set; } = new();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            var restored = _undoService.Undo(_layout);
            if (restored != null)
            {
                _layout = restored;
                _selectionService.ClearSelection();
                RefreshAll();
                StatusText.Text = "Undo";
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            var restored = _undoService.Redo(_layout);
            if (restored != null)
            {
                _layout = restored;
                _selectionService.ClearSelection();
                RefreshAll();
                StatusText.Text = "Redo";
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            Copy_Click(sender, e);
            Delete_Click(sender, e);
        }

 private void Copy_Click(object sender, RoutedEventArgs e)
{
    var selectedNodes = _selectionService.GetSelectedNodes(_layout);
    if (selectedNodes.Count == 0) return;
    var selectedIds = new HashSet<string>(selectedNodes.Select(n => n.Id));
    var clipboard = new ClipboardData();
    
    // Copy nodes
    foreach (var node in selectedNodes)
    {
        var clone = JsonHelper.Deserialize<NodeData>(JsonHelper.Serialize(node));
        if (clone != null)
            clipboard.Nodes.Add(clone);
    }
    
    // Copy internal paths (both endpoints in selection)
    foreach (var path in _layout.Paths)
    {
        if (selectedIds.Contains(path.From) && selectedIds.Contains(path.To))
        {
            var clone = JsonHelper.Deserialize<PathData>(JsonHelper.Serialize(path));
            if (clone != null)
                clipboard.Paths.Add(clone);
        }
    }
    
    // Copy groups/cells that are fully contained in selection
    foreach (var group in _layout.Groups)
    {
        if (group.Members.All(m => selectedIds.Contains(m)))
        {
            var clone = JsonHelper.Deserialize<GroupData>(JsonHelper.Serialize(group));
            if (clone != null)
                clipboard.Groups.Add(clone);
        }
    }
    
    var json = JsonHelper.Serialize(clipboard);
    
    try
    {
        Clipboard.SetText(json);
    }
    catch
    {
        StatusText.Text = "Clipboard busy - try again";
        return;
    }
    
    var msg = $"Copied {selectedNodes.Count} node(s)";
    if (clipboard.Paths.Count > 0) msg += $", {clipboard.Paths.Count} path(s)";
    if (clipboard.Groups.Count > 0) msg += $", {clipboard.Groups.Count} cell(s)";
    StatusText.Text = msg;
 }

private void Paste_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var json = Clipboard.GetText();
        
        // Try new format first
        var clipboard = JsonHelper.Deserialize<ClipboardData>(json);
        
        if (clipboard != null && clipboard.Nodes.Count > 0)
        {
            PasteClipboardData(clipboard);
            return;
        }
        // Fall back to old format (just nodes)
        var nodes = JsonHelper.Deserialize<List<NodeData>>(json);
        if (nodes != null && nodes.Count > 0)
        {
            SaveUndoState();
            foreach (var node in nodes)
            {
                node.Id = Guid.NewGuid().ToString();
                node.Name = GetUniqueNodeName(node.Name);  // Add this line
                node.Visual.X += 20;
                node.Visual.Y += 20;
                _layout.Nodes.Add(node);
            }
            _selectionService.SelectNodes(nodes.Select(n => n.Id));
            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Pasted {nodes.Count} node(s)";
        }
    }
    catch
    {
        StatusText.Text = "Nothing to paste";
    }
}

private string GetUniqueNodeName(string baseName)
{
    var existingNames = new HashSet<string>(_layout.Nodes.Select(n => n.Name));
    
    if (!existingNames.Contains(baseName))
        return baseName;
    
    // Try to find existing number suffix (e.g., "Station-2" -> prefix="Station-", startNum=3)
    var match = System.Text.RegularExpressions.Regex.Match(baseName, @"^(.+?)(\d+)$");
    string prefix;
    int startNum;
    
    if (match.Success)
    {
        prefix = match.Groups[1].Value;
        startNum = int.Parse(match.Groups[2].Value) + 1;
    }
    else
    {
        prefix = baseName + "-";
        startNum = 1;
    }
    
    // Find next available number
    for (int i = startNum; i < 10000; i++)
    {
        var candidate = $"{prefix}{i}";
        if (!existingNames.Contains(candidate))
            return candidate;
    }
    
    return baseName + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
}
        private void PasteClipboardData(ClipboardData clipboard)
        {
            SaveUndoState();

            // Map old IDs to new IDs
            var idMap = new Dictionary<string, string>();
            var newNodeIds = new List<string>();

            // Paste nodes with new IDs
            foreach (var node in clipboard.Nodes)
            {
                var oldId = node.Id;
                var newId = Guid.NewGuid().ToString();
                idMap[oldId] = newId;
                
                node.Id = newId;
                node.Name = GetUniqueNodeName(node.Name);  // Add this line
                node.Visual.X += 30;
                node.Visual.Y += 30;
                _layout.Nodes.Add(node);
                newNodeIds.Add(newId);
            }

            // Paste paths with remapped IDs
            int pathCount = 0;
            foreach (var path in clipboard.Paths)
            {
                if (idMap.TryGetValue(path.From, out var newFrom) && 
                    idMap.TryGetValue(path.To, out var newTo))
                {
                    path.Id = Guid.NewGuid().ToString();
                    path.From = newFrom;
                    path.To = newTo;
                    _layout.Paths.Add(path);
                    pathCount++;
                }
            }

            // Paste groups/cells with remapped member IDs
            int cellCount = 0;
            foreach (var group in clipboard.Groups)
            {
                var newMembers = group.Members
                    .Where(m => idMap.ContainsKey(m))
                    .Select(m => idMap[m])
                    .ToList();

                if (newMembers.Count > 0)
                {
                    group.Id = Guid.NewGuid().ToString();
                    group.Members = newMembers;
                    
                    // Remap entry/exit points
                    group.EntryPoints = group.EntryPoints
                        .Where(e => idMap.ContainsKey(e))
                        .Select(e => idMap[e])
                        .ToList();
                    group.ExitPoints = group.ExitPoints
                        .Where(e => idMap.ContainsKey(e))
                        .Select(e => idMap[e])
                        .ToList();
                    
                    // Remap internal paths - find newly pasted paths that match
                    var newInternalPaths = new List<string>();
                    foreach (var oldPathId in group.InternalPaths)
                    {
                        var origPath = clipboard.Paths.FirstOrDefault(cp => cp.Id == oldPathId);
                        if (origPath != null && 
                            idMap.ContainsKey(origPath.From) && 
                            idMap.ContainsKey(origPath.To))
                        {
                            var newPath = _layout.Paths.FirstOrDefault(lp => 
                                lp.From == idMap[origPath.From] && 
                                lp.To == idMap[origPath.To]);
                            if (newPath != null)
                                newInternalPaths.Add(newPath.Id);
                        }
                    }
                    group.InternalPaths = newInternalPaths;

                    // Remap internal routing
                    var newRouting = new Dictionary<string, List<InternalRoute>>();
                    foreach (var kvp in group.InternalRouting)
                    {
                        if (idMap.ContainsKey(kvp.Key))
                        {
                            var newRoutes = kvp.Value
                                .Where(r => idMap.ContainsKey(r.To))
                                .Select(r => new InternalRoute 
                                { 
                                    To = idMap[r.To], 
                                    Priority = r.Priority, 
                                    Probability = r.Probability 
                                })
                                .ToList();
                            if (newRoutes.Count > 0)
                                newRouting[idMap[kvp.Key]] = newRoutes;
                        }
                    }
                    group.InternalRouting = newRouting;

                    // Update cell index
                    if (group.IsCell)
                    {
                        group.CellIndex = _layout.Groups.Count(g => g.IsCell) + 1;
                        group.Name = $"Cell_{group.CellIndex}";
                    }

                    _layout.Groups.Add(group);
                    cellCount++;
                }
            }

            _selectionService.SelectNodes(newNodeIds);
            MarkDirty();
            RefreshAll();

            var msg = $"Pasted {newNodeIds.Count} node(s)";
            if (pathCount > 0) msg += $", {pathCount} path(s)";
            if (cellCount > 0) msg += $", {cellCount} cell(s)";
            StatusText.Text = msg;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selectedIds = _selectionService.SelectedNodeIds.ToList();
            var selectedPathId = _selectionService.SelectedPathId;

            if (selectedIds.Count == 0 && selectedPathId == null) return;

            SaveUndoState();

            // Delete selected nodes
            foreach (var id in selectedIds)
            {
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == id);
                if (node != null)
                {
                    _layout.Nodes.Remove(node);

                    // Remove connected paths
                    var connectedPaths = _layout.Paths
                        .Where(p => p.From == id || p.To == id)
                        .ToList();

                    foreach (var path in connectedPaths)
                        _layout.Paths.Remove(path);

                    // Remove from groups
                    foreach (var group in _layout.Groups)
                    {
                        group.Members.Remove(id);
                        group.EntryPoints.Remove(id);
                        group.ExitPoints.Remove(id);
                    }

                    // Remove empty groups
                    var groupsToRemove = _layout.Groups.Where(g => g.Members.Count == 0).ToList();
                    foreach (var g in groupsToRemove) _layout.Groups.Remove(g);
                    
                    // ═══════════════════════════════════════════════════════════════
                    // TRANSPORT GROUP CLEANUP: Remove deleted nodes from transport groups
                    // ═══════════════════════════════════════════════════════════════
                    TransportGroupPanel?.RemoveMemberFromAllGroups(id);
                }
            }

            // Delete selected path
            if (selectedPathId != null)
            {
                var path = _layout.Paths.FirstOrDefault(p => p.Id == selectedPathId);
                if (path != null)
                    _layout.Paths.Remove(path);
            }

            _selectionService.ClearSelection();
            MarkDirty();
            RefreshAll();
            StatusText.Text = $"Deleted {selectedIds.Count} item(s)";
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            _selectionService.SelectNodes(_layout.Nodes.Select(n => n.Id));
            UpdateSelectionVisuals();
            UpdatePropertyPanel();
            StatusText.Text = $"Selected {_layout.Nodes.Count} node(s)";
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            // Use copy/paste logic for consistency
            Copy_Click(sender, e);
            Paste_Click(sender, e);
        }

        #endregion
    }
}
