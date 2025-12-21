using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for path waypoint editing
    /// </summary>
    public partial class MainWindow
    {
        #region Waypoint Handle Click

        private bool CheckWaypointHandleClick(Point pos, MouseButtonEventArgs e)
        {
            // Use visual tree hit testing to find waypoint handles
            HitTestResult? result = null;
            VisualTreeHelper.HitTest(EditorCanvas,
                null,
                r => { result = r; return HitTestResultBehavior.Stop; },
                new PointHitTestParameters(pos));

            if (result?.VisualHit is Ellipse ellipse && ellipse.Tag is string tag)
            {
                var parts = tag.Split(':');
                if (parts.Length >= 3 && parts[0] == "waypoint")
                {
                    // Existing waypoint - start dragging
                    _draggingPathId = parts[1];
                    _draggingWaypointIndex = int.Parse(parts[2]);
                    _isDraggingWaypoint = true;
                    SaveUndoState();
                    EditorCanvas.CaptureMouse();
                    StatusText.Text = "Drag to move waypoint, or right-click to delete";
                    return true;
                }
                else if (parts.Length >= 3 && parts[0] == "addwaypoint")
                {
                    // Add new waypoint at this position
                    var pathId = parts[1];
                    var segmentIndex = int.Parse(parts[2]);
                    var path = _layout.Paths.FirstOrDefault(p => p.Id == pathId);
                    if (path != null)
                    {
                        SaveUndoState();
                        path.Visual.Waypoints.Insert(segmentIndex, new PointData(pos.X, pos.Y));
                        _draggingPathId = pathId;
                        _draggingWaypointIndex = segmentIndex;
                        _isDraggingWaypoint = true;
                        EditorCanvas.CaptureMouse();
                        MarkDirty();
                        Redraw();
                        StatusText.Text = "Waypoint added - drag to position";
                    }
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Waypoint Right Click

        private bool CheckWaypointRightClick(Point pos)
        {
            HitTestResult? result = null;
            VisualTreeHelper.HitTest(EditorCanvas,
                null,
                r => { result = r; return HitTestResultBehavior.Stop; },
                new PointHitTestParameters(pos));

            if (result?.VisualHit is Ellipse ellipse && ellipse.Tag is string tag)
            {
                var parts = tag.Split(':');
                if (parts.Length >= 3 && parts[0] == "waypoint")
                {
                    var pathId = parts[1];
                    var waypointIndex = int.Parse(parts[2]);
                    var path = _layout.Paths.FirstOrDefault(p => p.Id == pathId);
                    
                    if (path != null && waypointIndex < path.Visual.Waypoints.Count)
                    {
                        ShowWaypointContextMenu(path, waypointIndex);
                        return true;
                    }
                }
            }
            return false;
        }

        private void ShowWaypointContextMenu(PathData path, int waypointIndex)
        {
            var menu = new ContextMenu();
            
            var deleteItem = new MenuItem { Header = "Delete Waypoint" };
            deleteItem.Click += (s, ev) => {
                SaveUndoState();
                path.Visual.Waypoints.RemoveAt(waypointIndex);
                MarkDirty();
                Redraw();
                StatusText.Text = "Waypoint deleted";
            };
            menu.Items.Add(deleteItem);
            
            var clearItem = new MenuItem { Header = "Clear All Waypoints" };
            clearItem.Click += (s, ev) => {
                SaveUndoState();
                path.Visual.Waypoints.Clear();
                MarkDirty();
                Redraw();
                StatusText.Text = "All waypoints cleared";
            };
            menu.Items.Add(clearItem);
            
            menu.IsOpen = true;
        }

        #endregion
    }
}
