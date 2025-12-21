using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;
using Microsoft.Win32;

namespace LayoutEditor.Dialogs
{
    public partial class NodeTypeEditorDialog : Window
    {
        private readonly NodeTypeConfigService _configService;
        private readonly IconService _iconService;
        private NodeTypeConfig? _currentNodeType;
        private bool _isDirty, _isLoading;

        private static readonly string[] PresetColors = { "#3498DB", "#E74C3C", "#2ECC71", "#F5A623", 
            "#9B59B6", "#1ABC9C", "#34495E", "#E91E63", "#FF9800", "#795548" };

        public NodeTypeEditorDialog()
        {
            _isLoading = true;
            InitializeComponent();
            _configService = NodeTypeConfigService.Instance;
            _iconService = IconService.Instance;

            try
            {
                PopulateIconCombo();
                PopulateCategoryCombo();
                RefreshList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Init error: {ex.Message}");
            }
            finally { _isLoading = false; }
        }

        private void PopulateIconCombo()
        {
            IconComboBox.Items.Clear();
            foreach (var icon in _iconService.GetAvailableIcons())
                IconComboBox.Items.Add(icon);
        }

        private void PopulateCategoryCombo()
        {
            CategoryComboBox.Items.Clear();
            var cats = new[] { "Processing", "Storage", "Transport", "Flow", "Shipping", "People", "General" };
            foreach (var cat in cats) CategoryComboBox.Items.Add(cat);
            foreach (var cat in _configService.GetCategories().Except(cats, StringComparer.OrdinalIgnoreCase))
                CategoryComboBox.Items.Add(cat);
        }

        private void RefreshList(string? filter = null, string? category = null)
        {
            var types = _configService.GetAllNodeTypes().ToList();
            if (!string.IsNullOrWhiteSpace(filter))
                types = types.Where(n => n.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    n.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(category) && category != "All Categories")
                types = types.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            
            NodeTypeList.ItemsSource = types.OrderBy(n => n.Category).ThenBy(n => n.DisplayName).ToList();
        }

