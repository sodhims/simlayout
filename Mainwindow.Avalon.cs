using System.Linq;
using System.Windows;
using AvalonDock.Layout;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private LayoutAnchorable? FindAnchorable(string contentId)
        {
            return DockManager?.Layout?
                .Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == contentId);
        }

        private void ToggleToolboxPanel_Click(object sender, RoutedEventArgs e)
        {
            var a = FindAnchorable("toolbox");
            if (a == null) return;
            a.IsVisible = !a.IsVisible;
            if (a.IsVisible) a.IsActive = true;
        }

        private void ToggleTransportPanel_Click(object sender, RoutedEventArgs e)
        {
            var a = FindAnchorable("transport");
            if (a == null) return;
            a.IsVisible = !a.IsVisible;
            if (a.IsVisible) a.IsActive = true;
        }

        private void ToggleExplorerDock_Click(object sender, RoutedEventArgs e)
        {
            var a = FindAnchorable("explorer");
            if (a == null) return;
            a.ToggleAutoHide();
        }

        private void ToggleLayersDock_Click(object sender, RoutedEventArgs e)
        {
            var a = FindAnchorable("layers");
            if (a == null) return;
            a.ToggleAutoHide();
        }

        private void ToggleValidationDock_Click(object sender, RoutedEventArgs e)
        {
            var a = FindAnchorable("validation");
            if (a == null) return;
            a.ToggleAutoHide();
        }
    }
}
