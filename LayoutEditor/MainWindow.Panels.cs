using System;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Controls;
using LayoutEditor.Helpers;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Partial class for MainWindow - handles floating panels
    /// Call InitializeFloatingPanels() from your MainWindow constructor
    /// </summary>
    public partial class MainWindow
    {
        private PanelManager? _panelManager;
        
        /// <summary>
        /// Call this from your MainWindow constructor after InitializeComponent()
        /// </summary>
        public void InitializeFloatingPanels()
        {
            _panelManager = new PanelManager(this);
            
            // Position panels after window is loaded
            Loaded += (s, e) =>
            {
                _panelManager.PositionPanels();
                
                // Load current layout into panels
                if (_layout != null)
                {
                    _panelManager.LoadLayout(_layout);
                }
            };
            
            // Wire up toolbox events
            _panelManager.Toolbox.NodeTypeSelected += OnToolboxNodeSelected;
            _panelManager.Toolbox.StartNodeDrag += OnToolboxDragStart;
            
            // Wire up explorer events
            _panelManager.Explorer.NodeSelected += OnExplorerNodeSelected;
            _panelManager.Explorer.PathSelected += OnExplorerPathSelected;
            
            // Wire up layout templates
            _panelManager.Layouts.LayoutLoaded += OnTemplateLayoutLoaded;
            
            // Wire up property changes
            _panelManager.Properties.NodePropertyChanged += OnPanelPropertyChanged;
        }
        
        #region Panel Toggle Methods
        
        public void ToggleToolboxPanel()
        {
            _panelManager?.TogglePanel("toolbox");
        }
        
        public void TogglePropertiesPanel()
        {
            _panelManager?.TogglePanel("properties");
        }
        
        public void ToggleExplorerPanel()
        {
            _panelManager?.TogglePanel("explorer");
        }
        
        public void ToggleLayoutsPanel()
        {
            _panelManager?.TogglePanel("layouts");
        }
        
        public void ShowAllFloatingPanels()
        {
            _panelManager?.ShowAllPanels();
        }
        
        public void HideAllFloatingPanels()
        {
            _panelManager?.HideAllPanels();
        }
        
        #endregion
        
        #region Panel Event Handlers
        
        private void OnToolboxNodeSelected(string nodeType)
        {
            // Switch to node placement mode with this type
            StatusText.Text = $"Click on canvas to place {nodeType}";
        }
        
        private void OnToolboxDragStart(string nodeType)
        {
            var data = new DataObject("NodeType", nodeType);
            DragDrop.DoDragDrop(_panelManager!.Toolbox, data, DragDropEffects.Copy);
        }
        
        private void OnExplorerNodeSelected(NodeData node)
        {
            // Select the node on canvas
            _selectionService?.ClearSelection();
            _selectionService?.SelectNode(node.Id);
            _panelManager?.ShowNodeProperties(node);
            RefreshAll();
        }
        
        private void OnExplorerPathSelected(PathData path)
        {
            // Optional: select path if you have path selection
        }
        
        private void OnTemplateLayoutLoaded(LayoutData layout)
        {
            if (_layout?.Nodes?.Count > 0)
            {
                var result = MessageBox.Show(
                    "Replace current layout with template?",
                    "Load Template",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            _layout = layout;
            _panelManager?.LoadLayout(_layout);
            _panelManager?.Layouts.Hide();
            RefreshAll();
            StatusText.Text = "Template loaded";
        }
        
        private void OnPanelPropertyChanged(NodeData node)
        {
            RefreshAll();
        }
        
        #endregion
        
        #region Update Panels
        
        /// <summary>
        /// Call after loading a new layout
        /// </summary>
        public void UpdateFloatingPanels()
        {
            if (_panelManager != null && _layout != null)
            {
                _panelManager.LoadLayout(_layout);
            }
        }
        
        /// <summary>
        /// Call when selection changes
        /// </summary>
        public void UpdatePropertiesPanel()
        {
            if (_panelManager == null) return;
            
            // Use SelectionService methods directly
            if (_selectionService?.HasSingleNodeSelection == true)
            {
                var node = _selectionService?.GetSelectedNode(_layout);
                if (node != null)
                {
                    _panelManager.ShowNodeProperties(node);
                    return;
                }
            }
            
            _panelManager.ClearSelection();
        }
        
        /// <summary>
        /// Call after any layout changes
        /// </summary>
        public void RefreshFloatingPanels()
        {
            _panelManager?.RefreshAll();
        }
        
        /// <summary>
        /// Call on window closing
        /// </summary>
        public void CleanupFloatingPanels()
        {
            _panelManager?.Dispose();
        }
        
        #endregion
        
        #region Keyboard Shortcuts
        
        /// <summary>
        /// Call from Window_KeyDown. Returns true if handled.
        /// </summary>
        public bool HandlePanelShortcuts(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.T:
                        ToggleToolboxPanel();
                        return true;
                    case Key.D1:
                        TogglePropertiesPanel();
                        return true;
                    case Key.D2:
                        ToggleExplorerPanel();
                        return true;
                    case Key.D3:
                        ToggleLayoutsPanel();
                        return true;
                }
            }
            return false;
        }
        
        #endregion
        
        #region Canvas Drop Handler
        
        /// <summary>
        /// Add to EditorCanvas: AllowDrop="True" Drop="EditorCanvas_Drop"
        /// </summary>
        private void EditorCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("NodeType"))
            {
                string nodeType = (string)e.Data.GetData("NodeType");
                Point position = e.GetPosition(EditorCanvas);
                
                // Snap to grid if enabled
                if (SnapToGridMenu?.IsChecked == true)
                {
                    double gridSize = 20;
                    position = new Point(
                        Math.Round(position.X / gridSize) * gridSize,
                        Math.Round(position.Y / gridSize) * gridSize
                    );
                }
                
                // Create node using factory
                var node = Models.LayoutFactory.CreateNode(nodeType, position.X, position.Y);
                node.Name = $"{node.Name} {(_layout?.Nodes?.Count ?? 0) + 1}";
                
                _layout?.Nodes?.Add(node);
                
                RefreshAll();
                RefreshFloatingPanels();
                
                // Select the new node
                _selectionService?.ClearSelection();
                _selectionService?.SelectNode(node.Id);
            }
        }
        
        #endregion
    }
}
