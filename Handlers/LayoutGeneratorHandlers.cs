using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
    }
}