        #region Event Handlers

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoading) RefreshList(SearchBox.Text, GetSelectedCategory());
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading) RefreshList(SearchBox.Text, GetSelectedCategory());
        }

        private string? GetSelectedCategory() => (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();

        private void NodeTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            if (_isDirty && MessageBox.Show("Discard unsaved changes?", "Unsaved Changes",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            if (NodeTypeList.SelectedItem is NodeTypeConfig config) LoadIntoEditor(config);
            else ClearEditor();
        }

        private void Field_Changed(object sender, EventArgs e)
        {
            if (_isLoading) return;
            _isDirty = true;
            SaveButton.IsEnabled = true;
            if (sender == ColorTextBox) UpdateColorPreview();
            UpdateNodePreview();
        }

        private void IconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            _isDirty = true;
            SaveButton.IsEnabled = true;
            UpdateIconPreview();
            UpdateNodePreview();
        }

        private void ColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var current = ColorTextBox?.Text?.ToUpper() ?? "";
            var idx = Array.FindIndex(PresetColors, c => c.Equals(current, StringComparison.OrdinalIgnoreCase));
            ColorTextBox.Text = PresetColors[(idx + 1) % PresetColors.Length];
        }

        private void ImportIcon_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Import Icon"
            };
            if (dlg.ShowDialog() != true) return;

            var name = KeyTextBox?.Text?.Trim().ToLower().Replace(" ", "_") ?? "custom";
            if (string.IsNullOrEmpty(name) || name == "new_type")
                name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName).ToLower().Replace(" ", "_");

            var result = _iconService.ImportIcon(dlg.FileName, name);
            if (result != null)
            {
                PopulateIconCombo();
                IconComboBox.Text = result;
                UpdateIconPreview();
                UpdateNodePreview();
                MessageBox.Show($"Icon imported as '{result}'", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else MessageBox.Show("Failed to import icon.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty && MessageBox.Show("Discard unsaved changes?", "Unsaved",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            _isLoading = true;
            _currentNodeType = null;
            NodeTypeList.SelectedItem = null;
            SetEditorFields("new_type", "New Type", "", "#3498DB", "machine", "General", 80, 60, "");
            EditorPanel.IsEnabled = true;
            _isLoading = false;
            _isDirty = true;
            SaveButton.IsEnabled = true;
            KeyTextBox.Focus();
            KeyTextBox.SelectAll();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (NodeTypeList.SelectedItem is not NodeTypeConfig config) return;
            if (MessageBox.Show($"Delete '{config.DisplayName}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _configService.DeleteNodeType(config.Key);
                RefreshList();
                ClearEditor();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(KeyTextBox.Text))
            {
                MessageBox.Show("Key is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                KeyTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(DisplayNameTextBox.Text))
            {
                MessageBox.Show("Display Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                DisplayNameTextBox.Focus();
                return;
            }

            var key = KeyTextBox.Text.Trim().ToLower().Replace(" ", "_");
            var isNew = _currentNodeType == null || !_currentNodeType.Key.Equals(key, StringComparison.OrdinalIgnoreCase);
            
            if (isNew && _configService.HasNodeType(key))
            {
                MessageBox.Show($"Key '{key}' already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                KeyTextBox.Focus();
                return;
            }

            if (_currentNodeType != null && !_currentNodeType.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                _configService.DeleteNodeType(_currentNodeType.Key);

            var config = new NodeTypeConfig
            {
                Key = key,
                DisplayName = DisplayNameTextBox.Text.Trim(),
                Label = LabelTextBox.Text.Trim(),
                Color = ColorTextBox.Text.Trim(),
                Icon = IconComboBox.Text.Trim(),
                Category = CategoryComboBox.Text.Trim(),
                DefaultWidth = int.TryParse(WidthTextBox.Text, out var w) ? w : 80,
                DefaultHeight = int.TryParse(HeightTextBox.Text, out var h) ? h : 60,
                Description = DescriptionTextBox.Text.Trim()
            };

            _configService.SaveNodeType(config);
            _isDirty = false;
            SaveButton.IsEnabled = false;
            RefreshList();
            NodeTypeList.SelectedItem = ((IEnumerable<NodeTypeConfig>)NodeTypeList.ItemsSource)
                .FirstOrDefault(n => n.Key == key);
            MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Editor Helpers

        private void LoadIntoEditor(NodeTypeConfig c)
        {
            _isLoading = true;
            _currentNodeType = c;
            SetEditorFields(c.Key, c.DisplayName, c.Label, c.Color ?? "#3498DB", 
                c.Icon ?? "machine", c.Category ?? "General", c.DefaultWidth, c.DefaultHeight, c.Description);
            EditorPanel.IsEnabled = true;
            _isDirty = false;
            SaveButton.IsEnabled = false;
            _isLoading = false;
        }

        private void ClearEditor()
        {
            _isLoading = true;
            _currentNodeType = null;
            SetEditorFields("", "", "", "#3498DB", "machine", "General", 80, 60, "");
            NodePreview?.Children.Clear();
            EditorPanel.IsEnabled = false;
            _isDirty = false;
            SaveButton.IsEnabled = false;
            _isLoading = false;
        }

        private void SetEditorFields(string key, string name, string label, string color, 
            string icon, string category, int width, int height, string desc)
        {
            KeyTextBox.Text = key;
            DisplayNameTextBox.Text = name;
            LabelTextBox.Text = label;
            ColorTextBox.Text = color;
            IconComboBox.Text = icon;
            CategoryComboBox.Text = category;
            WidthTextBox.Text = width.ToString();
            HeightTextBox.Text = height.ToString();
            DescriptionTextBox.Text = desc;
            UpdateColorPreview();
            UpdateIconPreview();
            UpdateNodePreview();
        }

        private void UpdateColorPreview()
        {
            if (ColorPreview == null || ColorTextBox == null) return;
            try { ColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorTextBox.Text)); }
            catch { ColorPreview.Background = Brushes.Gray; }
        }

        private void UpdateIconPreview()
        {
            if (IconPreview != null)
                IconPreview.Source = _iconService.GetIcon(IconComboBox?.Text ?? "machine", 48);
        }

        private void UpdateNodePreview()
        {
            if (NodePreview == null) return;
            NodePreview.Children.Clear();

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(ColorTextBox?.Text ?? "#3498DB");
                var brush = new SolidColorBrush(color);

                // Rectangle
                var rect = new Rectangle
                {
                    Width = 80, Height = 50,
                    Fill = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B)),
                    Stroke = brush, StrokeThickness = 2, RadiusX = 4, RadiusY = 4
                };
                Canvas.SetLeft(rect, 10); Canvas.SetTop(rect, 10);
                NodePreview.Children.Add(rect);

                // Icon
                var icon = _iconService.GetIcon(IconComboBox?.Text ?? "machine", 24);
                if (icon != null)
                {
                    var img = new Image { Source = icon, Width = 24, Height = 24 };
                    Canvas.SetLeft(img, 38); Canvas.SetTop(img, 14);
                    NodePreview.Children.Add(img);
                }

                // Name
                var nameBlock = new TextBlock
                {
                    Text = DisplayNameTextBox?.Text ?? "Node", FontSize = 9,
                    TextAlignment = TextAlignment.Center, Width = 76,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(nameBlock, 12); Canvas.SetTop(nameBlock, 42);
                NodePreview.Children.Add(nameBlock);

                // Label badge
                var label = LabelTextBox?.Text;
                if (!string.IsNullOrEmpty(label))
                {
                    var badge = new Border
                    {
                        Background = brush, CornerRadius = new CornerRadius(2), Padding = new Thickness(3, 1, 3, 1),
                        Child = new TextBlock { Text = label, FontSize = 8, FontWeight = FontWeights.Bold, Foreground = Brushes.White }
                    };
                    Canvas.SetLeft(badge, 12); Canvas.SetTop(badge, 12);
                    NodePreview.Children.Add(badge);
                }
            }
            catch { /* ignore preview errors */ }
        }

        #endregion
    }
}
