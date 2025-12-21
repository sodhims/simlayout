using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Path constraint for movement along a polyline
    /// Use cases: AGV path, conveyor path, guided vehicle
    /// </summary>
    public class PathConstraint : IConstraint
    {
        private readonly List<Point> _waypoints;
        private readonly List<double> _segmentLengths;
        private readonly double _totalLength;

        public ConstraintType ConstraintType => ConstraintType.Path;

        public PathConstraint(List<Point> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2)
                throw new ArgumentException("PathConstraint requires at least 2 waypoints");

            _waypoints = new List<Point>(waypoints);
            _segmentLengths = new List<double>();
            _totalLength = 0;

            // Calculate segment lengths
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                var dx = _waypoints[i + 1].X - _waypoints[i].X;
                var dy = _waypoints[i + 1].Y - _waypoints[i].Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                _segmentLengths.Add(length);
                _totalLength += length;
            }
        }

        /// <summary>
        /// Project a point onto the polyline path
        /// Returns parameter [0, 1] representing position along entire path
        /// </summary>
        public double ProjectPoint(Point mouseWorld)
        {
            if (_totalLength == 0)
                return 0;

            double minDistance = double.MaxValue;
            double bestParameter = 0;

            double accumulatedLength = 0;

            // Check each segment
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                var start = _waypoints[i];
                var end = _waypoints[i + 1];
                var segmentLength = _segmentLengths[i];

                if (segmentLength == 0)
                    continue;

                // Vector from start to end
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;

                // Vector from start to mouse
                var mx = mouseWorld.X - start.X;
                var my = mouseWorld.Y - start.Y;

                // Project onto segment
                var dotProduct = (mx * dx + my * dy) / (segmentLength * segmentLength);
                var t = Math.Max(0, Math.Min(1, dotProduct)); // Clamp to [0, 1]

                // Point on segment
                var projX = start.X + t * dx;
                var projY = start.Y + t * dy;

                // Distance to projected point
                var distX = mouseWorld.X - projX;
                var distY = mouseWorld.Y - projY;
                var distance = Math.Sqrt(distX * distX + distY * distY);

                // Update best if closer
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestParameter = (accumulatedLength + t * segmentLength) / _totalLength;
                }

                accumulatedLength += segmentLength;
            }

            return Math.Max(0, Math.Min(1, bestParameter));
        }

        /// <summary>
        /// Evaluate position at parameter t (0 = start, 1 = end of path)
        /// </summary>
        public Point Evaluate(double parameter)
        {
            // Clamp parameter
            parameter = Math.Max(0, Math.Min(1, parameter));

            if (_totalLength == 0)
                return _waypoints[0];

            // Convert parameter to distance along path
            var targetDistance = parameter * _totalLength;
            var accumulatedLength = 0.0;

            // Find which segment contains this distance
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                var segmentLength = _segmentLengths[i];

                if (accumulatedLength + segmentLength >= targetDistance)
                {
                    // This segment contains the target
                    var distanceInSegment = targetDistance - accumulatedLength;
                    var t = segmentLength > 0 ? distanceInSegment / segmentLength : 0;

                    var start = _waypoints[i];
                    var end = _waypoints[i + 1];

                    return new Point(
                        start.X + t * (end.X - start.X),
                        start.Y + t * (end.Y - start.Y)
                    );
                }

                accumulatedLength += segmentLength;
            }

            // If we get here, return the last point
            return _waypoints[_waypoints.Count - 1];
        }

        /// <summary>
        /// Get parameter range [0, 1]
        /// </summary>
        public (double min, double max) GetParameterRange()
        {
            return (0, 1);
        }

        /// <summary>
        /// Get visual guide as a polyline
        /// </summary>
        public Geometry GetVisualGuide()
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                if (_waypoints.Count > 0)
                {
                    context.BeginFigure(_waypoints[0], false, false);

                    for (int i = 1; i < _waypoints.Count; i++)
                    {
                        context.LineTo(_waypoints[i], true, false);
                    }
                }
            }
            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Get waypoints
        /// </summary>
        public List<Point> Waypoints => new List<Point>(_waypoints);

        /// <summary>
        /// Get total path length
        /// </summary>
        public double TotalLength => _totalLength;
    }
}
