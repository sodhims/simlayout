using System;
using System.IO;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;
using Microsoft.Win32;

namespace LayoutEditor.Dialogs
{
    public partial class PostgresExportDialog : Window
    {
        private readonly LayoutData _layout;

        public PostgresExportDialog(LayoutData layout, string layoutName)
        {
            InitializeComponent();
            _layout = layout;
            LayoutNameBox.Text = layoutName;
        }

        private void SaveSqlFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName = $"{LayoutNameBox.Text.Replace(" ", "_")}_export.sql",
                Title = "Save SQL Export File"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var service = new PostgresExportService("");
                    var sql = service.GenerateSqlScript(_layout, LayoutNameBox.Text);
                    File.WriteAllText(dialog.FileName, sql);
                    
                    StatusText.Text = $"✓ SQL file saved to:\n{dialog.FileName}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    
                    MessageBox.Show($"SQL file saved successfully!\n\n{dialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"✗ Error: {ex.Message}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show($"Error saving SQL file:\n{ex.Message}", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportToDatabase_Click(object sender, RoutedEventArgs e)
        {
            var connectionString = $"Host={HostBox.Text};Port={PortBox.Text};Database={DatabaseBox.Text};Username={UsernameBox.Text};Password={PasswordBox.Password}";
            
            try
            {
                ExportDbButton.IsEnabled = false;
                StatusText.Text = "Connecting to database...";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;

                var service = new PostgresExportService(connectionString);
                await service.ExportAsync(_layout, LayoutNameBox.Text);

                StatusText.Text = $"✓ Successfully exported '{LayoutNameBox.Text}' to database";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                
                MessageBox.Show($"Layout exported to database successfully!", 
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"✗ Database error: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Error exporting to database:\n{ex.Message}\n\nMake sure PostgreSQL is running and the database exists.", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExportDbButton.IsEnabled = true;
            }
        }

        private void SaveSchema_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName = "LayoutSchema.sql",
                Title = "Save Database Schema"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Try to find schema from resources or embedded
                    var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "LayoutSchema.sql");
                    
                    if (File.Exists(schemaPath))
                    {
                        File.Copy(schemaPath, dialog.FileName, overwrite: true);
                    }
                    else
                    {
                        // Generate basic schema inline
                        var schema = GenerateBasicSchema();
                        File.WriteAllText(dialog.FileName, schema);
                    }
                    
                    StatusText.Text = $"✓ Schema saved to:\n{dialog.FileName}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    
                    MessageBox.Show($"Schema file saved!\n\nRun this in PostgreSQL to create the tables:\npsql -d your_database -f \"{dialog.FileName}\"", 
                        "Schema Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving schema:\n{ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GenerateBasicSchema()
        {
            return @"-- Layout Editor PostgreSQL Schema
-- Run this to create the required tables

CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

CREATE TABLE IF NOT EXISTS layouts (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    canvas_width DOUBLE PRECISION DEFAULT 1200,
    canvas_height DOUBLE PRECISION DEFAULT 800,
    grid_size INTEGER DEFAULT 20,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS nodes (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    node_type VARCHAR(50) NOT NULL,
    label VARCHAR(255) DEFAULT '',
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    width DOUBLE PRECISION DEFAULT 80,
    height DOUBLE PRECISION DEFAULT 60,
    rotation DOUBLE PRECISION DEFAULT 0,
    color VARCHAR(20) DEFAULT '#4A90D9',
    icon VARCHAR(100) DEFAULT 'machine_generic',
    input_terminal_position VARCHAR(20) DEFAULT 'left',
    output_terminal_position VARCHAR(20) DEFAULT 'right',
    servers INTEGER DEFAULT 1,
    capacity INTEGER DEFAULT 1,
    mtbf DOUBLE PRECISION,
    mttr DOUBLE PRECISION,
    process_time DOUBLE PRECISION,
    setup_time DOUBLE PRECISION,
    queue_discipline VARCHAR(20) DEFAULT 'FIFO',
    entity_type VARCHAR(50) DEFAULT 'part',
    batch_size INTEGER DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS paths (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    from_node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    to_node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    connection_type VARCHAR(50) DEFAULT 'partFlow',
    path_type VARCHAR(50) DEFAULT 'single',
    routing_mode VARCHAR(50) DEFAULT 'direct',
    color VARCHAR(20) DEFAULT '#888888',
    distance DOUBLE PRECISION,
    speed DOUBLE PRECISION DEFAULT 1.0,
    capacity INTEGER DEFAULT 10,
    lanes INTEGER DEFAULT 1,
    bidirectional BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS path_waypoints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    path_id UUID NOT NULL REFERENCES paths(id) ON DELETE CASCADE,
    sequence_order INTEGER NOT NULL,
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL
);

CREATE TABLE IF NOT EXISTS cells (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    cell_index INTEGER DEFAULT 0,
    cell_type VARCHAR(50) DEFAULT 'simple',
    color VARCHAR(20) DEFAULT '#9B59B6',
    input_terminal_position VARCHAR(20) DEFAULT 'left',
    output_terminal_position VARCHAR(20) DEFAULT 'right',
    is_collapsed BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS cell_members (
    cell_id UUID NOT NULL REFERENCES cells(id) ON DELETE CASCADE,
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (cell_id, node_id)
);

CREATE TABLE IF NOT EXISTS cell_entry_points (
    cell_id UUID NOT NULL REFERENCES cells(id) ON DELETE CASCADE,
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (cell_id, node_id)
);

CREATE TABLE IF NOT EXISTS cell_exit_points (
    cell_id UUID NOT NULL REFERENCES cells(id) ON DELETE CASCADE,
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (cell_id, node_id)
);

CREATE TABLE IF NOT EXISTS walls (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    x1 DOUBLE PRECISION NOT NULL,
    y1 DOUBLE PRECISION NOT NULL,
    x2 DOUBLE PRECISION NOT NULL,
    y2 DOUBLE PRECISION NOT NULL,
    thickness DOUBLE PRECISION DEFAULT 6,
    wall_type VARCHAR(50) DEFAULT 'standard',
    color VARCHAR(20) DEFAULT '#444444',
    line_style VARCHAR(20) DEFAULT 'solid',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS transport_networks (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    color VARCHAR(20) DEFAULT '#E67E22',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS transport_stations (
    id UUID PRIMARY KEY,
    network_id UUID NOT NULL REFERENCES transport_networks(id) ON DELETE CASCADE,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    station_type VARCHAR(50) DEFAULT 'pickup',
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    rotation DOUBLE PRECISION DEFAULT 0,
    color VARCHAR(20) DEFAULT '#9B59B6',
    size DOUBLE PRECISION DEFAULT 50,
    dwell_time DOUBLE PRECISION DEFAULT 10,
    capacity INTEGER DEFAULT 5,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS transport_tracks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    network_id UUID NOT NULL REFERENCES transport_networks(id) ON DELETE CASCADE,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    from_point_id UUID NOT NULL REFERENCES transport_stations(id) ON DELETE CASCADE,
    to_point_id UUID NOT NULL REFERENCES transport_stations(id) ON DELETE CASCADE,
    is_bidirectional BOOLEAN DEFAULT true,
    distance DOUBLE PRECISION DEFAULT 0,
    speed_limit DOUBLE PRECISION DEFAULT 2.0,
    color VARCHAR(20) DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS transporters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    network_id UUID NOT NULL REFERENCES transport_networks(id) ON DELETE CASCADE,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    transporter_type VARCHAR(50) DEFAULT 'agv',
    home_station_id UUID REFERENCES transport_stations(id),
    speed DOUBLE PRECISION DEFAULT 1.5,
    load_capacity INTEGER DEFAULT 1,
    color VARCHAR(20) DEFAULT '#E74C3C',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_nodes_layout ON nodes(layout_id);
CREATE INDEX IF NOT EXISTS idx_paths_layout ON paths(layout_id);
CREATE INDEX IF NOT EXISTS idx_cells_layout ON cells(layout_id);
CREATE INDEX IF NOT EXISTS idx_walls_layout ON walls(layout_id);
";
        }
    }
}
