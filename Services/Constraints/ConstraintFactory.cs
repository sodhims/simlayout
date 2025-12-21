using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Constraints
{
    /// <summary>
    /// Factory for creating constraints from layout entities
    /// Resolves dependencies like runways for cranes and waypoints for AGV paths
    /// </summary>
    public class ConstraintFactory
    {
        private readonly LayoutData _layout;

        public ConstraintFactory(LayoutData layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        /// <summary>
        /// Get constraint for any constrained entity
        /// Automatically resolves dependencies (runways, waypoints, etc.)
        /// </summary>
        public IConstraint GetConstraintForEntity(object entity)
        {
            if (entity == null)
                return null;

            // Try direct IConstrainedEntity first
            if (entity is IConstrainedEntity constrained)
            {
                // For entities that need external data, handle specifically
                if (entity is EOTCraneData eotCrane)
                {
                    return GetEOTCraneConstraint(eotCrane);
                }
                else if (entity is AGVPathData agvPath)
                {
                    return GetAGVPathConstraint(agvPath);
                }
                else
                {
                    // For entities that don't need external data, use GetConstraint directly
                    return constrained.GetConstraint();
                }
            }

            return null;
        }

        /// <summary>
        /// Get constraint for EOT crane (requires runway lookup)
        /// </summary>
        public IConstraint GetEOTCraneConstraint(EOTCraneData crane)
        {
            if (crane == null || string.IsNullOrEmpty(crane.RunwayId))
                return null;

            var runway = _layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
            if (runway == null)
                return null;

            return crane.GetConstraint(runway);
        }

        /// <summary>
        /// Get constraint for AGV path (requires waypoint lookup)
        /// </summary>
        public IConstraint GetAGVPathConstraint(AGVPathData agvPath)
        {
            if (agvPath == null)
                return null;

            var fromWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == agvPath.FromWaypointId);
            var toWaypoint = _layout.AGVWaypoints.FirstOrDefault(w => w.Id == agvPath.ToWaypointId);

            if (fromWaypoint == null || toWaypoint == null)
                return null;

            return agvPath.GetConstraint(fromWaypoint, toWaypoint);
        }

        /// <summary>
        /// Get constraint for jib crane (no external dependencies)
        /// </summary>
        public IConstraint GetJibCraneConstraint(JibCraneData crane)
        {
            return crane?.GetConstraint();
        }

        /// <summary>
        /// Get constraint for conveyor (no external dependencies)
        /// </summary>
        public IConstraint GetConveyorConstraint(ConveyorData conveyor)
        {
            return conveyor?.GetConstraint();
        }

        /// <summary>
        /// Get constraint for zone (no external dependencies)
        /// </summary>
        public IConstraint GetZoneConstraint(ZoneData zone)
        {
            return zone?.GetConstraint();
        }

        /// <summary>
        /// Check if an entity supports constrained movement
        /// </summary>
        public bool SupportsConstrainedMovement(object entity)
        {
            if (entity is IConstrainedEntity constrained)
                return constrained.SupportsConstrainedMovement;

            return false;
        }
    }
}
