using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Import layout from exported SQL file without needing PostgreSQL
    /// Parses INSERT statements and reconstructs the layout
    /// </summary>
    public class SqlFileImporter
    {
        public LayoutData ImportFromSqlFile(string filePath)
        {
            var sql = File.ReadAllText(filePath);
            var layout = new LayoutData();
            
            // Parse layout settings
            ParseLayout(sql, layout);
            
            // Parse nodes and build ID map
            var nodeIdMap = ParseNodes(sql, layout);
            
            // Parse paths
            ParsePaths(sql, layout, nodeIdMap);
            
            // Parse cells
            ParseCells(sql, layout, nodeIdMap);
            
            // Parse walls
            ParseWalls(sql, layout);
            
            // Parse transport networks
            ParseTransportNetworks(sql, layout);
            
            return layout;
        }

        private void ParseLayout(string sql, LayoutData layout)
        {
            // Match: INSERT INTO layouts ... VALUES ('id', 'name', width, height, gridSize, ...)
            var pattern = @"INSERT INTO layouts\s*\([^)]+\)\s*VALUES\s*\(\s*'([^']+)'\s*,\s*'([^']+)'\s*,\s*([0-9.]+)\s*,\s*([0-9.]+)\s*,\s*([0-9.]+)";
            var match = Regex.Match(sql, pattern, RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                layout.Canvas = new CanvasSettings
                {
                    Width = ParseDouble(match.Groups[3].Value, 1200),
                    Height = ParseDouble(match.Groups[4].Value, 800),
                    GridSize = (int)ParseDouble(match.Groups[5].Value, 20)
                };
            }
        }

        private Dictionary<string, string> ParseNodes(string sql, LayoutData layout)
        {
            var nodeIdMap = new Dictionary<string, string>(); // DB UUID -> local ID
            
            // Match node INSERT statements
            var pattern = @"INSERT INTO nodes\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 10) continue;
                
                var dbId = values[0].Trim('\'');
                var localId = Guid.NewGuid().ToString();
                nodeIdMap[dbId] = localId;
                
                var node = new NodeData
                {
                    Id = localId,
                    Name = values[2].Trim('\''),
                    Type = values[3].Trim('\''),
                    Label = values.Count > 4 ? values[4].Trim('\'') : "",
                    Visual = new NodeVisual
                    {
                        X = ParseDouble(values.Count > 5 ? values[5] : "0"),
                        Y = ParseDouble(values.Count > 6 ? values[6] : "0"),
                        Width = ParseDouble(values.Count > 7 ? values[7] : "80", 80),
                        Height = ParseDouble(values.Count > 8 ? values[8] : "60", 60),
                        Rotation = ParseDouble(values.Count > 9 ? values[9] : "0"),
                        Color = values.Count > 10 ? values[10].Trim('\'') : "#4A90D9",
                        Icon = values.Count > 11 ? values[11].Trim('\'') : "machine_generic",
                        InputTerminalPosition = values.Count > 12 ? values[12].Trim('\'') : "left",
                        OutputTerminalPosition = values.Count > 13 ? values[13].Trim('\'') : "right"
                    },
                    Simulation = new SimulationParams
                    {
                        Servers = (int)ParseDouble(values.Count > 14 ? values[14] : "1", 1),
                        Capacity = (int)ParseDouble(values.Count > 15 ? values[15] : "1", 1)
                    }
                };
                
                layout.Nodes.Add(node);
            }
            
            return nodeIdMap;
        }

        private void ParsePaths(string sql, LayoutData layout, Dictionary<string, string> nodeIdMap)
        {
            var pattern = @"INSERT INTO paths\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 4) continue;
                
                var fromDbId = values[2].Trim('\'');
                var toDbId = values[3].Trim('\'');
                
                if (!nodeIdMap.TryGetValue(fromDbId, out var fromId) ||
                    !nodeIdMap.TryGetValue(toDbId, out var toId))
                    continue;
                
                var path = new PathData
                {
                    Id = Guid.NewGuid().ToString(),
                    From = fromId,
                    To = toId,
                    PathType = values.Count > 4 ? values[4].Trim('\'') : "single",
                    ConnectionType = values.Count > 5 ? values[5].Trim('\'') : "partFlow",
                    RoutingMode = values.Count > 6 ? values[6].Trim('\'') : "direct",
                    Visual = new PathVisual
                    {
                        Color = values.Count > 7 ? values[7].Trim('\'') : "#888888"
                    },
                    Simulation = new PathSimulation
                    {
                        TransportType = values.Count > 8 ? values[8].Trim('\'') : "conveyor",
                        Speed = ParseDouble(values.Count > 10 ? values[10] : "1"),
                        Capacity = (int)ParseDouble(values.Count > 11 ? values[11] : "10", 10)
                    }
                };
                
                layout.Paths.Add(path);
            }
        }

        private void ParseCells(string sql, LayoutData layout, Dictionary<string, string> nodeIdMap)
        {
            // Parse cells
            var cellIdMap = new Dictionary<string, string>();
            var pattern = @"INSERT INTO cells\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 3) continue;
                
                var dbId = values[0].Trim('\'');
                var localId = Guid.NewGuid().ToString();
                cellIdMap[dbId] = localId;
                
                var cell = new GroupData
                {
                    Id = localId,
                    Name = values[2].Trim('\''),
                    CellIndex = (int)ParseDouble(values.Count > 3 ? values[3] : "0"),
                    CellType = values.Count > 5 ? values[5].Trim('\'') : "simple",
                    Color = values.Count > 7 ? values[7].Trim('\'') : "#9B59B6",
                    IsCell = true
                };
                
                layout.Groups.Add(cell);
            }
            
            // Parse cell members
            var memberPattern = @"INSERT INTO cell_members\s*\([^)]+\)\s*VALUES\s*\(\s*'([^']+)'\s*,\s*'([^']+)'\s*\)";
            var memberMatches = Regex.Matches(sql, memberPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in memberMatches)
            {
                var cellDbId = match.Groups[1].Value;
                var nodeDbId = match.Groups[2].Value;
                
                var cell = layout.Groups.FirstOrDefault(g => 
                    cellIdMap.TryGetValue(cellDbId, out var localCellId) && g.Id == localCellId);
                    
                if (cell != null && nodeIdMap.TryGetValue(nodeDbId, out var nodeLocalId))
                {
                    cell.Members.Add(nodeLocalId);
                }
            }
            
            // Similar for entry/exit points
            ParseCellPoints(sql, layout, cellIdMap, nodeIdMap, "cell_entry_points", (cell, nodeId) => cell.EntryPoints.Add(nodeId));
            ParseCellPoints(sql, layout, cellIdMap, nodeIdMap, "cell_exit_points", (cell, nodeId) => cell.ExitPoints.Add(nodeId));
        }
        
        private void ParseCellPoints(string sql, LayoutData layout, Dictionary<string, string> cellIdMap, 
            Dictionary<string, string> nodeIdMap, string tableName, Action<GroupData, string> addPoint)
        {
            var pattern = $@"INSERT INTO {tableName}\s*\([^)]+\)\s*VALUES\s*\(\s*'([^']+)'\s*,\s*'([^']+)'\s*\)";
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var cellDbId = match.Groups[1].Value;
                var nodeDbId = match.Groups[2].Value;
                
                var cell = layout.Groups.FirstOrDefault(g => 
                    cellIdMap.TryGetValue(cellDbId, out var localCellId) && g.Id == localCellId);
                    
                if (cell != null && nodeIdMap.TryGetValue(nodeDbId, out var nodeLocalId))
                {
                    addPoint(cell, nodeLocalId);
                }
            }
        }

        private void ParseWalls(string sql, LayoutData layout)
        {
            var pattern = @"INSERT INTO walls\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 6) continue;
                
                var wall = new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = ParseDouble(values[2]),
                    Y1 = ParseDouble(values[3]),
                    X2 = ParseDouble(values[4]),
                    Y2 = ParseDouble(values[5]),
                    WallType = values.Count > 6 ? values[6].Trim('\'') : "standard",
                    Thickness = ParseDouble(values.Count > 7 ? values[7] : "6", 6),
                    Color = values.Count > 8 ? values[8].Trim('\'') : "#444444",
                    LineStyle = values.Count > 9 ? values[9].Trim('\'') : "solid"
                };
                
                layout.Walls.Add(wall);
            }
        }

        private void ParseTransportNetworks(string sql, LayoutData layout)
        {
            var networkIdMap = new Dictionary<string, string>();
            var stationIdMap = new Dictionary<string, string>();
            
            // Parse networks
            var netPattern = @"INSERT INTO transport_networks\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var netMatches = Regex.Matches(sql, netPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in netMatches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 3) continue;
                
                var dbId = values[0].Trim('\'');
                var localId = Guid.NewGuid().ToString();
                networkIdMap[dbId] = localId;
                
                var network = new TransportNetworkData
                {
                    Id = localId,
                    Name = values[2].Trim('\''),
                    Visual = new TransportNetworkVisual
                    {
                        Color = values.Count > 4 ? values[4].Trim('\'') : "#E67E22"
                    }
                };
                
                layout.TransportNetworks.Add(network);
            }
            
            // Parse stations
            var stationPattern = @"INSERT INTO transport_stations\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var stationMatches = Regex.Matches(sql, stationPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in stationMatches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 7) continue;
                
                var dbId = values[0].Trim('\'');
                var networkDbId = values[1].Trim('\'');
                var stationType = values[4].Trim('\'');
                
                if (!networkIdMap.TryGetValue(networkDbId, out var networkLocalId)) continue;
                var network = layout.TransportNetworks.FirstOrDefault(n => n.Id == networkLocalId);
                if (network == null) continue;
                
                var localId = Guid.NewGuid().ToString();
                stationIdMap[dbId] = localId;
                
                if (stationType == "waypoint")
                {
                    network.Waypoints.Add(new WaypointData
                    {
                        Id = localId,
                        Name = values[3].Trim('\''),
                        X = ParseDouble(values[5]),
                        Y = ParseDouble(values[6])
                    });
                }
                else
                {
                    network.Stations.Add(new TransportStationData
                    {
                        Id = localId,
                        Name = values[3].Trim('\''),
                        Visual = new TransportStationVisual
                        {
                            X = ParseDouble(values[5]),
                            Y = ParseDouble(values[6]),
                            Rotation = ParseDouble(values.Count > 7 ? values[7] : "0"),
                            Color = values.Count > 8 ? values[8].Trim('\'') : "#9B59B6"
                        },
                        Simulation = new TransportStationSimulation
                        {
                            StationType = stationType
                        }
                    });
                }
            }
            
            // Parse tracks
            var trackPattern = @"INSERT INTO transport_tracks\s*\([^)]+\)\s*VALUES\s*\(([^;]+)\);";
            var trackMatches = Regex.Matches(sql, trackPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in trackMatches)
            {
                var values = ParseValues(match.Groups[1].Value);
                if (values.Count < 5) continue;
                
                var networkDbId = values[1].Trim('\'');
                var fromDbId = values[3].Trim('\'');
                var toDbId = values[4].Trim('\'');
                
                if (!networkIdMap.TryGetValue(networkDbId, out var networkLocalId)) continue;
                var network = layout.TransportNetworks.FirstOrDefault(n => n.Id == networkLocalId);
                if (network == null) continue;
                
                if (!stationIdMap.TryGetValue(fromDbId, out var fromId) ||
                    !stationIdMap.TryGetValue(toDbId, out var toId)) continue;
                
                network.Segments.Add(new TrackSegmentData
                {
                    Id = Guid.NewGuid().ToString(),
                    From = fromId,
                    To = toId,
                    Bidirectional = values.Count > 5 && values[5].ToLower() == "true",
                    Distance = ParseDouble(values.Count > 6 ? values[6] : "0"),
                    SpeedLimit = ParseDouble(values.Count > 7 ? values[7] : "2")
                });
            }
        }

        #region Helpers

        private List<string> ParseValues(string valueStr)
        {
            var values = new List<string>();
            var current = "";
            var inQuote = false;
            var parenDepth = 0;
            
            foreach (var c in valueStr)
            {
                if (c == '\'' && parenDepth == 0)
                {
                    inQuote = !inQuote;
                    current += c;
                }
                else if (c == '(' && !inQuote)
                {
                    parenDepth++;
                    current += c;
                }
                else if (c == ')' && !inQuote)
                {
                    parenDepth--;
                    current += c;
                }
                else if (c == ',' && !inQuote && parenDepth == 0)
                {
                    values.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            if (!string.IsNullOrWhiteSpace(current))
                values.Add(current.Trim());
            
            return values;
        }

        private double ParseDouble(string value, double defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return defaultValue;
            return double.TryParse(value.Trim('\''), out var result) ? result : defaultValue;
        }

        #endregion
    }
}
