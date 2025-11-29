using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Node Property Events

        private void PropNodeType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            if (PropNodeType.SelectedItem is ComboBoxItem item)
            {
                SaveUndoState();
                node.Type = item.Tag?.ToString() ?? NodeTypes.Machine;
                MarkDirty();
                Redraw();
            }
        }

        private void PropNode_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            node.Name = PropNodeName.Text;
            node.Label = PropNodeLabel.Text;
            MarkDirty();
            Redraw();
        }

        private void PropNodePosition_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            if (double.TryParse(PropNodeX.Text, out double x))
                node.Visual.X = x;
            if (double.TryParse(PropNodeY.Text, out double y))
                node.Visual.Y = y;

            MarkDirty();
            Redraw();
        }

        private void PropNodeSize_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            if (double.TryParse(PropNodeWidth.Text, out double w) && w > 0)
                node.Visual.Width = w;
            if (double.TryParse(PropNodeHeight.Text, out double h) && h > 0)
                node.Visual.Height = h;

            MarkDirty();
            Redraw();
        }

        private void PropNodeVisual_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            if (double.TryParse(PropNodeRotation.Text, out double rot))
                node.Visual.Rotation = rot;

            MarkDirty();
            Redraw();
        }

        private void PropNodeVisual_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            if (PropNodeIcon.SelectedItem is ComboBoxItem iconItem)
            {
                var tag = iconItem.Tag?.ToString();
                if (tag == "__browse__")
                {
                    BrowseIcon_Click(sender, new RoutedEventArgs());
                    return;
                }
                node.Visual.Icon = tag ?? "machine_generic";
            }

            if (PropLabelPosition.SelectedItem is ComboBoxItem posItem)
            {
                node.Visual.LabelPosition = posItem.Content?.ToString()?.ToLower() ?? "bottom";
            }

            MarkDirty();
            Redraw();
        }

        private void PropNodeColor_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(PropNodeColor.Text);
                node.Visual.Color = PropNodeColor.Text;
                PropNodeColorPreview.Background = new SolidColorBrush(color);
                MarkDirty();
                Redraw();
            }
            catch { }
        }

        #endregion

        #region Path Property Events

        private void PropPath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var path = _selectionService.GetSelectedPath(_layout);
            if (path == null) return;

            SaveUndoState();

            if (PropPathFrom.SelectedItem is ComboBoxItem fromItem)
                path.From = fromItem.Tag?.ToString() ?? "";
            if (PropPathTo.SelectedItem is ComboBoxItem toItem)
                path.To = toItem.Tag?.ToString() ?? "";
            if (PropPathType.SelectedItem is ComboBoxItem typeItem)
                path.PathType = typeItem.Tag?.ToString() ?? PathTypes.Single;
            if (PropPathRouting.SelectedItem is ComboBoxItem routeItem)
                path.RoutingMode = routeItem.Tag?.ToString() ?? RoutingModes.Direct;
            if (PropTransportType.SelectedItem is ComboBoxItem transItem)
                path.Simulation.TransportType = transItem.Content?.ToString()?.ToLower() ?? TransportTypes.Conveyor;

            MarkDirty();
            Redraw();
        }

        private void PropPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _isUpdatingProperties) return;
            var path = _selectionService.GetSelectedPath(_layout);
            if (path == null) return;

            path.Visual.Color = PropPathColor.Text;

            if (double.TryParse(PropPathSpeed.Text, out double speed))
                path.Simulation.Speed = speed;
            if (int.TryParse(PropPathCapacity.Text, out int cap))
                path.Simulation.Capacity = cap;

            MarkDirty();
            Redraw();
        }

        #endregion

        #region Group Property Events

        private void PropGroup_Changed(object sender, TextChangedEventArgs e)
        {
            if (_selectionService == null || _layout == null) return;
            if (_isUpdatingProperties || _selectionService.SelectedGroupId == null) return;

            var group = _layout.Groups.FirstOrDefault(g => g.Id == _selectionService.SelectedGroupId);
            if (group == null) return;

            SaveUndoState();
            group.Name = PropGroupName.Text;
            MarkDirty();
            RefreshElementList();
        }

        private void PropGroupIsCell_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectionService == null || _layout == null) return;
            if (_isUpdatingProperties || _selectionService.SelectedGroupId == null) return;

            var group = _layout.Groups.FirstOrDefault(g => g.Id == _selectionService.SelectedGroupId);
            if (group == null) return;

            SaveUndoState();
            group.IsCell = PropGroupIsCell.IsChecked ?? false;
            PropCellInputPos.IsEnabled = group.IsCell;
            PropCellOutputPos.IsEnabled = group.IsCell;
            MarkDirty();
            Redraw();
        }

        private void PropCellTerminal_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_selectionService == null || _layout == null) return;
            if (_isUpdatingProperties || _selectionService.SelectedGroupId == null) return;

            var group = _layout.Groups.FirstOrDefault(g => g.Id == _selectionService.SelectedGroupId);
            if (group == null) return;

            SaveUndoState();
            if (PropCellInputPos.SelectedItem is ComboBoxItem inItem)
                group.InputTerminalPosition = inItem.Tag?.ToString() ?? "left";
            if (PropCellOutputPos.SelectedItem is ComboBoxItem outItem)
                group.OutputTerminalPosition = outItem.Tag?.ToString() ?? "right";
            MarkDirty();
            Redraw();
        }

        #endregion
    }
}
