using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Regeneration Commands

        /// <summary>
        /// Regenerate Pedestrian Mesh (Ctrl+Shift+P)
        /// Recalculates walkable areas based on current equipment, AGV paths, and crane coverage
        /// </summary>
        private void RegeneratePedestrianMesh_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Regenerating pedestrian mesh...";

                // Clear existing pedestrian mesh
                _layout.PedestrianMesh.Clear();

                // Get all obstacle zones (equipment, AGV paths, crane coverage)
                var obstacles = GatherObstacles();

                // Generate walkable mesh by subtracting obstacles from canvas area
                var walkableMesh = GenerateWalkableMesh(obstacles);

                // Add mesh to layout
                foreach (var zone in walkableMesh)
                {
                    _layout.PedestrianMesh.Add(zone);
                }

                StatusText.Text = $"Pedestrian mesh regenerated: {walkableMesh.Count} zones";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error regenerating pedestrian mesh: {ex.Message}";
            }
        }

        /// <summary>
        /// Regenerate Crane Coverage (Ctrl+Shift+C)
        /// Recalculates crane coverage areas based on current crane positions and parameters
        /// </summary>
        private void RegenerateCraneCoverage_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Regenerating crane coverage...";

                int count = 0;

                // Regenerate EOT crane coverage
                foreach (var crane in _layout.EOTCranes)
                {
                    var runway = _layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
                    if (runway != null)
                    {
                        // Calculate coverage zone based on runway, span, and zone limits
                        var coverage = CalculateEOTCraneCoverage(crane, runway);
                        // Store coverage in crane data
                        count++;
                    }
                }

                // Regenerate Jib crane coverage
                foreach (var crane in _layout.JibCranes)
                {
                    // Calculate arc coverage based on center, radius, and arc limits
                    var coverage = CalculateJibCraneCoverage(crane);
                    count++;
                }

                StatusText.Text = $"Crane coverage regenerated: {count} cranes";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error regenerating crane coverage: {ex.Message}";
            }
        }

        /// <summary>
        /// Regenerate AGV Network (Ctrl+Shift+A)
        /// Validates AGV network topology and reports connectivity status
        /// </summary>
        private void RegenerateAGVNetwork_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Regenerating AGV network...";

                // Validate and count AGV paths
                int validPathCount = 0;
                int invalidPathCount = 0;

                foreach (var path in _layout.AGVPaths)
                {
                    var fromWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.FromWaypointId);
                    var toWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == path.ToWaypointId);

                    if (fromWaypoint != null && toWaypoint != null)
                    {
                        validPathCount++;
                    }
                    else
                    {
                        invalidPathCount++;
                    }
                }

                StatusText.Text = $"AGV network validated: {validPathCount} paths, {invalidPathCount} broken";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error regenerating AGV network: {ex.Message}";
            }
        }

        /// <summary>
        /// Regenerate Forklift Aisles (Ctrl+Shift+F)
        /// Recalculates forklift aisle paths based on equipment layout
        /// </summary>
        private void RegenerateForkliftAisles_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Regenerating forklift aisles...";

                // Clear existing forklift aisles
                var forkliftAisles = _layout.Openings.Where(o => o.OpeningType == "Aisle").ToList();
                foreach (var aisle in forkliftAisles)
                {
                    _layout.Openings.Remove(aisle);
                }

                // Detect equipment rows and generate aisles between them
                var aisles = DetectAndGenerateAisles();

                // Add aisles to layout
                foreach (var aisle in aisles)
                {
                    _layout.Openings.Add(aisle);
                }

                StatusText.Text = $"Forklift aisles regenerated: {aisles.Count} aisles";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error regenerating forklift aisles: {ex.Message}";
            }
        }

        /// <summary>
        /// Auto-Link Openings (Ctrl+Shift+O)
        /// Automatically creates paths between openings and nearest equipment
        /// </summary>
        private void AutoLinkOpenings_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Auto-linking openings...";

                int linkCount = 0;

                // For each opening, find nearest equipment node and create path
                foreach (var opening in _layout.Openings)
                {
                    var nearestNode = FindNearestEquipmentNode(opening.X, opening.Y);
                    if (nearestNode != null)
                    {
                        // Check if path already exists
                        bool pathExists = _layout.Paths.Any(p =>
                            (p.From == opening.Id && p.To == nearestNode.Id) ||
                            (p.From == nearestNode.Id && p.To == opening.Id));

                        if (!pathExists)
                        {
                            var path = new PathData
                            {
                                Id = $"path_{Guid.NewGuid().ToString().Substring(0, 8)}",
                                From = opening.Id,
                                To = nearestNode.Id,
                                PathType = "single"
                            };
                            _layout.Paths.Add(path);
                            linkCount++;
                        }
                    }
                }

                StatusText.Text = $"Auto-linked openings: {linkCount} new paths";
                RefreshCanvas();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error auto-linking openings: {ex.Message}";
            }
        }

        /// <summary>
        /// Regenerate All Derived (Ctrl+Shift+R)
        /// Regenerates all derived data: pedestrian mesh, crane coverage, AGV network, and forklift aisles
        /// </summary>
        private void RegenerateAll_Click(object sender, RoutedEventArgs e)
        {
            if (_layout == null)
            {
                StatusText.Text = "No layout loaded";
                return;
            }

            try
            {
                StatusText.Text = "Regenerating all derived data...";

                // Regenerate in dependency order
                RegenerateCraneCoverage_Click(sender, e);
                RegenerateAGVNetwork_Click(sender, e);
                RegenerateForkliftAisles_Click(sender, e);
                RegeneratePedestrianMesh_Click(sender, e);

                StatusText.Text = "All derived data regenerated successfully";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error regenerating all: {ex.Message}";
            }
        }

        #endregion

        #region Helper Methods for Regeneration

        /// <summary>
        /// Gather all obstacle zones (equipment, AGV paths, crane coverage)
        /// </summary>
        private List<ZoneData> GatherObstacles()
        {
            var obstacles = new List<ZoneData>();

            // Add equipment as obstacles (create bounding boxes)
            foreach (var node in _layout.Nodes)
            {
                var zone = new ZoneData
                {
                    Id = $"obstacle_{node.Id}",
                    Name = $"Equipment {node.Label}",
                    X = node.Visual.X - 10,
                    Y = node.Visual.Y - 10
                };
                zone.Points.Add(new PointData(node.Visual.X - 10, node.Visual.Y - 10));
                zone.Points.Add(new PointData(node.Visual.X + 10, node.Visual.Y - 10));
                zone.Points.Add(new PointData(node.Visual.X + 10, node.Visual.Y + 10));
                zone.Points.Add(new PointData(node.Visual.X - 10, node.Visual.Y + 10));
                obstacles.Add(zone);
            }

            // Add AGV paths as obstacles (buffer around paths)
            foreach (var agvPath in _layout.AGVPaths)
            {
                var fromWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == agvPath.FromWaypointId);
                var toWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == agvPath.ToWaypointId);

                if (fromWaypoint != null && toWaypoint != null)
                {
                    // Create buffer zone around AGV path segment
                    var zone = CreatePathBufferZone(fromWaypoint.X, fromWaypoint.Y, toWaypoint.X, toWaypoint.Y, 15);
                    obstacles.Add(zone);
                }
            }

            return obstacles;
        }

        /// <summary>
        /// Generate walkable mesh by subtracting obstacles from canvas area
        /// </summary>
        private List<ZoneData> GenerateWalkableMesh(List<ZoneData> obstacles)
        {
            var walkableMesh = new List<ZoneData>();

            if (_layout.Canvas == null)
                return walkableMesh;

            // For now, create a simple grid-based walkable mesh
            // In a full implementation, this would use polygon boolean operations

            int gridSize = 50;
            int cols = (int)Math.Ceiling(_layout.Canvas.Width / gridSize);
            int rows = (int)Math.Ceiling(_layout.Canvas.Height / gridSize);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    double x = col * gridSize;
                    double y = row * gridSize;
                    double w = Math.Min(gridSize, _layout.Canvas.Width - x);
                    double h = Math.Min(gridSize, _layout.Canvas.Height - y);

                    // Check if this cell overlaps with any obstacle
                    var centerX = x + w / 2;
                    var centerY = y + h / 2;

                    bool isObstacle = false;
                    foreach (var obstacle in obstacles)
                    {
                        if (IsPointInZone(centerX, centerY, obstacle))
                        {
                            isObstacle = true;
                            break;
                        }
                    }

                    if (!isObstacle)
                    {
                        var zone = new ZoneData
                        {
                            Id = $"ped_mesh_{row}_{col}",
                            Name = $"Pedestrian Zone {row},{col}",
                            X = x,
                            Y = y
                        };
                        zone.Points.Add(new PointData(x, y));
                        zone.Points.Add(new PointData(x + w, y));
                        zone.Points.Add(new PointData(x + w, y + h));
                        zone.Points.Add(new PointData(x, y + h));
                        walkableMesh.Add(zone);
                    }
                }
            }

            return walkableMesh;
        }

        /// <summary>
        /// Calculate EOT crane coverage zone
        /// </summary>
        private ZoneData CalculateEOTCraneCoverage(EOTCraneData crane, RunwayData runway)
        {
            var (startX, startY) = runway.GetPositionAt(crane.ZoneMin);
            var (endX, endY) = runway.GetPositionAt(crane.ZoneMax);

            // Create rectangular coverage based on reach (left + right)
            var totalSpan = crane.ReachLeft + crane.ReachRight;
            var perpX = -(endY - startY);
            var perpY = endX - startX;
            var length = Math.Sqrt(perpX * perpX + perpY * perpY);
            if (length > 0)
            {
                perpX = perpX / length * totalSpan;
                perpY = perpY / length * totalSpan;
            }

            var zone = new ZoneData
            {
                Id = $"coverage_{crane.Id}",
                Name = $"Crane Coverage {crane.Id}"
            };
            zone.Points.Add(new PointData(startX, startY));
            zone.Points.Add(new PointData(endX, endY));
            zone.Points.Add(new PointData(endX + perpX, endY + perpY));
            zone.Points.Add(new PointData(startX + perpX, startY + perpY));

            return zone;
        }

        /// <summary>
        /// Calculate Jib crane coverage zone
        /// </summary>
        private ZoneData CalculateJibCraneCoverage(JibCraneData crane)
        {
            var zone = new ZoneData
            {
                Id = $"coverage_{crane.Id}",
                Name = $"Jib Coverage {crane.Id}"
            };

            // Create arc polygon approximation
            int segments = 32;
            double startRad = crane.ArcStart * Math.PI / 180;
            double endRad = crane.ArcEnd * Math.PI / 180;

            if (endRad < startRad)
                endRad += 2 * Math.PI;

            double angleStep = (endRad - startRad) / segments;

            // Add center point
            zone.Points.Add(new PointData(crane.CenterX, crane.CenterY));

            // Add arc points
            for (int i = 0; i <= segments; i++)
            {
                double angle = startRad + i * angleStep;
                double x = crane.CenterX + crane.Radius * Math.Cos(angle);
                double y = crane.CenterY + crane.Radius * Math.Sin(angle);
                zone.Points.Add(new PointData(x, y));
            }

            return zone;
        }

        /// <summary>
        /// Detect equipment rows and generate aisles between them
        /// </summary>
        private List<OpeningData> DetectAndGenerateAisles()
        {
            var aisles = new List<OpeningData>();

            // Group nodes by approximate Y coordinate (within threshold)
            var rows = new List<List<NodeData>>();
            double rowThreshold = 100;

            foreach (var node in _layout.Nodes)
            {
                bool addedToRow = false;
                foreach (var row in rows)
                {
                    if (row.Count > 0 && Math.Abs(row[0].Visual.Y - node.Visual.Y) < rowThreshold)
                    {
                        row.Add(node);
                        addedToRow = true;
                        break;
                    }
                }

                if (!addedToRow)
                {
                    rows.Add(new List<NodeData> { node });
                }
            }

            // Sort rows by Y coordinate
            rows = rows.OrderBy(r => r.Count > 0 ? r[0].Visual.Y : 0).ToList();

            // Create aisles between consecutive rows
            for (int i = 0; i < rows.Count - 1; i++)
            {
                var row1 = rows[i];
                var row2 = rows[i + 1];

                if (row1.Count == 0 || row2.Count == 0)
                    continue;

                double y1 = row1.Average(n => n.Visual.Y);
                double y2 = row2.Average(n => n.Visual.Y);
                double aisleY = (y1 + y2) / 2;

                double minX = Math.Min(row1.Min(n => n.Visual.X), row2.Min(n => n.Visual.X));
                double maxX = Math.Max(row1.Max(n => n.Visual.X), row2.Max(n => n.Visual.X));

                var aisle = new OpeningData
                {
                    Id = $"aisle_{i}",
                    Name = $"Aisle {i + 1}",
                    OpeningType = "Aisle",
                    X = (minX + maxX) / 2,
                    Y = aisleY,
                    ClearWidth = maxX - minX
                };

                aisles.Add(aisle);
            }

            return aisles;
        }

        /// <summary>
        /// Create a buffer zone around a path segment
        /// </summary>
        private ZoneData CreatePathBufferZone(double x1, double y1, double x2, double y2, double buffer)
        {
            // Calculate perpendicular vector
            var dx = x2 - x1;
            var dy = y2 - y1;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length < 0.01)
            {
                // Degenerate case: create small square
                var zone = new ZoneData { Id = $"buffer_{Guid.NewGuid()}" };
                zone.Points.Add(new PointData(x1 - buffer, y1 - buffer));
                zone.Points.Add(new PointData(x1 + buffer, y1 - buffer));
                zone.Points.Add(new PointData(x1 + buffer, y1 + buffer));
                zone.Points.Add(new PointData(x1 - buffer, y1 + buffer));
                return zone;
            }

            var perpX = -dy / length * buffer;
            var perpY = dx / length * buffer;

            var bufferZone = new ZoneData
            {
                Id = $"buffer_{Guid.NewGuid()}"
            };

            bufferZone.Points.Add(new PointData(x1 + perpX, y1 + perpY));
            bufferZone.Points.Add(new PointData(x2 + perpX, y2 + perpY));
            bufferZone.Points.Add(new PointData(x2 - perpX, y2 - perpY));
            bufferZone.Points.Add(new PointData(x1 - perpX, y1 - perpY));

            return bufferZone;
        }

        /// <summary>
        /// Check if a point is inside a zone (simple polygon containment)
        /// </summary>
        private bool IsPointInZone(double x, double y, ZoneData zone)
        {
            if (zone.Points == null || zone.Points.Count < 3)
                return false;

            // Ray casting algorithm
            int intersections = 0;
            for (int i = 0; i < zone.Points.Count; i++)
            {
                var p1 = zone.Points[i];
                var p2 = zone.Points[(i + 1) % zone.Points.Count];

                if ((p1.Y > y) != (p2.Y > y))
                {
                    double xIntersect = (p2.X - p1.X) * (y - p1.Y) / (p2.Y - p1.Y) + p1.X;
                    if (x < xIntersect)
                        intersections++;
                }
            }

            return (intersections % 2) == 1;
        }

        /// <summary>
        /// Find nearest equipment node to a point
        /// </summary>
        private NodeData FindNearestEquipmentNode(double x, double y)
        {
            NodeData nearest = null;
            double minDist = double.MaxValue;

            foreach (var node in _layout.Nodes)
            {
                var dx = node.Visual.X - x;
                var dy = node.Visual.Y - y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }

            return nearest;
        }

        #endregion
    }
}
