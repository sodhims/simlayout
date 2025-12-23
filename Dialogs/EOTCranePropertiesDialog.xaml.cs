using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class EOTCranePropertiesDialog : Window
    {
        private readonly EOTCraneData _crane;
        private readonly LayoutData _layout;

        public EOTCranePropertiesDialog(EOTCraneData crane, LayoutData layout)
        {
            InitializeComponent();

            _crane = crane ?? throw new ArgumentNullException(nameof(crane));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            LoadCraneData();
        }

        private void LoadCraneData()
        {
            // Header
            CraneNameHeader.Text = _crane.Name;

            // Basic info
            NameInput.Text = _crane.Name;

            // Populate runway dropdown
            if (_layout.Runways != null)
            {
                foreach (var runway in _layout.Runways)
                {
                    RunwayCombo.Items.Add(new ComboBoxItem { Content = runway.Name, Tag = runway.Id });
                }

                // Select current runway
                for (int i = 0; i < RunwayCombo.Items.Count; i++)
                {
                    if (RunwayCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == _crane.RunwayId)
                    {
                        RunwayCombo.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Bay dimensions
            BayWidthInput.Text = _crane.BayWidth.ToString("F1");
            ReachLeftInput.Text = _crane.ReachLeft.ToString("F1");
            ReachRightInput.Text = _crane.ReachRight.ToString("F1");

            // Zone constraints (convert 0-1 to 0-100 for display)
            ZoneMinInput.Text = (_crane.ZoneMin * 100).ToString("F1");
            ZoneMaxInput.Text = (_crane.ZoneMax * 100).ToString("F1");
            BridgePositionInput.Text = (_crane.BridgePosition * 100).ToString("F1");

            // Speeds
            SpeedBridgeInput.Text = _crane.SpeedBridge.ToString("F2");
            SpeedTrolleyInput.Text = _crane.SpeedTrolley.ToString("F2");
            SpeedHoistInput.Text = _crane.SpeedHoist.ToString("F2");

            // Color
            ColorInput.Text = _crane.Color ?? "#E67E22";
            UpdateColorPreview();
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
            // Simple color picker - cycle through preset colors
            string[] colors = { "#E67E22", "#3498DB", "#27AE60", "#9B59B6", "#E74C3C", "#F39C12", "#1ABC9C" };
            int currentIndex = Array.IndexOf(colors, ColorInput.Text.ToUpper());
            int nextIndex = (currentIndex + 1) % colors.Length;
            ColorInput.Text = colors[nextIndex];
            UpdateColorPreview();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            // Apply changes
            _crane.Name = NameInput.Text.Trim();

            // Update runway assignment
            if (RunwayCombo.SelectedItem is ComboBoxItem selectedRunway)
            {
                _crane.RunwayId = selectedRunway.Tag?.ToString() ?? _crane.RunwayId;
            }

            // Bay dimensions
            if (double.TryParse(BayWidthInput.Text, out double bayWidth))
                _crane.BayWidth = bayWidth;
            if (double.TryParse(ReachLeftInput.Text, out double reachLeft))
                _crane.ReachLeft = reachLeft;
            if (double.TryParse(ReachRightInput.Text, out double reachRight))
                _crane.ReachRight = reachRight;

            // Zone constraints (convert 0-100 back to 0-1)
            if (double.TryParse(ZoneMinInput.Text, out double zoneMin))
                _crane.ZoneMin = Math.Clamp(zoneMin / 100.0, 0, 1);
            if (double.TryParse(ZoneMaxInput.Text, out double zoneMax))
                _crane.ZoneMax = Math.Clamp(zoneMax / 100.0, 0, 1);
            if (double.TryParse(BridgePositionInput.Text, out double bridgePos))
                _crane.BridgePosition = Math.Clamp(bridgePos / 100.0, _crane.ZoneMin, _crane.ZoneMax);

            // Speeds
            if (double.TryParse(SpeedBridgeInput.Text, out double speedBridge))
                _crane.SpeedBridge = speedBridge;
            if (double.TryParse(SpeedTrolleyInput.Text, out double speedTrolley))
                _crane.SpeedTrolley = speedTrolley;
            if (double.TryParse(SpeedHoistInput.Text, out double speedHoist))
                _crane.SpeedHoist = speedHoist;

            // Color
            _crane.Color = ColorInput.Text;

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

            // Validate zone constraints
            if (double.TryParse(ZoneMinInput.Text, out double zoneMin) &&
                double.TryParse(ZoneMaxInput.Text, out double zoneMax))
            {
                if (zoneMin >= zoneMax)
                {
                    MessageBox.Show("Zone Start must be less than Zone End.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ZoneMinInput.Focus();
                    return false;
                }
            }

            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Missing ComboBoxItem class - using System.Windows.Controls
        private class ComboBoxItem : System.Windows.Controls.ComboBoxItem { }
    }
}
