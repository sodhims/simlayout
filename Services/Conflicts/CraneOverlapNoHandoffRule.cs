using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Conflicts
{
    /// <summary>
    /// Detects conflicts where crane coverage areas overlap without handoff points
    /// Note: This rule is a placeholder until CraneCoverageZoneData model is implemented
    /// </summary>
    public class CraneOverlapNoHandoffRule : IConflictRule
    {
        public ConflictType Type => ConflictType.CraneOverlapNoHandoff;

        public List<Conflict> Check(LayoutData layout)
        {
            var conflicts = new List<Conflict>();

            if (layout == null)
                return conflicts;

            // TODO: Implement when CraneCoverageZoneData model is available
            // For now, return empty list (no conflicts)

            return conflicts;
        }
    }
}
