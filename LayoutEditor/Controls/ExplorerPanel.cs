using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Floating explorer panel showing object hierarchy with inputs/outputs
    /// </summary>
    public class ExplorerPanel : FloatingPanel
    {
        private TreeView _treeView = null!;
        private LayoutData? _currentLayout;
        
        public event Action<NodeData>? NodeSelected;
        public event Action<PathData>? PathSelected;
        public event Action<GroupData>? GroupSelected;
        
        public ExplorerPanel()
        {
            Title = "Explorer";
            Width = 280;
            Height = 450;
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Header
            var header = CreateHeader("Explorer");
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Search box
            var searchPanel = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(6, 4, 6, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCE, 0xDB)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var searchBox = new TextBox
            {
                Height = 22,
                FontSize = 8
            };
            searchBox.GotFocus += (s, e) => { if (searchBox.Text == "Search...") searchBox.Text = ""; };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrEmpty(searchBox.Text)) searchBox.Text = "Search..."; };
            searchBox.Text = "Search...";
            searchBox.Foreground = Brushes.Gray;
            searchBox.TextChanged += (s, e) =>
            {
                if (searchBox.Text != "Search...")
                {
                    searchBox.Foreground = Brushes.Black;
                    FilterTree(searchBox.Text);
                }
            };
            searchPanel.Child = searchBox;
            Grid.SetRow(searchPanel, 1);
            grid.Children.Add(searchPanel);
            
            // Tree view
            _treeView = new TreeView
            {
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 8
            };
            _treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
            Grid.SetRow(_treeView, 2);
            grid.Children.Add(_treeView);
            
            // Toolbar
            var toolbar = CreateToolbar();
            Grid.SetRow(toolbar, 3);
            grid.Children.Add(toolbar);
            
            Content = grid;
        }
        
        private Border CreateToolbar()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCE, 0xDB)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(4)
            };
            
            var panel = new WrapPanel();
            
            var refreshBtn = new Button { Content = "🔄 Refresh", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), FontSize = 8 };
            refreshBtn.Click += (s, e) => RefreshTree();
            panel.Children.Add(refreshBtn);
            
            var expandBtn = new Button { Content = "➕ Expand All", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), FontSize = 8 };
            expandBtn.Click += (s, e) => ExpandAll();
            panel.Children.Add(expandBtn);
            
            var collapseBtn = new Button { Content = "➖ Collapse All", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), FontSize = 8 };
            collapseBtn.Click += (s, e) => CollapseAll();
            panel.Children.Add(collapseBtn);
            
            border.Child = panel;
            return border;
        }
        
        public void LoadLayout(LayoutData layout)
        {
            _currentLayout = layout;
            RefreshTree();
        }
        
        public void RefreshTree()
        {
            _treeView.Items.Clear();
            if (_currentLayout == null) return;
            
            // Layout root
            var layoutNode = new TreeViewItem
            {
                Header = CreateTreeHeader("📋", _currentLayout.Metadata?.Name ?? "Layout"),
                IsExpanded = true
            };
            
            // Cells (Groups marked as cells)
            var cellsNode = new TreeViewItem
            {
                Header = CreateTreeHeader("🏭", "Cells"),
                IsExpanded = true
            };
            
            var cells = _currentLayout.Groups?.Where(g => g.IsCell).ToList() ?? new List<GroupData>();
            foreach (var cell in cells)
            {
                var cellItem = CreateCellTreeItem(cell);
                cellsNode.Items.Add(cellItem);
            }
            
            if (cells.Count > 0)
                layoutNode.Items.Add(cellsNode);
            
            // Nodes by type
            var nodesNode = new TreeViewItem
            {
                Header = CreateTreeHeader("⚙️", $"Nodes ({_currentLayout.Nodes?.Count ?? 0})"),
                IsExpanded = true
            };
            
            // Group nodes by type
            var nodesByType = _currentLayout.Nodes?.GroupBy(n => n.Type ?? "unknown") ?? Enumerable.Empty<IGrouping<string, NodeData>>();
            foreach (var group in nodesByType.OrderBy(g => g.Key))
            {
                var typeNode = new TreeViewItem
                {
                    Header = CreateTreeHeader(GetNodeIcon(group.Key), $"{group.Key} ({group.Count()})"),
                    IsExpanded = false
                };
                
                foreach (var node in group.OrderBy(n => n.Name))
                {
                    var nodeItem = CreateNodeTreeItem(node);
                    typeNode.Items.Add(nodeItem);
                }
                
                nodesNode.Items.Add(typeNode);
            }
            
            layoutNode.Items.Add(nodesNode);
            
            // Paths
            var pathsNode = new TreeViewItem
            {
                Header = CreateTreeHeader("↔️", $"Paths ({_currentLayout.Paths?.Count ?? 0})"),
                IsExpanded = false
            };
            
            foreach (var path in _currentLayout.Paths ?? Enumerable.Empty<PathData>())
            {
                var fromNode = _currentLayout.Nodes?.FirstOrDefault(n => n.Id == path.From);
                var toNode = _currentLayout.Nodes?.FirstOrDefault(n => n.Id == path.To);
                
                var pathItem = new TreeViewItem
                {
                    Header = $"➡️ {fromNode?.Name ?? "?"} → {toNode?.Name ?? "?"}",
                    Tag = path
                };
                pathsNode.Items.Add(pathItem);
            }
            
            layoutNode.Items.Add(pathsNode);
            
            // Walls
            if (_currentLayout.Walls?.Count > 0)
            {
                var wallsNode = new TreeViewItem
                {
                    Header = CreateTreeHeader("▬", $"Walls ({_currentLayout.Walls.Count})"),
                    IsExpanded = false
                };
                layoutNode.Items.Add(wallsNode);
            }
            
            _treeView.Items.Add(layoutNode);
        }
        
        private TreeViewItem CreateCellTreeItem(GroupData cell)
        {
            var cellItem = new TreeViewItem
            {
                Header = CreateTreeHeader("🏭", cell.Name ?? "Cell"),
                Tag = cell,
                IsExpanded = true
            };
            
            // Entry points
            if (cell.EntryPoints?.Count > 0)
            {
                var entryNode = new TreeViewItem
                {
                    Header = CreateTreeHeader("📥", $"Entry Points ({cell.EntryPoints.Count})"),
                    IsExpanded = false
                };
                cellItem.Items.Add(entryNode);
            }
            
            // Members
            var membersNode = new TreeViewItem
            {
                Header = CreateTreeHeader("📦", $"Members ({cell.Members?.Count ?? 0})"),
                IsExpanded = true
            };
            
            foreach (var memberId in cell.Members ?? Enumerable.Empty<string>())
            {
                var node = _currentLayout?.Nodes?.FirstOrDefault(n => n.Id == memberId);
                if (node != null)
                {
                    var nodeItem = CreateNodeTreeItem(node);
                    membersNode.Items.Add(nodeItem);
                }
            }
            
            cellItem.Items.Add(membersNode);
            
            // Exit points
            if (cell.ExitPoints?.Count > 0)
            {
                var exitNode = new TreeViewItem
                {
                    Header = CreateTreeHeader("📤", $"Exit Points ({cell.ExitPoints.Count})"),
                    IsExpanded = false
                };
                cellItem.Items.Add(exitNode);
            }
            
            return cellItem;
        }
        
        private TreeViewItem CreateNodeTreeItem(NodeData node)
        {
            var nodeItem = new TreeViewItem
            {
                Header = CreateTreeHeader(GetNodeIcon(node.Type), node.Name ?? node.Type ?? "Node"),
                Tag = node
            };
            
            // Incoming paths
            var incoming = _currentLayout?.Paths?.Where(p => p.To == node.Id).ToList();
            if (incoming?.Count > 0)
            {
                var inNode = new TreeViewItem
                {
                    Header = CreateTreeHeader("📥", $"Inputs ({incoming.Count})"),
                    Foreground = Brushes.Green
                };
                foreach (var path in incoming)
                {
                    var fromNode = _currentLayout?.Nodes?.FirstOrDefault(n => n.Id == path.From);
                    inNode.Items.Add(new TreeViewItem { Header = $"← {fromNode?.Name ?? "?"}", Tag = path });
                }
                nodeItem.Items.Add(inNode);
            }
            
            // Outgoing paths
            var outgoing = _currentLayout?.Paths?.Where(p => p.From == node.Id).ToList();
            if (outgoing?.Count > 0)
            {
                var outNode = new TreeViewItem
                {
                    Header = CreateTreeHeader("📤", $"Outputs ({outgoing.Count})"),
                    Foreground = Brushes.Red
                };
                foreach (var path in outgoing)
                {
                    var toNode = _currentLayout?.Nodes?.FirstOrDefault(n => n.Id == path.To);
                    outNode.Items.Add(new TreeViewItem { Header = $"→ {toNode?.Name ?? "?"}", Tag = path });
                }
                nodeItem.Items.Add(outNode);
            }
            
            return nodeItem;
        }
        
        private StackPanel CreateTreeHeader(string icon, string text)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock { Text = icon, Margin = new Thickness(0, 0, 4, 0) });
            panel.Children.Add(new TextBlock { Text = text });
            return panel;
        }
        
        private string GetNodeIcon(string? type)
        {
            return type switch
            {
                "machine" => "⚙️",
                "source" => "📥",
                "sink" => "📤",
                "buffer" => "📦",
                "conveyor" => "➡️",
                "robot" => "🤖",
                "agv" => "🚗",
                "inspection" => "🔍",
                "assembly" => "🔧",
                "storage" => "🏭",
                _ => "⬜"
            };
        }
        
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
            {
                switch (item.Tag)
                {
                    case NodeData node:
                        NodeSelected?.Invoke(node);
                        break;
                    case PathData path:
                        PathSelected?.Invoke(path);
                        break;
                    case GroupData group:
                        GroupSelected?.Invoke(group);
                        break;
                }
            }
        }
        
        private void FilterTree(string filter)
        {
            // Simple filter - show/hide based on text match
            // For now, just refresh (could be optimized)
            if (string.IsNullOrWhiteSpace(filter) || filter == "Search...")
            {
                RefreshTree();
            }
        }
        
        private void ExpandAll()
        {
            foreach (TreeViewItem item in _treeView.Items)
                ExpandRecursive(item, true);
        }
        
        private void CollapseAll()
        {
            foreach (TreeViewItem item in _treeView.Items)
                ExpandRecursive(item, false);
        }
        
        private void ExpandRecursive(TreeViewItem item, bool expand)
        {
            item.IsExpanded = expand;
            foreach (TreeViewItem child in item.Items)
                ExpandRecursive(child, expand);
        }
    }
}
