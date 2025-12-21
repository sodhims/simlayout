using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;
using LayoutEditor.Renderers;

namespace LayoutEditor
{
    /// <summary>
    /// Main window for the Layout Editor application.
    /// Implementation is split across multiple partial class files in the Handlers folder.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        // Data
        private LayoutData _layout = null!;
        private string? _currentFilePath;
        private bool _isDirty;
        private double _currentZoom = 1.0;
        private bool _showTracks = true;  // Legacy flag for transport track visibility


        // Services - initialized in constructor
        private readonly SelectionService _selectionService = null!;
        private readonly UndoService _undoService = null!;
        private readonly AlignmentService _alignmentService = null!;
        private readonly HitTestService _hitTestService = null!;
        private readonly GridRenderer _gridRenderer = null!;
        private readonly NodeRenderer _nodeRenderer = null!;
        private readonly PathRenderer _pathRenderer = null!;
        private readonly GroupRenderer _groupRenderer = null!;
        private readonly WallRenderer _wallRenderer = null!;
        private readonly ArchitectureLayerManager _transportArchitectureLayerManager = null!;

        // Layer renderers
        private readonly InfrastructureRenderer _infrastructureRenderer = null!;
        private readonly SpatialRenderer _spatialRenderer = null!;
        private readonly EquipmentRenderer _equipmentRenderer = null!;
        private readonly LocalFlowRenderer _localFlowRenderer = null!;
        private readonly GuidedTransportRenderer _guidedTransportRenderer = null!;
        private readonly OverheadTransportRenderer _overheadTransportRenderer = null!;
        private readonly FlexibleTransportRenderer _flexibleTransportRenderer = null!;
        private readonly PedestrianRenderer _pedestrianRenderer = null!;

        // Element tracking
        private readonly Dictionary<string, UIElement> _elementMap = new();

        #endregion

        #region Constructor

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Initialize services
                _selectionService = new SelectionService();
                _undoService = new UndoService();
                _alignmentService = new AlignmentService();
                _hitTestService = new HitTestService();
                _gridRenderer = new GridRenderer();
                _nodeRenderer = new NodeRenderer(_selectionService);
                _pathRenderer = new PathRenderer(_selectionService);
                _groupRenderer = new GroupRenderer(_selectionService);
                _wallRenderer = new WallRenderer(_selectionService);
                _transportArchitectureLayerManager = new ArchitectureLayerManager();

                // Initialize layer renderers
                _infrastructureRenderer = new InfrastructureRenderer(_selectionService);
                _spatialRenderer = new SpatialRenderer();
                _equipmentRenderer = new EquipmentRenderer(_selectionService);
                _localFlowRenderer = new LocalFlowRenderer(_selectionService);
                _guidedTransportRenderer = new GuidedTransportRenderer(_selectionService);
                _overheadTransportRenderer = new OverheadTransportRenderer(_selectionService);
                _flexibleTransportRenderer = new FlexibleTransportRenderer(_selectionService);
                _pedestrianRenderer = new PedestrianRenderer(_selectionService);

                // Subscribe to selection changes
                _selectionService.SelectionChanged += (s, e) =>
                {
                    UpdateToolbarState();
                };

                // Subscribe to transport layer manager events
                _transportArchitectureLayerManager.VisibilityChanged += (s, e) =>
                {
                    Redraw();
                };

                _transportArchitectureLayerManager.ActiveLayerChanged += (s, layer) =>
                {
                    UpdateActiveLayerStatus(layer);
                };

                // Initialize layout
                InitializeLayout();
                InitializeTransport();
                InitializeCranes();
                InitializeTransportMarkerSystem();
                InitializeTransportGroupPanelEvents();
                InitializeToolbox();

