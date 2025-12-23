using System;
using System.Windows;

namespace LayoutEditor.Dialogs
{
    public partial class CustomLayoutDialog : Window
    {
        public LayoutGenerationConfig Config { get; private set; }

        public CustomLayoutDialog()
        {
            InitializeComponent();
            Config = new LayoutGenerationConfig();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            // Parse and validate all inputs
            if (!TryParseInt(StorageBinsBox.Text, out int storageBins, "Storage Bins")) return;
            if (!TryParseInt(BuffersBox.Text, out int buffers, "Buffers")) return;
            if (!TryParseInt(MachinesBox.Text, out int machines, "Machines")) return;
            if (!TryParseInt(AGVStationsBox.Text, out int agvStations, "AGV Stations")) return;
            if (!TryParseInt(AGVsBox.Text, out int agvs, "AGVs")) return;
            if (!TryParseInt(EOTCranesBox.Text, out int eotCranes, "EOT Cranes")) return;
            if (!TryParseInt(JibCranesBox.Text, out int jibCranes, "Jib Cranes")) return;

            // Parse simple zone count (always available)
            if (!TryParseInt(SimpleZoneCountBox.Text, out int simpleZoneCount, "Number of Zones")) return;

            // Parse zone configuration if grid layout is enabled
            int zoneCount = 0;
            int entitiesPerZone = 0;
            if (UseGridLayoutCheck.IsChecked == true)
            {
                if (!TryParseInt(ZoneCountBox.Text, out zoneCount, "Number of Zones (Grid)")) return;
                if (!TryParseInt(EntitiesPerZoneBox.Text, out entitiesPerZone, "Entities per Zone")) return;
            }

            // Use simple zone count if not using grid layout, otherwise use grid zone count
            int finalZoneCount = (UseGridLayoutCheck.IsChecked == true) ? zoneCount : simpleZoneCount;

            // Build configuration
            Config = new LayoutGenerationConfig
            {
                StorageBinCount = storageBins,
                BufferCount = buffers,
                MachineCount = machines,
                AGVStationCount = agvStations,
                AGVCount = agvs,
                EOTCraneCount = eotCranes,
                JibCraneCount = jibCranes,
                GenerateAGVPaths = GenerateAGVPathsCheck.IsChecked ?? true,
                GenerateZones = GenerateZonesCheck.IsChecked ?? true,
                RandomizePlacement = PlaceRandomlyCheck.IsChecked ?? false,
                UseGridLayout = UseGridLayoutCheck.IsChecked ?? false,
                ZoneCount = finalZoneCount,
                EntitiesPerZone = entitiesPerZone,
                ZoneServiceType = (ZoneServiceCombo.SelectedIndex >= 0) ? ZoneServiceCombo.SelectedIndex : 0
            };

            DialogResult = true;
            Close();
        }

        private void GridLayout_Checked(object sender, RoutedEventArgs e)
        {
            if (ZoneConfigGroup != null)
                ZoneConfigGroup.IsEnabled = true;
        }

        private void GridLayout_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ZoneConfigGroup != null)
                ZoneConfigGroup.IsEnabled = false;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool TryParseInt(string text, out int value, string fieldName)
        {
            if (!int.TryParse(text, out value) || value < 0)
            {
                MessageBox.Show($"{fieldName} must be a non-negative integer.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (value > 100)
            {
                var result = MessageBox.Show($"{fieldName} is set to {value}. This may take a while to generate. Continue?",
                    "Large Count Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Configuration for custom layout generation
    /// </summary>
    public class LayoutGenerationConfig
    {
        public int StorageBinCount { get; set; }
        public int BufferCount { get; set; }
        public int MachineCount { get; set; }
        public int AGVStationCount { get; set; }
        public int AGVCount { get; set; }
        public int EOTCraneCount { get; set; }
        public int JibCraneCount { get; set; }
        public bool GenerateAGVPaths { get; set; }
        public bool GenerateZones { get; set; }
        public bool RandomizePlacement { get; set; }

        // Grid layout with zones
        public bool UseGridLayout { get; set; }
        public int ZoneCount { get; set; }
        public int EntitiesPerZone { get; set; }
        public int ZoneServiceType { get; set; } // 0=AGV, 1=Jib, 2=EOT, 3=Mixed
    }
}
