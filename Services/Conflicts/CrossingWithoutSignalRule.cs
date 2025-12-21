using System.Collections.Generic;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Conflicts
{
    /// <summary>
    /// Detects conflicts where AGV paths or forklift aisles cross without proper crossing control
    /// Placeholder implementation - requires model properties (FromWaypoint, ToWaypoint, Path, etc.)
    /// </summary>
    public class CrossingWithoutSignalRule : IConflictRule
    {
        public ConflictType Type => ConflictType.CrossingWithoutSignal;

        public List<Conflict> Check(LayoutData layout)
        {
            var conflicts = new List<Conflict>();

            if (layout == null)
                return conflicts;

            // TODO: Full implementation requires:
            // - AGVPathData.FromWaypoint, ToWaypoint properties
            // - ForkliftAisleData.Path property
            // - CrossingZoneData.CrossedEntityIds property
            //
            // Placeholder: Returns no conflicts until model properties are available

            return conflicts;
        }
    }
}
