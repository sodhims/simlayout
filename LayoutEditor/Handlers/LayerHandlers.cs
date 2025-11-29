using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Layer Panel Integration

        private void InitializeLayers()
        {
            if (LayersPanelControl == null || _layout == null) return;

            // Initialize default layers if not present
            if (_layout.LayerManager.Layers.Count == 0)
            {
                _layout.LayerManager.InitializeDefaultLayers();
            }

            // Set up the panel
            LayersPanelControl.SetLayerManager(_layout.LayerManager);

            // Subscribe to events
            LayersPanelControl.LayerChanged += LayersPanel_LayerChanged;
            LayersPanelControl.ActiveLayerChanged += LayersPanel_ActiveLayerChanged;
            LayersPanelControl.LayerStyleChanged += LayersPanel_LayerStyleChanged;
        }

        private void LayersPanel_LayerChanged(object? sender, EventArgs e)
        {
            // Visibility or lock changed - redraw
            SyncLayerVisibilityToDisplay();
            Redraw();
        }

        private void LayersPanel_ActiveLayerChanged(object? sender, EventArgs e)
        {
            // Update tool based on active layer
            var activeLayer = _layout.LayerManager.ActiveLayer;
            if (activeLayer != null)
            {
                // Optionally switch to appropriate tool based on layer
                switch (activeLayer.LayerType)
                {
                    case LayerTypes.Walls:
                        // Don't auto-switch, just update status
                        StatusText.Text = $"Active layer: {activeLayer.Name} (W for wall tool)";
                        break;
                    case LayerTypes.Nodes:
                        StatusText.Text = $"Active layer: {activeLayer.Name}";
                        break;
                    case LayerTypes.Paths:
                        StatusText.Text = $"Active layer: {activeLayer.Name} (P for path tool)";
                        break;
                    case LayerTypes.Measurements:
                        StatusText.Text = $"Active layer: {activeLayer.Name} (R for measure tool)";
                        break;
                    default:
                        StatusText.Text = $"Active layer: {activeLayer.Name}";
                        break;
                }
            }
        }

        private void LayersPanel_LayerStyleChanged(object? sender, LayerData layer)
        {
            // Style changed - mark dirty and redraw
            MarkDirty();
            Redraw();
        }

        /// <summary>
        /// Sync layer visibility states to the old Display.Layers flags for backward compatibility
        /// </summary>
        private void SyncLayerVisibilityToDisplay()
        {
            var lm = _layout.LayerManager;
            
            _layout.Display.Layers.Background = lm.IsLayerVisible(LayerTypes.Grid);
            _layout.Display.Layers.BackgroundImage = lm.IsLayerVisible(LayerTypes.BackgroundImage);
            _layout.Display.Layers.Walls = lm.IsLayerVisible(LayerTypes.Walls);
            _layout.Display.Layers.Zones = lm.IsLayerVisible(LayerTypes.Zones);
            _layout.Display.Layers.Corridors = lm.IsLayerVisible(LayerTypes.Corridors);
            _layout.Display.Layers.Paths = lm.IsLayerVisible(LayerTypes.Paths);
            _layout.Display.Layers.Nodes = lm.IsLayerVisible(LayerTypes.Nodes);
            _layout.Display.Layers.Measurements = lm.IsLayerVisible(LayerTypes.Measurements);
            _layout.Display.Layers.Labels = lm.IsLayerVisible(LayerTypes.Annotations);

            // Update checkboxes
            UpdateLayerCheckboxes();
        }

        /// <summary>
        /// Get the layer style for a specific layer type
        /// </summary>
        public LayerStyle? GetLayerStyle(string layerType)
        {
            return _layout.LayerManager.GetLayerByType(layerType)?.Style;
        }

        /// <summary>
        /// Check if a layer is locked (prevent editing)
        /// </summary>
        public bool IsLayerLocked(string layerType)
        {
            return _layout.LayerManager.IsLayerLocked(layerType);
        }

        /// <summary>
        /// Get the active layer for new element creation
        /// </summary>
        public LayerData? GetActiveLayer()
        {
            return _layout.LayerManager.ActiveLayer;
        }

        /// <summary>
        /// Refresh the layers panel display
        /// </summary>
        public void RefreshLayersPanel()
        {
            LayersPanelControl?.RefreshLayerList();
        }

        #endregion
    }
}
