using System;
using System.Windows;
using System.Windows.Media;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Linear constraint for movement along a straight line
    /// Use cases: crane runway, trolley on bridge, slider
    /// </summary>
    public class LinearConstraint : IConstraint
    {
        private readonly Point _start;
        private readonly Point _end;
        private readonly Vector _direction;
        private readonly double _length;

        public ConstraintType ConstraintType => ConstraintType.Linear;

        public LinearConstraint(Point start, Point end)
        {
            _start = start;
            _end = end;
            _direction = end - start;
            _length = _direction.Length;

            if (_length > 0)
                _direction.Normalize();
        }

        /// <summary>
        /// Project a point onto the line (perpendicular projection)
        /// Returns parameter clamped to [0, 1]
        /// </summary>
        public double ProjectPoint(Point mouseWorld)
        {
            if (_length == 0)
                return 0;

            // Vector from start to mouse
            Vector toMouse = mouseWorld - _start;

            // Project onto direction vector
            double dotProduct = toMouse.X * _direction.X + toMouse.Y * _direction.Y;
            double parameter = dotProduct / _length;

            // Clamp to [0, 1]
            return Math.Max(0, Math.Min(1, parameter));
        }

        /// <summary>
        /// Evaluate position at parameter t (0 = start, 1 = end)
        /// </summary>
        public Point Evaluate(double parameter)
        {
            // Clamp parameter
            parameter = Math.Max(0, Math.Min(1, parameter));

            return new Point(
                _start.X + parameter * (_end.X - _start.X),
                _start.Y + parameter * (_end.Y - _start.Y)
            );
        }

        /// <summary>
        /// Get parameter range [0, 1]
        /// </summary>
        public (double min, double max) GetParameterRange()
        {
            return (0, 1);
        }

        /// <summary>
        /// Get visual guide as a dashed line
        /// </summary>
        public Geometry GetVisualGuide()
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(_start, false, false);
                context.LineTo(_end, true, false);
            }
            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Get start point
        /// </summary>
        public Point Start => _start;

        /// <summary>
        /// Get end point
        /// </summary>
        public Point End => _end;

        /// <summary>
        /// Get constraint length
        /// </summary>
        public double Length => _length;
    }
}
