using System.Windows;
using System.Windows.Media;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Type of movement constraint
    /// </summary>
    public enum ConstraintType
    {
        Linear,   // Straight line (crane runway, trolley)
        Arc,      // Circular arc (jib crane rotation)
        Path,     // Polyline path (AGV path, conveyor)
        Polygon   // Bounded region (forklift aisle)
    }

    /// <summary>
    /// Interface for movement constraints
    /// Defines how entities can move in the frictionless mode
    /// </summary>
    public interface IConstraint
    {
        /// <summary>
        /// Type of this constraint
        /// </summary>
        ConstraintType ConstraintType { get; }

        /// <summary>
        /// Project a world point onto the constraint
        /// Returns a parameter value (0-1 for linear, angle for arc, etc.)
        /// </summary>
        double ProjectPoint(Point mouseWorld);

        /// <summary>
        /// Evaluate the constraint at a parameter value
        /// Returns the world position on the constraint
        /// </summary>
        Point Evaluate(double parameter);

        /// <summary>
        /// Get the valid parameter range for this constraint
        /// </summary>
        (double min, double max) GetParameterRange();

        /// <summary>
        /// Get visual guide geometry for rendering the constraint
        /// Used to show the constraint path to the user
        /// </summary>
        Geometry GetVisualGuide();
    }
}
