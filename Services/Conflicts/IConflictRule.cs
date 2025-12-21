using System.Collections.Generic;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Conflicts
{
    /// <summary>
    /// Interface for conflict detection rules
    /// </summary>
    public interface IConflictRule
    {
        /// <summary>
        /// The type of conflict this rule detects
        /// </summary>
        ConflictType Type { get; }

        /// <summary>
        /// Checks the layout for conflicts of this type
        /// </summary>
        /// <param name="layout">The layout to check</param>
        /// <returns>List of conflicts found (empty if none)</returns>
        List<Conflict> Check(LayoutData layout);
    }
}
