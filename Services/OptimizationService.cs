using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Optimization service that connects to genetic algorithm for layout optimization
    /// </summary>
    public class OptimizationService
    {
        public event EventHandler<OptimizationProgressEventArgs>? ProgressChanged;
        public event EventHandler<OptimizationCompletedEventArgs>? OptimizationCompleted;

        /// <summary>
        /// Run optimization using genetic algorithm
        /// </summary>
        /// <param name="layout">Current layout to optimize</param>
        /// <param name="options">Optimization parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<OptimizationResult> OptimizeAsync(
            LayoutData layout,
            OptimizationOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = new OptimizationResult
            {
                StartTime = DateTime.Now,
                OriginalLayout = layout
            };

            try
            {
                // Extract optimization parameters from layout
                var parameters = ExtractParameters(layout, options);

                // Report initial progress
                ReportProgress(0, "Initializing genetic algorithm...");

                // TODO: Connect to user-provided genetic algorithm
                // This is where the GA will be called
                // For now, placeholder implementation
                await Task.Run(async () =>
                {
                    for (int generation = 1; generation <= options.MaxGenerations; generation++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // Simulate generation processing (reduced delay for tests)
                        await Task.Delay(10, cancellationToken);

                        var progress = (double)generation / options.MaxGenerations * 100;
                        ReportProgress(progress, $"Generation {generation}/{options.MaxGenerations}");
                    }
                }, cancellationToken);

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.Message = "Optimization placeholder complete. Connect GA for actual optimization.";
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "Optimization cancelled by user";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Optimization failed: {ex.Message}";
            }

            OptimizationCompleted?.Invoke(this, new OptimizationCompletedEventArgs(result));
            return result;
        }

        /// <summary>
        /// Extract optimization parameters from layout for GA
        /// </summary>
        private Dictionary<string, object> ExtractParameters(LayoutData layout, OptimizationOptions options)
        {
            var parameters = new Dictionary<string, object>
            {
                // Entity positions (decision variables)
                { "entities", ExtractEntityPositions(layout) },

                // Constraints
                { "canvasWidth", layout.Canvas.Width },
                { "canvasHeight", layout.Canvas.Height },
                { "zones", ExtractZoneBoundaries(layout) },

                // Optimization objective
                { "objective", options.Objective },
                { "constraints", options.Constraints }
            };

            return parameters;
        }

        private List<EntityPosition> ExtractEntityPositions(LayoutData layout)
        {
            var positions = new List<EntityPosition>();

            // Nodes
            foreach (var node in layout.Nodes)
            {
                positions.Add(new EntityPosition
                {
                    Id = node.Id,
                    Type = "node",
                    X = node.Visual.X,
                    Y = node.Visual.Y,
                    Width = node.Visual.Width,
                    Height = node.Visual.Height,
                    IsMovable = true
                });
            }

            // AGV Stations
            foreach (var station in layout.AGVStations)
            {
                positions.Add(new EntityPosition
                {
                    Id = station.Id,
                    Type = "agv_station",
                    X = station.X,
                    Y = station.Y,
                    Width = 30,
                    Height = 30,
                    IsMovable = true
                });
            }

            // AGV Waypoints
            foreach (var waypoint in layout.AGVWaypoints)
            {
                positions.Add(new EntityPosition
                {
                    Id = waypoint.Id,
                    Type = "agv_waypoint",
                    X = waypoint.X,
                    Y = waypoint.Y,
                    Width = 10,
                    Height = 10,
                    IsMovable = true
                });
            }

            // EOT Cranes (position on runway - use BridgePosition)
            foreach (var crane in layout.EOTCranes)
            {
                // EOT crane position is determined by BridgePosition along its runway
                // For optimization, we track BridgePosition (0-1) along the runway
                positions.Add(new EntityPosition
                {
                    Id = crane.Id,
                    Type = "eot_crane",
                    X = crane.BridgePosition, // Store bridge position as X (normalized 0-1)
                    Y = 0, // Not applicable for runway-constrained cranes
                    Width = crane.BayWidth,
                    Height = crane.TotalReach,
                    IsMovable = false // Constrained to runway
                });
            }

            // Jib Cranes (use CenterX, CenterY)
            foreach (var crane in layout.JibCranes)
            {
                positions.Add(new EntityPosition
                {
                    Id = crane.Id,
                    Type = "jib_crane",
                    X = crane.CenterX,
                    Y = crane.CenterY,
                    Width = crane.Radius * 2,
                    Height = crane.Radius * 2,
                    IsMovable = true
                });
            }

            return positions;
        }

        private List<ZoneBoundary> ExtractZoneBoundaries(LayoutData layout)
        {
            var zones = new List<ZoneBoundary>();

            foreach (var zone in layout.Zones)
            {
                var boundary = new ZoneBoundary
                {
                    Id = zone.Id,
                    Name = zone.Name,
                    Type = zone.Type,
                    Points = new List<PointData>()
                };

                if (zone.Points != null)
                {
                    foreach (var pt in zone.Points)
                    {
                        boundary.Points.Add(new PointData(pt.X, pt.Y));
                    }
                }

                zones.Add(boundary);
            }

            return zones;
        }

        private void ReportProgress(double percentage, string message)
        {
            ProgressChanged?.Invoke(this, new OptimizationProgressEventArgs(percentage, message));
        }

        /// <summary>
        /// Apply optimized positions back to layout
        /// </summary>
        public void ApplyOptimizedPositions(LayoutData layout, List<EntityPosition> optimizedPositions)
        {
            foreach (var pos in optimizedPositions)
            {
                switch (pos.Type)
                {
                    case "node":
                        var node = layout.Nodes.FirstOrDefault(n => n.Id == pos.Id);
                        if (node != null)
                        {
                            node.Visual.X = pos.X;
                            node.Visual.Y = pos.Y;
                        }
                        break;

                    case "agv_station":
                        var station = layout.AGVStations.FirstOrDefault(s => s.Id == pos.Id);
                        if (station != null)
                        {
                            station.X = pos.X;
                            station.Y = pos.Y;
                        }
                        break;

                    case "agv_waypoint":
                        var waypoint = layout.AGVWaypoints.FirstOrDefault(w => w.Id == pos.Id);
                        if (waypoint != null)
                        {
                            waypoint.X = pos.X;
                            waypoint.Y = pos.Y;
                        }
                        break;

                    case "jib_crane":
                        var jib = layout.JibCranes.FirstOrDefault(j => j.Id == pos.Id);
                        if (jib != null)
                        {
                            jib.CenterX = pos.X;
                            jib.CenterY = pos.Y;
                        }
                        break;
                }
            }
        }
    }

    #region Data Classes

    public class OptimizationOptions
    {
        public string Objective { get; set; } = "minimize_travel";
        public int MaxGenerations { get; set; } = 100;
        public int PopulationSize { get; set; } = 50;
        public double MutationRate { get; set; } = 0.1;
        public double CrossoverRate { get; set; } = 0.8;
        public List<string> Constraints { get; set; } = new();
        public bool RespectZones { get; set; } = true;
        public bool MaintainConnectivity { get; set; } = true;
    }

    public class OptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public LayoutData OriginalLayout { get; set; } = null!;
        public LayoutData? OptimizedLayout { get; set; }
        public double ImprovementPercentage { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();
    }

    public class EntityPosition
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMovable { get; set; } = true;
    }

    public class ZoneBoundary
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public List<PointData> Points { get; set; } = new();
    }

    public class OptimizationProgressEventArgs : EventArgs
    {
        public double ProgressPercentage { get; }
        public string Message { get; }

        public OptimizationProgressEventArgs(double progress, string message)
        {
            ProgressPercentage = progress;
            Message = message;
        }
    }

    public class OptimizationCompletedEventArgs : EventArgs
    {
        public OptimizationResult Result { get; }

        public OptimizationCompletedEventArgs(OptimizationResult result)
        {
            Result = result;
        }
    }

    #endregion
}
