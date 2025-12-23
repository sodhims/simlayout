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

        /// <summary>
        /// Updates the property display based on current selection.
        /// In Design Mode, automatically shows properties for selected entities.
        /// </summary>
        private void UpdatePropertyPanel()
        {
            // In Design Mode, automatically show properties panel for selected entities
            if (_layout?.DesignMode == true && _panelManager != null)
            {
                // Check for selected node
                var selectedNode = _selectionService.GetSelectedNode(_layout);
                if (selectedNode != null)
                {
                    _panelManager.ShowNodeProperties(selectedNode);
                    return;
                }

                // Check for selected path
                var selectedPathId = _selectionService.SelectedPathId;
                if (!string.IsNullOrEmpty(selectedPathId))
                {
                    var selectedPath = _layout.Paths.FirstOrDefault(p => p.Id == selectedPathId);
                    if (selectedPath != null)
                    {
                        _panelManager.ShowPathProperties(selectedPath);
                        return;
                    }
                }

                // Check for selected group
                if (_selectionService.SelectedGroupIds.Count == 1)
                {
                    var groupId = _selectionService.SelectedGroupIds.First();
                    var selectedGroup = _layout.Groups.FirstOrDefault(g => g.Id == groupId);
                    if (selectedGroup != null)
                    {
                        _panelManager.ShowGroupProperties(selectedGroup);
                        return;
                    }
                }

                // Check for selected wall
                if (_selectedWallIds.Count == 1)
                {
                    var wall = _layout.Walls.FirstOrDefault(w => w.Id == _selectedWallIds[0]);
                    if (wall != null)
                    {
                        _panelManager.ShowWallProperties(wall);
                        return;
                    }
                }

                // No single selection - clear panel if visible
                _panelManager.ClearSelection();
            }
        }

        /// <summary>
        /// Show properties for a specific node in the floating panel
        /// </summary>
        public void ShowNodePropertiesInPanel(NodeData node)
        {
            _panelManager?.ShowNodeProperties(node);
        }

        /// <summary>
        /// Show properties for a specific path in the floating panel
        /// </summary>
        public void ShowPathPropertiesInPanel(PathData path)
        {
            _panelManager?.ShowPathProperties(path);
        }

        /// <summary>
        /// Show properties for a group/cell in the floating panel
        /// </summary>
        public void ShowGroupPropertiesInPanel(GroupData group)
        {
            _panelManager?.ShowGroupProperties(group);
        }

        /// <summary>
        /// Clear the floating properties panel selection
        /// </summary>
        public void ClearPropertiesPanel()
        {
            _panelManager?.ClearSelection();
        }

        #endregion
    }
}
