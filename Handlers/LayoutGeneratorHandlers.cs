using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Dialogs;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Layout Generator Commands

        /// <summary>
        /// Generate Warehouse Layout
        /// Creates a procedural warehouse with racks, aisles, and equipment
        /// </summary>
        private void GenerateWarehouseLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                // Prompt user for warehouse parameters
                var result = PromptWarehouseParameters();
                if (result == null)
                    return;

                StatusText.Text = "Generating warehouse layout...";

                var (rows, cols, rackWidth, rackDepth, aisleWidth, startX, startY) = result.Value;

                int nodesCreated = 0;
                int aislesCreated = 0;

                // Generate rack positions in grid
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        double x = startX + col * (rackWidth + aisleWidth);
                        double y = startY + row * (rackDepth + aisleWidth);

                        // Create rack node
                        var node = new NodeData
                        {
                            Id = $"rack_{row}_{col}",
                            Label = $"R{row + 1}C{col + 1}",
                            Type = "Storage",
                            Visual = new NodeVisual
                            {
                                X = x,
                                Y = y,
                                Width = rackWidth,
                                Height = rackDepth,
                                Color = "#95A5A6"
                            }
                        };
                        _layout.Nodes.Add(node);
                        nodesCreated++;
                    }
                }

                // Generate horizontal aisles between rows
                for (int row = 0; row < rows - 1; row++)
                {
                    double aisleY = startY + (row + 1) * rackDepth + row * aisleWidth + aisleWidth / 2;
                    double aisleX = startX + (cols * rackWidth + (cols - 1) * aisleWidth) / 2;

                    var aisle = new OpeningData
                    {
                        Id = $"aisle_h_{row}",
                        Name = $"Horizontal Aisle {row + 1}",
                        OpeningType = "Aisle",
                        X = aisleX,
                        Y = aisleY,
                        ClearWidth = cols * rackWidth + (cols - 1) * aisleWidth
                    };
                    _layout.Openings.Add(aisle);
                    aislesCreated++;
                }

                // Generate vertical aisles between columns
                for (int col = 0; col < cols - 1; col++)
                {
                    double aisleX = startX + (col + 1) * rackWidth + col * aisleWidth + aisleWidth / 2;
                    double aisleY = startY + (rows * rackDepth + (rows - 1) * aisleWidth) / 2;

                    var aisle = new OpeningData
                    {
                        Id = $"aisle_v_{col}",
                        Name = $"Vertical Aisle {col + 1}",
                        OpeningType = "Aisle",
                        X = aisleX,
                        Y = aisleY,
                        ClearWidth = rows * rackDepth + (rows - 1) * aisleWidth
                    };
                    _layout.Openings.Add(aisle);
                    aislesCreated++;
                }

                StatusText.Text = $"Warehouse generated: {nodesCreated} racks, {aislesCreated} aisles";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error generating warehouse: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate Assembly Line Layout
        /// Creates a linear production line with stations
        /// </summary>
        private void GenerateAssemblyLine_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                // Prompt for assembly line parameters
                var result = PromptAssemblyLineParameters();
                if (result == null)
                    return;

                StatusText.Text = "Generating assembly line...";

                var (stations, spacing, startX, startY, addConveyor) = result.Value;

                int nodesCreated = 0;
                int pathsCreated = 0;

                // Create stations
                for (int i = 0; i < stations; i++)
                {
                    double x = startX + i * spacing;
                    double y = startY;

                    var node = new NodeData
                    {
                        Id = $"station_{i}",
                        Label = $"Station {i + 1}",
                        Type = "Process",
                        Visual = new NodeVisual
                        {
                            X = x,
                            Y = y,
                            Width = 40,
                            Height = 40,
                            Color = "#3498DB"
                        }
                    };
                    _layout.Nodes.Add(node);
                    nodesCreated++;

                    // Connect to previous station
                    if (i > 0)
                    {
                        if (addConveyor)
                        {
                            // Create conveyor with path
                            var conveyor = new ConveyorData
                            {
                                Id = $"conv_{i - 1}_{i}",
                                Name = $"Conveyor {i - 1}-{i}",
                                Speed = 1.0
                            };
                            conveyor.Path.Add(new PointData(startX + (i - 1) * spacing, startY));
                            conveyor.Path.Add(new PointData(x, y));
                            _layout.Conveyors.Add(conveyor);
                        }
                        else
                        {
                            // Create regular path
                            var path = new PathData
                            {
                                Id = $"path_{i - 1}_{i}",
                                From = $"station_{i - 1}",
                                To = $"station_{i}",
                                PathType = "single"
                            };
                            _layout.Paths.Add(path);
                        }
                        pathsCreated++;
                    }
                }

                StatusText.Text = $"Assembly line generated: {nodesCreated} stations, {pathsCreated} connections";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error generating assembly line: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate Storage Grid Layout
        /// Creates a dense grid of storage locations
        /// </summary>
        private void GenerateStorageGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                var result = PromptStorageGridParameters();
                if (result == null)
                    return;

                StatusText.Text = "Generating storage grid...";

                var (rows, cols, cellSize, spacing, startX, startY) = result.Value;

                int cellsCreated = 0;

                // Create storage cells in grid
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        double x = startX + col * (cellSize + spacing);
                        double y = startY + row * (cellSize + spacing);

                        var node = new NodeData
                        {
                            Id = $"cell_{row}_{col}",
                            Label = $"{row * cols + col + 1}",
                            Type = "Storage",
                            Visual = new NodeVisual
                            {
                                X = x,
                                Y = y,
                                Width = cellSize,
                                Height = cellSize,
                                Color = "#E67E22"
                            }
                        };
                        _layout.Nodes.Add(node);
                        cellsCreated++;
                    }
                }

                StatusText.Text = $"Storage grid generated: {cellsCreated} cells";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error generating storage grid: {ex.Message}";
            }
        }

        /// <summary>
        /// Auto-Place Cranes
        /// Analyzes layout and automatically places overhead cranes
        /// </summary>
        private void AutoPlaceCranes_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Auto-placing cranes...";

                // Find equipment zones that need crane coverage
                var zones = DetectEquipmentZones();

                int cranesCreated = 0;

                foreach (var zone in zones)
                {
                    // Create runway spanning the zone
                    var runway = new RunwayData
                    {
                        Id = $"runway_{cranesCreated}",
                        Name = $"Runway {cranesCreated + 1}",
                        StartX = zone.MinX,
                        StartY = zone.CenterY,
                        EndX = zone.MaxX,
                        EndY = zone.CenterY
                    };
                    _layout.Runways.Add(runway);

                    // Create EOT crane on runway
                    var crane = new EOTCraneData
                    {
                        Id = $"eot_{cranesCreated}",
                        Name = $"Crane {cranesCreated + 1}",
                        RunwayId = runway.Id,
                        ZoneMin = 0,
                        ZoneMax = 1,
                        ReachLeft = zone.Height / 2,
                        ReachRight = zone.Height / 2
                    };
                    _layout.EOTCranes.Add(crane);
                    cranesCreated++;
                }

                StatusText.Text = $"Auto-placed {cranesCreated} cranes";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error auto-placing cranes: {ex.Message}";
            }
        }

        /// <summary>
        /// Auto-Generate AGV Network
        /// Creates AGV waypoints and paths connecting equipment
        /// </summary>
        private void AutoGenerateAGVNetwork_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Generating AGV network...";

                int waypointsCreated = 0;
                int pathsCreated = 0;

                // Create waypoints at each equipment node
                foreach (var node in _layout.Nodes)
                {
                    var waypoint = new AGVWaypointData
                    {
                        Id = $"wp_{node.Id}",
                        Name = $"WP {node.Label}",
                        X = node.Visual.X,
                        Y = node.Visual.Y
                    };
                    _layout.AGVWaypoints.Add(waypoint);
                    waypointsCreated++;
                }

                // Create paths between nearby waypoints
                var waypoints = _layout.AGVWaypoints.ToList();
                for (int i = 0; i < waypoints.Count; i++)
                {
                    var wp1 = waypoints[i];

                    // Find nearest neighbors
                    var neighbors = waypoints
                        .Where(wp2 => wp2.Id != wp1.Id)
                        .Select(wp2 => new
                        {
                            Waypoint = wp2,
                            Distance = Math.Sqrt(Math.Pow(wp2.X - wp1.X, 2) + Math.Pow(wp2.Y - wp1.Y, 2))
                        })
                        .OrderBy(x => x.Distance)
                        .Take(3) // Connect to 3 nearest neighbors
                        .ToList();

                    foreach (var neighbor in neighbors)
                    {
                        // Check if path already exists
                        bool pathExists = _layout.AGVPaths.Any(p =>
                            (p.FromWaypointId == wp1.Id && p.ToWaypointId == neighbor.Waypoint.Id) ||
                            (p.FromWaypointId == neighbor.Waypoint.Id && p.ToWaypointId == wp1.Id));

                        if (!pathExists && neighbor.Distance < 200) // Max connection distance
                        {
                            var path = new AGVPathData
                            {
                                Id = $"agv_path_{pathsCreated}",
                                FromWaypointId = wp1.Id,
                                ToWaypointId = neighbor.Waypoint.Id,
                                Direction = "bidirectional"
                            };
                            _layout.AGVPaths.Add(path);
                            pathsCreated++;
                        }
                    }
                }

                StatusText.Text = $"AGV network generated: {waypointsCreated} waypoints, {pathsCreated} paths";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error generating AGV network: {ex.Message}";
            }
        }

        /// <summary>
        /// Optimize Layout using genetic algorithm
        /// Opens dialog to configure and run optimization
        /// </summary>
        private void OptimizeLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            if (_layout.Nodes.Count == 0 && _layout.AGVStations.Count == 0)
            {
                StatusText.Text = "Layout is empty - nothing to optimize";
                return;
            }

            SaveUndoState();

            var dialog = new OptimizeDialog(_layout, () =>
            {
                MarkDirty();
                RefreshAll();  // Use RefreshAll to update Equipment Browser Panel
            });
            dialog.Owner = this;
            dialog.ShowDialog();

            StatusText.Text = "Optimization dialog closed";
        }

        #endregion

        #region Helper Methods for Layout Generation

        /// <summary>
        /// Prompt user for warehouse parameters
        /// </summary>
        private (int rows, int cols, double rackWidth, double rackDepth, double aisleWidth, double startX, double startY)? PromptWarehouseParameters()
        {
            // Simple default parameters - in real implementation, show dialog
            return (5, 4, 60, 40, 30, 50, 50);
        }

        /// <summary>
        /// Prompt user for assembly line parameters
        /// </summary>
        private (int stations, double spacing, double startX, double startY, bool addConveyor)? PromptAssemblyLineParameters()
        {
            // Simple default parameters
            return (6, 100, 50, 200, true);
        }

        /// <summary>
        /// Prompt user for storage grid parameters
        /// </summary>
        private (int rows, int cols, double cellSize, double spacing, double startX, double startY)? PromptStorageGridParameters()
        {
            // Simple default parameters
            return (8, 10, 30, 5, 50, 400);
        }

        /// <summary>
        /// Detect equipment zones for crane placement
        /// </summary>
        private List<EquipmentZone> DetectEquipmentZones()
        {
            var zones = new List<EquipmentZone>();

            if (_layout.Nodes.Count == 0)
                return zones;

            // Group nodes by approximate Y coordinate (horizontal zones)
            var groups = new List<List<NodeData>>();
            double rowThreshold = 100;

            foreach (var node in _layout.Nodes)
            {
                bool addedToGroup = false;
                foreach (var group in groups)
                {
                    if (group.Count > 0 && Math.Abs(group[0].Visual.Y - node.Visual.Y) < rowThreshold)
                    {
                        group.Add(node);
                        addedToGroup = true;
                        break;
                    }
                }

                if (!addedToGroup)
                {
                    groups.Add(new List<NodeData> { node });
                }
            }

            // Create zones from groups
            foreach (var group in groups)
            {
                if (group.Count >= 2) // Need at least 2 nodes for a crane
                {
                    var minX = group.Min(n => n.Visual.X);
                    var maxX = group.Max(n => n.Visual.X);
                    var avgY = group.Average(n => n.Visual.Y);
                    var height = group.Max(n => n.Visual.Y) - group.Min(n => n.Visual.Y) + 100;

                    zones.Add(new EquipmentZone
                    {
                        MinX = minX - 50,
                        MaxX = maxX + 50,
                        CenterY = avgY,
                        Height = height
                    });
                }
            }

            return zones;
        }

        #endregion

        #region Helper Classes

        private class EquipmentZone
        {
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double CenterY { get; set; }
            public double Height { get; set; }
        }

        #endregion

        #region Custom Layout Generator

        /// <summary>
        /// Custom Layout Generator - User specifies exact entity counts
        /// </summary>
        private void GenerateCustomLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            var dialog = new Dialogs.CustomLayoutDialog();
            if (dialog.ShowDialog() != true)
                return;

            var config = dialog.Config;

            try
            {
                SaveUndoState();
                StatusText.Text = "Generating custom layout...";

                // Clear existing layout
                _layout.Nodes.Clear();
                _layout.Paths.Clear();
                _layout.Groups.Clear();
                _layout.AGVWaypoints.Clear();
                _layout.AGVPaths.Clear();
                _layout.Zones.Clear();
                _layout.EOTCranes.Clear();
                _layout.JibCranes.Clear();
                _layout.Runways.Clear();

                double canvasWidth = _layout.Canvas.Width;
                double canvasHeight = _layout.Canvas.Height;

                Random rand = config.RandomizePlacement ? new Random() : null;
                int nodeIndex = 0;

                // Check if using grid layout with zones
                if (config.UseGridLayout && config.ZoneCount > 0)
                {
                    GenerateGridLayoutWithZones(config, canvasWidth, canvasHeight, rand);
                    return;
                }

                // Original non-grid layout generation
                // Calculate grid layout
                int totalStorageNodes = config.StorageBinCount + config.BufferCount + config.MachineCount;
                int gridCols = (int)Math.Ceiling(Math.Sqrt(totalStorageNodes * 1.5));
                int gridRows = (int)Math.Ceiling((double)totalStorageNodes / gridCols);

                double cellWidth = (canvasWidth - 100) / gridCols;
                double cellHeight = (canvasHeight - 200) / gridRows;
                double startX = 50;
                double startY = 50;

                // Generate storage bins
                for (int i = 0; i < config.StorageBinCount; i++)
                {
                    var (x, y) = GetGridPosition(nodeIndex++, gridCols, cellWidth, cellHeight, startX, startY, rand);
                    var node = new NodeData
                    {
                        Id = $"storage_{i + 1}",
                        Label = $"Storage R{i / gridCols + 1}C{i % gridCols + 1}",
                        Type = "storage",
                        Visual = new NodeVisual { X = x, Y = y, Color = "#95A5A6", Width = 40, Height = 30 }
                    };
                    _layout.Nodes.Add(node);
                }

                // Generate buffers
                for (int i = 0; i < config.BufferCount; i++)
                {
                    var (x, y) = GetGridPosition(nodeIndex++, gridCols, cellWidth, cellHeight, startX, startY, rand);
                    var node = new NodeData
                    {
                        Id = $"buffer_{i + 1}",
                        Label = $"Buffer {i + 1}",
                        Type = "buffer",
                        Visual = new NodeVisual { X = x, Y = y, Color = "#F39C12", Width = 35, Height = 35 }
                    };
                    _layout.Nodes.Add(node);
                }

                // Generate machines
                for (int i = 0; i < config.MachineCount; i++)
                {
                    var (x, y) = GetGridPosition(nodeIndex++, gridCols, cellWidth, cellHeight, startX, startY, rand);
                    var node = new NodeData
                    {
                        Id = $"machine_{i + 1}",
                        Label = $"Machine {i + 1}",
                        Type = "machine",
                        Visual = new NodeVisual { X = x, Y = y, Color = "#3498DB", Width = 50, Height = 40 }
                    };
                    _layout.Nodes.Add(node);
                }

                // Generate AGV stations with linked waypoints
                for (int i = 0; i < config.AGVStationCount; i++)
                {
                    double x = startX + (i % 2) * (canvasWidth - 100);
                    double y = startY + 50 + (i / 2) * 100;

                    string waypointId = $"agv_waypoint_{i + 1}";
                    string stationId = $"agv_station_{i + 1}";

                    // Create waypoint first
                    var waypoint = new AGVWaypointData
                    {
                        Id = waypointId,
                        Name = $"Waypoint {i + 1}",
                        X = x,
                        Y = y,
                        WaypointType = WaypointTypes.Stop
                    };
                    _layout.AGVWaypoints.Add(waypoint);

                    // Create AGV station linked to waypoint
                    var station = new AGVStationData
                    {
                        Id = stationId,
                        Name = $"AGV Station {i + 1}",
                        X = x,
                        Y = y,
                        LinkedWaypointId = waypointId,
                        Color = "#FF0000"  // Red
                    };
                    _layout.AGVStations.Add(station);
                }

                // Generate EOT cranes with runways
                if (config.EOTCraneCount > 0)
                {
                    double runwaySpacing = canvasHeight / (config.EOTCraneCount + 1);
                    for (int i = 0; i < config.EOTCraneCount; i++)
                    {
                        double runwayY = startY + runwaySpacing * (i + 1);

                        // Create runway
                        var runway = new RunwayData
                        {
                            Id = $"runway_{i + 1}",
                            Name = $"Runway {i + 1}",
                            StartX = 20,
                            StartY = runwayY,
                            EndX = canvasWidth - 20,
                            EndY = runwayY
                        };
                        _layout.Runways.Add(runway);

                        // Create EOT crane on this runway
                        var crane = new EOTCraneData
                        {
                            Id = $"eot_{i + 1}",
                            Name = $"EOT Crane {i + 1}",
                            RunwayId = runway.Id,
                            ReachLeft = 15,
                            ReachRight = 15,
                            ZoneMin = 0.0,
                            ZoneMax = 1.0,
                            BridgePosition = 0.5,
                            SpeedBridge = 1.0,
                            SpeedTrolley = 0.5,
                            Color = "#E67E22"
                        };
                        _layout.EOTCranes.Add(crane);
                    }
                }

                // Generate Jib cranes
                for (int i = 0; i < config.JibCraneCount; i++)
                {
                    double x = startX + 30 + (i % 3) * (canvasWidth / 3);
                    double y = canvasHeight - 80 - (i / 3) * 100;

                    var crane = new JibCraneData
                    {
                        Id = $"jib_{i + 1}",
                        Name = $"Jib Crane {i + 1}",
                        CenterX = x,
                        CenterY = y,
                        Radius = 25,           // Use Radius, not Reach
                        ArcStart = 0,          // Use ArcStart, not RotationMin
                        ArcEnd = 270,          // Use ArcEnd, not RotationMax
                        SpeedSlew = 10,        // Use SpeedSlew, not SpeedRotation
                        SpeedHoist = 0.5,
                        Color = "#9B59B6"
                    };
                    _layout.JibCranes.Add(crane);
                }

                // Auto-generate AGV path network if requested
                if (config.GenerateAGVPaths && config.AGVStationCount >= 2)
                {
                    var stations = _layout.AGVWaypoints.ToList();
                    for (int i = 0; i < stations.Count - 1; i++)
                    {
                        var path = new AGVPathData
                        {
                            Id = $"agv_path_{i + 1}",
                            Name = $"Path {i + 1}",
                            FromWaypointId = stations[i].Id,
                            ToWaypointId = stations[i + 1].Id,
                            Width = 1.2,
                            SpeedLimit = 1.0,
                            Direction = PathDirections.Bidirectional
                        };
                        _layout.AGVPaths.Add(path);
                    }
                    // Close the loop
                    if (stations.Count > 2)
                    {
                        var closingPath = new AGVPathData
                        {
                            Id = $"agv_path_{stations.Count}",
                            Name = $"Path {stations.Count}",
                            FromWaypointId = stations[stations.Count - 1].Id,
                            ToWaypointId = stations[0].Id,
                            Width = 1.2,
                            SpeedLimit = 1.0,
                            Direction = PathDirections.Bidirectional
                        };
                        _layout.AGVPaths.Add(closingPath);
                    }
                }

                // Auto-generate traffic zones if requested
                if (config.GenerateZones && config.ZoneCount > 0)
                {
                    // Calculate zone grid layout
                    int zoneCols = (int)Math.Ceiling(Math.Sqrt(config.ZoneCount));
                    int zoneRows = (int)Math.Ceiling((double)config.ZoneCount / zoneCols);
                    double zoneWidth = (canvasWidth - 100) / zoneCols;
                    double zoneHeight = (canvasHeight - 150) / zoneRows;

                    string[] zoneTypes = { "warehouse", "storage", "production", "shipping", "receiving" };
                    string[] zoneNames = { "Warehouse", "Storage", "Production", "Shipping", "Receiving" };

                    for (int zoneIdx = 0; zoneIdx < config.ZoneCount; zoneIdx++)
                    {
                        int row = zoneIdx / zoneCols;
                        int col = zoneIdx % zoneCols;

                        double zoneX = startX + col * zoneWidth;
                        double zoneY = startY + row * zoneHeight;

                        var zone = new ZoneData
                        {
                            Id = $"zone_{zoneIdx + 1}",
                            Name = $"{zoneNames[zoneIdx % zoneNames.Length]} Zone {zoneIdx + 1}",
                            Type = zoneTypes[zoneIdx % zoneTypes.Length]
                        };

                        // Add polygon points for the zone (with small padding)
                        double pad = 5;
                        zone.Points.Add(new PointData(zoneX + pad, zoneY + pad));
                        zone.Points.Add(new PointData(zoneX + zoneWidth - pad, zoneY + pad));
                        zone.Points.Add(new PointData(zoneX + zoneWidth - pad, zoneY + zoneHeight - pad));
                        zone.Points.Add(new PointData(zoneX + pad, zoneY + zoneHeight - pad));

                        _layout.Zones.Add(zone);
                    }
                }

                MarkDirty();
                RefreshAll();  // Use RefreshAll to update Equipment Browser Panel

                int totalEntities = config.StorageBinCount + config.BufferCount + config.MachineCount +
                                  config.AGVStationCount + config.EOTCraneCount + config.JibCraneCount;

                StatusText.Text = $"Generated custom layout: {totalEntities} entities " +
                                $"({_layout.AGVPaths.Count} AGV paths, {_layout.Zones.Count} zones)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating custom layout: {ex.Message}", "Generation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Layout generation failed";
            }
        }

        /// <summary>
        /// Calculate grid position for entity placement
        /// </summary>
        private (double x, double y) GetGridPosition(int index, int gridCols, double cellWidth, double cellHeight,
            double startX, double startY, Random rand)
        {
            int row = index / gridCols;
            int col = index % gridCols;

            double x = startX + col * cellWidth + cellWidth / 2;
            double y = startY + row * cellHeight + cellHeight / 2;

            if (rand != null)
            {
                // Add random offset within cell
                x += rand.NextDouble() * cellWidth * 0.3 - cellWidth * 0.15;
                y += rand.NextDouble() * cellHeight * 0.3 - cellHeight * 0.15;
            }

            return (x, y);
        }

        /// <summary>
        /// Generate grid layout with zones - entities placed in arrays within zones
        /// </summary>
        private void GenerateGridLayoutWithZones(LayoutGenerationConfig config, double canvasWidth, double canvasHeight, Random rand)
        {
            try
            {
                // Calculate zone grid layout
                int zoneCols = (int)Math.Ceiling(Math.Sqrt(config.ZoneCount));
                int zoneRows = (int)Math.Ceiling((double)config.ZoneCount / zoneCols);

                double zoneWidth = (canvasWidth - 100) / zoneCols;
                double zoneHeight = (canvasHeight - 100) / zoneRows;
                double margin = 30; // Space between zones

                // AGV stations go around the perimeter
                List<(double x, double y)> agvStationPositions = new List<(double, double)>();

                // Generate zones and their entities
                for (int zoneIdx = 0; zoneIdx < config.ZoneCount; zoneIdx++)
                {
                    int zoneRow = zoneIdx / zoneCols;
                    int zoneCol = zoneIdx % zoneCols;

                    double zoneLeft = 50 + zoneCol * zoneWidth;
                    double zoneTop = 50 + zoneRow * zoneHeight;
                    double zoneRight = zoneLeft + zoneWidth - margin;
                    double zoneBottom = zoneTop + zoneHeight - margin;

                    // Create zone polygon
                    var zone = new ZoneData
                    {
                        Id = $"zone_{zoneIdx + 1}",
                        Name = $"Zone {zoneIdx + 1}",
                        Type = "production"
                    };
                    zone.Points.Add(new PointData(zoneLeft, zoneTop));
                    zone.Points.Add(new PointData(zoneRight, zoneTop));
                    zone.Points.Add(new PointData(zoneRight, zoneBottom));
                    zone.Points.Add(new PointData(zoneLeft, zoneBottom));
                    _layout.Zones.Add(zone);

                    // Place entities in grid within this zone
                    int entitiesPerZone = config.EntitiesPerZone;
                    int entityCols = (int)Math.Ceiling(Math.Sqrt(entitiesPerZone));
                    int entityRows = (int)Math.Ceiling((double)entitiesPerZone / entityCols);

                    double entityCellWidth = (zoneRight - zoneLeft - 20) / entityCols;
                    double entityCellHeight = (zoneBottom - zoneTop - 20) / entityRows;
                    double entityStartX = zoneLeft + 10;
                    double entityStartY = zoneTop + 10;

                    for (int entityIdx = 0; entityIdx < entitiesPerZone && entityIdx < config.StorageBinCount + config.MachineCount; entityIdx++)
                    {
                        int entityRow = entityIdx / entityCols;
                        int entityCol = entityIdx % entityCols;

                        double x = entityStartX + entityCol * entityCellWidth + entityCellWidth / 2;
                        double y = entityStartY + entityRow * entityCellHeight + entityCellHeight / 2;

                        // Alternate between storage and machines
                        bool isStorage = (entityIdx % 2 == 0);
                        var node = new NodeData
                        {
                            Id = $"zone{zoneIdx + 1}_entity_{entityIdx + 1}",
                            Label = isStorage ? $"S{zoneIdx + 1}.{entityIdx + 1}" : $"M{zoneIdx + 1}.{entityIdx + 1}",
                            Type = isStorage ? "storage" : "machine",
                            Visual = new NodeVisual
                            {
                                X = x,
                                Y = y,
                                Color = isStorage ? "#95A5A6" : "#3498DB",
                                Width = isStorage ? 30 : 40,
                                Height = isStorage ? 25 : 35
                            }
                        };
                        _layout.Nodes.Add(node);
                    }

                    // Place AGV station near zone entrance (bottom center)
                    if (zoneIdx < config.AGVStationCount)
                    {
                        double stationX = (zoneLeft + zoneRight) / 2;
                        double stationY = zoneBottom + 15;
                        agvStationPositions.Add((stationX, stationY));
                    }

                    // Determine zone service type
                    int serviceType = config.ZoneServiceType;
                    if (serviceType == 3) // Mixed
                        serviceType = zoneIdx % 3; // Alternate AGV, Jib, EOT

                    // Add service equipment based on zone type
                    switch (serviceType)
                    {
                        case 0: // AGV
                            // AGV station already added above
                            break;

                        case 1: // Jib Crane
                            if (zoneIdx < config.JibCraneCount)
                            {
                                double jibX = (zoneLeft + zoneRight) / 2;
                                double jibY = (zoneTop + zoneBottom) / 2;

                                var jibCrane = new JibCraneData
                                {
                                    Id = $"jib_zone{zoneIdx + 1}",
                                    Name = $"Jib Z{zoneIdx + 1}",
                                    CenterX = jibX,
                                    CenterY = jibY,
                                    Radius = Math.Min(zoneWidth, zoneHeight) / 3,
                                    ArcStart = 0,
                                    ArcEnd = 360,
                                    SpeedSlew = 10,
                                    SpeedHoist = 0.5,
                                    Color = "#9B59B6"
                                };
                                _layout.JibCranes.Add(jibCrane);
                            }
                            break;

                        case 2: // EOT Crane
                            if (zoneIdx < config.EOTCraneCount)
                            {
                                double runwayY = (zoneTop + zoneBottom) / 2;

                                var runway = new RunwayData
                                {
                                    Id = $"runway_zone{zoneIdx + 1}",
                                    Name = $"Runway Z{zoneIdx + 1}",
                                    StartX = zoneLeft,
                                    StartY = runwayY,
                                    EndX = zoneRight,
                                    EndY = runwayY
                                };
                                _layout.Runways.Add(runway);

                                var eotCrane = new EOTCraneData
                                {
                                    Id = $"eot_zone{zoneIdx + 1}",
                                    Name = $"EOT Z{zoneIdx + 1}",
                                    RunwayId = runway.Id,
                                    ReachLeft = 15,
                                    ReachRight = 15,
                                    ZoneMin = 0.0,
                                    ZoneMax = 1.0,
                                    BridgePosition = 0.5,
                                    SpeedBridge = 1.0,
                                    SpeedTrolley = 0.5,
                                    Color = "#E67E22"
                                };
                                _layout.EOTCranes.Add(eotCrane);
                            }
                            break;
                    }
                }

                // Create AGV stations with linked waypoints
                for (int i = 0; i < agvStationPositions.Count; i++)
                {
                    var (x, y) = agvStationPositions[i];

                    string waypointId = $"agv_waypoint_{i + 1}";
                    string stationId = $"agv_station_{i + 1}";

                    // Create waypoint first
                    var waypoint = new AGVWaypointData
                    {
                        Id = waypointId,
                        Name = $"Waypoint {i + 1}",
                        X = x,
                        Y = y,
                        WaypointType = WaypointTypes.Stop
                    };
                    _layout.AGVWaypoints.Add(waypoint);

                    // Create AGV station linked to waypoint
                    var station = new AGVStationData
                    {
                        Id = stationId,
                        Name = $"AGV Station {i + 1}",
                        X = x,
                        Y = y,
                        LinkedWaypointId = waypointId,
                        Color = "#FF0000"  // Red
                    };
                    _layout.AGVStations.Add(station);
                }

                // Connect AGV stations in a network
                if (config.GenerateAGVPaths && _layout.AGVWaypoints.Count >= 2)
                {
                    var stations = _layout.AGVWaypoints.ToList();
                    for (int i = 0; i < stations.Count - 1; i++)
                    {
                        var path = new AGVPathData
                        {
                            Id = $"agv_path_{i + 1}",
                            Name = $"Path {i + 1}",
                            FromWaypointId = stations[i].Id,
                            ToWaypointId = stations[i + 1].Id,
                            Width = 1.2,
                            SpeedLimit = 1.0,
                            Direction = PathDirections.Bidirectional
                        };
                        _layout.AGVPaths.Add(path);
                    }
                    // Close the loop
                    if (stations.Count > 2)
                    {
                        var closingPath = new AGVPathData
                        {
                            Id = $"agv_path_{stations.Count}",
                            Name = $"Path {stations.Count}",
                            FromWaypointId = stations[stations.Count - 1].Id,
                            ToWaypointId = stations[0].Id,
                            Width = 1.2,
                            SpeedLimit = 1.0,
                            Direction = PathDirections.Bidirectional
                        };
                        _layout.AGVPaths.Add(closingPath);
                    }
                }

                MarkDirty();
                RefreshAll();  // Use RefreshAll to update Equipment Browser Panel

                int totalEntities = _layout.Nodes.Count + _layout.AGVWaypoints.Count +
                                  _layout.EOTCranes.Count + _layout.JibCranes.Count;

                StatusText.Text = $"Generated grid layout: {config.ZoneCount} zones, {totalEntities} entities " +
                                $"({_layout.AGVPaths.Count} AGV paths)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating grid layout: {ex.Message}", "Generation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Layout generation failed";
            }
        }

        #endregion
    }
}
