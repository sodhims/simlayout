using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Interface for entities that can be moved with constraints in frictionless mode
    /// Implemented by: Cranes, Conveyors, AGVs, Zones
    /// </summary>
    public interface IConstrainedEntity
    {
        /// <summary>
        /// Get the movement constraint for this entity
        /// Returns null if no constraint is defined
        /// </summary>
        IConstraint GetConstraint();

        /// <summary>
        /// Check if this entity supports constrained movement
        /// </summary>
        bool SupportsConstrainedMovement { get; }
    }
}
