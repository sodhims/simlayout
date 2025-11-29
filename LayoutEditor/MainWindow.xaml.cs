using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;

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

        // Services
        private readonly SelectionService _selectionService;
        private readonly UndoService _undoService;
        private readonly AlignmentService _alignmentService;
        private readonly HitTestService _hitTestService;
        private readonly GridRenderer _gridRenderer;
        private readonly NodeRenderer _nodeRenderer;
        private readonly PathRenderer _pathRenderer;
        private readonly GroupRenderer _groupRenderer;
        private readonly WallRenderer _wallRenderer;

        // Element tracking
        private readonly Dictionary<string, UIElement> _elementMap = new();

        #endregion

        #region Constructor

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            InitializeFloatingPanels();
                LayersPanelControl.ActiveLayerChanged += (s, e) => UpdateActiveLayerDisplay();
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

                // Subscribe to selection changes
                _selectionService.SelectionChanged += (s, e) =>
                {
                    UpdateToolbarState();
                };

                // Initialize layout
                InitializeLayout();
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
            InitializeLayers();
            RefreshAll();
            UpdateTitle();
        }

        private void TryInitializeNodeTypeCombo()
        {
            if (PropNodeType == null) return;
            
            PropNodeType.Items.Clear();
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Source", Tag = NodeTypes.Source });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Sink", Tag = NodeTypes.Sink });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Machine", Tag = NodeTypes.Machine });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Buffer", Tag = NodeTypes.Buffer });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Workstation", Tag = NodeTypes.Workstation });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Inspection", Tag = NodeTypes.Inspection });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Storage", Tag = NodeTypes.Storage });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Conveyor", Tag = NodeTypes.Conveyor });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "Junction", Tag = NodeTypes.Junction });
            PropNodeType.Items.Add(new ComboBoxItem { Content = "AGV Station", Tag = NodeTypes.AgvStation });
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
            UpdateActiveLayerDisplay();
            PopulateLayerCombo();  // Add this

        }

        private void Redraw()
        {
            if (EditorCanvas == null) return;
            
            EditorCanvas.Children.Clear();
            _elementMap.Clear();

            // Layer 1: Background image (if present)
            if (_layout.Display.Layers.BackgroundImage && _layout.Background != null)
                DrawBackgroundImage();

            // Layer 2: Grid
            if (_layout.Display.Layers.Background)
                _gridRenderer.DrawGrid(EditorCanvas, _layout, _currentZoom);

            // Layer 3: Zones
            if (_layout.Display.Layers.Zones)
                _groupRenderer.DrawZones(EditorCanvas, _layout);

            // Layer 4: Walls and columns
            if (_layout.Display.Layers.Walls)
                _wallRenderer.DrawWalls(EditorCanvas, _layout, RegisterElement);

            // Layer 5: Paths
            if (_layout.Display.Layers.Paths)
            {
                _pathRenderer.IsEditMode = _isPathEditMode;
                _pathRenderer.DrawPaths(EditorCanvas, _layout, RegisterElement);
            }

            // Layer 6: Nodes
            if (_layout.Display.Layers.Nodes)
                _nodeRenderer.DrawNodes(EditorCanvas, _layout, RegisterElement);

            // Layer 7: Groups/Cells
            _groupRenderer.DrawGroups(EditorCanvas, _layout);

            // Layer 8: Measurements (on top)
            if (_layout.Display.Layers.Measurements)
                _wallRenderer.DrawMeasurements(EditorCanvas, _layout);
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
            if (LayerBackground != null) LayerBackground.IsChecked = _layout.Display.Layers.Background;
            if (LayerBackgroundImage != null) LayerBackgroundImage.IsChecked = _layout.Display.Layers.BackgroundImage;
            if (LayerWalls != null) LayerWalls.IsChecked = _layout.Display.Layers.Walls;
            if (LayerCorridors != null) LayerCorridors.IsChecked = _layout.Display.Layers.Corridors;
            if (LayerZones != null) LayerZones.IsChecked = _layout.Display.Layers.Zones;
            if (LayerPaths != null) LayerPaths.IsChecked = _layout.Display.Layers.Paths;
            if (LayerNodes != null) LayerNodes.IsChecked = _layout.Display.Layers.Nodes;
            if (LayerLabels != null) LayerLabels.IsChecked = _layout.Display.Layers.Labels;
            if (LayerMeasurements != null) LayerMeasurements.IsChecked = _layout.Display.Layers.Measurements;
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

            foreach (var node in _layout.Nodes)
            {
                var nodeRect = new Rect(
                    node.Visual.X, node.Visual.Y,
                    node.Visual.Width, node.Visual.Height);

                if (rect.IntersectsWith(nodeRect))
                {
                    nodeIds.Add(node.Id);
                }
            }

            _selectionService.SelectNodes(nodeIds);
            UpdatePropertyPanel();
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


        #region layer selection
        private void PopulateLayerCombo()
        {
            if (_layout?.LayerManager == null) return;

            ActiveLayerCombo.SelectionChanged -= ActiveLayer_Changed;
            ActiveLayerCombo.Items.Clear();

            foreach (var layer in _layout.LayerManager.Layers)
            {
                var item = new ComboBoxItem
                {
                    Content = layer.Name,
                    Tag = layer.Id
                };
                ActiveLayerCombo.Items.Add(item);

                if (layer.Id == _layout.LayerManager.ActiveLayerId)
                {
                    ActiveLayerCombo.SelectedItem = item;
                }
            }

            ActiveLayerCombo.SelectionChanged += ActiveLayer_Changed;
        }

        private void ActiveLayer_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveLayerCombo.SelectedItem is ComboBoxItem item && item.Tag is string layerId)
            {
                _layout?.LayerManager?.SetActiveLayer(layerId);
                StatusText.Text = $"Active layer: {item.Content}";
            }
        }

        private void UpdateActiveLayerDisplay()
        {
            if (_layout?.LayerManager == null) return;

            var activeId = _layout.LayerManager.ActiveLayerId;
            foreach (ComboBoxItem item in ActiveLayerCombo.Items)
            {
                if (item.Tag is string id && id == activeId)
                {
                    ActiveLayerCombo.SelectionChanged -= ActiveLayer_Changed;
                    ActiveLayerCombo.SelectedItem = item;
                    ActiveLayerCombo.SelectionChanged += ActiveLayer_Changed;
                    break;
                }
            }
        }
        #endregion

    }
}
