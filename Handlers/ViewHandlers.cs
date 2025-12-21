using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Services;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for view-related operations: panel toggles, routing, etc.
    /// </summary>
    public partial class MainWindow
    {
        #region Panel Toggle Handlers

        private double _leftPanelWidth = 200;
        private double _rightPanelWidth = 220;

        private void ToggleLeftPanel_Click(object sender, RoutedEventArgs e)
        {
            if (LeftPanel.Visibility == Visibility.Visible)
            {
                // Save current width before collapsing
                _leftPanelWidth = LeftPanelColumn.Width.Value > 0 ? LeftPanelColumn.Width.Value : 200;
                LeftPanel.Visibility = Visibility.Collapsed;
                LeftPanelColumn.Width = new GridLength(0);
                LeftPanelColumn.MinWidth = 0;
                LeftPanelToggle.Content = new TextBlock { Text = "▶", FontSize = 10 };
                LeftPanelToggle.IsChecked = false;
            }
            else
            {
                LeftPanel.Visibility = Visibility.Visible;
                LeftPanelColumn.Width = new GridLength(_leftPanelWidth);
                LeftPanelColumn.MinWidth = 150;
                LeftPanelToggle.Content = new TextBlock { Text = "◀", FontSize = 10 };
                LeftPanelToggle.IsChecked = true;
            }
        }

        private void ToggleRightPanel_Click(object sender, RoutedEventArgs e)
        {
            if (RightPanel.Visibility == Visibility.Visible)
            {
                // Save current width before collapsing
                _rightPanelWidth = RightPanelColumn.Width.Value > 0 ? RightPanelColumn.Width.Value : 220;
                RightPanel.Visibility = Visibility.Collapsed;
                RightPanelColumn.Width = new GridLength(0);
                RightPanelColumn.MinWidth = 0;
                RightPanelToggle.Content = new TextBlock { Text = "◀", FontSize = 10 };
                RightPanelToggle.IsChecked = false;
            }
            else
            {
                RightPanel.Visibility = Visibility.Visible;
                RightPanelColumn.Width = new GridLength(_rightPanelWidth);
                RightPanelColumn.MinWidth = 180;
                RightPanelToggle.Content = new TextBlock { Text = "▶", FontSize = 10 };
                RightPanelToggle.IsChecked = true;
            }
        }

        #endregion

        #region Routing Handlers

        private void RerouteAllPaths_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Walls.Count == 0)
            {
                StatusText.Text = "No walls to route around";
                return;
            }

            if (_layout.Paths.Count == 0)
            {
                StatusText.Text = "No paths to re-route";
                return;
            }

            SaveUndoState();

            var router = new AutoRouterService(_layout);
            int rerouted = router.RerouteAllPaths();

            MarkDirty();
            Redraw();

            var stats = router.GetRoutingStats();
            StatusText.Text = $"Re-routed {rerouted} paths. {stats.crossing} still crossing walls.";
        }

        #endregion
        private void ToggleExplorerPanel_Click(object sender, RoutedEventArgs e)
        {
            var anchorable = FindAnchorable("explorer");
            if (anchorable != null)
            {
                if (anchorable.IsVisible)
                    anchorable.Hide();
                else
                    anchorable.Show();
            }
        }

    }
}
