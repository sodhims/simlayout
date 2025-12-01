using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private string? _pendingTemplateId;
        
        #region Template Operations

        private void RefreshTemplateList()
        {
            if (TemplateList == null) return;

            TemplateList.Items.Clear();

            foreach (var template in _layout.Templates)
            {
                var pathInfo = template.Paths.Count > 0 ? $", {template.Paths.Count}p" : "";
                var cellInfo = template.Groups.Count > 0 ? $", {template.Groups.Count}c" : "";
                var item = new ListBoxItem
                {
                    Content = $"{template.Name} ({template.Nodes.Count}n{pathInfo}{cellInfo})",
                    Tag = template.Id,
                    ToolTip = $"Double-click to place at center, or select and click canvas\n{template.Nodes.Count} nodes, {template.Paths.Count} paths, {template.Groups.Count} cells"
                };
                item.MouseDoubleClick += TemplateItem_DoubleClick;
                TemplateList.Items.Add(item);
            }
        }

        private void TemplateItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Tag is string templateId)
            {
                PlaceTemplateAtCenter(templateId);
            }
        }

        private void Template_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_layout == null) return;
            
            if (TemplateList?.SelectedItem is ListBoxItem item)
            {
                _pendingTemplateId = item.Tag?.ToString();
                StatusText.Text = $"Template selected - double-click to place at center, or click canvas to place at click position";
            }
        }

        private void PlaceTemplateAtCenter(string templateId)
        {
            var template = _layout.Templates.FirstOrDefault(t => t.Id == templateId);
            if (template == null || template.Nodes.Count == 0) return;

            // Calculate center of canvas view
            var centerX = EditorCanvas.ActualWidth / 2;
            var centerY = EditorCanvas.ActualHeight / 2;

            PlaceTemplateAt(template, new Point(centerX, centerY));
        }

        public void PlaceTemplateAt(Models.TemplateData template, Point position)
        {
            SaveUndoState();

            // Calculate template bounds to center it
            var minX = template.Nodes.Min(n => n.Visual.X);
            var minY = template.Nodes.Min(n => n.Visual.Y);
            var maxX = template.Nodes.Max(n => n.Visual.X + n.Visual.Width);
            var maxY = template.Nodes.Max(n => n.Visual.Y + n.Visual.Height);
            
            var templateWidth = maxX - minX;
            var templateHeight = maxY - minY;
            
            var offsetX = position.X - (minX + templateWidth / 2);
            var offsetY = position.Y - (minY + templateHeight / 2);

            // Create new nodes with new IDs and offset positions
            var idMap = new System.Collections.Generic.Dictionary<string, string>();
            
            foreach (var templateNode in template.Nodes)
            {
                var newId = Guid.NewGuid().ToString();
                idMap[templateNode.Id] = newId;
                
                var newNode = Helpers.JsonHelper.Deserialize<Models.NodeData>(
                    Helpers.JsonHelper.Serialize(templateNode))!;
                
                newNode.Id = newId;
                newNode.Visual.X += offsetX;
                newNode.Visual.Y += offsetY;
                
                // Generate unique name - only add suffix if name already exists
                var baseName = templateNode.Name ?? templateNode.Type ?? "Node";
                var newName = baseName;
                var suffix = 2;
                while (_layout.Nodes.Any(n => n.Name == newName))
                {
                    newName = $"{baseName}_{suffix++}";
                }
                newNode.Name = newName;
                
                _layout.Nodes.Add(newNode);
            }

            // Create new paths with updated node IDs
            foreach (var templatePath in template.Paths)
            {
                if (idMap.ContainsKey(templatePath.From) && idMap.ContainsKey(templatePath.To))
                {
                    var newPath = Helpers.JsonHelper.Deserialize<Models.PathData>(
                        Helpers.JsonHelper.Serialize(templatePath))!;
                    
                    newPath.Id = Guid.NewGuid().ToString();
                    newPath.From = idMap[templatePath.From];
                    newPath.To = idMap[templatePath.To];
                    
                    // Offset waypoints if any
                    foreach (var wp in newPath.Visual.Waypoints)
                    {
                        wp.X += offsetX;
                        wp.Y += offsetY;
                    }
                    
                    _layout.Paths.Add(newPath);
                }
            }

            // Create new groups/cells with updated member IDs
            Helpers.DebugLogger.Log($"=== Placing template '{template.Name}' ===");
            Helpers.DebugLogger.Log($"Template has {template.Groups.Count} groups");
            Helpers.DebugLogger.Log($"ID Map: {string.Join(", ", idMap.Select(kv => $"{kv.Key}->{kv.Value}"))}");
            
            foreach (var templateGroup in template.Groups)
            {
                Helpers.DebugLogger.Log($"Processing group: {templateGroup.Name}");
                Helpers.DebugLogger.Log($"  Template group members ({templateGroup.Members.Count}): {string.Join(", ", templateGroup.Members)}");
                
                var newGroup = Helpers.JsonHelper.Deserialize<Models.GroupData>(
                    Helpers.JsonHelper.Serialize(templateGroup))!;
                
                newGroup.Id = Guid.NewGuid().ToString();
                
                // Generate unique cell name
                var baseName = templateGroup.Name ?? (templateGroup.IsCell ? "Cell" : "Group");
                var newName = baseName;
                var suffix = 2;
                while (_layout.Groups.Any(g => g.Name == newName))
                {
                    newName = $"{baseName}_{suffix++}";
                }
                newGroup.Name = newName;
                
                // Update member IDs to new node IDs
                newGroup.Members.Clear();
                foreach (var oldMemberId in templateGroup.Members)
                {
                    if (idMap.ContainsKey(oldMemberId))
                    {
                        newGroup.Members.Add(idMap[oldMemberId]);
                        Helpers.DebugLogger.Log($"  Mapped member {oldMemberId} -> {idMap[oldMemberId]}");
                    }
                    else
                    {
                        Helpers.DebugLogger.Log($"  WARNING: No mapping for member {oldMemberId}");
                    }
                }
                
                Helpers.DebugLogger.Log($"  Final group {newGroup.Name} has {newGroup.Members.Count} members: {string.Join(", ", newGroup.Members)}");
                
                // Update entry/exit points
                newGroup.EntryPoints.Clear();
                foreach (var oldId in templateGroup.EntryPoints)
                {
                    if (idMap.ContainsKey(oldId))
                        newGroup.EntryPoints.Add(idMap[oldId]);
                }
                
                newGroup.ExitPoints.Clear();
                foreach (var oldId in templateGroup.ExitPoints)
                {
                    if (idMap.ContainsKey(oldId))
                        newGroup.ExitPoints.Add(idMap[oldId]);
                }
                
                if (newGroup.Members.Count > 0)
                    _layout.Groups.Add(newGroup);
            }

            MarkDirty();
            RefreshAll();
            
            var pathInfo = template.Paths.Count > 0 ? $", {template.Paths.Count} paths" : "";
            var cellInfo = template.Groups.Count > 0 ? $", {template.Groups.Count} cells" : "";
            StatusText.Text = $"Placed template '{template.Name}' with {template.Nodes.Count} nodes{pathInfo}{cellInfo}";
            _pendingTemplateId = null;
        }

        private void AddTemplate_Click(object sender, RoutedEventArgs e)
        {
            var selectedNodes = _selectionService.GetSelectedNodes(_layout);
            if (selectedNodes.Count == 0)
            {
                MessageBox.Show("Select nodes to save as template.\n\nHow to use templates:\n" +
                    "1. Select one or more nodes\n" +
                    "2. Click '+ Add Selected' to save as template\n" +
                    "3. Double-click template to place at canvas center\n" +
                    "4. Or select template and click on canvas to place", 
                    "Create Template", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Prompt for name
            var dialog = new Window
            {
                Title = "Create Template",
                Width = 300, Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };
            
            var stack = new StackPanel { Margin = new Thickness(10) };
            stack.Children.Add(new TextBlock { Text = "Template name:", Margin = new Thickness(0, 0, 0, 5) });
            var nameBox = new TextBox { Text = $"Template_{_layout.Templates.Count + 1}" };
            stack.Children.Add(nameBox);
            
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            var okBtn = new Button { Content = "Create", Width = 70, IsDefault = true };
            var cancelBtn = new Button { Content = "Cancel", Width = 70, Margin = new Thickness(10, 0, 0, 0), IsCancel = true };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            stack.Children.Add(btnPanel);
            dialog.Content = stack;
            
            okBtn.Click += (s, args) => { dialog.DialogResult = true; dialog.Close(); };
            
            if (dialog.ShowDialog() != true) return;
            
            var templateName = string.IsNullOrWhiteSpace(nameBox.Text) ? $"Template_{_layout.Templates.Count + 1}" : nameBox.Text;

            // Get node IDs for path filtering
            var selectedNodeIds = selectedNodes.Select(n => n.Id).ToHashSet();
            
            // Find paths that connect selected nodes
            var internalPaths = _layout.Paths.Where(p => 
                selectedNodeIds.Contains(p.From) && selectedNodeIds.Contains(p.To)).ToList();

            // Find cells/groups that contain ONLY selected nodes
            var internalGroups = _layout.Groups.Where(g => 
                g.Members.Count > 0 && g.Members.All(m => selectedNodeIds.Contains(m))).ToList();

            // Debug: verify groups have members
            Helpers.DebugLogger.Log($"=== Creating template '{templateName}' ===");
            Helpers.DebugLogger.Log($"Selected nodes: {string.Join(", ", selectedNodeIds)}");
            foreach (var g in internalGroups)
            {
                Helpers.DebugLogger.Log($"Found group {g.Name} with {g.Members.Count} members: {string.Join(", ", g.Members)}");
            }

            var template = new Models.TemplateData
            {
                Id = Guid.NewGuid().ToString(),
                Name = templateName,
                Nodes = selectedNodes.Select(n => Helpers.JsonHelper.Deserialize<Models.NodeData>(
                    Helpers.JsonHelper.Serialize(n))!).ToList(),
                Paths = internalPaths.Select(p => Helpers.JsonHelper.Deserialize<Models.PathData>(
                    Helpers.JsonHelper.Serialize(p))!).ToList(),
                Groups = internalGroups.Select(g => Helpers.JsonHelper.Deserialize<Models.GroupData>(
                    Helpers.JsonHelper.Serialize(g))!).ToList()
            };

            _layout.Templates.Add(template);
            RefreshTemplateList();
            
            var pathInfo = internalPaths.Count > 0 ? $", {internalPaths.Count} paths" : "";
            var cellInfo = internalGroups.Count > 0 ? $", {internalGroups.Count} cells" : "";
            StatusText.Text = $"Created template '{templateName}' with {selectedNodes.Count} nodes{pathInfo}{cellInfo}";
        }

        private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (TemplateList?.SelectedItem is not ListBoxItem item) return;

            var templateId = item.Tag?.ToString();
            var template = _layout.Templates.FirstOrDefault(t => t.Id == templateId);

            if (template != null)
            {
                _layout.Templates.Remove(template);
                RefreshTemplateList();
                StatusText.Text = $"Deleted template '{template.Name}'";
            }
        }

        #endregion
    }
}
