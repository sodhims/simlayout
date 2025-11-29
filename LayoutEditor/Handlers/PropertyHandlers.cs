using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Property Panel Updates

        private bool _isUpdatingProperties;

        private void UpdatePropertyPanel()
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            _isUpdatingProperties = true;

            try
            {
                HideAllPropertyPanels();

                if (!_selectionService.HasSelection && _selectionService.SelectedGroupId == null)
                {
                    if (NoSelectionText != null)
                        NoSelectionText.Visibility = Visibility.Visible;
                }
                else if (_selectionService.SelectedGroupId != null)
                {
                    var group = _layout?.Groups.FirstOrDefault(g => g.Id == _selectionService.SelectedGroupId);
                    if (group != null) ShowGroupProperties(group);
                }
                else if (_selectionService.HasMultipleSelection)
                {
                    ShowMultiSelectPanel();
                }
                else if (_selectionService.HasSingleNodeSelection)
                {
                    var node = _selectionService.GetSelectedNode(_layout);
                    if (node != null) ShowNodeProperties(node);
                }
                else if (_selectionService.SelectedPathId != null)
                {
                    var path = _selectionService.GetSelectedPath(_layout);
                    if (path != null) ShowPathProperties(path);
                }
            }
            finally
            {
                _isUpdatingProperties = false;
            }
        }

        private void HideAllPropertyPanels()
        {
            NoSelectionText.Visibility = Visibility.Collapsed;
            NodePropertiesPanel.Visibility = Visibility.Collapsed;
            PathPropertiesPanel.Visibility = Visibility.Collapsed;
            GroupPropertiesPanel.Visibility = Visibility.Collapsed;
            MultiSelectPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowMultiSelectPanel()
        {
            MultiSelectPanel.Visibility = Visibility.Visible;
            MultiSelectCount.Text = $"{_selectionService.SelectedCount} nodes selected";
        }

        private void ShowNodeProperties(NodeData node)
        {
            NodePropertiesPanel.Visibility = Visibility.Visible;

            PropNodeId.Text = node.Id;
            PropNodeName.Text = node.Name;
            PropNodeLabel.Text = node.Label;

            // Position & size
            PropNodeX.Text = node.Visual.X.ToString("F1");
            PropNodeY.Text = node.Visual.Y.ToString("F1");
            PropNodeWidth.Text = node.Visual.Width.ToString("F1");
            PropNodeHeight.Text = node.Visual.Height.ToString("F1");
            PropNodeRotation.Text = node.Visual.Rotation.ToString("F1");

            // Visual
            PopulateIconDropdown(node);
            PropNodeColor.Text = node.Visual.Color;
            UpdateColorPreview(node.Visual.Color);

            // Type dropdown
            SelectComboItemByTag(PropNodeType, node.Type);
            SelectComboItemByContent(PropLabelPosition, node.Visual.LabelPosition ?? "bottom");
        }

        private void ShowPathProperties(PathData path)
        {
            PathPropertiesPanel.Visibility = Visibility.Visible;

            PropPathId.Text = path.Id;
            PropPathColor.Text = path.Visual.Color;
            PropPathSpeed.Text = path.Simulation.Speed.ToString("F2");
            PropPathCapacity.Text = path.Simulation.Capacity.ToString();
            PropPathDistance.Text = path.Simulation.Distance?.ToString("F2") ?? "";

            // Populate From/To dropdowns
            PopulateNodeDropdowns();

            SelectComboItemByTag(PropPathFrom, path.From);
            SelectComboItemByTag(PropPathTo, path.To);
            SelectComboItemByTag(PropPathType, path.PathType);
            SelectComboItemByTag(PropPathRouting, path.RoutingMode);
            SelectComboItemByContent(PropTransportType, path.Simulation.TransportType);
        }

        private void ShowGroupProperties(GroupData group)
        {
            GroupPropertiesPanel.Visibility = Visibility.Visible;

            PropGroupId.Text = group.Id;
            PropGroupName.Text = group.Name;
            PropGroupIsCell.IsChecked = group.IsCell;
            PropGroupMembers.Text = $"{group.Members.Count} members";
            
            // Terminal positions (only relevant for cells)
            SelectComboItemByTag(PropCellInputPos, group.InputTerminalPosition ?? "left");
            SelectComboItemByTag(PropCellOutputPos, group.OutputTerminalPosition ?? "right");
            PropCellInputPos.IsEnabled = group.IsCell;
            PropCellOutputPos.IsEnabled = group.IsCell;
        }

        private void PopulateIconDropdown(NodeData node)
        {
            PropNodeIcon.Items.Clear();

            var suggestedIcons = IconLibrary.GetSuggestedIcons(node.Type ?? NodeTypes.Machine).ToArray();
            int selectedIndex = 0;

            for (int i = 0; i < suggestedIcons.Length; i++)
            {
                var iconKey = suggestedIcons[i];
                if (IconLibrary.Icons.TryGetValue(iconKey, out var iconDef))
                {
                    PropNodeIcon.Items.Add(new ComboBoxItem
                    {
                        Content = iconDef.Name,
                        Tag = iconKey
                    });

                    if (iconKey == node.Visual.Icon)
                        selectedIndex = i;
                }
            }

            PropNodeIcon.Items.Add(new Separator());
            PropNodeIcon.Items.Add(new ComboBoxItem { Content = "Browse All...", Tag = "__browse__" });
            PropNodeIcon.SelectedIndex = selectedIndex;
        }

        private void PopulateNodeDropdowns()
        {
            PropPathFrom.Items.Clear();
            PropPathTo.Items.Clear();

            foreach (var node in _layout.Nodes)
            {
                PropPathFrom.Items.Add(new ComboBoxItem { Content = node.Name, Tag = node.Id });
                PropPathTo.Items.Add(new ComboBoxItem { Content = node.Name, Tag = node.Id });
            }
        }

        private void UpdateColorPreview(string color)
        {
            try
            {
                PropNodeColorPreview.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color));
            }
            catch
            {
                PropNodeColorPreview.Background = Brushes.Gray;
            }
        }

        private void SelectComboItemByTag(ComboBox combo, string? tag)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tag)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SelectComboItemByContent(ComboBox combo, string? content)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString()?.Equals(content, StringComparison.OrdinalIgnoreCase) == true)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        #endregion
    }
}
