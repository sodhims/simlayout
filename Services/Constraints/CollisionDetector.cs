using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Collision detection for constrained entities in frictionless mode
    /// </summary>
    public class CollisionDetector
    {
        private readonly LayoutData _layout;
        private readonly ConstraintFactory _constraintFactory;

        public CollisionDetector(LayoutData layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _constraintFactory = new ConstraintFactory(layout);
        }

        /// <summary>
        /// Check if a position on a constraint would collide with other entities
        /// </summary>
        public bool CheckConstraintCollision(object entity, Point position)
        {
            if (entity == null)
                return false;

            // Check collision based on entity type
            if (entity is EOTCraneData eotCrane)
            {
                return CheckEOTCraneCollision(eotCrane, position);
            }
            else if (entity is JibCraneData jibCrane)
            {
                return CheckJibCraneCollision(jibCrane, position);
            }
            else if (entity is ZoneData zone)
            {
                return CheckZoneCollision(zone, position);
            }
            else if (entity is ConveyorData conveyor)
            {
                return CheckConveyorCollision(conveyor, position);
            }

            return false;
        }

        /// <summary>
        /// Check if a position violates constraint boundaries
        /// </summary>
        public bool CheckBoundaryViolation(object entity, Point position)
        {
            if (entity == null)
                return false;

            var constraint = _constraintFactory.GetConstraintForEntity(entity);
            if (constraint == null)
                return false;

            // Project the position onto the constraint
            var parameter = constraint.ProjectPoint(position);
            var projectedPos = constraint.Evaluate(parameter);

            // Check if the projected position is significantly different from requested position
            // (indicates position is outside constraint boundaries)
            var distance = (position - projectedPos).Length;

            // Tolerance: 1 pixel
            return distance > 1.0;
        }

        /// <summary>
        /// Get collision warnings for a position
        /// </summary>
        public List<string> GetCollisionWarnings(object entity, Point position)
        {
            var warnings = new List<string>();

            if (CheckBoundaryViolation(entity, position))
            {
                warnings.Add("Position outside constraint boundary");
            }

            if (CheckConstraintCollision(entity, position))
            {
                warnings.Add("Collision with other entity");
            }

            return warnings;
        }

        /// <summary>
        /// Check if EOT crane at position would collide with other cranes on same runway
        /// </summary>
        private bool CheckEOTCraneCollision(EOTCraneData crane, Point position)
        {
            // Find other cranes on the same runway
            var otherCranes = _layout.EOTCranes
                .Where(c => c.Id != crane.Id && c.RunwayId == crane.RunwayId)
                .ToList();

            if (!otherCranes.Any())
                return false;

            // Get runway
            var runway = _layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
            if (runway == null)
                return false;

            // Calculate crane's parameter on runway
            var constraint = crane.GetConstraint(runway);
            if (constraint == null)
                return false;

            var craneParameter = constraint.ProjectPoint(position);

            // Check collision with each other crane
            foreach (var other in otherCranes)
            {
                var otherConstraint = other.GetConstraint(runway);
                if (otherConstraint == null)
                    continue;

                // Get other crane's current position (using its center)
                var otherCenter = new Point(
                    runway.StartX + (runway.EndX - runway.StartX) * 0.5,
                    runway.StartY + (runway.EndY - runway.StartY) * 0.5
                );
                var otherParameter = otherConstraint.ProjectPoint(otherCenter);

                // Check if parameters are too close (collision distance)
                var parameterDistance = Math.Abs(craneParameter - otherParameter);

                // Collision threshold: 10% of runway length
                if (parameterDistance < 0.1)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if jib crane at position would collide with other entities
        /// </summary>
        private bool CheckJibCraneCollision(JibCraneData crane, Point position)
        {
            // Check collision with other jib cranes
            var otherJibs = _layout.JibCranes
                .Where(j => j.Id != crane.Id)
                .ToList();

            foreach (var other in otherJibs)
            {
                // Check if arc positions overlap
                var distance = Math.Sqrt(
                    Math.Pow(position.X - other.CenterX, 2) +
                    Math.Pow(position.Y - other.CenterY, 2)
                );

                // Collision if within other crane's radius
                if (distance < other.Radius + 5) // 5 pixel margin
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if zone at position would overlap with other zones
        /// </summary>
        private bool CheckZoneCollision(ZoneData zone, Point position)
        {
            // For zones, check if the new center position would cause polygon overlap
            // This is a simplified check - in production, use polygon intersection

            var otherZones = _layout.Zones
                .Where(z => z.Id != zone.Id)
                .ToList();

            foreach (var other in otherZones)
            {
                // Calculate zone centers
                var otherCenterX = other.Points.Average(p => p.X);
                var otherCenterY = other.Points.Average(p => p.Y);
                var otherCenter = new Point(otherCenterX, otherCenterY);

                // Simple distance check
                var distance = (position - otherCenter).Length;

                // Collision if centers are very close
                if (distance < 20) // 20 pixel minimum separation
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if conveyor at position would collide with other conveyors
        /// </summary>
        private bool CheckConveyorCollision(ConveyorData conveyor, Point position)
        {
            // Simplified conveyor collision check
            var otherConveyors = _layout.Conveyors
                .Where(c => c.Id != conveyor.Id)
                .ToList();

            foreach (var other in otherConveyors)
            {
                if (other.Path.Count == 0)
                    continue;

                // Check distance to first waypoint of other conveyor
                var otherStart = new Point(other.Path[0].X, other.Path[0].Y);
                var distance = (position - otherStart).Length;

                if (distance < 10) // 10 pixel minimum separation
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if position is within layout canvas bounds
        /// </summary>
        public bool IsWithinCanvasBounds(Point position)
        {
            if (_layout.Canvas == null)
                return true; // No bounds defined

            return position.X >= 0 && position.X <= _layout.Canvas.Width &&
                   position.Y >= 0 && position.Y <= _layout.Canvas.Height;
        }
    }
}
