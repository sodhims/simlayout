using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Template Operations

        private void RefreshTemplateList()
        {
            if (TemplateList == null) return;

            TemplateList.Items.Clear();

            foreach (var template in _layout.Templates)
            {
                TemplateList.Items.Add(new ListBoxItem
                {
                    Content = template.Name,
                    Tag = template.Id
                });
            }
        }

        private void Template_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_layout == null) return;  // Not initialized yet
            
            // Handle template selection for placing
            if (TemplateList?.SelectedItem is ListBoxItem item)
            {
                var templateId = item.Tag?.ToString();
                // TODO: Enable template placement mode
            }
        }

        private void AddTemplate_Click(object sender, RoutedEventArgs e)
        {
            var selectedNodes = _selectionService.GetSelectedNodes(_layout);
            if (selectedNodes.Count == 0)
            {
                MessageBox.Show("Select nodes to save as template.", "Create Template",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // TODO: Show dialog to name template
            var templateName = $"Template_{_layout.Templates.Count + 1}";

            var template = new Models.TemplateData
            {
                Id = System.Guid.NewGuid().ToString(),
                Name = templateName,
                Nodes = selectedNodes.Select(n => Helpers.JsonHelper.Deserialize<Models.NodeData>(
                    Helpers.JsonHelper.Serialize(n))!).ToList()
            };

            _layout.Templates.Add(template);
            RefreshTemplateList();
            StatusText.Text = $"Created template '{templateName}'";
        }

        private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (TemplateList?.SelectedItem is not ListBoxItem item) return;

            var templateId = item.Tag?.ToString();
            var template = _layout.Templates.FirstOrDefault(t => t.Id == templateId);

            if (template != null)
            {
                _layout.Templates.Remove(template);
                RefreshTemplateList();
                StatusText.Text = $"Deleted template '{template.Name}'";
            }
        }

        #endregion
    }
}
