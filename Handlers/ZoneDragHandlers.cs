using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private ZoneData? _draggingZone;
        private bool _isDraggingZone;
        private bool _isDraggingZoneVertex;
        private int _draggingZoneVertexIndex = -1;
        private Point _zoneDragStartPos;

        /// <summary>
        /// Handle click on zone interior - start dragging whole zone in design mode
        /// </summary>
        private void HandleZoneClick(ZoneData zone, MouseButtonEventArgs e)
        {
            DebugLogger.Log($"[Zone] Zone clicked: {zone.Name}");

            if (_layout == null) return;

            // Check if Spatial layer is locked
            if (_transportArchitectureLayerManager != null && _transportArchitectureLayerManager.IsLocked(LayerType.Spatial))
            {
                StatusText.Text = $"Zone '{zone.Name}' is on a locked layer (Spatial)";
                e.Handled = true;
                return;
            }

            // Only allow dragging in design mode
            if (_layout.DesignMode)
            {
                DebugLogger.Log($"[Zone] Starting zone drag for {zone.Name}");
                _draggingZone = zone;
                _isDraggingZone = true;
                _isDraggingZoneVertex = false;
                _zoneDragStartPos = e.GetPosition(EditorCanvas);
                EditorCanvas.CaptureMouse();
                SaveUndoState();

                StatusText.Text = $"Design Mode: Dragging zone '{zone.Name}'";
                e.Handled = true;
            }
            else
            {
                StatusText.Text = $"Zone '{zone.Name}' selected - Enable design mode (D) to drag";
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle click on zone vertex - start dragging vertex for reshaping
        /// </summary>
        private void HandleZoneVertexClick(ZoneData zone, int vertexIndex, MouseButtonEventArgs e)
        {
            DebugLogger.Log($"[Zone] Zone vertex {vertexIndex} clicked on {zone.Name}");

            if (_layout == null) return;

            // Check if Spatial layer is locked
            if (_transportArchitectureLayerManager != null && _transportArchitectureLayerManager.IsLocked(LayerType.Spatial))
            {
                StatusText.Text = $"Zone '{zone.Name}' is on a locked layer (Spatial)";
                e.Handled = true;
                return;
            }

            // Only allow in design mode
            if (_layout.DesignMode)
            {
                DebugLogger.Log($"[Zone] Starting vertex drag for {zone.Name}, vertex {vertexIndex}");
                _draggingZone = zone;
                _isDraggingZone = false;
                _isDraggingZoneVertex = true;
                _draggingZoneVertexIndex = vertexIndex;
                EditorCanvas.CaptureMouse();
                SaveUndoState();

                StatusText.Text = $"Design Mode: Reshaping zone '{zone.Name}' - dragging vertex {vertexIndex + 1}";
                e.Handled = true;
            }
            else
            {
                StatusText.Text = $"Zone vertex selected - Enable design mode (D) to reshape";
                e.Handled = true;
            }
        }

        /// <summary>
        /// Continue dragging zone (move whole zone)
        /// </summary>
        private void DragZone(Point currentPos)
        {
            if (!_isDraggingZone || _draggingZone == null || _layout == null) return;

            // Calculate delta from drag start
            double dx = currentPos.X - _zoneDragStartPos.X;
            double dy = currentPos.Y - _zoneDragStartPos.Y;

            DebugLogger.Log($"[Zone] Dragging zone by ({dx:F1}, {dy:F1})");

            // Move all vertices by the delta
            foreach (var pt in _draggingZone.Points)
            {
                pt.X += dx;
                pt.Y += dy;
            }

            // Update drag start position for next move
            _zoneDragStartPos = currentPos;

            MarkDirty();
            Redraw();
        }

        /// <summary>
        /// Continue dragging zone vertex (reshape zone)
        /// </summary>
        private void DragZoneVertex(Point currentPos)
        {
            if (!_isDraggingZoneVertex || _draggingZone == null || _draggingZoneVertexIndex < 0) return;
            if (_draggingZoneVertexIndex >= _draggingZone.Points.Count) return;

            DebugLogger.Log($"[Zone] Moving vertex {_draggingZoneVertexIndex} to ({currentPos.X:F1}, {currentPos.Y:F1})");

            // Move the specific vertex
            _draggingZone.Points[_draggingZoneVertexIndex].X = currentPos.X;
            _draggingZone.Points[_draggingZoneVertexIndex].Y = currentPos.Y;

            MarkDirty();
            Redraw();
        }

        /// <summary>
        /// End zone dragging
        /// </summary>
        private void EndZoneDrag()
        {
            if (_isDraggingZone || _isDraggingZoneVertex)
            {
                DebugLogger.Log($"[Zone] Ended zone drag");
                _draggingZone = null;
                _isDraggingZone = false;
                _isDraggingZoneVertex = false;
                _draggingZoneVertexIndex = -1;
                EditorCanvas.ReleaseMouseCapture();
                StatusText.Text = "Zone drag completed";
            }
        }

        /// <summary>
        /// Check if currently dragging a zone
        /// </summary>
        private bool IsDraggingZone => _isDraggingZone || _isDraggingZoneVertex;

        /// <summary>
        /// Show context menu for zone operations (add vertex, delete vertex, delete zone)
        /// </summary>
        private void ShowZoneContextMenu(ZoneData zone, Point clickPos)
        {
            var menu = new System.Windows.Controls.ContextMenu();

            // Find which edge was clicked (to insert vertex there)
            int edgeIndex = FindNearestZoneEdge(zone, clickPos);

            // Add vertex option
            var addVertexItem = new System.Windows.Controls.MenuItem { Header = $"Add Vertex Here (after point {edgeIndex + 1})" };
            addVertexItem.Click += (s, e) =>
            {
                SaveUndoState();
                // Insert new point after the edge start point
                var newPoint = new PointData(clickPos.X, clickPos.Y);
                zone.Points.Insert(edgeIndex + 1, newPoint);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Added vertex {edgeIndex + 2} to zone '{zone.Name}'";
            };
            menu.Items.Add(addVertexItem);

            // Delete vertex option (only if zone has more than 3 vertices)
            if (zone.Points.Count > 3)
            {
                // Find nearest vertex to click
                int nearestVertex = FindNearestZoneVertex(zone, clickPos);
                var deleteVertexItem = new System.Windows.Controls.MenuItem { Header = $"Delete Vertex {nearestVertex + 1}" };
                deleteVertexItem.Click += (s, e) =>
                {
                    SaveUndoState();
                    zone.Points.RemoveAt(nearestVertex);
                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Deleted vertex from zone '{zone.Name}'";
                };
                menu.Items.Add(deleteVertexItem);
            }

            menu.Items.Add(new System.Windows.Controls.Separator());

            // Delete zone option
            var deleteZoneItem = new System.Windows.Controls.MenuItem { Header = $"Delete Zone '{zone.Name}'" };
            deleteZoneItem.Click += (s, e) =>
            {
                SaveUndoState();
                _layout.Zones.Remove(zone);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted zone '{zone.Name}'";
            };
            menu.Items.Add(deleteZoneItem);

            menu.IsOpen = true;
        }

        /// <summary>
        /// Find the zone edge nearest to the click point
        /// </summary>
        private int FindNearestZoneEdge(ZoneData zone, Point clickPos)
        {
            if (zone.Points == null || zone.Points.Count < 2)
                return 0;

            int nearestEdge = 0;
            double minDist = double.MaxValue;

            for (int i = 0; i < zone.Points.Count; i++)
            {
                int j = (i + 1) % zone.Points.Count;
                var p1 = new Point(zone.Points[i].X, zone.Points[i].Y);
                var p2 = new Point(zone.Points[j].X, zone.Points[j].Y);

                double dist = ZoneDistanceToLineSegment(clickPos, p1, p2);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestEdge = i;
                }
            }

            return nearestEdge;
        }

        /// <summary>
        /// Find the zone vertex nearest to the click point
        /// </summary>
        private int FindNearestZoneVertex(ZoneData zone, Point clickPos)
        {
            if (zone.Points == null || zone.Points.Count == 0)
                return 0;

            int nearestVertex = 0;
            double minDist = double.MaxValue;

            for (int i = 0; i < zone.Points.Count; i++)
            {
                double dx = clickPos.X - zone.Points[i].X;
                double dy = clickPos.Y - zone.Points[i].Y;
                double dist = System.Math.Sqrt(dx * dx + dy * dy);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearestVertex = i;
                }
            }

            return nearestVertex;
        }

        /// <summary>
        /// Calculate distance from point to line segment (for zone edge detection)
        /// </summary>
        private double ZoneDistanceToLineSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return System.Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));

            double t = System.Math.Max(0, System.Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;

            return System.Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        /// <summary>
        /// Check for zone right-click and show context menu
        /// </summary>
        private bool CheckZoneRightClick(Point pos)
        {
            if (_layout == null || !_layout.DesignMode)
                return false;

            var hitResult = _hitTestService.HitTest(_layout, pos, _transportArchitectureLayerManager, false, true);

            if (hitResult.Type == HitType.Zone && hitResult.Zone != null)
            {
                ShowZoneContextMenu(hitResult.Zone, pos);
                return true;
            }

            if (hitResult.Type == HitType.ZoneVertex && hitResult.Zone != null)
            {
                ShowZoneContextMenu(hitResult.Zone, pos);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Show zone colors dialog from View menu
        /// </summary>
        private void ShowZoneColorsDialog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_layout == null) return;

            var dialog = new Dialogs.ZoneColorsDialog(_layout, () =>
            {
                MarkDirty();
                Redraw();
            });
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}
