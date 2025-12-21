using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Manages the 8-layer architecture including active layer tracking,
    /// visibility states, editability, and locked states.
    /// </summary>
    public class ArchitectureLayerManager
    {
        private LayerType _activeLayer;
        private readonly Dictionary<LayerType, bool> _visibilityStates;
        private readonly Dictionary<LayerType, bool> _editableStates;
        private readonly Dictionary<LayerType, bool> _lockedStates;

        /// <summary>
        /// Fired when the active layer changes.
        /// </summary>
        public event EventHandler<LayerType>? ActiveLayerChanged;

        /// <summary>
        /// Fired when any layer's visibility changes.
        /// </summary>
        public event EventHandler<LayerVisibilityChangedEventArgs>? VisibilityChanged;

        /// <summary>
        /// Fired when any layer's locked state changes.
        /// </summary>
        public event EventHandler<LayerType>? LockedStateChanged;

        /// <summary>
        /// Gets or sets the currently active layer.
        /// </summary>
        public LayerType ActiveLayer
        {
            get => _activeLayer;
            set
            {
                if (_activeLayer != value)
                {
                    _activeLayer = value;
                    ActiveLayerChanged?.Invoke(this, value);
                }
            }
        }

        public ArchitectureLayerManager()
        {
            _visibilityStates = new Dictionary<LayerType, bool>();
            _editableStates = new Dictionary<LayerType, bool>();
            _lockedStates = new Dictionary<LayerType, bool>();

            // Initialize all layers with defaults from metadata
            foreach (var metadata in LayerMetadata.AllLayers)
            {
                _visibilityStates[metadata.Layer] = metadata.DefaultVisible;
                _editableStates[metadata.Layer] = metadata.DefaultEditable;
                _lockedStates[metadata.Layer] = metadata.DefaultLocked;
            }

            // Set default active layer to Equipment
            _activeLayer = LayerType.Equipment;
        }

        /// <summary>
        /// Sets the visibility state for a layer.
        /// </summary>
        public void SetVisibility(LayerType layer, bool isVisible)
        {
            if (_visibilityStates[layer] != isVisible)
            {
                _visibilityStates[layer] = isVisible;
                VisibilityChanged?.Invoke(this, new LayerVisibilityChangedEventArgs(layer, isVisible));
            }
        }

        /// <summary>
        /// Gets the visibility state for a layer.
        /// </summary>
        public bool IsVisible(LayerType layer)
        {
            return _visibilityStates.TryGetValue(layer, out var visible) && visible;
        }

        /// <summary>
        /// Sets the editable state for a layer.
        /// </summary>
        public void SetEditable(LayerType layer, bool isEditable)
        {
            if (_editableStates[layer] != isEditable)
            {
                _editableStates[layer] = isEditable;
            }
        }

        /// <summary>
        /// Gets whether a layer is editable.
        /// Locked layers are never editable.
        /// </summary>
        public bool IsEditable(LayerType layer)
        {
            if (IsLocked(layer))
                return false;

            return _editableStates.TryGetValue(layer, out var editable) && editable;
        }

        /// <summary>
        /// Sets the locked state for a layer.
        /// </summary>
        public void SetLocked(LayerType layer, bool isLocked)
        {
            if (_lockedStates[layer] != isLocked)
            {
                _lockedStates[layer] = isLocked;
                LockedStateChanged?.Invoke(this, layer);
            }
        }

        /// <summary>
        /// Gets whether a layer is locked.
        /// </summary>
        public bool IsLocked(LayerType layer)
        {
            return _lockedStates.TryGetValue(layer, out var locked) && locked;
        }

        /// <summary>
        /// Gets all layers that are currently visible.
        /// </summary>
        public IEnumerable<LayerType> GetVisibleLayers()
        {
            return _visibilityStates
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .OrderBy(layer => (int)layer); // Return in layer order
        }

        /// <summary>
        /// Toggles visibility for a specific layer.
        /// </summary>
        public void ToggleVisibility(LayerType layer)
        {
            SetVisibility(layer, !IsVisible(layer));
        }

        /// <summary>
        /// Toggles locked state for a specific layer.
        /// </summary>
        public void ToggleLocked(LayerType layer)
        {
            SetLocked(layer, !IsLocked(layer));
        }

        /// <summary>
        /// Shows all layers.
        /// </summary>
        public void ShowAllLayers()
        {
            foreach (var layer in LayerMetadata.AllLayers)
            {
                SetVisibility(layer.Layer, true);
            }
        }

        /// <summary>
        /// Hides all layers.
        /// </summary>
        public void HideAllLayers()
        {
            foreach (var layer in LayerMetadata.AllLayers)
            {
                SetVisibility(layer.Layer, false);
            }
        }

        /// <summary>
        /// Resets all layers to their default states.
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (var metadata in LayerMetadata.AllLayers)
            {
                SetVisibility(metadata.Layer, metadata.DefaultVisible);
                SetEditable(metadata.Layer, metadata.DefaultEditable);
                SetLocked(metadata.Layer, metadata.DefaultLocked);
            }
            ActiveLayer = LayerType.Equipment;
        }
    }

    /// <summary>
    /// Event args for layer visibility changes.
    /// </summary>
    public class LayerVisibilityChangedEventArgs : EventArgs
    {
        public LayerType Layer { get; }
        public bool IsVisible { get; }

        public LayerVisibilityChangedEventArgs(LayerType layer, bool isVisible)
        {
            Layer = layer;
            IsVisible = isVisible;
        }
    }
}
