using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class ZonePropertiesDialog : Window
    {
        private readonly ZoneData _zone;

        public ZonePropertiesDialog(ZoneData zone)
        {
            InitializeComponent();

            _zone = zone ?? throw new ArgumentNullException(nameof(zone));
            LoadZoneData();
        }

        private void LoadZoneData()
        {
            // Header
            ZoneNameHeader.Text = _zone.Name;

            // Basic info
            NameInput.Text = _zone.Name;

            // Select zone type
            string zoneType = _zone.Type?.ToLower() ?? "storage";
            foreach (ComboBoxItem item in ZoneTypeCombo.Items)
            {
                if (item.Tag?.ToString() == zoneType)
                {
                    ZoneTypeCombo.SelectedItem = item;
                    break;
                }
            }
            if (ZoneTypeCombo.SelectedItem == null)
                ZoneTypeCombo.SelectedIndex = 0;

            // Dimensions
            XInput.Text = _zone.X.ToString("F1");
            YInput.Text = _zone.Y.ToString("F1");
            WidthInput.Text = _zone.Width.ToString("F1");
            HeightInput.Text = _zone.Height.ToString("F1");
            UpdateAreaLabel();

            // Capacity
            CapacityInput.Text = (_zone.Capacity ?? 100).ToString();
            MaxOccupancyInput.Text = (_zone.MaxOccupancy ?? 0).ToString();
            IsRestrictedCheck.IsChecked = _zone.IsRestricted ?? false;

            // Colors
            FillColorInput.Text = _zone.Visual?.FillColor ?? "#3498DB";
            BorderColorInput.Text = _zone.Visual?.BorderColor ?? "#2980B9";
            UpdateColorPreviews();
        }

        private void UpdateAreaLabel()
        {
            if (double.TryParse(WidthInput.Text, out double w) && double.TryParse(HeightInput.Text, out double h))
            {
                double area = w * h;
                AreaLabel.Text = $"Area: {area:N0} sq units";
            }
        }

        private void UpdateColorPreviews()
        {
            try
            {
                var fillColor = (Color)ColorConverter.ConvertFromString(FillColorInput.Text);
                FillColorPreview.Background = new SolidColorBrush(fillColor);
            }
            catch
            {
                FillColorPreview.Background = Brushes.Blue;
            }

            try
            {
                var borderColor = (Color)ColorConverter.ConvertFromString(BorderColorInput.Text);
                BorderColorPreview.Background = new SolidColorBrush(borderColor);
            }
            catch
            {
                BorderColorPreview.Background = Brushes.DarkBlue;
            }
        }

        private void FillColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            string[] colors = { "#3498DB", "#27AE60", "#E67E22", "#9B59B6", "#E74C3C", "#1ABC9C", "#F39C12", "#95A5A6" };
            CycleColor(FillColorInput, colors);
            UpdateColorPreviews();
        }

        private void BorderColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            string[] colors = { "#2980B9", "#1E8449", "#CA6F1E", "#7D3C98", "#C0392B", "#148F77", "#D68910", "#7F8C8D" };
            CycleColor(BorderColorInput, colors);
            UpdateColorPreviews();
        }

        private void CycleColor(System.Windows.Controls.TextBox input, string[] colors)
        {
            int currentIndex = Array.IndexOf(colors, input.Text.ToUpper());
            int nextIndex = (currentIndex + 1) % colors.Length;
            input.Text = colors[nextIndex];
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            // Apply changes
            _zone.Name = NameInput.Text.Trim();

            // Zone type
            if (ZoneTypeCombo.SelectedItem is ComboBoxItem typeItem)
            {
                _zone.Type = typeItem.Tag?.ToString() ?? "storage";
            }

            // Dimensions
            if (double.TryParse(XInput.Text, out double x))
                _zone.X = x;
            if (double.TryParse(YInput.Text, out double y))
                _zone.Y = y;
            if (double.TryParse(WidthInput.Text, out double width))
                _zone.Width = width;
            if (double.TryParse(HeightInput.Text, out double height))
                _zone.Height = height;

            // Capacity
            if (int.TryParse(CapacityInput.Text, out int capacity))
                _zone.Capacity = capacity;
            if (int.TryParse(MaxOccupancyInput.Text, out int maxOccupancy))
                _zone.MaxOccupancy = maxOccupancy;
            _zone.IsRestricted = IsRestrictedCheck.IsChecked ?? false;

            // Colors
            if (_zone.Visual == null)
                _zone.Visual = new ZoneVisual();
            _zone.Visual.FillColor = FillColorInput.Text;
            _zone.Visual.BorderColor = BorderColorInput.Text;

            DialogResult = true;
            Close();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameInput.Focus();
                return false;
            }

            if (!double.TryParse(WidthInput.Text, out double width) || width <= 0)
            {
                MessageBox.Show("Width must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                WidthInput.Focus();
                return false;
            }

            if (!double.TryParse(HeightInput.Text, out double height) || height <= 0)
            {
                MessageBox.Show("Height must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                HeightInput.Focus();
                return false;
            }

            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