                // Wire up Transport Layers Panel
                if (TransportLayersPanel != null)
                {
                    TransportLayersPanel.SetArchitectureLayerManager(_transportArchitectureLayerManager);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeLayout()
        {
            _layout = LayoutFactory.CreateDefault();
            _isDirty = false;

            TryInitializeNodeTypeCombo();
            RefreshAll();
            UpdateTitle();
        }

        private void SnapModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SnapModeCombo == null || _layout?.Canvas == null) return;
            if (SnapModeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (tag == "Grid")
                {
                    _layout.Canvas.SnapMode = SnapMode.Grid;
                    if (SnapToGridMenu != null) SnapToGridMenu.IsChecked = true;
                }
                else
                {
                    _layout.Canvas.SnapMode = SnapMode.None;
                    if (SnapToGridMenu != null) SnapToGridMenu.IsChecked = false;
                }
            }
        }

        private bool IsSnapGridEnabled()
        {
            if (SnapToGridMenu != null && SnapToGridMenu.IsChecked == true) return true;
            return _layout?.Canvas?.SnapMode == SnapMode.Grid;
        }

        private void TryInitializeNodeTypeCombo()
        {
            // Docked properties panel has been removed.
            // Node type combo is now in the floating PropertiesPanel.
        }

        private void ConfigureIcons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Dialogs.IconEditorDialog { Owner = this };
                dlg.ShowDialog();
                // reload icons registry in case of changes
                // NodeRenderer will pick up files automatically next redraw
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Icon Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Core Methods

        private void RefreshAll()
        {
            RefreshElementList();
            RefreshTemplateList();
            UpdateLayerCheckboxes();
            Redraw();
            UpdateNodeCount();
            UpdateNodeCountDisplay();
            _panelManager?.LoadLayout(_layout);
        }

        /// <summary>
        /// Public method for panels to request canvas refresh
        /// </summary>
        public void RefreshCanvas()
        {
            MarkDirty();
            Redraw();
            _panelManager?.RefreshAll();
        }

        private void Redraw()
        {
            if (EditorCanvas == null) return;

            EditorCanvas.Children.Clear();
            _elementMap.Clear();

            // Layer 0: Background image (if present)
            if (_layout.Display.Layers.BackgroundImage && _layout.Background != null)
                DrawBackgroundImage();

            // Layer 0.5: Grid (always visible if enabled)
            if (_layout.Display.Layers.Background)
                _gridRenderer.DrawGrid(EditorCanvas, _layout, _currentZoom);

            // Get visible transport layers in Z-order
            var visibleLayers = _transportArchitectureLayerManager.GetVisibleLayers();

            foreach (var layer in visibleLayers)
            {
                ILayerRenderer? renderer = layer switch
                {
                    LayerType.Infrastructure => _infrastructureRenderer,
                    LayerType.Spatial => _spatialRenderer,
                    LayerType.Equipment => _equipmentRenderer,
                    LayerType.LocalFlow => _localFlowRenderer,
                    LayerType.GuidedTransport => _guidedTransportRenderer,
                    LayerType.OverheadTransport => _overheadTransportRenderer,
                    LayerType.FlexibleTransport => _flexibleTransportRenderer,
                    LayerType.Pedestrian => _pedestrianRenderer,
                    // Future layers will be added here as they're implemented
                    _ => null
                };

                renderer?.Render(EditorCanvas, _layout, RegisterElement);
            }

            // Legacy rendering (temporary - will be migrated to layer renderers)
            // These checks use old Display.Layers flags for backward compatibility

            // Zones (will move to Spatial renderer fully)
            if (_layout.Display.Layers.Zones)
                _groupRenderer.DrawZones(EditorCanvas, _layout);

            // Paths (future: LocalFlow layer)
            if (_layout.Display.Layers.Paths)
            {
                _pathRenderer.IsEditMode = _isPathEditMode;
                _pathRenderer.DrawPaths(EditorCanvas, _layout, RegisterElement);
            }

            // Nodes now rendered by EquipmentRenderer (Equipment layer)
            // Legacy rendering commented out - Week 3
            // if (_layout.Display.Layers.Nodes)
            //     _nodeRenderer.DrawNodes(EditorCanvas, _layout, RegisterElement);

            // Groups/Cells
            _groupRenderer.DrawGroups(EditorCanvas, _layout);

            // Measurements (on top)
            if (_layout.Display.Layers.Measurements)
                _wallRenderer.DrawMeasurements(EditorCanvas, _layout);

            // Constraint guides (frictionless mode)
            if (_layout.FrictionlessMode)
                DrawConstraintGuides();

            // Transport elements (tracks, stations, waypoints)
            DrawTransportElements();

            // EOT crane elements
            DrawCraneElements();

            // Transport markers (always draw for now - controlled by layer visibility)
            DrawTransportMarkerElements();
        }

        private void DrawBackgroundImage()
        {
            if (_layout.Background == null || string.IsNullOrEmpty(_layout.Background.Base64Data))
                return;

            try
            {
                var bytes = Convert.FromBase64String(_layout.Background.Base64Data);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var image = new Image
                {
                    Source = bitmap,
                    Width = _layout.Background.Width * _layout.Background.Scale,
                    Height = _layout.Background.Height * _layout.Background.Scale,
                    Opacity = _layout.Background.Opacity,
                    Stretch = Stretch.Fill
                };

                Canvas.SetLeft(image, _layout.Background.X);
                Canvas.SetTop(image, _layout.Background.Y);
                EditorCanvas.Children.Add(image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing background: {ex.Message}");
            }
        }

        private void MoveSelectedVisuals()
        {
            // Fast path: just move existing visual elements without full redraw
            foreach (var nodeId in _selectionService.SelectedNodeIds)
            {
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node != null && _elementMap.TryGetValue(nodeId, out var element))
                {
                    Canvas.SetLeft(element, node.Visual.X);
                    Canvas.SetTop(element, node.Visual.Y);
                }
            }
        }

        /// <summary>
        /// Recreate path UI elements for any paths connected to the moved nodes.
        /// This updates the visible connections during drag without a full redraw.
        /// </summary>
        private void UpdatePathVisualsForMovedNodes(List<string> movedNodeIds)
        {
            if (EditorCanvas == null || _layout == null) return;

            // Find affected paths
            var affectedPaths = _layout.Paths.Where(p => movedNodeIds.Contains(p.From) || movedNodeIds.Contains(p.To)).ToList();
            if (!affectedPaths.Any()) return;

            // Determine insertion index so paths are drawn beneath nodes (nodes rendered later)
            int insertIndex = -1;
            foreach (var node in _layout.Nodes)
            {
                if (_elementMap.TryGetValue(node.Id, out var nodeEl))
                {
                    int idx = EditorCanvas.Children.IndexOf(nodeEl);
                    if (idx >= 0 && (insertIndex == -1 || idx < insertIndex)) insertIndex = idx;
                }
            }

            foreach (var path in affectedPaths)
            {
                string key = $"path:{path.Id}";

                // Remove old element if present
                if (_elementMap.TryGetValue(key, out var oldEl))
                {
                    EditorCanvas.Children.Remove(oldEl);
                    _elementMap.Remove(key);
                }

                // Recreate element
                var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);
                if (fromNode == null || toNode == null) continue;

                var newEl = _pathRenderer.CreatePathElement(path, fromNode, toNode, _layout);

                if (insertIndex >= 0 && insertIndex <= EditorCanvas.Children.Count)
                    EditorCanvas.Children.Insert(insertIndex, newEl);
                else
                    EditorCanvas.Children.Add(newEl);

                RegisterElement(key, newEl);
            }
        }

        private void RegisterElement(string id, UIElement element)
        {
            _elementMap[id] = element;
        }

        private void UpdateSelectionVisuals()
        {
            // Redraw and update toolbar state
            Redraw();
            UpdateToolbarState();
        }

        private void UpdateLayerCheckboxes()
        {
            // LEGACY: Layer checkboxes removed - now using TransportLayersPanel
            // Layer visibility is managed by ArchitectureLayerManager
        }

        private void UpdateNodeCount()
        {
            if (NodeCountText != null)
                NodeCountText.Text = $"Nodes: {_layout.Nodes.Count}  Paths: {_layout.Paths.Count}  Walls: {_layout.Walls.Count}";
        }

        private void UpdateTitle()
        {
            var filename = string.IsNullOrEmpty(_currentFilePath)
                ? "New Layout"
                : System.IO.Path.GetFileName(_currentFilePath);

            var dirty = _isDirty ? "*" : "";
            Title = $"{filename}{dirty} - Simulation Layout Editor";
            
            if (FileText != null) FileText.Text = filename;
        }

        private void UpdateMousePosition(Point pos)
        {
            if (MousePosText != null)
                MousePosText.Text = $"X: {pos.X:F0}  Y: {pos.Y:F0}";
        }

        private void UpdateActiveLayerStatus(LayerType layer)
        {
            var metadata = LayerMetadata.GetMetadata(layer);
            if (StatusText != null)
                StatusText.Text = $"Active Layer: {metadata.Name}";
        }

        #endregion

        #region State Management

        private void SaveUndoState()
        {
            _undoService.SaveState(_layout);
        }

        private void MarkDirty()
        {
            if (!_isDirty)
            {
                _isDirty = true;
                UpdateTitle();
            }
        }

        #endregion

        #region Selection Rectangle

        private Rectangle? _selectionRect;

        private void UpdateSelectionRectangle(Point currentPos)
        {
            if (_selectionRect == null)
            {
                _selectionRect = new Rectangle
                {
                    Stroke = new SolidColorBrush(Colors.DodgerBlue),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = new SolidColorBrush(Color.FromArgb(30, 30, 144, 255))
                };
                EditorCanvas.Children.Add(_selectionRect);
            }

            var x = Math.Min(_dragStart.X, currentPos.X);
            var y = Math.Min(_dragStart.Y, currentPos.Y);
            var w = Math.Abs(currentPos.X - _dragStart.X);
            var h = Math.Abs(currentPos.Y - _dragStart.Y);

            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = w;
            _selectionRect.Height = h;

            // Select nodes in rectangle
            SelectNodesInRectangle(new Rect(x, y, w, h));
        }

        private void SelectNodesInRectangle(Rect rect)
        {
            var nodeIds = new List<string>();
            var wallIds = new List<string>();
            var pathIds = new List<string>();

            // Select nodes (intersects - partial overlap counts)
            foreach (var node in _layout.Nodes)
            {
                var nodeRect = new Rect(
                    node.Visual.X, node.Visual.Y,
                    node.Visual.Width, node.Visual.Height);

                if (rect.IntersectsWith(nodeRect))
                    nodeIds.Add(node.Id);
            }

            // Select walls - FULLY CONTAINED only (both endpoints must be inside rect)
            foreach (var wall in _layout.Walls)
            {
                var p1 = new Point(wall.X1, wall.Y1);
                var p2 = new Point(wall.X2, wall.Y2);
                
                if (rect.Contains(p1) && rect.Contains(p2))
                    wallIds.Add(wall.Id);
            }

            // Select paths (check if line intersects rect)
            foreach (var path in _layout.Paths)
            {
                var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);
                if (fromNode != null && toNode != null)
                {
                    var pathRect = new Rect(
                        Math.Min(fromNode.Visual.X, toNode.Visual.X),
                        Math.Min(fromNode.Visual.Y, toNode.Visual.Y),
                        Math.Abs(toNode.Visual.X - fromNode.Visual.X) + 10,
                        Math.Abs(toNode.Visual.Y - fromNode.Visual.Y) + 10);

                    if (rect.IntersectsWith(pathRect))
                        pathIds.Add(path.Id);
                }
            }

            _selectionService.SelectMultiple(nodeIds, wallIds, pathIds);
            
            // Also update WallHandlers selection state
            _selectedWallIds.Clear();
            _selectedWallIds.AddRange(wallIds);
            if (wallIds.Count > 0)
            {
                _selectedWallId = wallIds[0];
                UpdateWallRendererSelection();
            }
            
            UpdatePropertyPanel();
            UpdateSelectionStatus(nodeIds.Count, wallIds.Count, pathIds.Count);
        }

        private void UpdateSelectionStatus(int nodes, int walls, int paths)
        {
            var parts = new List<string>();
            if (nodes > 0) parts.Add($"{nodes} node(s)");
            if (walls > 0) parts.Add($"{walls} wall(s)");
            if (paths > 0) parts.Add($"{paths} path(s)");
            
            if (parts.Count > 0)
                StatusText.Text = $"Selected: {string.Join(", ", parts)}";
        }

        private void ClearSelectionRectangle()
        {
            if (_selectionRect != null)
            {
                EditorCanvas.Children.Remove(_selectionRect);
                _selectionRect = null;
            }
        }

        #endregion

        #region Cell Helpers

        /// <summary>
        /// Get paths that are internal to a cell (both From and To are members)
        /// </summary>
        private List<string> GetCellInternalPaths(GroupData cell)
        {
            var memberSet = new HashSet<string>(cell.Members);
            return _layout.Paths
                .Where(p => memberSet.Contains(p.From) && memberSet.Contains(p.To))
                .Select(p => p.Id)
                .ToList();
        }

        /// <summary>
        /// Select a cell/group with all its internal paths
        /// </summary>
        private void SelectCellWithPaths(GroupData cell, bool addToSelection = false)
        {
            var internalPaths = GetCellInternalPaths(cell);
            _selectionService.SelectGroupWithPaths(cell.Id, cell.Members, internalPaths, addToSelection);
        }

        #endregion

        #region Node Type Configuration

        /// <summary>
        /// Opens the Node Type Configuration dialog
        /// </summary>
        private void ConfigureNodeTypes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Dialogs.NodeTypeEditorDialog();
                dialog.Owner = this;
                dialog.ShowDialog();
                
                NodeToolbox?.RefreshToolbox();
                Redraw();
                StatusText.Text = "Node type configuration updated";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void RotateSelectedNodeTerminals90()
        {
            var selectedNodes = _selectionService.GetSelectedNodes(_layout).ToList();

            if (!selectedNodes.Any())
            {
                StatusText.Text = "Select a node first";
                return;
            }

            SaveUndoState();

            foreach (var node in selectedNodes)
            {
                node.RotateTerminals90();
            }

            MarkDirty();
            Redraw();

            var layout = selectedNodes.First().Visual.TerminalLayout;
            StatusText.Text = $"Terminals rotated: {layout}";
        }

        private void FlipSelectedNodeTerminals()
        {
            var selectedNodes = _selectionService.GetSelectedNodes(_layout).ToList();

            if (!selectedNodes.Any())
            {
                StatusText.Text = "Select a node first";
                return;
            }

            SaveUndoState();

            foreach (var node in selectedNodes)
            {
                node.FlipTerminals();
            }

            MarkDirty();
            Redraw();

            var layout = selectedNodes.First().Visual.TerminalLayout;
            StatusText.Text = $"Terminals: {layout}";
        }

        #endregion

        #region Cell Terminal and Flip Operations

        /// <summary>
        /// Get currently selected cell (if any)
        /// </summary>
        private GroupData? GetSelectedCell()
        {
            // Check if any cell is selected via selection service
            foreach (var group in _layout.Groups)
            {
                if (group.IsCell && _selectionService.IsGroupSelected(group.Id))
                    return group;
            }
            
            // Also check if all selected nodes belong to a single cell
            var selectedNodes = _selectionService.GetSelectedNodes(_layout).ToList();
            if (selectedNodes.Any())
            {
                var selectedIds = new HashSet<string>(selectedNodes.Select(n => n.Id));
                foreach (var group in _layout.Groups.Where(g => g.IsCell))
                {
                    if (group.Members.All(m => selectedIds.Contains(m)) && 
                        selectedIds.All(id => group.Members.Contains(id)))
                    {
                        return group;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Flip cell terminals (Ctrl+F on cell)
        /// </summary>
        private void FlipSelectedCellTerminals()
        {
            var cell = GetSelectedCell();
            if (cell == null) return;
            
            SaveUndoState();
            cell.FlipTerminals();
            MarkDirty();
            Redraw();
            StatusText.Text = $"Cell terminals: {cell.TerminalLayout}";
        }

        /// <summary>
        /// Rotate cell terminals to 90° mode (Ctrl+Shift+F on cell)
        /// </summary>
        private void RotateSelectedCellTerminals90()
        {
            var cell = GetSelectedCell();
            if (cell == null) return;
            
            SaveUndoState();
            cell.RotateTerminals90();
            MarkDirty();
            Redraw();
            StatusText.Text = $"Cell terminals: {cell.TerminalLayout}";
        }

        /// <summary>
        /// Flip cell contents horizontally - mirror left/right (Ctrl+H)
        /// </summary>
        private void FlipSelectedCellHorizontal()
        {
            var cell = GetSelectedCell();
            if (cell == null)
            {
                StatusText.Text = "Select a cell first";
                return;
            }
            
            SaveUndoState();
            cell.FlipHorizontal(_layout.Nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Cell '{cell.Name}' flipped horizontally";
        }

        /// <summary>
        /// Flip cell contents vertically - mirror top/bottom (Ctrl+Shift+H)
        /// </summary>
        private void FlipSelectedCellVertical()
        {
            var cell = GetSelectedCell();
            if (cell == null)
            {
                StatusText.Text = "Select a cell first";
                return;
            }
            
            SaveUndoState();
            cell.FlipVertical(_layout.Nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Cell '{cell.Name}' flipped vertically";
        }

        /// <summary>
        /// Rotate cell contents 90° clockwise (Ctrl+R on cell)
        /// </summary>
        private void RotateSelectedCell90()
        {
            var cell = GetSelectedCell();
            if (cell == null)
            {
                StatusText.Text = "Select a cell first";
                return;
            }
            
            SaveUndoState();
            cell.Rotate90Clockwise(_layout.Nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Cell '{cell.Name}' rotated 90°";
        }

        private void ExportPostgres_Click(object sender, RoutedEventArgs e)
        {
            var layoutName = System.IO.Path.GetFileNameWithoutExtension(_currentFilePath ?? "Untitled");
            var dialog = new Dialogs.PostgresExportDialog(_layout, layoutName);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        #endregion

        #region Transport Panel Handlers (for new XAML menu items)


        private void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            PlaceMarker_Click(sender, e);
        }

        private void ToggleLegend_Click(object sender, RoutedEventArgs e)
        {
            Redraw();
        }

        private void AutoConnectGroup_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Auto-connect group: Select stations with same GroupName";
        }

        private void AssignStationsToGroup_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Assign stations to group";
        }

        private void AddBlindPath_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Add blind path from selected station";
        }

        private void ConnectToNearest_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Connect selected station to nearest";
        }

        private void RecreateLoop_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Recreate loop for group";
        }

        #endregion

        #region Frictionless Mode

        private void ToggleFrictionlessMode_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null) return;

            _layout.FrictionlessMode = FrictionlessModeToggle.IsChecked == true;

            if (_layout.FrictionlessMode)
            {
                StatusText.Text = "Frictionless Mode: ON - Only constrained entities selectable";
                // Visual feedback
                FrictionlessModeToggle.Background = new SolidColorBrush(Color.FromRgb(204, 232, 255));
            }
            else
            {
                StatusText.Text = "Frictionless Mode: OFF";
                FrictionlessModeToggle.Background = new SolidColorBrush(Colors.Transparent);
            }

            // Refresh canvas to update selection behavior
            RefreshCanvas();
        }

        #endregion
    }
}
