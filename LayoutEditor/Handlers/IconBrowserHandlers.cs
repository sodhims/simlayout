using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Icon Browser

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            var iconWindow = new Window
            {
                Title = "Select Icon",
                Width = 420,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };

            var mainPanel = new StackPanel();

            foreach (var category in IconLibrary.GetCategories())
            {
                // Category header
                var header = new TextBlock
                {
                    Text = category,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 12, 0, 6),
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                };
                mainPanel.Children.Add(header);

                // Icons in category
                var wrapPanel = new WrapPanel();

                foreach (var kvp in IconLibrary.GetByCategory(category))
                {
                    var iconKey = kvp.Key;
                    var iconDef = kvp.Value;

                    var button = CreateIconButton(iconKey, iconDef, node, iconWindow);
                    wrapPanel.Children.Add(button);
                }

                mainPanel.Children.Add(wrapPanel);
            }

            scrollViewer.Content = mainPanel;
            iconWindow.Content = scrollViewer;
            iconWindow.ShowDialog();
        }

        private Button CreateIconButton(string iconKey, IconDefinition iconDef,
            Models.NodeData node, Window parentWindow)
        {
            var button = new Button
            {
                Width = 44,
                Height = 44,
                Margin = new Thickness(2),
                ToolTip = iconDef.Name,
                Background = node.Visual.Icon == iconKey
                    ? new SolidColorBrush(Color.FromRgb(204, 232, 255))
                    : Brushes.White,
                BorderBrush = node.Visual.Icon == iconKey
                    ? new SolidColorBrush(Color.FromRgb(0, 122, 204))
                    : new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(node.Visual.Icon == iconKey ? 2 : 1)
            };

            try
            {
                var path = new Path
                {
                    Data = IconLibrary.GetGeometry(iconKey),
                    Stroke = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(iconDef.DefaultColor)),
                    StrokeThickness = 1.5,
                    Width = 26,
                    Height = 26,
                    Stretch = Stretch.Uniform
                };
                button.Content = path;
            }
            catch
            {
                button.Content = new TextBlock { Text = "?" };
            }

            button.Click += (s, ev) =>
            {
                SaveUndoState();
                node.Visual.Icon = iconKey;
                MarkDirty();
                Redraw();
                UpdatePropertyPanel();
                parentWindow.Close();
                StatusText.Text = $"Icon changed to '{iconDef.Name}'";
            };

            return button;
        }

        private void ColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var node = _selectionService.GetSelectedNode(_layout);
            if (node == null) return;

            // Cycle through preset colors
            var colors = new[]
            {
                "#4A90D9", "#2ECC71", "#E74C3C", "#F5A623", "#9B59B6",
                "#7ED321", "#95A5A6", "#3498DB", "#E67E22", "#1ABC9C"
            };

            var currentIndex = System.Array.IndexOf(colors, node.Visual.Color);
            var nextIndex = (currentIndex + 1) % colors.Length;

            SaveUndoState();
            node.Visual.Color = colors[nextIndex];
            MarkDirty();
            Redraw();
            UpdatePropertyPanel();
        }

        #endregion
    }
}
