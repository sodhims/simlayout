using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for handling constrained dragging in frictionless mode
    /// Projects mouse positions onto entity constraints and updates positions
    /// </summary>
    public class ConstrainedDragService
    {
        private readonly LayoutData _layout;
        private readonly ConstraintFactory _constraintFactory;
        private readonly CollisionDetector _collisionDetector;

        public ConstrainedDragService(LayoutData layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _constraintFactory = new ConstraintFactory(layout);
            _collisionDetector = new CollisionDetector(layout);
        }

        /// <summary>
        /// Project a mouse position onto an entity's constraint
        /// Returns the constrained position and the parameter value
        /// </summary>
        public (Point position, double parameter) ProjectToConstraint(object entity, Point mouseWorld)
        {
            if (entity == null)
                return (mouseWorld, 0);

            // Get the constraint for this entity
            var constraint = _constraintFactory.GetConstraintForEntity(entity);
            if (constraint == null)
                return (mouseWorld, 0);

            // Project the mouse position onto the constraint
            var parameter = constraint.ProjectPoint(mouseWorld);

            // Evaluate the constraint at that parameter to get the actual position
            var position = constraint.Evaluate(parameter);

            return (position, parameter);
        }

        /// <summary>
        /// Update an entity's position based on constrained dragging
        /// Checks for collisions and boundary violations before updating
        /// </summary>
        public bool UpdateEntityPosition(object entity, Point mouseWorld)
        {
            if (entity == null)
                return false;

            var (position, parameter) = ProjectToConstraint(entity, mouseWorld);

            // Check for collisions and boundary violations
            if (_collisionDetector.CheckConstraintCollision(entity, position))
            {
                DebugLogger.Log($"[ConstrainedDrag] UpdateEntityPosition blocked by collision check");
                return false;
            }

            if (_collisionDetector.CheckBoundaryViolation(entity, position))
            {
                DebugLogger.Log($"[ConstrainedDrag] UpdateEntityPosition blocked by boundary violation: projected=({position.X:F1}, {position.Y:F1})");
                return false;
            }

            if (!_collisionDetector.IsWithinCanvasBounds(position))
            {
                DebugLogger.Log($"[ConstrainedDrag] UpdateEntityPosition blocked by canvas bounds: ({position.X:F1}, {position.Y:F1})");
                return false;
            }

            // Update the entity's position based on its type
            if (entity is JibCraneData jibCrane)
            {
                // Jib crane: center is fixed, only rotation changes
                // The parameter is the angle, but we don't move the crane itself
                // (In future stages, this could update an angle property)
                return true;
            }
            else if (entity is EOTCraneData eotCrane)
            {
                // EOT crane: moves along runway
                // The parameter from ProjectToConstraint is [0,1] on the zone segment (ZoneMin to ZoneMax)
                // Map it back to the runway's zone range
                var runway = _layout.Runways?.FirstOrDefault(r => r.Id == eotCrane.RunwayId);
                if (runway == null) return false;

                // The constraint is defined from ZoneMin to ZoneMax positions,
                // so parameter 0 = ZoneMin position, parameter 1 = ZoneMax position
                double bridgePos = eotCrane.ZoneMin + parameter * (eotCrane.ZoneMax - eotCrane.ZoneMin);

                // Update crane position (BridgePosition setter already clamps to zone)
                eotCrane.BridgePosition = bridgePos;

                return true;
            }
            else if (entity is ConveyorData conveyor)
            {
                // Conveyor: path can be moved
                // For now, we don't support moving conveyors in frictionless mode
                return false;
            }
            else if (entity is AGVPathData agvPath)
            {
                // AGV path: moves between waypoints
                // For now, we don't support moving AGV paths directly
                return false;
            }
            else if (entity is ZoneData zone)
            {
                // Zone: entire polygon moves
                if (zone.Points == null || zone.Points.Count == 0)
                    return false;

                // Calculate offset from current center to constrained position
                var currentCenter = CalculateZoneCenter(zone);
                var offset = new Vector(position.X - currentCenter.X, position.Y - currentCenter.Y);

                // Move all points by the offset
                foreach (var point in zone.Points)
                {
                    point.X += offset.X;
                    point.Y += offset.Y;
                }

                // Update zone bounds
                zone.X += offset.X;
                zone.Y += offset.Y;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the visual guide geometry for an entity's constraint
        /// </summary>
        public System.Windows.Media.Geometry GetConstraintGuide(object entity)
        {
            if (entity == null)
                return null;

            var constraint = _constraintFactory.GetConstraintForEntity(entity);
            return constraint?.GetVisualGuide();
        }

        /// <summary>
        /// Check if an entity supports constrained movement
        /// </summary>
        public bool SupportsConstrainedMovement(object entity)
        {
            return _constraintFactory.SupportsConstrainedMovement(entity);
        }

        /// <summary>
        /// Check if a position would cause a collision
        /// </summary>
        public bool WouldCollide(object entity, Point position)
        {
            return _collisionDetector.CheckConstraintCollision(entity, position);
        }

        /// <summary>
        /// Get collision warnings for a position
        /// </summary>
        public System.Collections.Generic.List<string> GetCollisionWarnings(object entity, Point position)
        {
            return _collisionDetector.GetCollisionWarnings(entity, position);
        }

        // Helper methods

        private Point CalculateZoneCenter(ZoneData zone)
        {
            if (zone.Points == null || zone.Points.Count == 0)
                return new Point(zone.X, zone.Y);

            double sumX = 0, sumY = 0;
            foreach (var point in zone.Points)
            {
                sumX += point.X;
                sumY += point.Y;
            }

            return new Point(sumX / zone.Points.Count, sumY / zone.Points.Count);
        }
    }
}
