using System;
using System.Windows;
using System.Windows.Controls;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for drawing style settings (wall type, units, grid)
    /// Simplified - layers removed
    /// </summary>
    public partial class MainWindow
    {
        #region Wall Type
        
        public string GetCurrentWallType()
        {
            if (WallTypeCombo?.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "Solid";
            }
            return "Solid";
        }
        
        #endregion
        
        #region Units
        
        public string GetCurrentUnits()
        {
            if (UnitsCombo?.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "m";
            }
            return "m";
        }
        
        public double GetPixelsPerUnit()
        {
            var units = GetCurrentUnits();
            return units switch
            {
                "ft" => 20.0 / 0.3048,  // ~65.6 pixels per foot
                "px" => 1.0,
                _ => 20.0  // 20 pixels per meter (default)
            };
        }
        
        #endregion
        
        #region Grid
        
        public int GetCurrentGridSize()
        {
            if (GridSizeCombo?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (int.TryParse(item.Tag.ToString(), out int size))
                    return size;
            }
            return 20;
        }
        
        #endregion
        
        #region Update Status Display
        
        private void UpdateNodeCountDisplay()
        {
            if (_layout == null || NodeCountText == null) return;
            
            var nodeCount = _layout.Nodes.Count;
            var pathCount = _layout.Paths.Count;
            var wallCount = _layout.Walls.Count;
            
            NodeCountText.Text = $"Nodes: {nodeCount}  Paths: {pathCount}  Walls: {wallCount}";
            
            if (NodeCountLabel != null)
                NodeCountLabel.Text = $" ({nodeCount} nodes)";
        }
        
        private void UpdateZoomDisplay()
        {
            if (ZoomText != null)
            {
                ZoomText.Text = $"{_currentZoom * 100:F0}%";
            }
        }
        
        #endregion
    }
}
