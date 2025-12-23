using System.Windows;
using System.Linq;
using AvalonDock.Layout;

namespace LayoutEditor
{
    /// <summary>
    /// Panel toggle handlers for View menu
    /// </summary>
    public partial class MainWindow
    {
        #region Panel Toggle Handlers

        private void ToggleTransportLayersPanel_Click(object sender, RoutedEventArgs e)
        {
            TogglePanelById("transport_layers");
        }

        private void ToggleValidationPanel_Click(object sender, RoutedEventArgs e)
        {
            TogglePanelById("validation");
        }

        private void ToggleEquipmentBrowserPanel_Click(object sender, RoutedEventArgs e)
        {
            TogglePanelById("equipment_browser");
        }

        private void ResetDockLayout_Click(object sender, RoutedEventArgs e)
        {
            // Show all panels
            ShowPanelById("toolbox");
            ShowPanelById("transport");
            ShowPanelById("explorer");
            ShowPanelById("transport_layers");
            ShowPanelById("validation");
            ShowPanelById("equipment_browser");

            StatusText.Text = "All panels now visible";
        }

        #endregion

        #region Section Toggle Handlers (within Toolbox)

        private void ToggleFloorSection_Click(object sender, RoutedEventArgs e)
        {
            if (FloorSection != null)
            {
                FloorSection.Visibility = ShowFloorSection.IsChecked 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void ToggleNodeToolbox_Click(object sender, RoutedEventArgs e)
        {
            if (NodeToolbox != null)
            {
                NodeToolbox.Visibility = ShowNodeToolbox.IsChecked 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void ToggleTemplatesSection_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesSection != null)
            {
                TemplatesSection.Visibility = ShowTemplatesSection.IsChecked 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        // X button close handlers
        private void CloseFloorSection_Click(object sender, RoutedEventArgs e)
        {
            if (FloorSection != null)
            {
                FloorSection.Visibility = Visibility.Collapsed;
                ShowFloorSection.IsChecked = false;
            }
        }

        private void CloseTemplatesSection_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesSection != null)
            {
                TemplatesSection.Visibility = Visibility.Collapsed;
                ShowTemplatesSection.IsChecked = false;
            }
        }

        #endregion

        #region Panel Helper Methods

        private void TogglePanelById(string contentId)
        {
            var anchorable = FindAnchorableById(contentId);
            if (anchorable != null)
            {
                if (anchorable.IsVisible)
                    anchorable.Hide();
                else
                    anchorable.Show();
            }
        }

        private void ShowPanelById(string contentId)
        {
            var anchorable = FindAnchorableById(contentId);
            if (anchorable != null && !anchorable.IsVisible)
            {
                anchorable.Show();
            }
        }

        private LayoutAnchorable? FindAnchorableById(string contentId)
        {
            if (DockManager?.Layout == null) return null;
            
            return DockManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == contentId);
        }

        #endregion
    }
}
