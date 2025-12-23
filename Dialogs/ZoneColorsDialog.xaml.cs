using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class ZoneColorsDialog : Window
    {
        private readonly LayoutData _layout;
        private readonly Action _onColorChanged;

        public ZoneColorsDialog(LayoutData layout, Action onColorChanged)
        {
            InitializeComponent();
            _layout = layout;
            _onColorChanged = onColorChanged;
            PopulateZoneList();
        }

        private void PopulateZoneList()
        {
            ZoneListPanel.Children.Clear();

            if (_layout.Zones.Count == 0)
            {
                ZoneListPanel.Children.Add(new TextBlock
                {
                    Text = "No zones in the current layout.\nGenerate a layout with zones first.",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(10)
                });
                return;
            }

            foreach (var zone in _layout.Zones)
            {
                var panel = CreateZoneColorRow(zone);
                ZoneListPanel.Children.Add(panel);
            }
        }

        private Grid CreateZoneColorRow(ZoneData zone)
        {
            var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Zone name
            var nameText = new TextBlock
            {
                Text = zone.Name ?? zone.Id,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            // Color swatch (clickable)
            var colorSwatch = new Border
            {
                Width = 50,
                Height = 25,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = GetZoneBrush(zone),
                Margin = new Thickness(5, 0, 5, 0)
            };
            colorSwatch.MouseLeftButtonDown += (s, e) => ShowColorPicker(zone, colorSwatch);
            Grid.SetColumn(colorSwatch, 1);
            grid.Children.Add(colorSwatch);

            // Color code display
            var colorCode = new TextBlock
            {
                Text = GetColorHex(zone),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(colorCode, 2);
            grid.Children.Add(colorCode);

            // Zone type
            var typeText = new TextBlock
            {
                Text = $"({zone.Type ?? "default"})",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(typeText, 3);
            grid.Children.Add(typeText);

            return grid;
        }

        private Brush GetZoneBrush(ZoneData zone)
        {
            if (zone.Visual != null && !string.IsNullOrEmpty(zone.Visual.FillColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(zone.Visual.FillColor);
                    return new SolidColorBrush(color);
                }
                catch { }
            }

            // Return default color based on type
            var defaultColor = zone.Type?.ToLower() switch
            {
                "warehouse" => Color.FromArgb(120, 255, 255, 150),
                "storage" => Color.FromArgb(120, 150, 255, 150),
                "production" => Color.FromArgb(120, 150, 200, 255),
                "shipping" => Color.FromArgb(120, 255, 180, 150),
                "receiving" => Color.FromArgb(120, 200, 150, 255),
                _ => Color.FromArgb(100, 200, 200, 200)
            };
            return new SolidColorBrush(defaultColor);
        }

        private string GetColorHex(ZoneData zone)
        {
            if (zone.Visual != null && !string.IsNullOrEmpty(zone.Visual.FillColor))
                return zone.Visual.FillColor;
            return "(default)";
        }

        private void ShowColorPicker(ZoneData zone, Border swatch)
        {
            // Create a simple color picker popup
            var popup = new Window
            {
                Title = $"Pick Color for {zone.Name}",
                Width = 300,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var mainPanel = new StackPanel { Margin = new Thickness(15) };

            // Predefined colors grid
            var colorsLabel = new TextBlock { Text = "Pick a color:", Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(colorsLabel);

            var colorGrid = new WrapPanel();
            var predefinedColors = new[]
            {
                // Light pastels
                "#80FFFFAA", "#80AAFFAA", "#80AADDFF", "#80FFDDAA", "#80DDAAFF",
                "#80FFAAAA", "#80FFFFDD", "#80DDFFDD", "#80DDDDFF", "#80FFDDDD",
                // Medium colors
                "#A0FFFF80", "#A080FF80", "#A080CCFF", "#A0FFCC80", "#A0CC80FF",
                "#A0FF8080", "#A0FFFF99", "#A099FF99", "#A09999FF", "#A0FF9999",
                // Darker tints
                "#C0DDDD60", "#C060DD60", "#C06099DD", "#C0DD9960", "#C09960DD",
                "#C0DD6060", "#C0DDDD77", "#C077DD77", "#C07777DD", "#C0DD7777",
            };

            foreach (var colorHex in predefinedColors)
            {
                var colorBtn = new Border
                {
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex))
                };
                colorBtn.MouseLeftButtonDown += (s, e) =>
                {
                    SetZoneColor(zone, colorHex);
                    swatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
                    popup.Close();
                    PopulateZoneList(); // Refresh to update color code display
                };
                colorGrid.Children.Add(colorBtn);
            }
            mainPanel.Children.Add(colorGrid);

            // Custom hex input
            var customLabel = new TextBlock { Text = "Or enter custom hex (#AARRGGBB):", Margin = new Thickness(0, 15, 0, 5) };
            mainPanel.Children.Add(customLabel);

            var customPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var customInput = new TextBox
            {
                Width = 150,
                Text = zone.Visual?.FillColor ?? "#80FFFF80",
                Margin = new Thickness(0, 0, 10, 0)
            };
            var applyBtn = new Button { Content = "Apply", Width = 60 };
            applyBtn.Click += (s, e) =>
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(customInput.Text);
                    SetZoneColor(zone, customInput.Text);
                    swatch.Background = new SolidColorBrush(color);
                    popup.Close();
                    PopulateZoneList();
                }
                catch
                {
                    MessageBox.Show("Invalid color format. Use #AARRGGBB or #RRGGBB format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            customPanel.Children.Add(customInput);
            customPanel.Children.Add(applyBtn);
            mainPanel.Children.Add(customPanel);

            // Cancel button
            var cancelBtn = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(0, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            cancelBtn.Click += (s, e) => popup.Close();
            mainPanel.Children.Add(cancelBtn);

            popup.Content = mainPanel;
            popup.ShowDialog();
        }

        private void SetZoneColor(ZoneData zone, string colorHex)
        {
            if (zone.Visual == null)
                zone.Visual = new ZoneVisual();

            zone.Visual.FillColor = colorHex;
            _onColorChanged?.Invoke();
        }

        private void ResetColors_Click(object sender, RoutedEventArgs e)
        {
            foreach (var zone in _layout.Zones)
            {
                if (zone.Visual != null)
                {
                    zone.Visual.FillColor = null;
                    zone.Visual.BorderColor = null;
                }
            }
            PopulateZoneList();
            _onColorChanged?.Invoke();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
