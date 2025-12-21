using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Controls
{
    public partial class TransportLayersPanel : UserControl
    {
        private ArchitectureLayerManager? _layerManager;

        public event EventHandler? LayerVisibilityChanged;
        public event EventHandler? ActiveLayerChanged;

        public TransportLayersPanel()
        {
            InitializeComponent();
        }

        public void SetArchitectureLayerManager(ArchitectureLayerManager manager)
        {
            _layerManager = manager;

            // Subscribe to manager events
            _layerManager.VisibilityChanged += (s, e) => RefreshLayerList();
            _layerManager.ActiveLayerChanged += (s, layer) => RefreshLayerList();
            _layerManager.LockedStateChanged += (s, layer) => RefreshLayerList();

            RefreshLayerList();
        }

        private void RefreshLayerList()
        {
            if (_layerManager == null) return;

            // Create view models for all 8 layers (in reverse order - Pedestrian at top)
            var items = LayerMetadata.AllLayers
                .Reverse()
                .Select(metadata => new TransportLayerViewModel
                {
                    LayerType = metadata.Layer,
                    Name = metadata.Name,
                    ColorCode = (Color)ColorConverter.ConvertFromString(metadata.DefaultColor),
                    IsVisible = _layerManager.IsVisible(metadata.Layer),
                    IsLocked = _layerManager.IsLocked(metadata.Layer),
                    IsActive = _layerManager.ActiveLayer == metadata.Layer
                })
                .ToList();

            LayersList.ItemsSource = items;
        }

        private void Layer_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is LayerType layerType)
            {
                if (_layerManager == null) return;

                _layerManager.ActiveLayer = layerType;
                RefreshLayerList();
                ActiveLayerChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Visibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is LayerType layerType)
            {
                if (_layerManager == null) return;

                _layerManager.SetVisibility(layerType, checkBox.IsChecked ?? true);
                LayerVisibilityChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true; // Prevent event from bubbling to Border click
            }
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is LayerType layerType)
            {
                if (_layerManager == null) return;

                _layerManager.SetLocked(layerType, checkBox.IsChecked ?? false);
                RefreshLayerList();
                e.Handled = true; // Prevent event from bubbling to Border click
            }
        }
    }

    /// <summary>
    /// View model for transport layer display
    /// </summary>
    public class TransportLayerViewModel
    {
        public LayerType LayerType { get; set; }
        public string Name { get; set; } = "";
        public Color ColorCode { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public bool IsActive { get; set; }
    }
}
