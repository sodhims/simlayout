using System;
using System.Windows.Controls;
using LayoutEditor.Models;

namespace LayoutEditor.Renderers
{
    /// <summary>
    /// Interface for layer-based renderers in the 8-layer transport architecture.
    /// Each renderer is responsible for drawing elements for a specific layer.
    /// </summary>
    public interface ILayerRenderer
    {
        /// <summary>
        /// The layer this renderer is responsible for.
        /// </summary>
        LayerType Layer { get; }

        /// <summary>
        /// Base Z-order for this layer (e.g., Infrastructure: 0-99, Spatial: 100-199).
        /// </summary>
        int ZOrderBase { get; }

        /// <summary>
        /// Render all elements for this layer onto the canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on</param>
        /// <param name="layout">The layout data containing elements to render</param>
        /// <param name="registerElement">Callback to register UI elements for hit testing (id, element)</param>
        void Render(Canvas canvas, LayoutData layout, Action<string, System.Windows.UIElement> registerElement);
    }
}
