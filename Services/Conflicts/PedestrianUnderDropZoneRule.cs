using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Conflicts
{
    /// <summary>
    /// Detects conflicts where pedestrian walkways pass under crane drop zones
    /// </summary>
    public class PedestrianUnderDropZoneRule : IConflictRule
    {
        public ConflictType Type => ConflictType.PedestrianUnderDropZone;

        public List<Conflict> Check(LayoutData layout)
        {
            var conflicts = new List<Conflict>();

            if (layout == null)
                return conflicts;

            // Check each walkway against each drop zone
            foreach (var walkway in layout.Walkways)
            {
                foreach (var dropZone in layout.DropZones)
                {
                    // Check if walkway centerline intersects drop zone boundary
                    if (GeometryHelper.LineIntersectsPolygon(walkway.Centerline.ToList(), dropZone.Boundary.ToList()))
                    {
                        var location = GeometryHelper.GetLineCenter(walkway.Centerline.ToList());

                        var conflict = new Conflict
                        {
                            Type = Type,
                            Description = $"Walkway '{walkway.Name}' passes through drop zone '{dropZone.Name}'",
                            Location = location,
                            Severity = walkway.WalkwayType == WalkwayTypes.Emergency
                                ? ConflictSeverity.Error
                                : ConflictSeverity.Warning,
                            SuggestedFix = walkway.WalkwayType == WalkwayTypes.Emergency
                                ? "Critical: Reroute emergency walkway away from drop zone"
                                : "Reroute walkway around drop zone or add protective overhead barrier"
                        };

                        conflict.InvolvedElementIds.Add(walkway.Id);
                        conflict.InvolvedElementIds.Add(dropZone.Id);
                        conflict.Metadata["WalkwayType"] = walkway.WalkwayType;
                        conflict.Metadata["DropZoneName"] = dropZone.Name;

                        conflicts.Add(conflict);
                    }
                }
            }

            return conflicts;
        }
    }
}
