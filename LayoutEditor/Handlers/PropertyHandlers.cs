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
        /// Since docked properties panel was removed, this now uses the floating panel manager.
        /// </summary>
        private void UpdatePropertyPanel()
        {
            // Property updates are now handled by the floating properties panel
            // which is shown via right-click context menu or the _panelManager
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
