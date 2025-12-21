using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using LayoutEditor.Models;
using Microsoft.Data.Sqlite;

namespace LayoutEditor.Services
{
    /// <summary>
    /// SQLite database service for portable layout storage
    /// Single .db file - no server required
    /// </summary>
    public class SqliteLayoutService
    {
        #region Save Layout

        /// <summary>
        /// Save layout to a SQLite database file
        /// </summary>
        public void SaveLayout(LayoutData layout, string filePath, string layoutName)
        {
            // Delete existing file to start fresh
            if (File.Exists(filePath))
                File.Delete(filePath);

            using var connection = new SqliteConnection($"Data Source={filePath}");
            connection.Open();

            CreateTables(connection);
            
            using var transaction = connection.BeginTransaction();
            try
            {
                var layoutId = Guid.NewGuid().ToString();
                InsertLayout(connection, layoutId, layout, layoutName);
                var nodeIdMap = InsertNodes(connection, layoutId, layout.Nodes);
                InsertPaths(connection, layoutId, layout.Paths, nodeIdMap);
                InsertCells(connection, layoutId, layout.Groups, nodeIdMap);
                InsertWalls(connection, layoutId, layout.Walls);
                InsertTransportNetworks(connection, layoutId, layout, nodeIdMap);
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void CreateTables(SqliteConnection conn)
        {
            var sql = @"
                CREATE TABLE layouts (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    canvas_width REAL DEFAULT 1200,
                    canvas_height REAL DEFAULT 800,
                    grid_size INTEGER DEFAULT 20,
                    frictionless_mode INTEGER DEFAULT 0,
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE nodes (
                    id TEXT PRIMARY KEY,
                    layout_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    node_type TEXT NOT NULL,
                    label TEXT DEFAULT '',
                    x REAL NOT NULL,
                    y REAL NOT NULL,
                    width REAL DEFAULT 80,
                    height REAL DEFAULT 60,
                    rotation REAL DEFAULT 0,
                    color TEXT DEFAULT '#4A90D9',
                    icon TEXT DEFAULT 'machine_generic',
                    input_terminal_position TEXT DEFAULT 'left',
                    output_terminal_position TEXT DEFAULT 'right',
                    servers INTEGER DEFAULT 1,
                    capacity INTEGER DEFAULT 1,
                    mtbf REAL,
                    mttr REAL,
                    process_time REAL,
                    setup_time REAL,
                    queue_discipline TEXT DEFAULT 'FIFO',
                    entity_type TEXT DEFAULT 'part',
                    batch_size INTEGER DEFAULT 1,
                    FOREIGN KEY (layout_id) REFERENCES layouts(id)
                );

                CREATE TABLE paths (
                    id TEXT PRIMARY KEY,
                    layout_id TEXT NOT NULL,
                    from_node_id TEXT NOT NULL,
                    to_node_id TEXT NOT NULL,
                    connection_type TEXT DEFAULT 'partFlow',
                    path_type TEXT DEFAULT 'single',
                    routing_mode TEXT DEFAULT 'direct',
                    color TEXT DEFAULT '#888888',
                    thickness REAL DEFAULT 2,
                    style TEXT DEFAULT 'solid',
                    distance REAL,
                    transport_type TEXT DEFAULT 'conveyor',
                    speed REAL DEFAULT 1.0,
                    capacity INTEGER DEFAULT 10,
                    lanes INTEGER DEFAULT 1,
                    bidirectional INTEGER DEFAULT 0,
                    FOREIGN KEY (layout_id) REFERENCES layouts(id),
                    FOREIGN KEY (from_node_id) REFERENCES nodes(id),
                    FOREIGN KEY (to_node_id) REFERENCES nodes(id)
                );

                CREATE TABLE path_waypoints (
                    id TEXT PRIMARY KEY,
                    path_id TEXT NOT NULL,
                    sequence_order INTEGER NOT NULL,
                    x REAL NOT NULL,
                    y REAL NOT NULL,
                    FOREIGN KEY (path_id) REFERENCES paths(id)
                );

                CREATE TABLE cells (
                    id TEXT PRIMARY KEY,
                    layout_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    cell_index INTEGER DEFAULT 0,
                    cell_type TEXT DEFAULT 'simple',
                    color TEXT DEFAULT '#9B59B6',
                    input_terminal_position TEXT DEFAULT 'left',
                    output_terminal_position TEXT DEFAULT 'right',
                    is_collapsed INTEGER DEFAULT 0,
                    FOREIGN KEY (layout_id) REFERENCES layouts(id)
                );

                CREATE TABLE cell_members (
                    cell_id TEXT NOT NULL,
                    node_id TEXT NOT NULL,
                    PRIMARY KEY (cell_id, node_id),
                    FOREIGN KEY (cell_id) REFERENCES cells(id),
                    FOREIGN KEY (node_id) REFERENCES nodes(id)
                );

                CREATE TABLE cell_entry_points (
                    cell_id TEXT NOT NULL,
                    node_id TEXT NOT NULL,
                    PRIMARY KEY (cell_id, node_id)
                );

                CREATE TABLE cell_exit_points (
                    cell_id TEXT NOT NULL,
                    node_id TEXT NOT NULL,
                    PRIMARY KEY (cell_id, node_id)
                );

                CREATE TABLE walls (
                    id TEXT PRIMARY KEY,
                    layout_id TEXT NOT NULL,
                    x1 REAL NOT NULL,
                    y1 REAL NOT NULL,
                    x2 REAL NOT NULL,
                    y2 REAL NOT NULL,
                    thickness REAL DEFAULT 6,
                    wall_type TEXT DEFAULT 'standard',
                    color TEXT DEFAULT '#444444',
                    line_style TEXT DEFAULT 'solid',
                    FOREIGN KEY (layout_id) REFERENCES layouts(id)
                );

                CREATE TABLE transport_networks (
                    id TEXT PRIMARY KEY,
                    layout_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    color TEXT DEFAULT '#E67E22',
                    FOREIGN KEY (layout_id) REFERENCES layouts(id)
                );

                CREATE TABLE transport_stations (
                    id TEXT PRIMARY KEY,
                    network_id TEXT NOT NULL,
                    layout_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    station_type TEXT DEFAULT 'pickup',
                    x REAL NOT NULL,
                    y REAL NOT NULL,
                    rotation REAL DEFAULT 0,
                    color TEXT DEFAULT '#9B59B6',
                    width REAL DEFAULT 50,
                    height REAL DEFAULT 50,
                    dwell_time REAL DEFAULT 10,
                    queue_capacity INTEGER DEFAULT 5,
                    FOREIGN KEY (network_id) REFERENCES transport_networks(id),
                    FOREIGN KEY (layout_id) REFERENCES layouts(id)
                );

                CREATE TABLE transport_tracks (
                    id TEXT PRIMARY KEY,
                    network_id TEXT NOT NULL,
                    layout_id TEXT NOT NULL,
                    from_point_id TEXT NOT NULL,
                    to_point_id TEXT NOT NULL,
                    is_bidirectional INTEGER DEFAULT 1,
                    distance REAL DEFAULT 0,
                    speed_limit REAL DEFAULT 2.0,
                    color TEXT DEFAULT '',
                    FOREIGN KEY (network_id) REFERENCES transport_networks(id)
                );

                CREATE TABLE transporters (
                    id TEXT PRIMARY KEY,
                    network_id TEXT NOT NULL,
                    layout_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    transporter_type TEXT DEFAULT 'agv',
                    home_station_id TEXT,
                    speed REAL DEFAULT 1.5,
                    capacity INTEGER DEFAULT 1,
                    color TEXT DEFAULT '#E74C3C',
                    FOREIGN KEY (network_id) REFERENCES transport_networks(id)
                );

                CREATE INDEX idx_nodes_layout ON nodes(layout_id);
                CREATE INDEX idx_paths_layout ON paths(layout_id);
                CREATE INDEX idx_cells_layout ON cells(layout_id);
                CREATE INDEX idx_walls_layout ON walls(layout_id);

                CREATE TABLE layout_templates (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    template_type TEXT NOT NULL,
                    description TEXT DEFAULT '',
                    parameters TEXT DEFAULT '{}',
                    generation_rules TEXT DEFAULT '',
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX idx_templates_type ON layout_templates(template_type);
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private void InsertLayout(SqliteConnection conn, string layoutId, LayoutData layout, string name)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO layouts (id, name, canvas_width, canvas_height, grid_size, frictionless_mode)
                VALUES (@id, @name, @w, @h, @grid, @frictionless)", conn);
            cmd.Parameters.AddWithValue("@id", layoutId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@w", layout.Canvas.Width);
            cmd.Parameters.AddWithValue("@h", layout.Canvas.Height);
            cmd.Parameters.AddWithValue("@grid", layout.Canvas.GridSize);
            cmd.Parameters.AddWithValue("@frictionless", layout.FrictionlessMode ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        private Dictionary<string, string> InsertNodes(SqliteConnection conn, string layoutId, IEnumerable<NodeData> nodes)
        {
            var nodeIdMap = new Dictionary<string, string>();
            
            foreach (var node in nodes)
            {
                var dbId = Guid.NewGuid().ToString();
                nodeIdMap[node.Id] = dbId;

                using var cmd = new SqliteCommand(@"
                    INSERT INTO nodes (id, layout_id, name, node_type, label, x, y, width, height, rotation, 
                        color, icon, input_terminal_position, output_terminal_position,
                        servers, capacity, mtbf, mttr, process_time, setup_time, queue_discipline, entity_type, batch_size)
                    VALUES (@id, @lid, @name, @type, @label, @x, @y, @w, @h, @rot,
                        @color, @icon, @itp, @otp, @servers, @cap, @mtbf, @mttr, @pt, @st, @qd, @et, @bs)", conn);
                
                cmd.Parameters.AddWithValue("@id", dbId);
                cmd.Parameters.AddWithValue("@lid", layoutId);
                cmd.Parameters.AddWithValue("@name", node.Name);
                cmd.Parameters.AddWithValue("@type", node.Type);
                cmd.Parameters.AddWithValue("@label", node.Label ?? "");
                cmd.Parameters.AddWithValue("@x", node.Visual.X);
                cmd.Parameters.AddWithValue("@y", node.Visual.Y);
                cmd.Parameters.AddWithValue("@w", node.Visual.Width);
                cmd.Parameters.AddWithValue("@h", node.Visual.Height);
                cmd.Parameters.AddWithValue("@rot", node.Visual.Rotation);
                cmd.Parameters.AddWithValue("@color", node.Visual.Color ?? "#4A90D9");
                cmd.Parameters.AddWithValue("@icon", node.Visual.Icon ?? "machine_generic");
                cmd.Parameters.AddWithValue("@itp", node.Visual.InputTerminalPosition);
                cmd.Parameters.AddWithValue("@otp", node.Visual.OutputTerminalPosition);
                cmd.Parameters.AddWithValue("@servers", node.Simulation.Servers);
                cmd.Parameters.AddWithValue("@cap", node.Simulation.Capacity);
                cmd.Parameters.AddWithValue("@mtbf", (object?)node.Simulation.Mtbf ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mttr", (object?)node.Simulation.Mttr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@pt", (object?)node.Simulation.ProcessTime?.Value ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@st", (object?)node.Simulation.SetupTime?.Value ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@qd", node.Simulation.QueueDiscipline);
                cmd.Parameters.AddWithValue("@et", node.Simulation.EntityType);
                cmd.Parameters.AddWithValue("@bs", node.Simulation.BatchSize);
                cmd.ExecuteNonQuery();
            }
            
            return nodeIdMap;
        }

        private void InsertPaths(SqliteConnection conn, string layoutId, IEnumerable<PathData> paths, Dictionary<string, string> nodeIdMap)
        {
            foreach (var path in paths)
            {
                if (!nodeIdMap.TryGetValue(path.From, out var fromId) || 
                    !nodeIdMap.TryGetValue(path.To, out var toId)) continue;

                var pathId = Guid.NewGuid().ToString();
                
                using var cmd = new SqliteCommand(@"
                    INSERT INTO paths (id, layout_id, from_node_id, to_node_id, connection_type, path_type, 
                        routing_mode, color, thickness, style, distance, transport_type, speed, capacity, lanes, bidirectional)
                    VALUES (@id, @lid, @from, @to, @ct, @pt, @rm, @color, @thick, @style, @dist, @tt, @speed, @cap, @lanes, @bi)", conn);
                
                cmd.Parameters.AddWithValue("@id", pathId);
                cmd.Parameters.AddWithValue("@lid", layoutId);
                cmd.Parameters.AddWithValue("@from", fromId);
                cmd.Parameters.AddWithValue("@to", toId);
                cmd.Parameters.AddWithValue("@ct", path.ConnectionType ?? "partFlow");
                cmd.Parameters.AddWithValue("@pt", path.PathType ?? "single");
                cmd.Parameters.AddWithValue("@rm", path.RoutingMode ?? "direct");
                cmd.Parameters.AddWithValue("@color", path.Visual.Color ?? "#888888");
                cmd.Parameters.AddWithValue("@thick", path.Visual.Thickness);
                cmd.Parameters.AddWithValue("@style", path.Visual.Style ?? "solid");
                cmd.Parameters.AddWithValue("@dist", (object?)path.Simulation.Distance ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tt", path.Simulation.TransportType ?? "conveyor");
                cmd.Parameters.AddWithValue("@speed", path.Simulation.Speed);
                cmd.Parameters.AddWithValue("@cap", path.Simulation.Capacity);
                cmd.Parameters.AddWithValue("@lanes", path.Simulation.Lanes);
                cmd.Parameters.AddWithValue("@bi", path.Simulation.Bidirectional ? 1 : 0);
                cmd.ExecuteNonQuery();

                // Waypoints
                if (path.Visual.Waypoints?.Count > 0)
                {
                    for (int i = 0; i < path.Visual.Waypoints.Count; i++)
                    {
                        var wp = path.Visual.Waypoints[i];
                        using var wpCmd = new SqliteCommand(@"
                            INSERT INTO path_waypoints (id, path_id, sequence_order, x, y) 
                            VALUES (@id, @pid, @seq, @x, @y)", conn);
                        wpCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                        wpCmd.Parameters.AddWithValue("@pid", pathId);
                        wpCmd.Parameters.AddWithValue("@seq", i);
                        wpCmd.Parameters.AddWithValue("@x", wp.X);
                        wpCmd.Parameters.AddWithValue("@y", wp.Y);
                        wpCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void InsertCells(SqliteConnection conn, string layoutId, IEnumerable<GroupData> groups, Dictionary<string, string> nodeIdMap)
        {
            foreach (var group in groups.Where(g => g.IsCell))
            {
                var cellId = Guid.NewGuid().ToString();
                
                using var cmd = new SqliteCommand(@"
                    INSERT INTO cells (id, layout_id, name, cell_index, cell_type, color, 
                        input_terminal_position, output_terminal_position, is_collapsed)
                    VALUES (@id, @lid, @name, @idx, @type, @color, @itp, @otp, @collapsed)", conn);
                
                cmd.Parameters.AddWithValue("@id", cellId);
                cmd.Parameters.AddWithValue("@lid", layoutId);
                cmd.Parameters.AddWithValue("@name", group.Name);
                cmd.Parameters.AddWithValue("@idx", group.CellIndex);
                cmd.Parameters.AddWithValue("@type", group.CellType ?? "simple");
                cmd.Parameters.AddWithValue("@color", group.Color ?? "#9B59B6");
                cmd.Parameters.AddWithValue("@itp", group.InputTerminalPosition);
                cmd.Parameters.AddWithValue("@otp", group.OutputTerminalPosition);
                cmd.Parameters.AddWithValue("@collapsed", group.Collapsed ? 1 : 0);
                cmd.ExecuteNonQuery();

                // Members
                foreach (var memberId in group.Members)
                {
                    if (!nodeIdMap.TryGetValue(memberId, out var nodeDbId)) continue;
                    using var mCmd = new SqliteCommand("INSERT INTO cell_members (cell_id, node_id) VALUES (@cid, @nid)", conn);
                    mCmd.Parameters.AddWithValue("@cid", cellId);
                    mCmd.Parameters.AddWithValue("@nid", nodeDbId);
                    mCmd.ExecuteNonQuery();
                }

                // Entry points
                foreach (var entryId in group.EntryPoints)
                {
                    if (!nodeIdMap.TryGetValue(entryId, out var nodeDbId)) continue;
                    using var eCmd = new SqliteCommand("INSERT INTO cell_entry_points (cell_id, node_id) VALUES (@cid, @nid)", conn);
                    eCmd.Parameters.AddWithValue("@cid", cellId);
                    eCmd.Parameters.AddWithValue("@nid", nodeDbId);
                    eCmd.ExecuteNonQuery();
                }

                // Exit points
                foreach (var exitId in group.ExitPoints)
                {
                    if (!nodeIdMap.TryGetValue(exitId, out var nodeDbId)) continue;
                    using var xCmd = new SqliteCommand("INSERT INTO cell_exit_points (cell_id, node_id) VALUES (@cid, @nid)", conn);
                    xCmd.Parameters.AddWithValue("@cid", cellId);
                    xCmd.Parameters.AddWithValue("@nid", nodeDbId);
                    xCmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertWalls(SqliteConnection conn, string layoutId, IEnumerable<WallData> walls)
        {
            foreach (var wall in walls)
            {
                using var cmd = new SqliteCommand(@"
                    INSERT INTO walls (id, layout_id, x1, y1, x2, y2, thickness, wall_type, color, line_style)
                    VALUES (@id, @lid, @x1, @y1, @x2, @y2, @thick, @type, @color, @style)", conn);
                
                cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@lid", layoutId);
                cmd.Parameters.AddWithValue("@x1", wall.X1);
                cmd.Parameters.AddWithValue("@y1", wall.Y1);
                cmd.Parameters.AddWithValue("@x2", wall.X2);
                cmd.Parameters.AddWithValue("@y2", wall.Y2);
                cmd.Parameters.AddWithValue("@thick", wall.Thickness);
                cmd.Parameters.AddWithValue("@type", wall.WallType ?? "standard");
                cmd.Parameters.AddWithValue("@color", wall.Color ?? "#444444");
                cmd.Parameters.AddWithValue("@style", wall.LineStyle ?? "solid");
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertTransportNetworks(SqliteConnection conn, string layoutId, LayoutData layout, Dictionary<string, string> nodeIdMap)
        {
            foreach (var network in layout.TransportNetworks)
            {
                var networkId = Guid.NewGuid().ToString();
                
                using var nCmd = new SqliteCommand(@"
                    INSERT INTO transport_networks (id, layout_id, name, color) VALUES (@id, @lid, @name, @color)", conn);
                nCmd.Parameters.AddWithValue("@id", networkId);
                nCmd.Parameters.AddWithValue("@lid", layoutId);
                nCmd.Parameters.AddWithValue("@name", network.Name);
                nCmd.Parameters.AddWithValue("@color", network.Visual?.Color ?? "#E67E22");
                nCmd.ExecuteNonQuery();

                var pointIdMap = new Dictionary<string, string>();

                // Stations
                foreach (var station in network.Stations)
                {
                    var stationId = Guid.NewGuid().ToString();
                    pointIdMap[station.Id] = stationId;

                    using var sCmd = new SqliteCommand(@"
                        INSERT INTO transport_stations (id, network_id, layout_id, name, station_type, x, y, rotation, color, width, height, dwell_time, queue_capacity)
                        VALUES (@id, @nid, @lid, @name, @type, @x, @y, @rot, @color, @w, @h, @dwell, @cap)", conn);
                    sCmd.Parameters.AddWithValue("@id", stationId);
                    sCmd.Parameters.AddWithValue("@nid", networkId);
                    sCmd.Parameters.AddWithValue("@lid", layoutId);
                    sCmd.Parameters.AddWithValue("@name", station.Name);
                    sCmd.Parameters.AddWithValue("@type", station.Simulation.StationType);
                    sCmd.Parameters.AddWithValue("@x", station.Visual.X);
                    sCmd.Parameters.AddWithValue("@y", station.Visual.Y);
                    sCmd.Parameters.AddWithValue("@rot", station.Visual.Rotation);
                    sCmd.Parameters.AddWithValue("@color", station.Visual.Color);
                    sCmd.Parameters.AddWithValue("@w", station.Visual.Width);
                    sCmd.Parameters.AddWithValue("@h", station.Visual.Height);
                    sCmd.Parameters.AddWithValue("@dwell", station.Simulation.DwellTime);
                    sCmd.Parameters.AddWithValue("@cap", station.Simulation.QueueCapacity);
                    sCmd.ExecuteNonQuery();
                }

                // Waypoints (stored as stations with type 'waypoint')
                foreach (var waypoint in network.Waypoints)
                {
                    var wpId = Guid.NewGuid().ToString();
                    pointIdMap[waypoint.Id] = wpId;

                    using var wCmd = new SqliteCommand(@"
                        INSERT INTO transport_stations (id, network_id, layout_id, name, station_type, x, y)
                        VALUES (@id, @nid, @lid, @name, 'waypoint', @x, @y)", conn);
                    wCmd.Parameters.AddWithValue("@id", wpId);
                    wCmd.Parameters.AddWithValue("@nid", networkId);
                    wCmd.Parameters.AddWithValue("@lid", layoutId);
                    wCmd.Parameters.AddWithValue("@name", waypoint.Name);
                    wCmd.Parameters.AddWithValue("@x", waypoint.X);
                    wCmd.Parameters.AddWithValue("@y", waypoint.Y);
                    wCmd.ExecuteNonQuery();
                }

                // Segments
                foreach (var segment in network.Segments)
                {
                    if (!pointIdMap.TryGetValue(segment.From, out var fromId) ||
                        !pointIdMap.TryGetValue(segment.To, out var toId)) continue;

                    using var tCmd = new SqliteCommand(@"
                        INSERT INTO transport_tracks (id, network_id, layout_id, from_point_id, to_point_id, is_bidirectional, distance, speed_limit, color)
                        VALUES (@id, @nid, @lid, @from, @to, @bi, @dist, @speed, @color)", conn);
                    tCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    tCmd.Parameters.AddWithValue("@nid", networkId);
                    tCmd.Parameters.AddWithValue("@lid", layoutId);
                    tCmd.Parameters.AddWithValue("@from", fromId);
                    tCmd.Parameters.AddWithValue("@to", toId);
                    tCmd.Parameters.AddWithValue("@bi", segment.Bidirectional ? 1 : 0);
                    tCmd.Parameters.AddWithValue("@dist", segment.Distance);
                    tCmd.Parameters.AddWithValue("@speed", segment.SpeedLimit);
                    tCmd.Parameters.AddWithValue("@color", segment.Color ?? "");
                    tCmd.ExecuteNonQuery();
                }

                // Transporters
                foreach (var transporter in network.Transporters)
                {
                    var homeId = !string.IsNullOrEmpty(transporter.HomeStationId) && pointIdMap.TryGetValue(transporter.HomeStationId, out var hid) ? hid : null;

                    using var trCmd = new SqliteCommand(@"
                        INSERT INTO transporters (id, network_id, layout_id, name, transporter_type, home_station_id, speed, capacity, color)
                        VALUES (@id, @nid, @lid, @name, @type, @home, @speed, @cap, @color)", conn);
                    trCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    trCmd.Parameters.AddWithValue("@nid", networkId);
                    trCmd.Parameters.AddWithValue("@lid", layoutId);
                    trCmd.Parameters.AddWithValue("@name", transporter.Name);
                    trCmd.Parameters.AddWithValue("@type", transporter.TransporterType);
                    trCmd.Parameters.AddWithValue("@home", (object?)homeId ?? DBNull.Value);
                    trCmd.Parameters.AddWithValue("@speed", transporter.Speed);
                    trCmd.Parameters.AddWithValue("@cap", transporter.Capacity);
                    trCmd.Parameters.AddWithValue("@color", transporter.Color);
                    trCmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Load Layout

        /// <summary>
        /// Get list of layouts in a database file
        /// </summary>
        public List<SqliteLayoutInfo> GetLayouts(string filePath)
        {
            var layouts = new List<SqliteLayoutInfo>();
            
            using var connection = new SqliteConnection($"Data Source={filePath}");
            connection.Open();

            using var cmd = new SqliteCommand(@"
                SELECT l.id, l.name, l.created_at, l.updated_at,
                       (SELECT COUNT(*) FROM nodes WHERE layout_id = l.id) as node_count,
                       (SELECT COUNT(*) FROM paths WHERE layout_id = l.id) as path_count,
                       (SELECT COUNT(*) FROM cells WHERE layout_id = l.id) as cell_count
                FROM layouts l ORDER BY l.updated_at DESC", connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                layouts.Add(new SqliteLayoutInfo
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    CreatedAt = reader.IsDBNull(2) ? DateTime.Now : DateTime.Parse(reader.GetString(2)),
                    UpdatedAt = reader.IsDBNull(3) ? DateTime.Now : DateTime.Parse(reader.GetString(3)),
                    NodeCount = reader.GetInt32(4),
                    PathCount = reader.GetInt32(5),
                    CellCount = reader.GetInt32(6)
                });
            }

            return layouts;
        }

        /// <summary>
        /// Load layout from a SQLite database file
        /// </summary>
        public LayoutData LoadLayout(string filePath, string? layoutId = null)
        {
            using var connection = new SqliteConnection($"Data Source={filePath}");
            connection.Open();

            // If no layoutId specified, get the first one
            if (string.IsNullOrEmpty(layoutId))
            {
                using var idCmd = new SqliteCommand("SELECT id FROM layouts LIMIT 1", connection);
                layoutId = idCmd.ExecuteScalar()?.ToString();
                if (string.IsNullOrEmpty(layoutId))
                    throw new Exception("No layouts found in database file");
            }

            var layout = new LayoutData();
            
            LoadLayoutSettings(connection, layoutId, layout);
            var nodeIdMap = LoadNodes(connection, layoutId, layout);
            LoadPaths(connection, layoutId, layout, nodeIdMap);
            LoadCells(connection, layoutId, layout, nodeIdMap);
            LoadWalls(connection, layoutId, layout);
            LoadTransportNetworks(connection, layoutId, layout);

            return layout;
        }

        private void LoadLayoutSettings(SqliteConnection conn, string layoutId, LayoutData layout)
        {
            // Try to read with frictionless_mode column first
            try
            {
                using var cmd = new SqliteCommand("SELECT name, canvas_width, canvas_height, grid_size, frictionless_mode FROM layouts WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", layoutId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    layout.Canvas = new CanvasSettings
                    {
                        Width = reader.GetDouble(1),
                        Height = reader.GetDouble(2),
                        GridSize = reader.GetInt32(3)
                    };

                    // Read frictionless_mode
                    layout.FrictionlessMode = reader.GetInt32(4) == 1;
                }
            }
            catch
            {
                // Fall back to old schema without frictionless_mode (backward compatibility)
                using var cmd = new SqliteCommand("SELECT name, canvas_width, canvas_height, grid_size FROM layouts WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", layoutId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    layout.Canvas = new CanvasSettings
                    {
                        Width = reader.GetDouble(1),
                        Height = reader.GetDouble(2),
                        GridSize = reader.GetInt32(3)
                    };
                    // FrictionlessMode stays at default (false)
                }
            }
        }

        private Dictionary<string, string> LoadNodes(SqliteConnection conn, string layoutId, LayoutData layout)
        {
            var nodeIdMap = new Dictionary<string, string>(); // DB ID -> local ID

            using var cmd = new SqliteCommand(@"
                SELECT id, name, node_type, label, x, y, width, height, rotation,
                       color, icon, input_terminal_position, output_terminal_position,
                       servers, capacity, mtbf, mttr, process_time, setup_time,
                       queue_discipline, entity_type, batch_size
                FROM nodes WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", layoutId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dbId = reader.GetString(0);
                var localId = Guid.NewGuid().ToString();
                nodeIdMap[dbId] = localId;

                var sim = new SimulationParams
                {
                    Servers = GetInt(reader, 13, 1),
                    Capacity = GetInt(reader, 14, 1),
                    Mtbf = GetNullableDouble(reader, 15),
                    Mttr = GetNullableDouble(reader, 16),
                    QueueDiscipline = GetString(reader, 19, "FIFO"),
                    EntityType = GetString(reader, 20, "part"),
                    BatchSize = GetInt(reader, 21, 1)
                };

                var processTime = GetNullableDouble(reader, 17);
                if (processTime.HasValue)
                    sim.ProcessTime = new DistributionData { Distribution = "constant", Value = processTime.Value };

                var setupTime = GetNullableDouble(reader, 18);
                if (setupTime.HasValue)
                    sim.SetupTime = new DistributionData { Distribution = "constant", Value = setupTime.Value };

                layout.Nodes.Add(new NodeData
                {
                    Id = localId,
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    Label = GetString(reader, 3, ""),
                    Visual = new NodeVisual
                    {
                        X = reader.GetDouble(4),
                        Y = reader.GetDouble(5),
                        Width = reader.GetDouble(6),
                        Height = reader.GetDouble(7),
                        Rotation = reader.GetDouble(8),
                        Color = GetString(reader, 9, "#4A90D9"),
                        Icon = GetString(reader, 10, "machine_generic"),
                        InputTerminalPosition = GetString(reader, 11, "left"),
                        OutputTerminalPosition = GetString(reader, 12, "right")
                    },
                    Simulation = sim
                });
            }

            return nodeIdMap;
        }

        private void LoadPaths(SqliteConnection conn, string layoutId, LayoutData layout, Dictionary<string, string> nodeIdMap)
        {
            // Load waypoints first
            var pathWaypoints = new Dictionary<string, List<PointData>>();
            using (var wpCmd = new SqliteCommand(@"
                SELECT pw.path_id, pw.x, pw.y FROM path_waypoints pw
                JOIN paths p ON pw.path_id = p.id WHERE p.layout_id = @id
                ORDER BY pw.path_id, pw.sequence_order", conn))
            {
                wpCmd.Parameters.AddWithValue("@id", layoutId);
                using var wpReader = wpCmd.ExecuteReader();
                while (wpReader.Read())
                {
                    var pathId = wpReader.GetString(0);
                    if (!pathWaypoints.ContainsKey(pathId)) pathWaypoints[pathId] = new List<PointData>();
                    pathWaypoints[pathId].Add(new PointData(wpReader.GetDouble(1), wpReader.GetDouble(2)));
                }
            }

            using var cmd = new SqliteCommand(@"
                SELECT id, from_node_id, to_node_id, connection_type, path_type, routing_mode,
                       color, thickness, style, distance, transport_type, speed, capacity, lanes, bidirectional
                FROM paths WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", layoutId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dbPathId = reader.GetString(0);
                var fromDbId = reader.GetString(1);
                var toDbId = reader.GetString(2);

                if (!nodeIdMap.TryGetValue(fromDbId, out var fromId) ||
                    !nodeIdMap.TryGetValue(toDbId, out var toId)) continue;

                var visual = new PathVisual
                {
                    Color = GetString(reader, 6, "#888888"),
                    Thickness = GetDouble(reader, 7, 2),
                    Style = GetString(reader, 8, "solid")
                };

                if (pathWaypoints.TryGetValue(dbPathId, out var wps))
                {
                    foreach (var wp in wps)
                        visual.Waypoints.Add(wp);
                }

                layout.Paths.Add(new PathData
                {
                    Id = Guid.NewGuid().ToString(),
                    From = fromId,
                    To = toId,
                    ConnectionType = GetString(reader, 3, "partFlow"),
                    PathType = GetString(reader, 4, "single"),
                    RoutingMode = GetString(reader, 5, "direct"),
                    Visual = visual,
                    Simulation = new PathSimulation
                    {
                        Distance = GetNullableDouble(reader, 9),
                        TransportType = GetString(reader, 10, "conveyor"),
                        Speed = GetDouble(reader, 11, 1.0),
                        Capacity = GetInt(reader, 12, 10),
                        Lanes = GetInt(reader, 13, 1),
                        Bidirectional = GetInt(reader, 14, 0) == 1
                    }
                });
            }
        }

        private void LoadCells(SqliteConnection conn, string layoutId, LayoutData layout, Dictionary<string, string> nodeIdMap)
        {
            var cellMembers = LoadCellRelation(conn, layoutId, nodeIdMap, "cell_members");
            var cellEntries = LoadCellRelation(conn, layoutId, nodeIdMap, "cell_entry_points");
            var cellExits = LoadCellRelation(conn, layoutId, nodeIdMap, "cell_exit_points");

            using var cmd = new SqliteCommand(@"
                SELECT id, name, cell_index, cell_type, color, input_terminal_position, output_terminal_position, is_collapsed
                FROM cells WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", layoutId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dbCellId = reader.GetString(0);
                layout.Groups.Add(new GroupData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = reader.GetString(1),
                    CellIndex = GetInt(reader, 2, 0),
                    CellType = GetString(reader, 3, "simple"),
                    Color = GetString(reader, 4, "#9B59B6"),
                    InputTerminalPosition = GetString(reader, 5, "left"),
                    OutputTerminalPosition = GetString(reader, 6, "right"),
                    Collapsed = GetInt(reader, 7, 0) == 1,
                    IsCell = true,
                    Members = cellMembers.TryGetValue(dbCellId, out var m) ? m : new List<string>(),
                    EntryPoints = cellEntries.TryGetValue(dbCellId, out var e) ? e : new List<string>(),
                    ExitPoints = cellExits.TryGetValue(dbCellId, out var x) ? x : new List<string>()
                });
            }
        }

        private Dictionary<string, List<string>> LoadCellRelation(SqliteConnection conn, string layoutId, Dictionary<string, string> nodeIdMap, string table)
        {
            var result = new Dictionary<string, List<string>>();
            using var cmd = new SqliteCommand($"SELECT r.cell_id, r.node_id FROM {table} r JOIN cells c ON r.cell_id = c.id WHERE c.layout_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", layoutId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var cellId = reader.GetString(0);
                var nodeDbId = reader.GetString(1);
                if (nodeIdMap.TryGetValue(nodeDbId, out var localId))
                {
                    if (!result.ContainsKey(cellId)) result[cellId] = new List<string>();
                    result[cellId].Add(localId);
                }
            }
            return result;
        }

        private void LoadWalls(SqliteConnection conn, string layoutId, LayoutData layout)
        {
            using var cmd = new SqliteCommand("SELECT x1, y1, x2, y2, thickness, wall_type, color, line_style FROM walls WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", layoutId);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                layout.Walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = reader.GetDouble(0),
                    Y1 = reader.GetDouble(1),
                    X2 = reader.GetDouble(2),
                    Y2 = reader.GetDouble(3),
                    Thickness = GetDouble(reader, 4, 6),
                    WallType = GetString(reader, 5, "standard"),
                    Color = GetString(reader, 6, "#444444"),
                    LineStyle = GetString(reader, 7, "solid")
                });
            }
        }

        private void LoadTransportNetworks(SqliteConnection conn, string layoutId, LayoutData layout)
        {
            var networks = new Dictionary<string, TransportNetworkData>();

            // Load networks
            using (var cmd = new SqliteCommand("SELECT id, name, color FROM transport_networks WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", layoutId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dbId = reader.GetString(0);
                    var network = new TransportNetworkData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = reader.GetString(1),
                        Visual = new TransportNetworkVisual { Color = GetString(reader, 2, "#E67E22") }
                    };
                    networks[dbId] = network;
                    layout.TransportNetworks.Add(network);
                }
            }

            // Load stations/waypoints
            var pointIdMap = new Dictionary<string, string>();
            using (var cmd = new SqliteCommand(@"
                SELECT id, network_id, name, station_type, x, y, rotation, color, width, height, dwell_time, queue_capacity
                FROM transport_stations WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", layoutId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dbId = reader.GetString(0);
                    var networkDbId = reader.GetString(1);
                    var stationType = reader.GetString(3);
                    
                    if (!networks.TryGetValue(networkDbId, out var network)) continue;

                    var localId = Guid.NewGuid().ToString();
                    pointIdMap[dbId] = localId;

                    if (stationType == "waypoint")
                    {
                        network.Waypoints.Add(new WaypointData
                        {
                            Id = localId,
                            Name = reader.GetString(2),
                            X = reader.GetDouble(4),
                            Y = reader.GetDouble(5)
                        });
                    }
                    else
                    {
                        network.Stations.Add(new TransportStationData
                        {
                            Id = localId,
                            Name = reader.GetString(2),
                            Visual = new TransportStationVisual
                            {
                                X = reader.GetDouble(4),
                                Y = reader.GetDouble(5),
                                Rotation = GetDouble(reader, 6, 0),
                                Color = GetString(reader, 7, "#9B59B6"),
                                Width = GetDouble(reader, 8, 50),
                                Height = GetDouble(reader, 9, 50)
                            },
                            Simulation = new TransportStationSimulation
                            {
                                StationType = stationType,
                                DwellTime = GetDouble(reader, 10, 10),
                                QueueCapacity = GetInt(reader, 11, 5)
                            }
                        });
                    }
                }
            }

            // Load segments
            using (var cmd = new SqliteCommand(@"
                SELECT network_id, from_point_id, to_point_id, is_bidirectional, distance, speed_limit, color
                FROM transport_tracks WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", layoutId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var networkDbId = reader.GetString(0);
                    if (!networks.TryGetValue(networkDbId, out var network)) continue;
                    if (!pointIdMap.TryGetValue(reader.GetString(1), out var fromId) ||
                        !pointIdMap.TryGetValue(reader.GetString(2), out var toId)) continue;

                    network.Segments.Add(new TrackSegmentData
                    {
                        Id = Guid.NewGuid().ToString(),
                        From = fromId,
                        To = toId,
                        Bidirectional = GetInt(reader, 3, 1) == 1,
                        Distance = GetDouble(reader, 4, 0),
                        SpeedLimit = GetDouble(reader, 5, 2.0),
                        Color = GetString(reader, 6, "")
                    });
                }
            }

            // Load transporters
            using (var cmd = new SqliteCommand(@"
                SELECT network_id, name, transporter_type, home_station_id, speed, capacity, color
                FROM transporters WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", layoutId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var networkDbId = reader.GetString(0);
                    if (!networks.TryGetValue(networkDbId, out var network)) continue;

                    var homeDbId = reader.IsDBNull(3) ? null : reader.GetString(3);
                    string? homeLocalId = homeDbId != null && pointIdMap.TryGetValue(homeDbId, out var hid) ? hid : null;

                    network.Transporters.Add(new TransporterData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = reader.GetString(1),
                        TransporterType = GetString(reader, 2, "agv"),
                        HomeStationId = homeLocalId ?? "",
                        Speed = GetDouble(reader, 4, 1.5),
                        Capacity = GetInt(reader, 5, 1),
                        Color = GetString(reader, 6, "#E74C3C")
                    });
                }
            }
        }

        #endregion

        #region Helpers

        private static string GetString(SqliteDataReader r, int i, string def) => r.IsDBNull(i) ? def : r.GetString(i);
        private static int GetInt(SqliteDataReader r, int i, int def) => r.IsDBNull(i) ? def : r.GetInt32(i);
        private static double GetDouble(SqliteDataReader r, int i, double def) => r.IsDBNull(i) ? def : r.GetDouble(i);
        private static double? GetNullableDouble(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDouble(i);

        #endregion

        #region Template Management

        /// <summary>
        /// Save a layout template to database
        /// </summary>
        public void SaveTemplate(LayoutTemplate template, string dbPath)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            // Ensure templates table exists
            CreateTables(conn);

            var sql = @"
                INSERT OR REPLACE INTO layout_templates
                (id, name, template_type, description, parameters, generation_rules, created_at, updated_at)
                VALUES (@id, @name, @type, @desc, @params, @rules, @created, @updated)";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", template.Id);
            cmd.Parameters.AddWithValue("@name", template.Name);
            cmd.Parameters.AddWithValue("@type", template.TemplateType);
            cmd.Parameters.AddWithValue("@desc", template.Description);
            cmd.Parameters.AddWithValue("@params", System.Text.Json.JsonSerializer.Serialize(template.Parameters));
            cmd.Parameters.AddWithValue("@rules", template.GenerationRules);
            cmd.Parameters.AddWithValue("@created", template.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Load all templates from database
        /// </summary>
        public List<LayoutTemplate> LoadTemplates(string dbPath)
        {
            var templates = new List<LayoutTemplate>();

            if (!File.Exists(dbPath))
                return templates;

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            try
            {
                var sql = "SELECT id, name, template_type, description, parameters, generation_rules, created_at FROM layout_templates ORDER BY name";

                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var template = new LayoutTemplate
                    {
                        Id = reader.GetString(0),
                        Name = reader.GetString(1),
                        TemplateType = reader.GetString(2),
                        Description = reader.GetString(3),
                        GenerationRules = reader.GetString(5),
                        CreatedAt = DateTime.Parse(reader.GetString(6))
                    };

                    try
                    {
                        template.Parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(4))
                            ?? new Dictionary<string, string>();
                    }
                    catch
                    {
                        template.Parameters = new Dictionary<string, string>();
                    }

                    templates.Add(template);
                }
            }
            catch
            {
                // Table doesn't exist yet
            }

            return templates;
        }

        /// <summary>
        /// Initialize default templates in database
        /// </summary>
        public void InitializeDefaultTemplates(string dbPath)
        {
            var templates = new List<LayoutTemplate>
            {
                new LayoutTemplate
                {
                    Name = "Job Shop",
                    TemplateType = LayoutTemplateTypes.JobShop,
                    Description = "Flexible job shop layout with general-purpose work centers arranged for easy material flow",
                    Parameters = new Dictionary<string, string>
                    {
                        { "work_centers", "6" },
                        { "spacing", "150" },
                        { "arrangement", "circular" },
                        { "add_material_handling", "true" },
                        { "tool_cribs", "2" }
                    }
                },
                new LayoutTemplate
                {
                    Name = "Cellular Manufacturing",
                    TemplateType = LayoutTemplateTypes.CellularManufacturing,
                    Description = "Group technology layout with dedicated manufacturing cells for product families",
                    Parameters = new Dictionary<string, string>
                    {
                        { "cells", "4" },
                        { "machines_per_cell", "5" },
                        { "cell_spacing", "200" },
                        { "cell_shape", "u_shape" },
                        { "shared_resources", "true" }
                    }
                },
                new LayoutTemplate
                {
                    Name = "Flow Shop",
                    TemplateType = LayoutTemplateTypes.FlowShop,
                    Description = "Product layout with sequential processing stages and continuous flow",
                    Parameters = new Dictionary<string, string>
                    {
                        { "stages", "5" },
                        { "machines_per_stage", "3" },
                        { "stage_spacing", "120" },
                        { "machine_spacing", "60" },
                        { "add_buffers", "true" },
                        { "add_conveyors", "true" }
                    }
                },
                new LayoutTemplate
                {
                    Name = "Fixed Position Assembly",
                    TemplateType = LayoutTemplateTypes.FixedPosition,
                    Description = "Large product remains stationary while workers and tools move to it",
                    Parameters = new Dictionary<string, string>
                    {
                        { "products", "2" },
                        { "tools_per_product", "8" },
                        { "product_spacing", "300" },
                        { "tool_radius", "150" }
                    }
                }
            };

            foreach (var template in templates)
            {
                SaveTemplate(template, dbPath);
            }
        }

        #endregion
    }

    public class SqliteLayoutInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int NodeCount { get; set; }
        public int PathCount { get; set; }
        public int CellCount { get; set; }
    }
}
