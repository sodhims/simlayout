using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutEditor.Models;
using Npgsql;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Export layout data to PostgreSQL database
    /// </summary>
    public class PostgresExportService
    {
        private readonly string _connectionString;

        public PostgresExportService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task ExportAsync(LayoutData layout, string layoutName)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                var layoutId = Guid.NewGuid();
                await InsertLayoutAsync(conn, layoutId, layout, layoutName);
                var nodeIdMap = await InsertNodesAsync(conn, layoutId, layout.Nodes);
                await InsertPathsAsync(conn, layoutId, layout.Paths, nodeIdMap);
                await InsertCellsAsync(conn, layoutId, layout.Groups, nodeIdMap);
                await InsertWallsAsync(conn, layoutId, layout.Walls);
                await InsertTransportNetworksAsync(conn, layoutId, layout);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public string GenerateSqlScript(LayoutData layout, string layoutName)
        {
            var sb = new StringBuilder();
            var layoutId = Guid.NewGuid();

            sb.AppendLine("-- Layout Export Script");
            sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"-- Layout: {layoutName}");
            sb.AppendLine();
            sb.AppendLine("BEGIN;");
            sb.AppendLine();

            // Layout
            sb.AppendLine("-- LAYOUT");
            sb.AppendLine($"INSERT INTO layouts (id, name, canvas_width, canvas_height, grid_size) VALUES ('{layoutId}', '{Escape(layoutName)}', {layout.Canvas.Width}, {layout.Canvas.Height}, {layout.Canvas.GridSize});");
            sb.AppendLine();

            // Nodes
            sb.AppendLine("-- NODES");
            var nodeIdMap = new Dictionary<string, Guid>();
            foreach (var node in layout.Nodes)
            {
                var nodeGuid = Guid.NewGuid();
                nodeIdMap[node.Id] = nodeGuid;
                var sim = node.Simulation;
                
                // Extract process time value if it exists
                var processTimeValue = sim.ProcessTime?.Value;
                var setupTimeValue = sim.SetupTime?.Value;
                
                sb.AppendLine($"INSERT INTO nodes (id, layout_id, name, node_type, label, x, y, width, height, rotation, color, icon, input_terminal_position, output_terminal_position, servers, capacity, mtbf, mttr, process_time, setup_time, queue_discipline, entity_type, batch_size) VALUES ('{nodeGuid}', '{layoutId}', '{Escape(node.Name)}', '{Escape(node.Type)}', '{Escape(node.Label ?? "")}', {node.Visual.X}, {node.Visual.Y}, {node.Visual.Width}, {node.Visual.Height}, {node.Visual.Rotation}, '{Escape(node.Visual.Color ?? "#3498DB")}', '{Escape(node.Visual.Icon ?? "")}', '{node.Visual.InputTerminalPosition}', '{node.Visual.OutputTerminalPosition}', {sim.Servers}, {sim.Capacity}, {Null(sim.Mtbf)}, {Null(sim.Mttr)}, {Null(processTimeValue)}, {Null(setupTimeValue)}, '{sim.QueueDiscipline}', '{sim.EntityType}', {sim.BatchSize});");
            }
            sb.AppendLine();

            // Paths
            sb.AppendLine("-- PATHS");
            foreach (var path in layout.Paths)
            {
                if (!nodeIdMap.TryGetValue(path.From, out var fromId) || !nodeIdMap.TryGetValue(path.To, out var toId)) continue;
                var pathId = Guid.NewGuid();
                var sim = path.Simulation;
                sb.AppendLine($"INSERT INTO paths (id, layout_id, from_node_id, to_node_id, path_type, connection_type, routing_mode, color, transport_type, distance, speed, capacity, lanes, bidirectional) VALUES ('{pathId}', '{layoutId}', '{fromId}', '{toId}', '{path.PathType ?? "single"}', '{path.ConnectionType ?? "partFlow"}', '{path.RoutingMode ?? "direct"}', '{Escape(path.Visual.Color ?? "#666666")}', '{sim.TransportType ?? "conveyor"}', {Null(sim.Distance)}, {sim.Speed}, {sim.Capacity}, {sim.Lanes}, {sim.Bidirectional.ToString().ToLower()});");
                
                if (path.Visual.Waypoints?.Count > 0)
                {
                    for (int i = 0; i < path.Visual.Waypoints.Count; i++)
                    {
                        var wp = path.Visual.Waypoints[i];
                        sb.AppendLine($"INSERT INTO path_waypoints (path_id, sequence_order, x, y) VALUES ('{pathId}', {i}, {wp.X}, {wp.Y});");
                    }
                }
            }
            sb.AppendLine();

            // Cells
            sb.AppendLine("-- CELLS");
            foreach (var group in layout.Groups.Where(g => g.IsCell))
            {
                var cellId = Guid.NewGuid();
                sb.AppendLine($"INSERT INTO cells (id, layout_id, name, cell_index, canonical_name, cell_type, is_collapsed, color, input_terminal_position, output_terminal_position, assembly_mode) VALUES ('{cellId}', '{layoutId}', '{Escape(group.Name)}', {group.CellIndex}, '{group.CanonicalName}', '{group.CellType ?? "simple"}', {group.Collapsed.ToString().ToLower()}, '{Escape(group.Color ?? "#9B59B6")}', '{group.InputTerminalPosition}', '{group.OutputTerminalPosition}', {NullStr(group.AssemblyRule?.Mode)});");
                foreach (var memberId in group.Members)
                    if (nodeIdMap.TryGetValue(memberId, out var nid))
                        sb.AppendLine($"INSERT INTO cell_members (cell_id, node_id) VALUES ('{cellId}', '{nid}');");
                foreach (var entryId in group.EntryPoints)
                    if (nodeIdMap.TryGetValue(entryId, out var nid))
                        sb.AppendLine($"INSERT INTO cell_entry_points (cell_id, node_id) VALUES ('{cellId}', '{nid}');");
                foreach (var exitId in group.ExitPoints)
                    if (nodeIdMap.TryGetValue(exitId, out var nid))
                        sb.AppendLine($"INSERT INTO cell_exit_points (cell_id, node_id) VALUES ('{cellId}', '{nid}');");
            }
            sb.AppendLine();

            // Walls
            sb.AppendLine("-- WALLS");
            foreach (var wall in layout.Walls)
            {
                sb.AppendLine($"INSERT INTO walls (id, layout_id, x1, y1, x2, y2, wall_type, thickness, color, line_style) VALUES ('{Guid.NewGuid()}', '{layoutId}', {wall.X1}, {wall.Y1}, {wall.X2}, {wall.Y2}, '{wall.WallType ?? "standard"}', {wall.Thickness}, '{Escape(wall.Color ?? "#444444")}', '{wall.LineStyle ?? "solid"}');");
            }
            sb.AppendLine();

            // Transport Networks
            sb.AppendLine("-- TRANSPORT NETWORKS");
            foreach (var network in layout.TransportNetworks)
            {
                var networkId = Guid.NewGuid();
                var networkColor = network.Visual?.Color ?? "#E74C3C";
                sb.AppendLine($"INSERT INTO transport_networks (id, layout_id, name, network_type, color, bidirectional) VALUES ('{networkId}', '{layoutId}', '{Escape(network.Name)}', 'agv', '{Escape(networkColor)}', false);");

                var pointIdMap = new Dictionary<string, Guid>();

                // Stations
                foreach (var station in network.Stations)
                {
                    var stationId = Guid.NewGuid();
                    pointIdMap[station.Id] = stationId;
                    var vis = station.Visual;
                    var sim = station.Simulation;
                    sb.AppendLine($"INSERT INTO transport_stations (id, network_id, layout_id, name, station_type, x, y, rotation, color, size, dwell_time, capacity) VALUES ('{stationId}', '{networkId}', '{layoutId}', '{Escape(station.Name)}', '{sim.StationType}', {vis.X}, {vis.Y}, {vis.Rotation}, '{Escape(vis.Color)}', {vis.Width}, {sim.DwellTime}, {sim.QueueCapacity});");
                }

                // Waypoints
                foreach (var waypoint in network.Waypoints)
                {
                    var wpId = Guid.NewGuid();
                    pointIdMap[waypoint.Id] = wpId;
                    sb.AppendLine($"INSERT INTO transport_stations (id, network_id, layout_id, name, station_type, x, y, size) VALUES ('{wpId}', '{networkId}', '{layoutId}', '{Escape(waypoint.Name)}', 'waypoint', {waypoint.X}, {waypoint.Y}, 20);");
                }

                // Segments
                foreach (var segment in network.Segments)
                {
                    if (!pointIdMap.TryGetValue(segment.From, out var fromId) || !pointIdMap.TryGetValue(segment.To, out var toId))
                    {
                        sb.AppendLine($"-- Skipped segment {segment.Id}: missing endpoint");
                        continue;
                    }
                    sb.AppendLine($"INSERT INTO transport_tracks (id, network_id, layout_id, from_point_id, to_point_id, is_bidirectional, distance, speed_limit, color) VALUES ('{Guid.NewGuid()}', '{networkId}', '{layoutId}', '{fromId}', '{toId}', {segment.Bidirectional.ToString().ToLower()}, {segment.Distance}, {segment.SpeedLimit}, '{Escape(segment.Color ?? "")}');");
                }

                // Transporters
                foreach (var transporter in network.Transporters)
                {
                    var homeId = string.IsNullOrEmpty(transporter.HomeStationId) || !pointIdMap.TryGetValue(transporter.HomeStationId, out var hid) ? "NULL" : $"'{hid}'";
                    sb.AppendLine($"INSERT INTO transporters (id, network_id, layout_id, name, transporter_type, home_station_id, speed, load_capacity, color) VALUES ('{Guid.NewGuid()}', '{networkId}', '{layoutId}', '{Escape(transporter.Name)}', '{transporter.TransporterType}', {homeId}, {transporter.Speed}, {transporter.Capacity}, '{Escape(transporter.Color)}');");
                }
            }
            sb.AppendLine();

            sb.AppendLine("COMMIT;");
            return sb.ToString();
        }

        private static string Escape(string s) => s?.Replace("'", "''") ?? "";
        private static string Null(double? v) => v.HasValue ? v.Value.ToString("G") : "NULL";
        private static string Null(int? v) => v.HasValue ? v.Value.ToString() : "NULL";
        private static string NullStr(string? v) => string.IsNullOrEmpty(v) ? "NULL" : $"'{Escape(v)}'";

        #region Async Methods
        private async Task InsertLayoutAsync(NpgsqlConnection c, Guid id, LayoutData l, string n) 
        {
            await using var cmd = new NpgsqlCommand(@"INSERT INTO layouts (id, name, canvas_width, canvas_height, grid_size) VALUES (@id, @name, @w, @h, @grid)", c);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", n);
            cmd.Parameters.AddWithValue("w", l.Canvas.Width);
            cmd.Parameters.AddWithValue("h", l.Canvas.Height);
            cmd.Parameters.AddWithValue("grid", l.Canvas.GridSize);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<Dictionary<string, Guid>> InsertNodesAsync(NpgsqlConnection c, Guid layoutId, IEnumerable<NodeData> nodes) 
        {
            var map = new Dictionary<string, Guid>();
            foreach (var node in nodes)
            {
                var nodeId = Guid.NewGuid();
                map[node.Id] = nodeId;
                await using var cmd = new NpgsqlCommand(@"INSERT INTO nodes (id, layout_id, name, node_type, x, y, width, height, rotation, color, servers, capacity) VALUES (@id, @lid, @name, @type, @x, @y, @w, @h, @rot, @color, @servers, @cap)", c);
                cmd.Parameters.AddWithValue("id", nodeId);
                cmd.Parameters.AddWithValue("lid", layoutId);
                cmd.Parameters.AddWithValue("name", node.Name);
                cmd.Parameters.AddWithValue("type", node.Type);
                cmd.Parameters.AddWithValue("x", node.Visual.X);
                cmd.Parameters.AddWithValue("y", node.Visual.Y);
                cmd.Parameters.AddWithValue("w", node.Visual.Width);
                cmd.Parameters.AddWithValue("h", node.Visual.Height);
                cmd.Parameters.AddWithValue("rot", node.Visual.Rotation);
                cmd.Parameters.AddWithValue("color", node.Visual.Color ?? "#3498DB");
                cmd.Parameters.AddWithValue("servers", node.Simulation.Servers);
                cmd.Parameters.AddWithValue("cap", node.Simulation.Capacity);
                await cmd.ExecuteNonQueryAsync();
            }
            return map;
        }

        private async Task InsertPathsAsync(NpgsqlConnection c, Guid layoutId, IEnumerable<PathData> paths, Dictionary<string, Guid> nodeMap) 
        {
            foreach (var path in paths)
            {
                if (!nodeMap.TryGetValue(path.From, out var fromId) || !nodeMap.TryGetValue(path.To, out var toId)) continue;
                await using var cmd = new NpgsqlCommand(@"INSERT INTO paths (id, layout_id, from_node_id, to_node_id, connection_type, color) VALUES (@id, @lid, @from, @to, @ct, @color)", c);
                cmd.Parameters.AddWithValue("id", Guid.NewGuid());
                cmd.Parameters.AddWithValue("lid", layoutId);
                cmd.Parameters.AddWithValue("from", fromId);
                cmd.Parameters.AddWithValue("to", toId);
                cmd.Parameters.AddWithValue("ct", path.ConnectionType ?? "partFlow");
                cmd.Parameters.AddWithValue("color", path.Visual.Color ?? "#888888");
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertCellsAsync(NpgsqlConnection c, Guid layoutId, IEnumerable<GroupData> groups, Dictionary<string, Guid> nodeMap) 
        {
            foreach (var group in groups.Where(g => g.IsCell))
            {
                var cellId = Guid.NewGuid();
                await using var cmd = new NpgsqlCommand(@"INSERT INTO cells (id, layout_id, name, cell_index, color) VALUES (@id, @lid, @name, @idx, @color)", c);
                cmd.Parameters.AddWithValue("id", cellId);
                cmd.Parameters.AddWithValue("lid", layoutId);
                cmd.Parameters.AddWithValue("name", group.Name);
                cmd.Parameters.AddWithValue("idx", group.CellIndex);
                cmd.Parameters.AddWithValue("color", group.Color ?? "#9B59B6");
                await cmd.ExecuteNonQueryAsync();
                
                foreach (var memberId in group.Members)
                {
                    if (!nodeMap.TryGetValue(memberId, out var nodeId)) continue;
                    await using var mcmd = new NpgsqlCommand(@"INSERT INTO cell_members (cell_id, node_id) VALUES (@cid, @nid)", c);
                    mcmd.Parameters.AddWithValue("cid", cellId);
                    mcmd.Parameters.AddWithValue("nid", nodeId);
                    await mcmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task InsertWallsAsync(NpgsqlConnection c, Guid layoutId, IEnumerable<WallData> walls) 
        {
            foreach (var wall in walls)
            {
                await using var cmd = new NpgsqlCommand(@"INSERT INTO walls (id, layout_id, x1, y1, x2, y2, wall_type, thickness, color) VALUES (@id, @lid, @x1, @y1, @x2, @y2, @type, @thick, @color)", c);
                cmd.Parameters.AddWithValue("id", Guid.NewGuid());
                cmd.Parameters.AddWithValue("lid", layoutId);
                cmd.Parameters.AddWithValue("x1", wall.X1);
                cmd.Parameters.AddWithValue("y1", wall.Y1);
                cmd.Parameters.AddWithValue("x2", wall.X2);
                cmd.Parameters.AddWithValue("y2", wall.Y2);
                cmd.Parameters.AddWithValue("type", wall.WallType ?? "standard");
                cmd.Parameters.AddWithValue("thick", wall.Thickness);
                cmd.Parameters.AddWithValue("color", wall.Color ?? "#444444");
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertTransportNetworksAsync(NpgsqlConnection c, Guid layoutId, LayoutData layout) 
        {
            foreach (var network in layout.TransportNetworks)
            {
                var networkId = Guid.NewGuid();
                await using var ncmd = new NpgsqlCommand(@"INSERT INTO transport_networks (id, layout_id, name, color) VALUES (@id, @lid, @name, @color)", c);
                ncmd.Parameters.AddWithValue("id", networkId);
                ncmd.Parameters.AddWithValue("lid", layoutId);
                ncmd.Parameters.AddWithValue("name", network.Name);
                ncmd.Parameters.AddWithValue("color", network.Visual?.Color ?? "#E74C3C");
                await ncmd.ExecuteNonQueryAsync();

                var pointMap = new Dictionary<string, Guid>();

                foreach (var station in network.Stations)
                {
                    var stationId = Guid.NewGuid();
                    pointMap[station.Id] = stationId;
                    await using var scmd = new NpgsqlCommand(@"INSERT INTO transport_stations (id, network_id, layout_id, name, station_type, x, y, color) VALUES (@id, @nid, @lid, @name, @type, @x, @y, @color)", c);
                    scmd.Parameters.AddWithValue("id", stationId);
                    scmd.Parameters.AddWithValue("nid", networkId);
                    scmd.Parameters.AddWithValue("lid", layoutId);
                    scmd.Parameters.AddWithValue("name", station.Name);
                    scmd.Parameters.AddWithValue("type", station.Simulation.StationType);
                    scmd.Parameters.AddWithValue("x", station.Visual.X);
                    scmd.Parameters.AddWithValue("y", station.Visual.Y);
                    scmd.Parameters.AddWithValue("color", station.Visual.Color);
                    await scmd.ExecuteNonQueryAsync();
                }

                foreach (var waypoint in network.Waypoints)
                {
                    var wpId = Guid.NewGuid();
                    pointMap[waypoint.Id] = wpId;
                    await using var wcmd = new NpgsqlCommand(@"INSERT INTO transport_stations (id, network_id, layout_id, name, station_type, x, y) VALUES (@id, @nid, @lid, @name, 'waypoint', @x, @y)", c);
                    wcmd.Parameters.AddWithValue("id", wpId);
                    wcmd.Parameters.AddWithValue("nid", networkId);
                    wcmd.Parameters.AddWithValue("lid", layoutId);
                    wcmd.Parameters.AddWithValue("name", waypoint.Name);
                    wcmd.Parameters.AddWithValue("x", waypoint.X);
                    wcmd.Parameters.AddWithValue("y", waypoint.Y);
                    await wcmd.ExecuteNonQueryAsync();
                }

                foreach (var segment in network.Segments)
                {
                    if (!pointMap.TryGetValue(segment.From, out var fromId) || !pointMap.TryGetValue(segment.To, out var toId)) continue;
                    await using var tcmd = new NpgsqlCommand(@"INSERT INTO transport_tracks (id, network_id, layout_id, from_point_id, to_point_id, is_bidirectional, distance, speed_limit) VALUES (@id, @nid, @lid, @from, @to, @bi, @dist, @speed)", c);
                    tcmd.Parameters.AddWithValue("id", Guid.NewGuid());
                    tcmd.Parameters.AddWithValue("nid", networkId);
                    tcmd.Parameters.AddWithValue("lid", layoutId);
                    tcmd.Parameters.AddWithValue("from", fromId);
                    tcmd.Parameters.AddWithValue("to", toId);
                    tcmd.Parameters.AddWithValue("bi", segment.Bidirectional);
                    tcmd.Parameters.AddWithValue("dist", segment.Distance);
                    tcmd.Parameters.AddWithValue("speed", segment.SpeedLimit);
                    await tcmd.ExecuteNonQueryAsync();
                }

                foreach (var transporter in network.Transporters)
                {
                    await using var tcmd = new NpgsqlCommand(@"INSERT INTO transporters (id, network_id, layout_id, name, transporter_type, speed, load_capacity, color) VALUES (@id, @nid, @lid, @name, @type, @speed, @cap, @color)", c);
                    tcmd.Parameters.AddWithValue("id", Guid.NewGuid());
                    tcmd.Parameters.AddWithValue("nid", networkId);
                    tcmd.Parameters.AddWithValue("lid", layoutId);
                    tcmd.Parameters.AddWithValue("name", transporter.Name);
                    tcmd.Parameters.AddWithValue("type", transporter.TransporterType);
                    tcmd.Parameters.AddWithValue("speed", transporter.Speed);
                    tcmd.Parameters.AddWithValue("cap", transporter.Capacity);
                    tcmd.Parameters.AddWithValue("color", transporter.Color);
                    await tcmd.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion
    }
}
