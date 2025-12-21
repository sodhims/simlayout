using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Conflicts
{
    /// <summary>
    /// Helper methods for geometric calculations in conflict detection
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Checks if a line segment (polyline) intersects with a polygon
        /// </summary>
        public static bool LineIntersectsPolygon(List<PointData> line, List<PointData> polygon)
        {
            if (line == null || line.Count < 2 || polygon == null || polygon.Count < 3)
                return false;

            // Check if any line segment intersects polygon edges
            for (int i = 0; i < line.Count - 1; i++)
            {
                var p1 = new Point(line[i].X, line[i].Y);
                var p2 = new Point(line[i + 1].X, line[i + 1].Y);

                // Check if line segment intersects any polygon edge
                for (int j = 0; j < polygon.Count; j++)
                {
                    var p3 = new Point(polygon[j].X, polygon[j].Y);
                    var p4 = new Point(polygon[(j + 1) % polygon.Count].X, polygon[(j + 1) % polygon.Count].Y);

                    if (LineSegmentsIntersect(p1, p2, p3, p4))
                        return true;
                }

                // Check if line segment is inside polygon
                if (IsPointInPolygon(p1, polygon) || IsPointInPolygon(p2, polygon))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a point is inside a polygon using ray casting
        /// </summary>
        public static bool IsPointInPolygon(Point point, List<PointData> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool inside = false;
            int count = polygon.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                var pi = new Point(polygon[i].X, polygon[i].Y);
                var pj = new Point(polygon[j].X, polygon[j].Y);

                if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                    (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Checks if two line segments intersect
        /// </summary>
        public static bool LineSegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d1 = Direction(p3, p4, p1);
            double d2 = Direction(p3, p4, p2);
            double d3 = Direction(p1, p2, p3);
            double d4 = Direction(p1, p2, p4);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            if (d1 == 0 && OnSegment(p3, p4, p1)) return true;
            if (d2 == 0 && OnSegment(p3, p4, p2)) return true;
            if (d3 == 0 && OnSegment(p1, p2, p3)) return true;
            if (d4 == 0 && OnSegment(p1, p2, p4)) return true;

            return false;
        }

        /// <summary>
        /// Calculates the direction of point p relative to line (p1, p2)
        /// </summary>
        private static double Direction(Point p1, Point p2, Point p)
        {
            return (p.X - p1.X) * (p2.Y - p1.Y) - (p.Y - p1.Y) * (p2.X - p1.X);
        }

        /// <summary>
        /// Checks if point p is on line segment (p1, p2)
        /// </summary>
        private static bool OnSegment(Point p1, Point p2, Point p)
        {
            return Math.Min(p1.X, p2.X) <= p.X && p.X <= Math.Max(p1.X, p2.X) &&
                   Math.Min(p1.Y, p2.Y) <= p.Y && p.Y <= Math.Max(p1.Y, p2.Y);
        }

        /// <summary>
        /// Checks if two polygons overlap
        /// </summary>
        public static bool PolygonsOverlap(List<PointData> polygon1, List<PointData> polygon2)
        {
            if (polygon1 == null || polygon1.Count < 3 || polygon2 == null || polygon2.Count < 3)
                return false;

            // Check if any vertex of polygon1 is inside polygon2
            foreach (var point in polygon1)
            {
                if (IsPointInPolygon(new Point(point.X, point.Y), polygon2))
                    return true;
            }

            // Check if any vertex of polygon2 is inside polygon1
            foreach (var point in polygon2)
            {
                if (IsPointInPolygon(new Point(point.X, point.Y), polygon1))
                    return true;
            }

            // Check if any edges intersect
            for (int i = 0; i < polygon1.Count; i++)
            {
                var p1 = new Point(polygon1[i].X, polygon1[i].Y);
                var p2 = new Point(polygon1[(i + 1) % polygon1.Count].X, polygon1[(i + 1) % polygon1.Count].Y);

                for (int j = 0; j < polygon2.Count; j++)
                {
                    var p3 = new Point(polygon2[j].X, polygon2[j].Y);
                    var p4 = new Point(polygon2[(j + 1) % polygon2.Count].X, polygon2[(j + 1) % polygon2.Count].Y);

                    if (LineSegmentsIntersect(p1, p2, p3, p4))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the center point of a polygon
        /// </summary>
        public static Point GetPolygonCenter(List<PointData> polygon)
        {
            if (polygon == null || polygon.Count == 0)
                return new Point(0, 0);

            double x = polygon.Average(p => p.X);
            double y = polygon.Average(p => p.Y);
            return new Point(x, y);
        }

        /// <summary>
        /// Gets the center point of a line
        /// </summary>
        public static Point GetLineCenter(List<PointData> line)
        {
            if (line == null || line.Count == 0)
                return new Point(0, 0);

            double x = line.Average(p => p.X);
            double y = line.Average(p => p.Y);
            return new Point(x, y);
        }
    }
}
