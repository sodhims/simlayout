using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Models;
using Npgsql;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Import layout data from PostgreSQL database
    /// </summary>
    public class PostgresImportService
    {
        private readonly string _connectionString;

        public PostgresImportService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<LayoutInfo>> GetAvailableLayoutsAsync()
        {
            var layouts = new List<LayoutInfo>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT l.id, l.name, l.created_at, l.updated_at,
                       (SELECT COUNT(*) FROM nodes WHERE layout_id = l.id) as node_count,
                       (SELECT COUNT(*) FROM paths WHERE layout_id = l.id) as path_count,
                       (SELECT COUNT(*) FROM cells WHERE layout_id = l.id) as cell_count
                FROM layouts l ORDER BY l.updated_at DESC", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                layouts.Add(new LayoutInfo
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    CreatedAt = reader.GetDateTime(2),
                    UpdatedAt = reader.GetDateTime(3),
                    NodeCount = reader.GetInt32(4),
                    PathCount = reader.GetInt32(5),
                    CellCount = reader.GetInt32(6)
                });
            }
            return layouts;
        }

        public async Task<LayoutData> LoadLayoutAsync(Guid layoutId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var layout = new LayoutData();
            await LoadLayoutSettingsAsync(conn, layoutId, layout);
            var nodeIdMap = await LoadNodesAsync(conn, layoutId, layout);
            await LoadPathsAsync(conn, layoutId, layout, nodeIdMap);
            await LoadCellsAsync(conn, layoutId, layout, nodeIdMap);
            await LoadWallsAsync(conn, layoutId, layout);
            await LoadTransportNetworksAsync(conn, layoutId, layout);
            return layout;
        }

        private async Task LoadLayoutSettingsAsync(NpgsqlConnection conn, Guid layoutId, LayoutData layout)
        {
            await using var cmd = new NpgsqlCommand("SELECT name, canvas_width, canvas_height, grid_size FROM layouts WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", layoutId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                layout.Canvas = new CanvasSettings
                {
                    Width = reader.GetDouble(1),
                    Height = reader.GetDouble(2),
                    GridSize = reader.GetInt32(3)
                };
            }
        }

        private async Task<Dictionary<Guid, string>> LoadNodesAsync(NpgsqlConnection conn, Guid layoutId, LayoutData layout)
        {
            var nodeIdMap = new Dictionary<Guid, string>();
            await using var cmd = new NpgsqlCommand(@"
                SELECT id, name, node_type, label, x, y, width, height, rotation,
                       color, icon, input_terminal_position, output_terminal_position,
                       servers, capacity, mtbf, mttr, process_time, setup_time,
                       queue_discipline, entity_type, batch_size
                FROM nodes WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("id", layoutId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dbId = reader.GetGuid(0);
                var localId = Guid.NewGuid().ToString();
                nodeIdMap[dbId] = localId;

                var sim = new SimulationParams
                {
                    Servers = GetIntOrDefault(reader, 13, 1),
                    Capacity = GetIntOrDefault(reader, 14, 1),
                    Mtbf = GetDoubleOrNull(reader, 15),
                    Mttr = GetDoubleOrNull(reader, 16),
                    QueueDiscipline = GetStringOrDefault(reader, 19, "FIFO"),
                    EntityType = GetStringOrDefault(reader, 20, "part"),
                    BatchSize = GetIntOrDefault(reader, 21, 1)
                };

                // Create ProcessTime distribution if value exists
                var processTimeValue = GetDoubleOrNull(reader, 17);
                if (processTimeValue.HasValue)
                {
                    sim.ProcessTime = new DistributionData { Distribution = "constant", Value = processTimeValue.Value };
                }

                // Create SetupTime distribution if value exists
                var setupTimeValue = GetDoubleOrNull(reader, 18);
                if (setupTimeValue.HasValue)
                {
                    sim.SetupTime = new DistributionData { Distribution = "constant", Value = setupTimeValue.Value };
                }

                layout.Nodes.Add(new NodeData
                {
                    Id = localId,
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    Label = GetStringOrDefault(reader, 3, ""),
                    Visual = new NodeVisual
                    {
                        X = reader.GetDouble(4),
                        Y = reader.GetDouble(5),
                        Width = reader.GetDouble(6),
                        Height = reader.GetDouble(7),
                        Rotation = reader.GetDouble(8),
                        Color = GetStringOrDefault(reader, 9, "#3498DB"),
                        Icon = GetStringOrDefault(reader, 10, "machine_generic"),
                        InputTerminalPosition = GetStringOrDefault(reader, 11, "left"),
                        OutputTerminalPosition = GetStringOrDefault(reader, 12, "right")
                    },
                    Simulation = sim
                });
            }
            return nodeIdMap;
        }

        private async Task LoadPathsAsync(NpgsqlConnection conn, Guid layoutId, LayoutData layout, Dictionary<Guid, string> nodeIdMap)
        {
            // Load waypoints first
            var pathWaypoints = new Dictionary<Guid, List<PointData>>();
            await using (var wpCmd = new NpgsqlCommand(@"
                SELECT pw.path_id, pw.x, pw.y FROM path_waypoints pw
                JOIN paths p ON pw.path_id = p.id WHERE p.layout_id = @id
                ORDER BY pw.path_id, pw.sequence_order", conn))
            {
                wpCmd.Parameters.AddWithValue("id", layoutId);
                await using var wpReader = await wpCmd.ExecuteReaderAsync();
                while (await wpReader.ReadAsync())
                {
                    var pathId = wpReader.GetGuid(0);
                    if (!pathWaypoints.ContainsKey(pathId)) pathWaypoints[pathId] = new List<PointData>();
                    pathWaypoints[pathId].Add(new PointData(wpReader.GetDouble(1), wpReader.GetDouble(2)));
                }
            }

            await using var cmd = new NpgsqlCommand(@"
                SELECT id, from_node_id, to_node_id, path_type, connection_type, routing_mode,
                       color, transport_type, distance, speed, capacity, lanes, bidirectional
                FROM paths WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("id", layoutId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dbPathId = reader.GetGuid(0);
                if (!nodeIdMap.TryGetValue(reader.GetGuid(1), out var fromId) ||
                    !nodeIdMap.TryGetValue(reader.GetGuid(2), out var toId)) continue;

                var visual = new PathVisual
                {
                    Color = GetStringOrDefault(reader, 6, "#888888")
                };
                
                // Add waypoints if any
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
                    PathType = GetStringOrDefault(reader, 3, "single"),
                    ConnectionType = GetStringOrDefault(reader, 4, "partFlow"),
                    RoutingMode = GetStringOrDefault(reader, 5, "direct"),
                    Visual = visual,
                    Simulation = new PathSimulation
                    {
                        TransportType = GetStringOrDefault(reader, 7, "conveyor"),
                        Distance = GetDoubleOrNull(reader, 8),
                        Speed = GetDoubleOrDefault(reader, 9, 1.0),
                        Capacity = GetIntOrDefault(reader, 10, 10),
                        Lanes = GetIntOrDefault(reader, 11, 1),
                        Bidirectional = GetBoolOrDefault(reader, 12, false)
                    }
                });
            }
        }

        private async Task LoadCellsAsync(NpgsqlConnection conn, Guid layoutId, LayoutData layout, Dictionary<Guid, string> nodeIdMap)
        {
            var cellMembers = await LoadCellRelation(conn, layoutId, nodeIdMap, "cell_members");
            var cellEntries = await LoadCellRelation(conn, layoutId, nodeIdMap, "cell_entry_points");
            var cellExits = await LoadCellRelation(conn, layoutId, nodeIdMap, "cell_exit_points");

            await using var cmd = new NpgsqlCommand(@"
                SELECT id, name, cell_index, canonical_name, cell_type, is_collapsed, color,
                       input_terminal_position, output_terminal_position
                FROM cells WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("id", layoutId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dbCellId = reader.GetGuid(0);
                layout.Groups.Add(new GroupData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = reader.GetString(1),
                    CellIndex = GetIntOrDefault(reader, 2, 0),
                    CellType = GetStringOrDefault(reader, 4, "simple"),
                    Collapsed = GetBoolOrDefault(reader, 5, false),
                    Color = GetStringOrDefault(reader, 6, "#9B59B6"),
                    InputTerminalPosition = GetStringOrDefault(reader, 7, "left"),
                    OutputTerminalPosition = GetStringOrDefault(reader, 8, "right"),
                    IsCell = true,
                    Members = cellMembers.TryGetValue(dbCellId, out var m) ? m : new List<string>(),
                    EntryPoints = cellEntries.TryGetValue(dbCellId, out var e) ? e : new List<string>(),
                    ExitPoints = cellExits.TryGetValue(dbCellId, out var x) ? x : new List<string>()
                });
            }
        }

        private async Task<Dictionary<Guid, List<string>>> LoadCellRelation(NpgsqlConnection conn, Guid layoutId, Dictionary<Guid, string> nodeIdMap, string table)
        {
            var result = new Dictionary<Guid, List<string>>();
            await using var cmd = new NpgsqlCommand($"SELECT r.cell_id, r.node_id FROM {table} r JOIN cells c ON r.cell_id = c.id WHERE c.layout_id = @id", conn);
            cmd.Parameters.AddWithValue("id", layoutId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var cellId = reader.GetGuid(0);
                if (nodeIdMap.TryGetValue(reader.GetGuid(1), out var localId))
                {
                    if (!result.ContainsKey(cellId)) result[cellId] = new List<string>();
                    result[cellId].Add(localId);
                }
            }
            return result;
        }

        private async Task LoadWallsAsync(NpgsqlConnection conn, Guid layoutId, LayoutData layout)
        {
            await using var cmd = new NpgsqlCommand("SELECT x1, y1, x2, y2, wall_type, thickness, color, line_style FROM walls WHERE layout_id = @id", conn);
            cmd.Parameters.AddWithValue("id", layoutId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                layout.Walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = reader.GetDouble(0), Y1 = reader.GetDouble(1),
                    X2 = reader.GetDouble(2), Y2 = reader.GetDouble(3),
                    WallType = GetStringOrDefault(reader, 4, "standard"),
                    Thickness = GetDoubleOrDefault(reader, 5, 6),
                    Color = GetStringOrDefault(reader, 6, "#444444"),
                    LineStyle = GetStringOrDefault(reader, 7, "solid")
                });
            }
        }

        private async Task LoadTransportNetworksAsync(NpgsqlConnection conn, Guid layoutId, LayoutData layout)
        {
            var networks = new Dictionary<Guid, TransportNetworkData>();

            // Load networks
            await using (var cmd = new NpgsqlCommand("SELECT id, name, color FROM transport_networks WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", layoutId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dbId = reader.GetGuid(0);
                    var network = new TransportNetworkData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = reader.GetString(1),
                        Visual = new TransportNetworkVisual { Color = GetStringOrDefault(reader, 2, "#E74C3C") }
                    };
                    networks[dbId] = network;
                    layout.TransportNetworks.Add(network);
                }
            }

            // Load stations/waypoints
            var pointIdMap = new Dictionary<Guid, string>();
            await using (var cmd = new NpgsqlCommand(@"
                SELECT id, network_id, name, station_type, x, y, rotation, color, size, dwell_time, capacity
                FROM transport_stations WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", layoutId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dbId = reader.GetGuid(0);
                    var networkDbId = reader.GetGuid(1);
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
                                Rotation = GetDoubleOrDefault(reader, 6, 0),
                                Color = GetStringOrDefault(reader, 7, "#9B59B6"),
                                Width = GetDoubleOrDefault(reader, 8, 50),
                                Height = GetDoubleOrDefault(reader, 8, 50)
                            },
                            Simulation = new TransportStationSimulation
                            {
                                StationType = stationType,
                                DwellTime = GetDoubleOrDefault(reader, 9, 5.0),
                                QueueCapacity = GetIntOrDefault(reader, 10, 5)
                            }
                        });
                    }
                }
            }

            // Load segments
            await using (var cmd = new NpgsqlCommand(@"
                SELECT network_id, from_point_id, to_point_id, is_bidirectional, distance, speed_limit, color
                FROM transport_tracks WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", layoutId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!networks.TryGetValue(reader.GetGuid(0), out var network)) continue;
                    if (!pointIdMap.TryGetValue(reader.GetGuid(1), out var fromId) ||
                        !pointIdMap.TryGetValue(reader.GetGuid(2), out var toId)) continue;

                    network.Segments.Add(new TrackSegmentData
                    {
                        Id = Guid.NewGuid().ToString(),
                        From = fromId,
                        To = toId,
                        Bidirectional = GetBoolOrDefault(reader, 3, true),
                        Distance = GetDoubleOrDefault(reader, 4, 0),
                        SpeedLimit = GetDoubleOrDefault(reader, 5, 2.0),
                        Color = GetStringOrDefault(reader, 6, "")
                    });
                }
            }

            // Load transporters
            await using (var cmd = new NpgsqlCommand(@"
                SELECT network_id, name, transporter_type, home_station_id, speed, load_capacity, color
                FROM transporters WHERE layout_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", layoutId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!networks.TryGetValue(reader.GetGuid(0), out var network)) continue;
                    var homeDbId = reader.IsDBNull(3) ? (Guid?)null : reader.GetGuid(3);
                    string? homeLocalId = homeDbId.HasValue && pointIdMap.TryGetValue(homeDbId.Value, out var hid) ? hid : null;

                    network.Transporters.Add(new TransporterData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = reader.GetString(1),
                        TransporterType = GetStringOrDefault(reader, 2, "agv"),
                        HomeStationId = homeLocalId ?? "",
                        Speed = GetDoubleOrDefault(reader, 4, 1.0),
                        Capacity = GetIntOrDefault(reader, 5, 1),
                        Color = GetStringOrDefault(reader, 6, "#E74C3C")
                    });
                }
            }
        }

        // Helper methods
        private static string GetStringOrDefault(NpgsqlDataReader r, int i, string d) => r.IsDBNull(i) ? d : r.GetString(i);
        private static double? GetDoubleOrNull(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDouble(i);
        private static double GetDoubleOrDefault(NpgsqlDataReader r, int i, double d) => r.IsDBNull(i) ? d : r.GetDouble(i);
        private static int GetIntOrDefault(NpgsqlDataReader r, int i, int d) => r.IsDBNull(i) ? d : r.GetInt32(i);
        private static bool GetBoolOrDefault(NpgsqlDataReader r, int i, bool d) => r.IsDBNull(i) ? d : r.GetBoolean(i);
    }

    public class LayoutInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int NodeCount { get; set; }
        public int PathCount { get; set; }
        public int CellCount { get; set; }
    }
}
