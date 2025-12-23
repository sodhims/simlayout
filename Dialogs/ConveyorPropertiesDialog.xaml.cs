using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class ConveyorPropertiesDialog : Window
    {
        private readonly ConveyorData _conveyor;

        public ConveyorPropertiesDialog(ConveyorData conveyor)
        {
            InitializeComponent();

            _conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));
            LoadConveyorData();
        }

        private void LoadConveyorData()
        {
            // Header
            ConveyorNameHeader.Text = _conveyor.Name;

            // Basic info
            NameInput.Text = _conveyor.Name;

            // Select conveyor type
            string conveyorType = _conveyor.ConveyorType?.ToLower() ?? "belt";
            foreach (ComboBoxItem item in ConveyorTypeCombo.Items)
            {
                if (item.Tag?.ToString() == conveyorType)
                {
                    ConveyorTypeCombo.SelectedItem = item;
                    break;
                }
            }
            if (ConveyorTypeCombo.SelectedItem == null)
                ConveyorTypeCombo.SelectedIndex = 0;

            // Physical properties
            WidthInput.Text = _conveyor.Width.ToString("F1");
            SpeedInput.Text = _conveyor.Speed.ToString("F2");
            UpdatePathLengthLabel();

            // Direction & flow
            string direction = _conveyor.Direction?.ToLower() ?? "forward";
            foreach (ComboBoxItem item in DirectionCombo.Items)
            {
                if (item.Tag?.ToString() == direction)
                {
                    DirectionCombo.SelectedItem = item;
                    break;
                }
            }
            if (DirectionCombo.SelectedItem == null)
                DirectionCombo.SelectedIndex = 0;

            IsAccumulatingCheck.IsChecked = _conveyor.IsAccumulating;

            // Connections
            FromNodeInput.Text = _conveyor.FromNodeId ?? "";
            ToNodeInput.Text = _conveyor.ToNodeId ?? "";

            // Appearance
            ColorInput.Text = _conveyor.Color ?? "#FFA500";
            UpdateColorPreview();
        }

        private void UpdatePathLengthLabel()
        {
            if (_conveyor.Path != null && _conveyor.Path.Count >= 2)
            {
                double totalLength = 0;
                for (int i = 1; i < _conveyor.Path.Count; i++)
                {
                    var p1 = _conveyor.Path[i - 1];
                    var p2 = _conveyor.Path[i];
                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    totalLength += Math.Sqrt(dx * dx + dy * dy);
                }
                PathLengthLabel.Text = totalLength.ToString("F1");
            }
            else
            {
                PathLengthLabel.Text = "N/A (no path defined)";
            }
        }

        private void UpdateColorPreview()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(ColorInput.Text);
                ColorPreview.Background = new SolidColorBrush(color);
            }
            catch
            {
                ColorPreview.Background = Brushes.Orange;
            }
        }

        private void ColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            string[] colors = { "#FFA500", "#FF6B35", "#2ECC71", "#3498DB", "#9B59B6", "#E74C3C", "#1ABC9C", "#F39C12" };
            CycleColor(ColorInput, colors);
            UpdateColorPreview();
        }

        private void CycleColor(TextBox input, string[] colors)
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
            _conveyor.Name = NameInput.Text.Trim();

            // Conveyor type
            if (ConveyorTypeCombo.SelectedItem is ComboBoxItem typeItem)
            {
                _conveyor.ConveyorType = typeItem.Tag?.ToString() ?? "belt";
            }

            // Physical properties
            if (double.TryParse(WidthInput.Text, out double width))
                _conveyor.Width = width;
            if (double.TryParse(SpeedInput.Text, out double speed))
                _conveyor.Speed = speed;

            // Direction & flow
            if (DirectionCombo.SelectedItem is ComboBoxItem dirItem)
            {
                _conveyor.Direction = dirItem.Tag?.ToString() ?? "forward";
            }
            _conveyor.IsAccumulating = IsAccumulatingCheck.IsChecked ?? false;

            // Connections
            _conveyor.FromNodeId = string.IsNullOrWhiteSpace(FromNodeInput.Text) ? null : FromNodeInput.Text.Trim();
            _conveyor.ToNodeId = string.IsNullOrWhiteSpace(ToNodeInput.Text) ? null : ToNodeInput.Text.Trim();

            // Appearance
            _conveyor.Color = ColorInput.Text;

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

            if (!double.TryParse(SpeedInput.Text, out double speed) || speed <= 0)
            {
                MessageBox.Show("Speed must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                SpeedInput.Focus();
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
