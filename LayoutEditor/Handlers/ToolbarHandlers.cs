using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Toolbar Operations

        private string _currentTool = "select";
        private bool _isPathEditMode = false;

        private void Tool_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectionService == null) return;  // Not initialized yet
            
            if (sender is ToggleButton btn && btn.Tag is string tool)
            {
                SetTool(tool);
            }
        }

        private void SetTool(string tool)
        {
            _currentTool = tool;

            // Update cursor
            if (EditorCanvas != null)
            {
                EditorCanvas.Cursor = tool switch
                {
                    "pan" => System.Windows.Input.Cursors.Hand,
                    "path" => System.Windows.Input.Cursors.Cross,
                    "zone" => System.Windows.Input.Cursors.Cross,
                    "wall" => System.Windows.Input.Cursors.Cross,
                    "column" => System.Windows.Input.Cursors.Cross,
                    "measure" => System.Windows.Input.Cursors.Cross,
                    _ => System.Windows.Input.Cursors.Arrow
                };
            }

            // Update radio buttons
            if (SelectTool != null) SelectTool.IsChecked = (tool == "select");
            if (MoveTool != null) MoveTool.IsChecked = (tool == "move");
            if (PanTool != null) PanTool.IsChecked = (tool == "pan");
            if (PathTool != null) PathTool.IsChecked = (tool == "path");
            if (CorridorTool != null) CorridorTool.IsChecked = (tool == "corridor");
            if (ZoneTool != null) ZoneTool.IsChecked = (tool == "zone");
            if (WallTool != null) WallTool.IsChecked = (tool == "wall");
            if (ColumnTool != null) ColumnTool.IsChecked = (tool == "column");
            if (MeasureTool != null) MeasureTool.IsChecked = (tool == "measure");

            // Update status
            if (ModeText != null)
                ModeText.Text = $"Mode: {char.ToUpper(tool[0]) + tool.Substring(1)}";

            // Show tool-specific hints
            if (StatusText != null)
            {
                StatusText.Text = tool switch
                {
                    "wall" => "Wall tool: Click to start, click again to end (Shift for H/V constraint)",
                    "column" => "Column tool: Click to place column (Shift for round)",
                    "measure" => "Measure tool: Click two points to measure distance",
                    _ => ""
                };
            }

            // Reset state
            if (tool != "path")
            {
                _pathStartNodeId = null;
                _isDrawingPath = false;
            }
        }

        private void PathEditMode_Changed(object sender, RoutedEventArgs e)
        {
            _isPathEditMode = PathEditMode.IsChecked == true;
            
            if (_isPathEditMode)
            {
                StatusText.Text = "Path Edit Mode: Click path to select, click again to add waypoint, drag to adjust";
                ModeText.Text = "Mode: Path Edit";
                EditorCanvas.Cursor = System.Windows.Input.Cursors.Cross;
            }
            else
            {
                StatusText.Text = "Path Edit Mode disabled";
                ModeText.Text = "Mode: Select";
                EditorCanvas.Cursor = System.Windows.Input.Cursors.Arrow;
            }
            
            Redraw(); // Redraw to show/hide all waypoint handles
        }

        private void UpdateToolbarState()
        {
            if (_selectionService == null) return;
            
            // Enable alignment for multiple nodes OR multiple groups
            var canAlign = _selectionService.SelectedCount >= 2 || _selectionService.HasMultipleGroups;
            var canDistribute = _selectionService.SelectedCount >= 3;

            // Try to update alignment buttons if they exist
            SetButtonEnabled("AlignLeftBtn", canAlign);
            SetButtonEnabled("AlignCenterHBtn", canAlign);
            SetButtonEnabled("AlignRightBtn", canAlign);
            SetButtonEnabled("AlignTopBtn", canAlign);
            SetButtonEnabled("AlignCenterVBtn", canAlign);
            SetButtonEnabled("AlignBottomBtn", canAlign);
            SetButtonEnabled("DistributeHBtn", canDistribute);
            SetButtonEnabled("DistributeVBtn", canDistribute);
        }

        private void SetButtonEnabled(string name, bool enabled)
        {
            if (FindName(name) is Button btn)
                btn.IsEnabled = enabled;
        }

        #endregion
    }
}
