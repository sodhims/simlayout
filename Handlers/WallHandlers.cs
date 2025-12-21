using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handles wall selection, dragging, editing, and context menus.
    /// Walls are line segments that define boundaries - organized for future routing.
    /// </summary>
    public partial class MainWindow
    {
        // Wall selection state
        private string? _selectedWallId;
        private List<string> _selectedWallIds = new();
        
        // Wall dragging state
        private bool _isDraggingWalls;
        private Point _wallDragStart;
        
        // Endpoint dragging state (for stretching walls)
        private bool _isDraggingWallEndpoint;
        private string? _draggingEndpointWallId;
        private bool _draggingStartEndpoint; // true = (X1,Y1), false = (X2,Y2)
        private const double EndpointHitTolerance = 10.0;
        private const double EndpointSnapTolerance = 15.0;

        #region Selection

        /// <summary>Check if any walls are selected</summary>
        public bool HasWallSelection => _selectedWallIds.Count > 0;

        /// <summary>Select a single wall by ID</summary>
        public void SelectWall(string wallId)
        {
            _selectedWallIds.Clear();
            _selectedWallIds.Add(wallId);
            _selectedWallId = wallId;
            
            _selectionService.ClearSelection();
            UpdateWallRendererSelection();
            
            var wall = _layout.Walls.FirstOrDefault(w => w.Id == wallId);
            if (wall != null)
            {
                _panelManager.ShowWallProperties(wall);
                StatusText.Text = $"Wall selected (Layer: {wall.Layer ?? "none"}) - drag to move, Del to delete";
            }
            
            Redraw();
        }

        /// <summary>Add wall to multi-selection (Shift+click)</summary>
        public void AddWallToSelection(string wallId)
        {
            if (_selectedWallIds.Count == 0)
                _selectionService.ClearSelection();
            
            if (!_selectedWallIds.Contains(wallId))
                _selectedWallIds.Add(wallId);
            
            _selectedWallId = wallId;
            UpdateWallRendererSelection();
            
            StatusText.Text = $"{_selectedWallIds.Count} walls selected - drag to move, Del to delete";
            Redraw();
        }

        /// <summary>Clear wall selection</summary>
        public void ClearWallSelection()
        {
            _selectedWallIds.Clear();
            _selectedWallId = null;
            _wallRenderer.SelectedWallId = null;
            _wallRenderer.SetSelectedWalls(_selectedWallIds);
        }

        private void UpdateWallRendererSelection()
        {
            _wallRenderer.SelectedWallId = _selectedWallId;
            _wallRenderer.SetSelectedWalls(_selectedWallIds);
        }

        /// <summary>Select all walls on a specific layer</summary>
        public void SelectWallsByLayer(string layer)
        {
            ClearWallSelection();
            _selectionService.ClearSelection();
            
            foreach (var wall in _layout.Walls.Where(w => w.Layer == layer))
                _selectedWallIds.Add(wall.Id);
            
            if (_selectedWallIds.Count > 0)
            {
                _selectedWallId = _selectedWallIds[0];
                UpdateWallRendererSelection();
                StatusText.Text = $"Selected {_selectedWallIds.Count} walls on layer '{layer}'";
            }
            Redraw();
        }

        /// <summary>Select walls fully contained within rectangle (for area selection)</summary>
        public int SelectWallsInRect(Rect rect, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                _selectedWallIds.Clear();
                _selectedWallId = null;
            }
            
            int count = 0;
            foreach (var wall in _layout.Walls)
            {
                // Wall is fully contained if BOTH endpoints are inside rect
                if (rect.Contains(new Point(wall.X1, wall.Y1)) && rect.Contains(new Point(wall.X2, wall.Y2)))
                {
                    if (!_selectedWallIds.Contains(wall.Id))
                    {
                        _selectedWallIds.Add(wall.Id);
                        count++;
                    }
                }
            }
            
            if (_selectedWallIds.Count > 0)
            {
                _selectedWallId = _selectedWallIds[0];
                UpdateWallRendererSelection();
            }
            
            return count;
        }

        /// <summary>Get selected wall IDs (for combined selection with nodes)</summary>
        public List<string> GetSelectedWallIds() => _selectedWallIds.ToList();

        #endregion

        #region Hit Testing

        /// <summary>Find wall at point (for click detection)</summary>
        public WallData? HitTestWall(Point point, double tolerance = 8)
        {
            foreach (var wall in _layout.Walls)
            {
                var dist = PointToLineDistance(point, wall.X1, wall.Y1, wall.X2, wall.Y2);
                
                if (dist <= tolerance + wall.Thickness / 2)
                {
                    var minX = Math.Min(wall.X1, wall.X2) - tolerance;
                    var maxX = Math.Max(wall.X1, wall.X2) + tolerance;
                    var minY = Math.Min(wall.Y1, wall.Y2) - tolerance;
                    var maxY = Math.Max(wall.Y1, wall.Y2) + tolerance;
                    
                    if (point.X >= minX && point.X <= maxX && 
                        point.Y >= minY && point.Y <= maxY)
                        return wall;
                }
            }
            return null;
        }

        private double PointToLineDistance(Point p, double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var lengthSq = dx * dx + dy * dy;
            
            if (lengthSq == 0) 
                return Math.Sqrt((p.X - x1) * (p.X - x1) + (p.Y - y1) * (p.Y - y1));
            
            var t = Math.Clamp(((p.X - x1) * dx + (p.Y - y1) * dy) / lengthSq, 0, 1);
            var projX = x1 + t * dx;
            var projY = y1 + t * dy;
            
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        /// <summary>Hit test for wall endpoint handles (returns wall and which endpoint)</summary>
        public (WallData? wall, bool isStart)? HitTestWallEndpoint(Point point)
        {
            foreach (var wallId in _selectedWallIds)
            {
                var wall = _layout.Walls.FirstOrDefault(w => w.Id == wallId);
                if (wall == null) continue;
                
                var distStart = Math.Sqrt(Math.Pow(point.X - wall.X1, 2) + Math.Pow(point.Y - wall.Y1, 2));
                if (distStart <= EndpointHitTolerance)
                    return (wall, true);
                
                var distEnd = Math.Sqrt(Math.Pow(point.X - wall.X2, 2) + Math.Pow(point.Y - wall.Y2, 2));
                if (distEnd <= EndpointHitTolerance)
                    return (wall, false);
            }
            return null;
        }

        #endregion

        #region Endpoint Dragging (Stretch)

        /// <summary>Start dragging a wall endpoint to stretch the wall</summary>
        public void StartEndpointDrag(WallData wall, bool isStart, Point pos)
        {
            _isDraggingWallEndpoint = true;
            _draggingEndpointWallId = wall.Id;
            _draggingStartEndpoint = isStart;
            _wallDragStart = pos;
            SaveUndoState();
            StatusText.Text = "Dragging endpoint - snaps to nearby endpoints";
        }

        /// <summary>Continue dragging endpoint</summary>
        public void DragEndpoint(Point pos)
        {
            if (!_isDraggingWallEndpoint || _draggingEndpointWallId == null) return;
            
            var wall = _layout.Walls.FirstOrDefault(w => w.Id == _draggingEndpointWallId);
            if (wall == null) return;

            // Try snap to nearby endpoint
            var snapped = TrySnapToEndpoint(pos, wall.Id);
            var target = snapped ?? pos;

            if (_draggingStartEndpoint)
            {
                wall.X1 = target.X;
                wall.Y1 = target.Y;
            }
            else
            {
                wall.X2 = target.X;
                wall.Y2 = target.Y;
            }
            Redraw();
        }

        /// <summary>Finish endpoint drag</summary>
        public void FinishEndpointDrag()
        {
            if (_isDraggingWallEndpoint)
            {
                _isDraggingWallEndpoint = false;
                _draggingEndpointWallId = null;
                MarkDirty();
                StatusText.Text = "Endpoint placed";
            }
        }

        public bool IsDraggingEndpoint => _isDraggingWallEndpoint;

        private Point? TrySnapToEndpoint(Point pos, string excludeWallId)
        {
            foreach (var wall in _layout.Walls.Where(w => w.Id != excludeWallId))
            {
                if (Math.Sqrt(Math.Pow(pos.X - wall.X1, 2) + Math.Pow(pos.Y - wall.Y1, 2)) <= EndpointSnapTolerance)
                    return new Point(wall.X1, wall.Y1);
                if (Math.Sqrt(Math.Pow(pos.X - wall.X2, 2) + Math.Pow(pos.Y - wall.Y2, 2)) <= EndpointSnapTolerance)
                    return new Point(wall.X2, wall.Y2);
            }
            return null;
        }

        #endregion

        #region Dragging

        /// <summary>Start dragging selected walls</summary>
        public void StartWallDrag(Point startPos)
        {
            if (_selectedWallIds.Count == 0) return;
            
            _isDraggingWalls = true;
            _wallDragStart = startPos;
            SaveUndoState();
        }

        /// <summary>Continue dragging walls (call from MouseMove)</summary>
        public void DragWalls(Point currentPos)
        {
            if (!_isDraggingWalls) return;
            
            var dx = currentPos.X - _wallDragStart.X;
            var dy = currentPos.Y - _wallDragStart.Y;
            
            MoveSelectedWalls(dx, dy);
            _wallDragStart = currentPos;
            Redraw();
        }

        /// <summary>Finish wall drag operation</summary>
        public void FinishWallDrag()
        {
            if (_isDraggingWalls)
            {
                _isDraggingWalls = false;
                MarkDirty();
            }
        }

        /// <summary>Check if currently dragging walls</summary>
        public bool IsDraggingWalls => _isDraggingWalls;

        #endregion

        #region Operations

        /// <summary>Move all selected walls by offset</summary>
        public void MoveSelectedWalls(double dx, double dy)
        {
            foreach (var wid in _selectedWallIds) { var w = _layout.Walls.FirstOrDefault(x => x.Id == wid); if (w != null) { w.X1 += dx; w.Y1 += dy; w.X2 += dx; w.Y2 += dy; } }
        }

        /// <summary>Delete all selected walls (call from Delete key handler)</summary>
        public void DeleteSelectedWalls()
        {
            if (_selectedWallIds.Count == 0) return;
            SaveUndoState();
            var count = _selectedWallIds.Count;
            foreach (var wid in _selectedWallIds.ToList()) { var w = _layout.Walls.FirstOrDefault(x => x.Id == wid); if (w != null) _layout.Walls.Remove(w); }
            ClearWallSelection(); MarkDirty(); Redraw();
            StatusText.Text = $"Deleted {count} wall(s)";
        }

        /// <summary>Scale selected walls around their center</summary>
        public void ScaleSelectedWalls(double scale)
        {
            var walls = _layout.Walls.Where(w => _selectedWallIds.Contains(w.Id)).ToList();
            if (walls.Count == 0) return;
            double cx = 0, cy = 0;
            foreach (var w in walls) { cx += (w.X1 + w.X2) / 2; cy += (w.Y1 + w.Y2) / 2; }
            cx /= walls.Count; cy /= walls.Count;
            foreach (var w in walls) { w.X1 = cx + (w.X1 - cx) * scale; w.Y1 = cy + (w.Y1 - cy) * scale; w.X2 = cx + (w.X2 - cx) * scale; w.Y2 = cy + (w.Y2 - cy) * scale; }
        }

        public void ApplyStyleToSelectedWalls(string style)
        {
            var pattern = LineStyles.GetDashPattern(style);
            foreach (var wid in _selectedWallIds) { var w = _layout.Walls.FirstOrDefault(x => x.Id == wid); if (w != null) w.DashPattern = pattern; }
        }

        public void ApplyTypeToSelectedWalls(string wallType)
        {
            foreach (var wid in _selectedWallIds) { var w = _layout.Walls.FirstOrDefault(x => x.Id == wid); if (w != null) w.WallType = wallType; }
        }

        public void ApplyThicknessToSelectedWalls(double thickness)
        {
            foreach (var wid in _selectedWallIds) { var w = _layout.Walls.FirstOrDefault(x => x.Id == wid); if (w != null) w.Thickness = thickness; }
        }

        public void ApplyColorToSelectedWalls(string color)
        {
            foreach (var wid in _selectedWallIds) { var w = _layout.Walls.FirstOrDefault(x => x.Id == wid); if (w != null) w.Color = color; }
        }

        /// <summary>Break wall at point, creating two walls with optional gap (doorway)</summary>
        public void BreakWallAtPoint(WallData wall, Point breakPoint, double gapSize = 0)
        {
            SaveUndoState();
            
            var dx = wall.X2 - wall.X1;
            var dy = wall.Y2 - wall.Y1;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;

            // Project click point onto line
            var t = Math.Clamp(((breakPoint.X - wall.X1) * dx + (breakPoint.Y - wall.Y1) * dy) / (len * len), 0.1, 0.9);
            var breakX = wall.X1 + t * dx;
            var breakY = wall.Y1 + t * dy;

            // Unit vector along wall
            var ux = dx / len;
            var uy = dy / len;
            var halfGap = gapSize / 2;

            // Create second wall (break to original end)
            var wall2 = new WallData
            {
                Id = Guid.NewGuid().ToString(),
                X1 = breakX + ux * halfGap,
                Y1 = breakY + uy * halfGap,
                X2 = wall.X2,
                Y2 = wall.Y2,
                Thickness = wall.Thickness,
                WallType = wall.WallType,
                Color = wall.Color,
                DashPattern = wall.DashPattern,
                Layer = wall.Layer
            };

            // Modify original wall (start to break)
            wall.X2 = breakX - ux * halfGap;
            wall.Y2 = breakY - uy * halfGap;

            _layout.Walls.Add(wall2);
            MarkDirty();
            Redraw();
            StatusText.Text = gapSize > 0 ? $"Created doorway ({gapSize}px gap)" : "Wall split";
        }

        /// <summary>Join two walls at their closest endpoints</summary>
        public void JoinWalls(WallData w1, WallData w2)
        {
            var pairs = new[] { (d: Dist(w1.X1, w1.Y1, w2.X1, w2.Y1), s1: true, s2: true), (d: Dist(w1.X1, w1.Y1, w2.X2, w2.Y2), s1: true, s2: false),
                (d: Dist(w1.X2, w1.Y2, w2.X1, w2.Y1), s1: false, s2: true), (d: Dist(w1.X2, w1.Y2, w2.X2, w2.Y2), s1: false, s2: false) };
            var c = pairs.OrderBy(p => p.d).First();
            if (c.d > 50) { StatusText.Text = "Walls too far apart to join"; return; }
            SaveUndoState();
            var mx = ((c.s1 ? w1.X1 : w1.X2) + (c.s2 ? w2.X1 : w2.X2)) / 2;
            var my = ((c.s1 ? w1.Y1 : w1.Y2) + (c.s2 ? w2.Y1 : w2.Y2)) / 2;
            if (c.s1) { w1.X1 = mx; w1.Y1 = my; } else { w1.X2 = mx; w1.Y2 = my; }
            if (c.s2) { w2.X1 = mx; w2.Y1 = my; } else { w2.X2 = mx; w2.Y2 = my; }
            MarkDirty(); Redraw(); StatusText.Text = "Walls joined";
        }

        private double Dist(double x1, double y1, double x2, double y2) =>
            Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

        #endregion

        #region Context Menu

        /// <summary>Show context menu for wall editing</summary>
        public void ShowWallContextMenu(WallData wall, Point pos)
        {
            var menu = new ContextMenu();

            menu.Items.Add(CreateMenuItem("Properties...", () => _panelManager.ShowWallProperties(wall)));
            menu.Items.Add(new Separator());

            // Break/Doorway options
            menu.Items.Add(CreateMenuItem("Break Line Here", () => BreakWallAtPoint(wall, pos)));
            menu.Items.Add(CreateMenuItem("Create Doorway (40px)", () => BreakWallAtPoint(wall, pos, 40)));
            menu.Items.Add(CreateMenuItem("Create Doorway (60px)", () => BreakWallAtPoint(wall, pos, 60)));
            menu.Items.Add(CreateMenuItem("Create Doorway (80px)", () => BreakWallAtPoint(wall, pos, 80)));

            // Join walls (if 2 selected)
            if (_selectedWallIds.Count == 2)
            {
                var other = _layout.Walls.FirstOrDefault(w => _selectedWallIds.Contains(w.Id) && w.Id != wall.Id);
                if (other != null)
                    menu.Items.Add(CreateMenuItem("Join Selected Walls", () => JoinWalls(wall, other)));
            }

            menu.Items.Add(new Separator());

            // Line Style submenu
            var styleMenu = new MenuItem { Header = "Line Style" };
            foreach (var s in new[] { LineStyles.Solid, LineStyles.Dashed, LineStyles.Dotted, LineStyles.DashDot, LineStyles.Hidden })
            { var p = LineStyles.GetDashPattern(s); styleMenu.Items.Add(CreateMenuItem(Capitalize(s), () => { SaveUndoState(); ApplyStyleToSelectedWalls(s); MarkDirty(); Redraw(); }, wall.DashPattern == p || (s == LineStyles.Solid && string.IsNullOrEmpty(wall.DashPattern)))); }
            menu.Items.Add(styleMenu);

            // Wall Type submenu  
            var typeMenu = new MenuItem { Header = "Wall Type" };
            foreach (var t in new[] { WallTypes.Standard, WallTypes.Exterior, WallTypes.Partition, WallTypes.Glass, WallTypes.Safety })
            { typeMenu.Items.Add(CreateMenuItem(Capitalize(t), () => { SaveUndoState(); ApplyTypeToSelectedWalls(t); MarkDirty(); Redraw(); }, wall.WallType == t)); }
            menu.Items.Add(typeMenu);

            // Thickness submenu
            var thickMenu = new MenuItem { Header = "Thickness" };
            foreach (var t in new[] { 1.0, 2.0, 4.0, 6.0, 8.0, 10.0 })
            { thickMenu.Items.Add(CreateMenuItem($"{t} px", () => { SaveUndoState(); ApplyThicknessToSelectedWalls(t); MarkDirty(); Redraw(); }, Math.Abs(wall.Thickness - t) < 0.1)); }
            menu.Items.Add(thickMenu);

            // Color submenu
            var colorMenu = new MenuItem { Header = "Color" };
            foreach (var (n, h) in new[] { ("Black", "#000000"), ("Gray", "#808080"), ("Blue", "#4A90D9"), ("Red", "#E74C3C"), ("Green", "#2ECC71") })
            { colorMenu.Items.Add(CreateMenuItem(n, () => { SaveUndoState(); ApplyColorToSelectedWalls(h); MarkDirty(); Redraw(); })); }
            menu.Items.Add(colorMenu);

            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Scale...", ShowScaleDialog));
            
            var delItem = CreateMenuItem("Delete", DeleteSelectedWalls);
            delItem.InputGestureText = "Del";
            menu.Items.Add(delItem);

            // Layer options
            if (!string.IsNullOrEmpty(wall.Layer))
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(new MenuItem { Header = $"Layer: {wall.Layer}", IsEnabled = false });
                menu.Items.Add(CreateMenuItem($"Select All on '{wall.Layer}'", () => SelectWallsByLayer(wall.Layer)));
            }

            menu.IsOpen = true;
        }

        private MenuItem CreateMenuItem(string header, Action action, bool isChecked = false)
        {
            var item = new MenuItem { Header = header, IsChecked = isChecked };
            item.Click += (s, e) => action();
            return item;
        }

        private string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private void ShowScaleDialog()
        {
            var dlg = new Window { Title = "Scale Walls", Width = 260, Height = 140, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize };
            var panel = new StackPanel { Margin = new Thickness(15) };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            row.Children.Add(new TextBlock { Text = "Scale factor:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            var box = new TextBox { Text = "1.0", Width = 80 }; row.Children.Add(box); panel.Children.Add(row);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 10, 0) };
            var cancel = new Button { Content = "Cancel", Width = 70 };
            ok.Click += (s, e) => { if (double.TryParse(box.Text, out var sc) && sc > 0) { SaveUndoState(); ScaleSelectedWalls(sc); MarkDirty(); Redraw(); dlg.DialogResult = true; } };
            cancel.Click += (s, e) => dlg.DialogResult = false;
            btns.Children.Add(ok); btns.Children.Add(cancel); panel.Children.Add(btns); dlg.Content = panel; dlg.ShowDialog();
        }

        #endregion
    }
}
