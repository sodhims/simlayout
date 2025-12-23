using System;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class AGVStationPropertiesDialog : Window
    {
        private readonly AGVStationData _station;

        public AGVStationPropertiesDialog(AGVStationData station)
        {
            InitializeComponent();

            _station = station ?? throw new ArgumentNullException(nameof(station));
            LoadStationData();
        }

        private void LoadStationData()
        {
            // Header
            StationNameHeader.Text = _station.Name;

            // Basic info
            NameInput.Text = _station.Name;

            // Select station type
            string stationType = _station.StationType?.ToLower() ?? "pickup";
            foreach (ComboBoxItem item in StationTypeCombo.Items)
            {
                if (item.Tag?.ToString() == stationType)
                {
                    StationTypeCombo.SelectedItem = item;
                    break;
                }
            }
            if (StationTypeCombo.SelectedItem == null)
                StationTypeCombo.SelectedIndex = 0;

            // Location
            XInput.Text = _station.X.ToString("F1");
            YInput.Text = _station.Y.ToString("F1");
            RotationInput.Text = _station.Rotation.ToString("F1");

            // Docking
            DockingToleranceInput.Text = _station.DockingTolerance.ToString("F1");
            ApproachAngleInput.Text = (_station.ApproachAngle ?? 0).ToString("F1");

            // Timing
            ServiceTimeInput.Text = _station.ServiceTime.ToString("F1");
            DwellTimeInput.Text = (_station.DwellTime ?? 5).ToString("F1");

            // Queue
            QueueCapacityInput.Text = (_station.QueueCapacity ?? 3).ToString();
            PriorityInput.Text = (_station.Priority ?? 0).ToString();
            IsBlockingCheck.IsChecked = _station.IsBlocking ?? false;
            IsHomingCheck.IsChecked = _station.IsHoming;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            // Apply changes
            _station.Name = NameInput.Text.Trim();

            // Station type
            if (StationTypeCombo.SelectedItem is ComboBoxItem typeItem)
            {
                _station.StationType = typeItem.Tag?.ToString() ?? "pickup";
            }

            // Location
            if (double.TryParse(XInput.Text, out double x))
                _station.X = x;
            if (double.TryParse(YInput.Text, out double y))
                _station.Y = y;
            if (double.TryParse(RotationInput.Text, out double rotation))
                _station.Rotation = rotation;

            // Docking
            if (double.TryParse(DockingToleranceInput.Text, out double dockTol))
                _station.DockingTolerance = dockTol;
            if (double.TryParse(ApproachAngleInput.Text, out double approachAngle))
                _station.ApproachAngle = approachAngle;

            // Timing
            if (double.TryParse(ServiceTimeInput.Text, out double serviceTime))
                _station.ServiceTime = serviceTime;
            if (double.TryParse(DwellTimeInput.Text, out double dwellTime))
                _station.DwellTime = dwellTime;

            // Queue
            if (int.TryParse(QueueCapacityInput.Text, out int queueCap))
                _station.QueueCapacity = queueCap;
            if (int.TryParse(PriorityInput.Text, out int priority))
                _station.Priority = priority;
            _station.IsBlocking = IsBlockingCheck.IsChecked ?? false;
            _station.IsHoming = IsHomingCheck.IsChecked ?? false;

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

            if (!double.TryParse(ServiceTimeInput.Text, out double serviceTime) || serviceTime < 0)
            {
                MessageBox.Show("Service Time must be a non-negative number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ServiceTimeInput.Focus();
                return false;
            }

            if (!int.TryParse(QueueCapacityInput.Text, out int queueCap) || queueCap < 0)
            {
                MessageBox.Show("Queue Capacity must be a non-negative integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                QueueCapacityInput.Focus();
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
