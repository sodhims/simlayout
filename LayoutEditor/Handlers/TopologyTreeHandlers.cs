using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Topology Tree

        private void RefreshElementList()
        {
            if (TopologyTree == null) return;

            TopologyTree.Items.Clear();

            // Nodes by type
            var nodesByType = _layout.Nodes.GroupBy(n => n.Type ?? "unknown");

            foreach (var group in nodesByType.OrderBy(g => g.Key))
            {
                var typeItem = new TreeViewItem
                {
                    Header = $"{group.Key} ({group.Count()})",
                    IsExpanded = true
                };

                foreach (var node in group.OrderBy(n => n.Name))
                {
                    var nodeItem = new TreeViewItem
                    {
                        Header = node.Name,
                        Tag = node.Id
                    };
                    typeItem.Items.Add(nodeItem);
                }

                TopologyTree.Items.Add(typeItem);
            }

            // Paths
            if (_layout.Paths.Count > 0)
            {
                var pathsItem = new TreeViewItem
                {
                    Header = $"Paths ({_layout.Paths.Count})",
                    IsExpanded = false
                };

                foreach (var path in _layout.Paths)
                {
                    var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                    var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);

                    var pathItem = new TreeViewItem
                    {
                        Header = $"{fromNode?.Name ?? "?"} → {toNode?.Name ?? "?"}",
                        Tag = $"path:{path.Id}"
                    };
                    pathsItem.Items.Add(pathItem);
                }

                TopologyTree.Items.Add(pathsItem);
            }

            // Groups/Cells
            if (_layout.Groups.Count > 0)
            {
                var groupsItem = new TreeViewItem
                {
                    Header = $"Groups ({_layout.Groups.Count})",
                    IsExpanded = true
                };

                foreach (var group in _layout.Groups)
                {
                    var prefix = group.IsCell ? "[Cell] " : "";
                    var groupItem = new TreeViewItem
                    {
                        Header = $"{prefix}{group.Name} ({group.Members.Count})",
                        Tag = $"group:{group.Id}"
                    };
                    groupsItem.Items.Add(groupItem);
                }

                TopologyTree.Items.Add(groupsItem);
            }
        }

        private void TopologyTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_layout == null) return;  // Not initialized yet
            if (TopologyTree?.SelectedItem is not TreeViewItem item) return;

            var tag = item.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            if (tag.StartsWith("path:"))
            {
                _selectionService.SelectPath(tag.Substring(5));
            }
            else if (tag.StartsWith("group:"))
            {
                var groupId = tag.Substring(6);
                var group = _layout.Groups.FirstOrDefault(g => g.Id == groupId);
                if (group != null)
                {
                    if (group.IsCell)
                    {
                        SelectCellWithPaths(group);
                    }
                    else
                    {
                        _selectionService.SelectGroup(group.Id, group.Members);
                    }
                }
            }
            else
            {
                _selectionService.SelectNode(tag);
            }

            UpdateSelectionVisuals();
            UpdatePropertyPanel();
        }

        private void RefreshTopology_Click(object sender, RoutedEventArgs e)
        {
            RefreshElementList();
        }

        private void ExpandAllTopology_Click(object sender, RoutedEventArgs e)
        {
            SetAllExpanded(TopologyTree, true);
        }

        private void CollapseAllTopology_Click(object sender, RoutedEventArgs e)
        {
            SetAllExpanded(TopologyTree, false);
        }

        private void SetAllExpanded(ItemsControl control, bool expanded)
        {
            foreach (var item in control.Items)
            {
                if (item is TreeViewItem tvi)
                {
                    tvi.IsExpanded = expanded;
                    SetAllExpanded(tvi, expanded);
                }
            }
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectionService.ClearSelection();
            UpdateSelectionVisuals();
            UpdatePropertyPanel();
            StatusText.Text = "Selection cleared";
        }

        #endregion
    }
}
