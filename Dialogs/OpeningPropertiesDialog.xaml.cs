using System;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class OpeningPropertiesDialog : Window
    {
        private readonly OpeningData _opening;

        public OpeningPropertiesDialog(OpeningData opening)
        {
            InitializeComponent();

            _opening = opening ?? throw new ArgumentNullException(nameof(opening));
            LoadOpeningData();
        }

        private void LoadOpeningData()
        {
            // Header
            OpeningNameHeader.Text = _opening.Name;

            // Basic info
            NameInput.Text = _opening.Name;

            // Select opening type
            string openingType = _opening.OpeningType?.ToLower() ?? "door";
            foreach (ComboBoxItem item in OpeningTypeCombo.Items)
            {
                if (item.Tag?.ToString() == openingType)
                {
                    OpeningTypeCombo.SelectedItem = item;
                    break;
                }
            }
            if (OpeningTypeCombo.SelectedItem == null)
                OpeningTypeCombo.SelectedIndex = 0;

            // Select state
            string state = _opening.State?.ToLower() ?? "open";
            foreach (ComboBoxItem item in StateCombo.Items)
            {
                if (item.Tag?.ToString() == state)
                {
                    StateCombo.SelectedItem = item;
                    break;
                }
            }
            if (StateCombo.SelectedItem == null)
                StateCombo.SelectedIndex = 0;

            // Location
            XInput.Text = _opening.X.ToString("F1");
            YInput.Text = _opening.Y.ToString("F1");
            RotationInput.Text = _opening.Rotation.ToString("F1");

            // Physical dimensions
            ClearWidthInput.Text = _opening.ClearWidth.ToString("F1");
            ClearHeightInput.Text = _opening.ClearHeight.ToString("F1");
            MaxLoadInput.Text = _opening.MaxLoadWeight.ToString("F1");

            // Flow control
            string directionMode = _opening.DirectionMode?.ToLower() ?? "bidirectional";
            foreach (ComboBoxItem item in DirectionCombo.Items)
            {
                if (item.Tag?.ToString() == directionMode)
                {
                    DirectionCombo.SelectedItem = item;
                    break;
                }
            }
            if (DirectionCombo.SelectedItem == null)
                DirectionCombo.SelectedIndex = 0;

            CapacityInput.Text = _opening.Capacity.ToString();
            TraversalTimeInput.Text = _opening.TraversalTime.ToString("F1");

            // Zone connections
            FromZoneInput.Text = _opening.FromZoneId ?? "";
            ToZoneInput.Text = _opening.ToZoneId ?? "";
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            // Apply changes
            _opening.Name = NameInput.Text.Trim();

            // Opening type
            if (OpeningTypeCombo.SelectedItem is ComboBoxItem typeItem)
            {
                _opening.OpeningType = typeItem.Tag?.ToString() ?? "door";
            }

            // State
            if (StateCombo.SelectedItem is ComboBoxItem stateItem)
            {
                _opening.State = stateItem.Tag?.ToString() ?? "open";
            }

            // Location
            if (double.TryParse(XInput.Text, out double x))
                _opening.X = x;
            if (double.TryParse(YInput.Text, out double y))
                _opening.Y = y;
            if (double.TryParse(RotationInput.Text, out double rotation))
                _opening.Rotation = rotation;

            // Physical dimensions
            if (double.TryParse(ClearWidthInput.Text, out double clearWidth))
                _opening.ClearWidth = clearWidth;
            if (double.TryParse(ClearHeightInput.Text, out double clearHeight))
                _opening.ClearHeight = clearHeight;
            if (double.TryParse(MaxLoadInput.Text, out double maxLoad))
                _opening.MaxLoadWeight = maxLoad;

            // Flow control
            if (DirectionCombo.SelectedItem is ComboBoxItem dirItem)
            {
                _opening.DirectionMode = dirItem.Tag?.ToString() ?? "bidirectional";
            }
            if (int.TryParse(CapacityInput.Text, out int capacity))
                _opening.Capacity = capacity;
            if (double.TryParse(TraversalTimeInput.Text, out double traversalTime))
                _opening.TraversalTime = traversalTime;

            // Zone connections
            _opening.FromZoneId = string.IsNullOrWhiteSpace(FromZoneInput.Text) ? "" : FromZoneInput.Text.Trim();
            _opening.ToZoneId = string.IsNullOrWhiteSpace(ToZoneInput.Text) ? "" : ToZoneInput.Text.Trim();

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

            if (!double.TryParse(ClearWidthInput.Text, out double clearWidth) || clearWidth <= 0)
            {
                MessageBox.Show("Clear Width must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearWidthInput.Focus();
                return false;
            }

            if (!double.TryParse(ClearHeightInput.Text, out double clearHeight) || clearHeight <= 0)
            {
                MessageBox.Show("Clear Height must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearHeightInput.Focus();
                return false;
            }

            if (!int.TryParse(CapacityInput.Text, out int capacity) || capacity < 0)
            {
                MessageBox.Show("Capacity must be a non-negative integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CapacityInput.Focus();
                return false;
            }

            if (!double.TryParse(TraversalTimeInput.Text, out double traversalTime) || traversalTime < 0)
            {
                MessageBox.Show("Traversal Time must be a non-negative number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TraversalTimeInput.Focus();
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
