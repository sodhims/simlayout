using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Polygon constraint for movement within a bounded region
    /// Use cases: forklift aisle, restricted zone, warehouse section
    /// </summary>
    public class PolygonConstraint : IConstraint
    {
        private readonly List<Point> _vertices;
        private readonly Point _centroid;

        public ConstraintType ConstraintType => ConstraintType.Polygon;

        public PolygonConstraint(List<Point> vertices)
        {
            if (vertices == null || vertices.Count < 3)
                throw new ArgumentException("PolygonConstraint requires at least 3 vertices");

            _vertices = new List<Point>(vertices);
            _centroid = CalculateCentroid(_vertices);
        }

        /// <summary>
        /// Project a point into the polygon
        /// If point is inside, return it as-is
        /// If point is outside, project to nearest edge
        /// Returns angle parameter (for consistency with other constraints)
        /// </summary>
        public double ProjectPoint(Point mouseWorld)
        {
            // For polygon, we don't use parameter for positioning
            // We return a dummy parameter (angle from centroid)
            var dx = mouseWorld.X - _centroid.X;
            var dy = mouseWorld.Y - _centroid.Y;
            var angle = Math.Atan2(dy, dx);

            // Normalize to [0, 2π]
            if (angle < 0)
                angle += 2 * Math.PI;

            return angle;
        }

        /// <summary>
        /// Evaluate position at parameter (angle from centroid)
        /// Intersect ray from centroid at given angle with polygon boundary
        /// </summary>
        public Point Evaluate(double parameter)
        {
            // Cast ray from centroid at given angle
            var direction = new Vector(Math.Cos(parameter), Math.Sin(parameter));
            var rayStart = _centroid;
            var rayEnd = new Point(
                _centroid.X + direction.X * 10000,
                _centroid.Y + direction.Y * 10000
            );

            // Find intersection with polygon edges
            Point? closestIntersection = null;
            double minDistance = double.MaxValue;

            for (int i = 0; i < _vertices.Count; i++)
            {
                var p1 = _vertices[i];
                var p2 = _vertices[(i + 1) % _vertices.Count];

                var intersection = LineIntersection(rayStart, rayEnd, p1, p2);
                if (intersection.HasValue)
                {
                    var dx = intersection.Value.X - _centroid.X;
                    var dy = intersection.Value.Y - _centroid.Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestIntersection = intersection;
                    }
                }
            }

            return closestIntersection ?? _centroid;
        }

        /// <summary>
        /// Get parameter range [0, 2π] (full rotation around centroid)
        /// </summary>
        public (double min, double max) GetParameterRange()
        {
            return (0, 2 * Math.PI);
        }

        /// <summary>
        /// Get visual guide as a closed polygon
        /// </summary>
        public Geometry GetVisualGuide()
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                if (_vertices.Count > 0)
                {
                    context.BeginFigure(_vertices[0], false, true); // true = closed

                    for (int i = 1; i < _vertices.Count; i++)
                    {
                        context.LineTo(_vertices[i], true, false);
                    }
                }
            }
            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Check if a point is inside the polygon using ray casting algorithm
        /// </summary>
        public bool Contains(Point point)
        {
            int intersections = 0;
            for (int i = 0; i < _vertices.Count; i++)
            {
                var p1 = _vertices[i];
                var p2 = _vertices[(i + 1) % _vertices.Count];

                if ((p1.Y > point.Y) != (p2.Y > point.Y))
                {
                    var xIntersection = (p2.X - p1.X) * (point.Y - p1.Y) / (p2.Y - p1.Y) + p1.X;
                    if (point.X < xIntersection)
                        intersections++;
                }
            }

            return (intersections % 2) == 1;
        }

        /// <summary>
        /// Clamp a point to be inside the polygon
        /// If outside, project to nearest edge
        /// </summary>
        public Point ClampToPolygon(Point point)
        {
            if (Contains(point))
                return point;

            // Find nearest point on polygon boundary
            Point nearestPoint = _vertices[0];
            double minDistance = double.MaxValue;

            for (int i = 0; i < _vertices.Count; i++)
            {
                var p1 = _vertices[i];
                var p2 = _vertices[(i + 1) % _vertices.Count];

                var projected = ProjectPointToSegment(point, p1, p2);
                var dx = point.X - projected.X;
                var dy = point.Y - projected.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = projected;
                }
            }

            return nearestPoint;
        }

        /// <summary>
        /// Get vertices
        /// </summary>
        public List<Point> Vertices => new List<Point>(_vertices);

        /// <summary>
        /// Get centroid
        /// </summary>
        public Point Centroid => _centroid;

        // Helper methods

        private Point CalculateCentroid(List<Point> vertices)
        {
            double x = 0, y = 0;
            foreach (var vertex in vertices)
            {
                x += vertex.X;
                y += vertex.Y;
            }
            return new Point(x / vertices.Count, y / vertices.Count);
        }

        private Point? LineIntersection(Point a1, Point a2, Point b1, Point b2)
        {
            var dxa = a2.X - a1.X;
            var dya = a2.Y - a1.Y;
            var dxb = b2.X - b1.X;
            var dyb = b2.Y - b1.Y;

            var denominator = dxa * dyb - dya * dxb;
            if (Math.Abs(denominator) < 1e-10)
                return null; // Parallel lines

            var t = ((b1.X - a1.X) * dyb - (b1.Y - a1.Y) * dxb) / denominator;
            var u = ((b1.X - a1.X) * dya - (b1.Y - a1.Y) * dxa) / denominator;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return new Point(a1.X + t * dxa, a1.Y + t * dya);
            }

            return null;
        }

        private Point ProjectPointToSegment(Point point, Point segmentStart, Point segmentEnd)
        {
            var dx = segmentEnd.X - segmentStart.X;
            var dy = segmentEnd.Y - segmentStart.Y;
            var lengthSquared = dx * dx + dy * dy;

            if (lengthSquared < 1e-10)
                return segmentStart;

            var t = ((point.X - segmentStart.X) * dx + (point.Y - segmentStart.Y) * dy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));

            return new Point(
                segmentStart.X + t * dx,
                segmentStart.Y + t * dy
            );
        }
    }
}
