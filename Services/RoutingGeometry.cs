using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Geometry calculations for routing paths between terminals.
    /// Handles node terminals, cell terminals, wall passages, and waypoint generation.
    /// </summary>
    public static class RoutingGeometry
    {
        #region Terminal Positions

        /// <summary>Get input terminal position for a node (where paths connect TO)</summary>
        public static Point GetNodeInputTerminal(NodeData node)
        {
            var centerY = node.Visual.Height / 2.0;
            return node.Visual.InputTerminalPosition?.ToLower() switch
            {
                "right" => new Point(node.Visual.X + node.Visual.Width + RenderConstants.NodeTerminalStickOut, node.Visual.Y + centerY),
                "top" => new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y - RenderConstants.NodeTerminalStickOut),
                "bottom" => new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y + node.Visual.Height + RenderConstants.NodeTerminalStickOut),
                _ => new Point(node.Visual.X - RenderConstants.NodeTerminalStickOut, node.Visual.Y + centerY) // left default
            };
        }

        /// <summary>Get output terminal position for a node (where paths connect FROM)</summary>
        public static Point GetNodeOutputTerminal(NodeData node)
        {
            var centerY = node.Visual.Height / 2.0;
            return node.Visual.OutputTerminalPosition?.ToLower() switch
            {
                "left" => new Point(node.Visual.X - RenderConstants.NodeTerminalStickOut, node.Visual.Y + centerY),
                "top" => new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y - RenderConstants.NodeTerminalStickOut),
                "bottom" => new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y + node.Visual.Height + RenderConstants.NodeTerminalStickOut),
                _ => new Point(node.Visual.X + node.Visual.Width + RenderConstants.NodeTerminalStickOut, node.Visual.Y + centerY) // right default
            };
        }

        /// <summary>Get terminal position for a cell/group</summary>
        public static Point GetCellTerminal(Rect bounds, string position, bool isOutput)
        {
            var offset = RenderConstants.CellTerminalStickOut;
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.X + bounds.Width / 2, bounds.Y - offset),
                "bottom" => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height + offset),
                "right" => new Point(bounds.X + bounds.Width + offset, bounds.Y + bounds.Height / 2),
                _ => new Point(bounds.X - offset, bounds.Y + bounds.Height / 2) // left default
            };
        }

        /// <summary>Get both terminals for a path</summary>
        public static (Point from, Point to) GetPathEndpoints(LayoutData layout, PathData path)
        {
            var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == path.From);
            var toNode = layout.Nodes.FirstOrDefault(n => n.Id == path.To);
            
            if (fromNode == null || toNode == null)
                return (new Point(0, 0), new Point(0, 0));
            
            return (GetNodeOutputTerminal(fromNode), GetNodeInputTerminal(toNode));
        }

        #endregion

        #region Passage Detection

        /// <summary>
        /// A passage is a gap between wall endpoints where routing can pass through.
        /// </summary>
        public class Passage
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public Point Start { get; set; }
            public Point End { get; set; }
            public Point Center => new Point((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);
            public double Width => Distance(Start, End);
            public string? Wall1Id { get; set; }  // Wall ending at Start
            public string? Wall2Id { get; set; }  // Wall starting at End
            public string PassageType { get; set; } = "gap";  // gap, door, opening
        }

        /// <summary>Find all passages (gaps) between walls</summary>
        public static List<Passage> DetectPassages(LayoutData layout, double maxGapSize = 100)
        {
            var passages = new List<Passage>();
            var walls = layout.Walls;
            if (walls.Count < 2) return passages;

            // Collect all wall endpoints
            var endpoints = new List<(Point point, string wallId, bool isEnd)>();
            foreach (var wall in walls)
            {
                endpoints.Add((new Point(wall.X1, wall.Y1), wall.Id, false));
                endpoints.Add((new Point(wall.X2, wall.Y2), wall.Id, true));
            }

            // Find nearby endpoints that form passages
            for (int i = 0; i < endpoints.Count; i++)
            {
                for (int j = i + 1; j < endpoints.Count; j++)
                {
                    // Skip if same wall
                    if (endpoints[i].wallId == endpoints[j].wallId) continue;
                    
                    var dist = Distance(endpoints[i].point, endpoints[j].point);
                    
                    // Gap found - potential passage
                    if (dist > 1 && dist <= maxGapSize)
                    {
                        // Check if walls are roughly collinear (forming a doorway)
                        var wall1 = walls.First(w => w.Id == endpoints[i].wallId);
                        var wall2 = walls.First(w => w.Id == endpoints[j].wallId);
                        
                        if (AreWallsAligned(wall1, wall2, 30)) // 30 degree tolerance
                        {
                            passages.Add(new Passage
                            {
                                Start = endpoints[i].point,
                                End = endpoints[j].point,
                                Wall1Id = endpoints[i].wallId,
                                Wall2Id = endpoints[j].wallId,
                                PassageType = dist > 60 ? "opening" : "door"
                            });
                        }
                    }
                }
            }

            // Also add explicit doors from DoorData
            foreach (var door in layout.Doors)
            {
                var wall = walls.FirstOrDefault(w => w.Id == door.WallId);
                if (wall == null) continue;
                
                var doorCenter = GetPointOnWall(wall, door.Position);
                var wallAngle = Math.Atan2(wall.Y2 - wall.Y1, wall.X2 - wall.X1);
                var halfWidth = door.Width / 2;
                
                passages.Add(new Passage
                {
                    Start = new Point(doorCenter.X - Math.Cos(wallAngle) * halfWidth, 
                                      doorCenter.Y - Math.Sin(wallAngle) * halfWidth),
                    End = new Point(doorCenter.X + Math.Cos(wallAngle) * halfWidth,
                                    doorCenter.Y + Math.Sin(wallAngle) * halfWidth),
                    Wall1Id = wall.Id,
                    Wall2Id = wall.Id,
                    PassageType = "door"
                });
            }

            return passages;
        }

        /// <summary>Get point at position along wall (0-1)</summary>
        public static Point GetPointOnWall(WallData wall, double t)
        {
            return new Point(
                wall.X1 + (wall.X2 - wall.X1) * t,
                wall.Y1 + (wall.Y2 - wall.Y1) * t
            );
        }

        /// <summary>Check if two walls are roughly parallel/aligned</summary>
        private static bool AreWallsAligned(WallData w1, WallData w2, double toleranceDegrees)
        {
            var angle1 = Math.Atan2(w1.Y2 - w1.Y1, w1.X2 - w1.X1);
            var angle2 = Math.Atan2(w2.Y2 - w2.Y1, w2.X2 - w2.X1);
            var diff = Math.Abs(angle1 - angle2) * 180 / Math.PI;
            // Normalize to 0-180 range
            while (diff > 180) diff -= 180;
            return diff < toleranceDegrees || diff > (180 - toleranceDegrees);
        }

        #endregion

        #region Wall Intersection

        /// <summary>Check if a line segment intersects any wall</summary>
        public static bool IntersectsWall(Point p1, Point p2, LayoutData layout, double margin = 0)
        {
            foreach (var wall in layout.Walls)
            {
                if (SegmentsIntersect(p1, p2, 
                    new Point(wall.X1, wall.Y1), 
                    new Point(wall.X2, wall.Y2), 
                    wall.Thickness / 2 + margin))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Find all walls that intersect a line segment</summary>
        public static List<WallData> GetIntersectingWalls(Point p1, Point p2, LayoutData layout)
        {
            return layout.Walls.Where(wall => 
                SegmentsIntersect(p1, p2, 
                    new Point(wall.X1, wall.Y1), 
                    new Point(wall.X2, wall.Y2), 
                    wall.Thickness / 2)
            ).ToList();
        }

        /// <summary>Check if two line segments intersect (with optional buffer)</summary>
        public static bool SegmentsIntersect(Point a1, Point a2, Point b1, Point b2, double buffer = 0)
        {
            // Check if minimum distance between segments is less than buffer
            var dist = SegmentToSegmentDistance(a1, a2, b1, b2);
            return dist <= buffer;
        }

        /// <summary>Get intersection point of two line segments (or null)</summary>
        public static Point? GetSegmentIntersection(Point a1, Point a2, Point b1, Point b2)
        {
            var d1 = a2.X - a1.X; var d2 = a2.Y - a1.Y;
            var d3 = b2.X - b1.X; var d4 = b2.Y - b1.Y;
            
            var denom = d1 * d4 - d2 * d3;
            if (Math.Abs(denom) < 0.0001) return null; // Parallel
            
            var t = ((b1.X - a1.X) * d4 - (b1.Y - a1.Y) * d3) / denom;
            var u = ((b1.X - a1.X) * d2 - (b1.Y - a1.Y) * d1) / denom;
            
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return new Point(a1.X + t * d1, a1.Y + t * d2);
            }
            return null;
        }

        #endregion

        #region Path Finding Helpers

        /// <summary>Find best passage to route through between two points</summary>
        public static Passage? FindBestPassage(Point from, Point to, List<Passage> passages, LayoutData layout)
        {
            // Direct path clear?
            if (!IntersectsWall(from, to, layout))
                return null; // No passage needed
            
            // Find passage that minimizes total path length
            Passage? best = null;
            double bestDist = double.MaxValue;
            
            foreach (var passage in passages)
            {
                var throughPassage = Distance(from, passage.Center) + Distance(passage.Center, to);
                
                // Check that path through passage doesn't hit walls
                if (!IntersectsWall(from, passage.Center, layout) && 
                    !IntersectsWall(passage.Center, to, layout))
                {
                    if (throughPassage < bestDist)
                    {
                        bestDist = throughPassage;
                        best = passage;
                    }
                }
            }
            
            return best;
        }

        /// <summary>Generate waypoints for routing from output to input terminal</summary>
        public static List<Point> GenerateWaypoints(Point from, Point to, LayoutData layout)
        {
            var waypoints = new List<Point>();
            
            // Direct path?
            if (!IntersectsWall(from, to, layout))
                return waypoints; // Empty - direct connection
            
            // Find passages
            var passages = DetectPassages(layout);
            if (passages.Count == 0)
                return waypoints; // No passages - try direct anyway
            
            // Simple single-passage routing
            var passage = FindBestPassage(from, to, passages, layout);
            if (passage != null)
            {
                waypoints.Add(passage.Center);
            }
            
            // TODO: Multi-passage routing for complex layouts
            // Would need A* or similar pathfinding
            
            return waypoints;
        }

        #endregion

        #region Utility

        public static double Distance(Point a, Point b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        public static double PointToSegmentDistance(Point p, Point a, Point b)
        {
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var lenSq = dx * dx + dy * dy;
            if (lenSq == 0) return Distance(p, a);
            var t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq, 0, 1);
            return Distance(p, new Point(a.X + t * dx, a.Y + t * dy));
        }

        public static double SegmentToSegmentDistance(Point a1, Point a2, Point b1, Point b2)
        {
            // Check actual intersection first
            if (GetSegmentIntersection(a1, a2, b1, b2).HasValue)
                return 0;
            
            // Otherwise minimum of point-to-segment distances
            return Math.Min(
                Math.Min(PointToSegmentDistance(a1, b1, b2), PointToSegmentDistance(a2, b1, b2)),
                Math.Min(PointToSegmentDistance(b1, a1, a2), PointToSegmentDistance(b2, a1, a2))
            );
        }

        /// <summary>Get node bounds as Rect</summary>
        public static Rect GetNodeBounds(NodeData node) =>
            new Rect(node.Visual.X, node.Visual.Y, node.Visual.Width, node.Visual.Height);

        /// <summary>Get wall bounds as Rect</summary>
        public static Rect GetWallBounds(WallData wall)
        {
            var minX = Math.Min(wall.X1, wall.X2);
            var minY = Math.Min(wall.Y1, wall.Y2);
            var maxX = Math.Max(wall.X1, wall.X2);
            var maxY = Math.Max(wall.Y1, wall.Y2);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion
    }
}
