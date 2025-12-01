using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Helpers;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Validation

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            var results = ValidationHelper.ValidateLayout(_layout);

            if (results.Count == 0)
            {
                if (ValidationStatus != null)
                {
                    ValidationStatus.Text = "✓ Valid";
                    ValidationStatus.Foreground = new SolidColorBrush(Colors.Green);
                }
                StatusText.Text = "Layout validation passed";
            }
            else
            {
                var errors = results.Count(r => r.Severity == "error");
                var warnings = results.Count(r => r.Severity == "warning");

                if (ValidationStatus != null)
                {
                    ValidationStatus.Text = $"✗ {errors} error(s), {warnings} warning(s)";
                    ValidationStatus.Foreground = errors > 0
                        ? new SolidColorBrush(Colors.Red)
                        : new SolidColorBrush(Colors.Orange);
                }

                // Show first few issues
                var message = string.Join("\n", results.Take(5).Select(r => $"• {r.Message}"));
                if (results.Count > 5)
                    message += $"\n... and {results.Count - 5} more";

                StatusText.Text = results.First().Message;

                MessageBox.Show(message, "Validation Results",
                    MessageBoxButton.OK,
                    errors > 0 ? MessageBoxImage.Error : MessageBoxImage.Warning);
            }
        }

        private void ValidationList_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Navigate to selected validation issue
            if (ValidationList?.SelectedItem is ListBoxItem item && item.Tag is string elementId)
            {
                // Try to select the element
                var node = _layout?.Nodes.FirstOrDefault(n => n.Id == elementId);
                if (node != null)
                {
                    _selectionService?.SelectNode(node.Id);
                    UpdateSelectionVisuals();
                    Redraw();
                }
            }
        }

        #endregion
    }
}
