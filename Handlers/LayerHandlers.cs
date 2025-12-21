using System.Windows;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Layer Visibility Handlers

        // LEGACY: Old layer visibility handlers removed
        // Layer visibility is now managed by ArchitectureLayerManager and TransportLayersPanel
        // This file is retained for potential future use but handlers are commented out

        /* REMOVED - Old checkbox-based layer visibility

        private bool _showTracks = true;

        private void Layer_Changed(object sender, RoutedEventArgs e)
        {
            // Layer visibility now managed by TransportLayersPanel
        }

        public void ShowFloorPlanOnly()
        {
            // Use ArchitectureLayerManager instead
        }

        public void ShowSimulationOnly()
        {
            // Use ArchitectureLayerManager instead
        }

        public void ShowAllLayers()
        {
            // Use ArchitectureLayerManager instead
        }

        */

        #endregion
    }
}
