using System;
using System.Windows;
using System.Windows.Media;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Arc constraint for movement along a circular arc
    /// Use cases: jib crane rotation, turntable
    /// </summary>
    public class ArcConstraint : IConstraint
    {
        private readonly Point _center;
        private readonly double _radius;
        private readonly double _startAngle;
        private readonly double _endAngle;

        public ConstraintType ConstraintType => ConstraintType.Arc;

        public ArcConstraint(Point center, double radius, double startAngle, double endAngle)
        {
            _center = center;
            _radius = radius;
            _startAngle = startAngle;
            _endAngle = endAngle;
        }

        /// <summary>
        /// Project a point onto the arc (find nearest angle on arc)
        /// Returns angle clamped to [startAngle, endAngle]
        /// </summary>
        public double ProjectPoint(Point mouseWorld)
        {
            // Vector from center to mouse
            var dx = mouseWorld.X - _center.X;
            var dy = mouseWorld.Y - _center.Y;

            // Calculate angle using atan2 (returns -π to π)
            var angle = Math.Atan2(dy, dx);

            // Normalize angle to [0, 2π]
            if (angle < 0)
                angle += 2 * Math.PI;

            // Clamp to [startAngle, endAngle]
            return Math.Max(_startAngle, Math.Min(_endAngle, angle));
        }

        /// <summary>
        /// Evaluate position at parameter (angle in radians)
        /// </summary>
        public Point Evaluate(double parameter)
        {
            // Clamp parameter to angle range
            parameter = Math.Max(_startAngle, Math.Min(_endAngle, parameter));

            return new Point(
                _center.X + _radius * Math.Cos(parameter),
                _center.Y + _radius * Math.Sin(parameter)
            );
        }

        /// <summary>
        /// Get parameter range [startAngle, endAngle] in radians
        /// </summary>
        public (double min, double max) GetParameterRange()
        {
            return (_startAngle, _endAngle);
        }

        /// <summary>
        /// Get visual guide as an arc
        /// </summary>
        public Geometry GetVisualGuide()
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                // Calculate start and end points
                var startPoint = Evaluate(_startAngle);
                var endPoint = Evaluate(_endAngle);

                // Determine if this is a large arc (> 180 degrees)
                var isLargeArc = (_endAngle - _startAngle) > Math.PI;

                context.BeginFigure(startPoint, false, false);
                context.ArcTo(
                    endPoint,
                    new Size(_radius, _radius),
                    0, // rotation angle
                    isLargeArc,
                    SweepDirection.Clockwise,
                    true, // is stroked
                    false // is smooth join
                );
            }
            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Get center point
        /// </summary>
        public Point Center => _center;

        /// <summary>
        /// Get radius
        /// </summary>
        public double Radius => _radius;

        /// <summary>
        /// Get start angle in radians
        /// </summary>
        public double StartAngle => _startAngle;

        /// <summary>
        /// Get end angle in radians
        /// </summary>
        public double EndAngle => _endAngle;
    }
}
