using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
using LayoutEditor.Models;

// Resolve ambiguity between ACadSharp.Entities.Point and System.Windows.Point
using WpfPoint = System.Windows.Point;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Imports DXF and DWG files using ACadSharp library.
    /// Converts CAD entities to layout walls and nodes.
    /// </summary>
    public class CadImportService
    {
        public class CadImportOptions
        {
            /// <summary>Scale factor to convert CAD units to pixels</summary>
            public double Scale { get; set; } = 1.0;
            
            /// <summary>Offset to apply to all coordinates</summary>
            public WpfPoint Offset { get; set; } = new WpfPoint(100, 100);
            
            /// <summary>Import lines as walls</summary>
            public bool ImportLinesAsWalls { get; set; } = true;
            
            /// <summary>Import polylines as walls</summary>
            public bool ImportPolylinesAsWalls { get; set; } = true;
            
            /// <summary>Import arcs as walls (approximated)</summary>
            public bool ImportArcsAsWalls { get; set; } = true;
            
            /// <summary>Import circles as node markers</summary>
            public bool ImportCirclesAsNodes { get; set; } = false;
            
            /// <summary>Import text as labels</summary>
            public bool ImportText { get; set; } = true;
            
            /// <summary>Minimum line length to import (filters out tiny lines)</summary>
            public double MinLineLength { get; set; } = 5.0;
            
            /// <summary>Layers to import (empty = all layers)</summary>
            public List<string> LayersToImport { get; set; } = new();
            
            /// <summary>Flip Y axis (CAD uses bottom-left origin, WPF uses top-left)</summary>
            public bool FlipY { get; set; } = true;
            
            /// <summary>Wall thickness for imported lines</summary>
            public double WallThickness { get; set; } = 4.0;
            
            /// <summary>Wall type for imported lines</summary>
            public string WallType { get; set; } = "exterior";
            
            /// <summary>Number of segments to use when converting arcs</summary>
            public int ArcSegments { get; set; } = 16;
            
            /// <summary>Store CAD layer name on walls</summary>
            public bool StoreLayers { get; set; } = true;
            
            /// <summary>Auto-fit to canvas dimensions</summary>
            public bool FitToCanvas { get; set; } = false;
            
            /// <summary>Canvas width for fit-to-canvas</summary>
            public double CanvasWidth { get; set; } = 800;
            
            /// <summary>Canvas height for fit-to-canvas</summary>
            public double CanvasHeight { get; set; } = 600;
            
            /// <summary>Margin for fit-to-canvas (pixels from edge)</summary>
            public double FitMargin { get; set; } = 50;
        }

        public class CadImportResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public int WallsImported { get; set; }
            public int NodesImported { get; set; }
            public int TextImported { get; set; }
            public int EntitiesSkipped { get; set; }
            public List<string> Layers { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public Rect Bounds { get; set; }
        }

        /// <summary>
        /// Get list of layers in a CAD file
        /// </summary>
        public List<string> GetLayers(string filePath)
        {
            var layers = new List<string>();
            
            try
            {
                CadDocument doc = ReadCadFile(filePath);
                if (doc != null)
                {
                    foreach (var layer in doc.Layers)
                    {
                        layers.Add(layer.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading layers: {ex.Message}");
            }
            
            return layers.OrderBy(l => l).ToList();
        }

        /// <summary>
        /// Get bounding box of entities in CAD file (for fit-to-canvas calculation)
        /// </summary>
        public Rect GetBounds(string filePath, List<string>? layerFilter = null)
        {
            try
            {
                CadDocument doc = ReadCadFile(filePath);
                if (doc?.ModelSpace == null) return Rect.Empty;

                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;

                foreach (var entity in doc.ModelSpace.Entities)
                {
                    // Apply layer filter
                    if (layerFilter != null && layerFilter.Count > 0)
                    {
                        var layerName = entity.Layer?.Name ?? "0";
                        if (!layerFilter.Contains(layerName, StringComparer.OrdinalIgnoreCase))
                            continue;
                    }

                    UpdateBounds(entity, ref minX, ref maxX, ref minY, ref maxY);
                }

                if (minX == double.MaxValue) return Rect.Empty;
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            catch
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// Calculate optimal scale to fit CAD content in canvas
        /// </summary>
        public double CalculateFitScale(Rect cadBounds, double canvasWidth, double canvasHeight, double margin)
        {
            if (cadBounds.IsEmpty || cadBounds.Width <= 0 || cadBounds.Height <= 0)
                return 1.0;

            double availableWidth = canvasWidth - 2 * margin;
            double availableHeight = canvasHeight - 2 * margin;

            if (availableWidth <= 0 || availableHeight <= 0)
                return 1.0;

            double scaleX = availableWidth / cadBounds.Width;
            double scaleY = availableHeight / cadBounds.Height;

            return Math.Min(scaleX, scaleY);
        }

        /// <summary>
        /// Import a CAD file (DXF or DWG) into existing layout data
        /// </summary>
        public CadImportResult Import(string filePath, LayoutData layout, CadImportOptions? options = null)
        {
            options ??= new CadImportOptions();
            var result = new CadImportResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "File not found";
                    return result;
                }

                CadDocument doc = ReadCadFile(filePath);
                if (doc == null)
                {
                    result.ErrorMessage = "Failed to read CAD file";
                    return result;
                }

                // Get layers
                result.Layers = doc.Layers.Select(l => l.Name).OrderBy(l => l).ToList();

                // Get model space entities
                var modelSpace = doc.ModelSpace;
                if (modelSpace == null)
                {
                    result.ErrorMessage = "No model space found in file";
                    return result;
                }

                // Calculate bounds for Y-flip
                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;
                
                foreach (var entity in modelSpace.Entities)
                {
                    // Filter by layer for bounds calculation
                    if (options.LayersToImport.Count > 0)
                    {
                        var layerName = entity.Layer?.Name ?? "0";
                        if (!options.LayersToImport.Contains(layerName, StringComparer.OrdinalIgnoreCase))
                            continue;
                    }
                    UpdateBounds(entity, ref minX, ref maxX, ref minY, ref maxY);
                }
                
                if (minX != double.MaxValue)
                {
                    result.Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
                }

                // Handle fit-to-canvas
                if (options.FitToCanvas && !result.Bounds.IsEmpty)
                {
                    options.Scale = CalculateFitScale(
                        result.Bounds, 
                        options.CanvasWidth, 
                        options.CanvasHeight, 
                        options.FitMargin);
                    options.Offset = new WpfPoint(options.FitMargin, options.FitMargin);
                }

                // Process entities
                foreach (var entity in modelSpace.Entities)
                {
                    // Filter by layer if specified
                    if (options.LayersToImport.Count > 0)
                    {
                        var layerName = entity.Layer?.Name ?? "0";
                        if (!options.LayersToImport.Contains(layerName, StringComparer.OrdinalIgnoreCase))
                        {
                            result.EntitiesSkipped++;
                            continue;
                        }
                    }

                    ProcessEntity(entity, layout, options, maxY, result);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.Warnings.Add($"Exception: {ex.GetType().Name}");
            }

            return result;
        }

        private CadDocument ReadCadFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            
            if (ext == ".dwg")
            {
                using var reader = new DwgReader(filePath);
                return reader.Read();
            }
            else // .dxf or other
            {
                using var reader = new DxfReader(filePath);
                return reader.Read();
            }
        }

        private void UpdateBounds(Entity entity, ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            switch (entity)
            {
                case Arc arc:
                    UpdatePointBounds(arc.Center.X - arc.Radius, arc.Center.Y - arc.Radius, ref minX, ref maxX, ref minY, ref maxY);
                    UpdatePointBounds(arc.Center.X + arc.Radius, arc.Center.Y + arc.Radius, ref minX, ref maxX, ref minY, ref maxY);
                    break;
                    
                case Circle circle:
                    UpdatePointBounds(circle.Center.X - circle.Radius, circle.Center.Y - circle.Radius, ref minX, ref maxX, ref minY, ref maxY);
                    UpdatePointBounds(circle.Center.X + circle.Radius, circle.Center.Y + circle.Radius, ref minX, ref maxX, ref minY, ref maxY);
                    break;
                    
                case Line line:
                    UpdatePointBounds(line.StartPoint.X, line.StartPoint.Y, ref minX, ref maxX, ref minY, ref maxY);
                    UpdatePointBounds(line.EndPoint.X, line.EndPoint.Y, ref minX, ref maxX, ref minY, ref maxY);
                    break;
                    
                case LwPolyline lwp:
                    foreach (var v in lwp.Vertices)
                        UpdatePointBounds(v.Location.X, v.Location.Y, ref minX, ref maxX, ref minY, ref maxY);
                    break;
                    
                case Polyline2D p2d:
                    foreach (var v in p2d.Vertices)
                        UpdatePointBounds(v.Location.X, v.Location.Y, ref minX, ref maxX, ref minY, ref maxY);
                    break;
            }
        }

        private void UpdatePointBounds(double x, double y, ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        private void ProcessEntity(Entity entity, LayoutData layout, CadImportOptions options, double maxY, CadImportResult result)
        {
            var layerName = entity.Layer?.Name ?? "0";
            
            switch (entity)
            {
                case Line line:
                    if (options.ImportLinesAsWalls)
                    {
                        var wall = ImportLine(line, options, maxY);
                        if (wall != null && GetWallLength(wall) >= options.MinLineLength)
                        {
                            if (options.StoreLayers) wall.Layer = layerName;
                            layout.Walls.Add(wall);
                            result.WallsImported++;
                        }
                    }
                    break;

                case LwPolyline lwPolyline:
                    if (options.ImportPolylinesAsWalls)
                    {
                        var walls = ImportLwPolyline(lwPolyline, options, maxY, layerName);
                        foreach (var wall in walls)
                        {
                            if (GetWallLength(wall) >= options.MinLineLength)
                            {
                                layout.Walls.Add(wall);
                                result.WallsImported++;
                            }
                        }
                    }
                    break;

                case Polyline2D polyline2D:
                    if (options.ImportPolylinesAsWalls)
                    {
                        var walls = ImportPolyline2D(polyline2D, options, maxY, layerName);
                        foreach (var wall in walls)
                        {
                            if (GetWallLength(wall) >= options.MinLineLength)
                            {
                                layout.Walls.Add(wall);
                                result.WallsImported++;
                            }
                        }
                    }
                    break;

                case Arc arc:
                    if (options.ImportArcsAsWalls)
                    {
                        var walls = ImportArc(arc, options, maxY, layerName);
                        foreach (var wall in walls)
                        {
                            layout.Walls.Add(wall);
                            result.WallsImported++;
                        }
                    }
                    break;

                case Circle circle:
                    if (options.ImportCirclesAsNodes)
                    {
                        var node = ImportCircle(circle, options, maxY);
                        if (node != null)
                        {
                            layout.Nodes.Add(node);
                            result.NodesImported++;
                        }
                    }
                    break;

                case TextEntity text:
                    if (options.ImportText)
                        result.TextImported++;
                    break;

                case MText mtext:
                    if (options.ImportText)
                        result.TextImported++;
                    break;

                default:
                    result.EntitiesSkipped++;
                    break;
            }
        }

        private WallData ImportLine(Line line, CadImportOptions options, double maxY)
        {
            var start = TransformPoint(line.StartPoint.X, line.StartPoint.Y, options, maxY);
            var end = TransformPoint(line.EndPoint.X, line.EndPoint.Y, options, maxY);

            return new WallData
            {
                Id = Guid.NewGuid().ToString(),
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                WallType = options.WallType,
                Thickness = options.WallThickness
            };
        }

        private List<WallData> ImportLwPolyline(LwPolyline polyline, CadImportOptions options, double maxY, string layer)
        {
            var walls = new List<WallData>();
            var vertices = polyline.Vertices.ToList();

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                var start = TransformPoint(vertices[i].Location.X, vertices[i].Location.Y, options, maxY);
                var end = TransformPoint(vertices[i + 1].Location.X, vertices[i + 1].Location.Y, options, maxY);

                walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = start.X, Y1 = start.Y,
                    X2 = end.X, Y2 = end.Y,
                    WallType = options.WallType,
                    Thickness = options.WallThickness,
                    Layer = options.StoreLayers ? layer : ""
                });
            }

            // Close polyline if needed
            if (polyline.IsClosed && vertices.Count > 2)
            {
                var start = TransformPoint(vertices[^1].Location.X, vertices[^1].Location.Y, options, maxY);
                var end = TransformPoint(vertices[0].Location.X, vertices[0].Location.Y, options, maxY);

                walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = start.X, Y1 = start.Y,
                    X2 = end.X, Y2 = end.Y,
                    WallType = options.WallType,
                    Thickness = options.WallThickness,
                    Layer = options.StoreLayers ? layer : ""
                });
            }

            return walls;
        }

        private List<WallData> ImportPolyline2D(Polyline2D polyline, CadImportOptions options, double maxY, string layer)
        {
            var walls = new List<WallData>();
            var vertices = polyline.Vertices.ToList();

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                var start = TransformPoint(vertices[i].Location.X, vertices[i].Location.Y, options, maxY);
                var end = TransformPoint(vertices[i + 1].Location.X, vertices[i + 1].Location.Y, options, maxY);

                walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = start.X, Y1 = start.Y,
                    X2 = end.X, Y2 = end.Y,
                    WallType = options.WallType,
                    Thickness = options.WallThickness,
                    Layer = options.StoreLayers ? layer : ""
                });
            }

            // Close polyline if needed
            if (polyline.IsClosed && vertices.Count > 2)
            {
                var start = TransformPoint(vertices[^1].Location.X, vertices[^1].Location.Y, options, maxY);
                var end = TransformPoint(vertices[0].Location.X, vertices[0].Location.Y, options, maxY);

                walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = start.X, Y1 = start.Y,
                    X2 = end.X, Y2 = end.Y,
                    WallType = options.WallType,
                    Thickness = options.WallThickness,
                    Layer = options.StoreLayers ? layer : ""
                });
            }

            return walls;
        }

        private List<WallData> ImportArc(Arc arc, CadImportOptions options, double maxY, string layer)
        {
            var walls = new List<WallData>();
            
            double startAngle = arc.StartAngle * Math.PI / 180.0;
            double endAngle = arc.EndAngle * Math.PI / 180.0;
            
            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;
            
            double angleStep = (endAngle - startAngle) / options.ArcSegments;
            
            for (int i = 0; i < options.ArcSegments; i++)
            {
                double a1 = startAngle + i * angleStep;
                double a2 = startAngle + (i + 1) * angleStep;
                
                double x1 = arc.Center.X + arc.Radius * Math.Cos(a1);
                double y1 = arc.Center.Y + arc.Radius * Math.Sin(a1);
                double x2 = arc.Center.X + arc.Radius * Math.Cos(a2);
                double y2 = arc.Center.Y + arc.Radius * Math.Sin(a2);
                
                var start = TransformPoint(x1, y1, options, maxY);
                var end = TransformPoint(x2, y2, options, maxY);

                walls.Add(new WallData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = start.X, Y1 = start.Y,
                    X2 = end.X, Y2 = end.Y,
                    WallType = options.WallType,
                    Thickness = options.WallThickness,
                    Layer = options.StoreLayers ? layer : ""
                });
            }

            return walls;
        }

        private NodeData? ImportCircle(Circle circle, CadImportOptions options, double maxY)
        {
            double radius = circle.Radius * options.Scale;
            if (radius < 5) radius = 30;

            var center = TransformPoint(circle.Center.X, circle.Center.Y, options, maxY);

            return new NodeData
            {
                Id = Guid.NewGuid().ToString(),
                Type = "marker",
                Name = $"Circle_{circle.Layer?.Name ?? "0"}",
                Visual = new NodeVisual
                {
                    X = center.X - radius,
                    Y = center.Y - radius,
                    Width = radius * 2,
                    Height = radius * 2
                }
            };
        }

        private WpfPoint TransformPoint(double x, double y, CadImportOptions options, double maxY)
        {
            double tx = x * options.Scale + options.Offset.X;
            double ty = options.FlipY 
                ? (maxY - y) * options.Scale + options.Offset.Y 
                : y * options.Scale + options.Offset.Y;
            return new WpfPoint(tx, ty);
        }

        private double GetWallLength(WallData wall)
        {
            var dx = wall.X2 - wall.X1;
            var dy = wall.Y2 - wall.Y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
