using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services.Conflicts
{
    /// <summary>
    /// Service for detecting conflicts in layout designs
    /// </summary>
    public class ConflictChecker
    {
        private readonly List<IConflictRule> _rules = new List<IConflictRule>();

        public ConflictChecker()
        {
            // Register all conflict detection rules
            RegisterRule(new PedestrianUnderDropZoneRule());
            RegisterRule(new CrossingWithoutSignalRule());
            RegisterRule(new CraneOverlapNoHandoffRule());
        }

        /// <summary>
        /// Registers a conflict detection rule
        /// </summary>
        public void RegisterRule(IConflictRule rule)
        {
            if (rule != null && !_rules.Any(r => r.Type == rule.Type))
            {
                _rules.Add(rule);
            }
        }

        /// <summary>
        /// Runs all registered rules and returns all detected conflicts
        /// </summary>
        public List<Conflict> CheckAll(LayoutData layout)
        {
            var allConflicts = new List<Conflict>();

            if (layout == null)
                return allConflicts;

            foreach (var rule in _rules)
            {
                var conflicts = rule.Check(layout);
                if (conflicts != null && conflicts.Any())
                {
                    allConflicts.AddRange(conflicts);
                }
            }

            return allConflicts;
        }

        /// <summary>
        /// Runs a specific rule by type
        /// </summary>
        public List<Conflict> CheckByType(LayoutData layout, ConflictType type)
        {
            var rule = _rules.FirstOrDefault(r => r.Type == type);
            if (rule == null)
                return new List<Conflict>();

            return rule.Check(layout) ?? new List<Conflict>();
        }

        /// <summary>
        /// Filters conflicts by severity
        /// </summary>
        public List<Conflict> FilterBySeverity(List<Conflict> conflicts, ConflictSeverity severity)
        {
            return conflicts.Where(c => c.Severity == severity).ToList();
        }

        /// <summary>
        /// Gets only error-level conflicts
        /// </summary>
        public List<Conflict> GetErrors(List<Conflict> conflicts)
        {
            return FilterBySeverity(conflicts, ConflictSeverity.Error);
        }

        /// <summary>
        /// Gets only warning-level conflicts
        /// </summary>
        public List<Conflict> GetWarnings(List<Conflict> conflicts)
        {
            return FilterBySeverity(conflicts, ConflictSeverity.Warning);
        }

        /// <summary>
        /// Filters out acknowledged conflicts
        /// </summary>
        public List<Conflict> GetUnacknowledged(List<Conflict> conflicts)
        {
            return conflicts.Where(c => !c.IsAcknowledged).ToList();
        }

        /// <summary>
        /// Gets count of registered rules
        /// </summary>
        public int RuleCount => _rules.Count;

        /// <summary>
        /// Gets all registered rule types
        /// </summary>
        public List<ConflictType> GetRegisteredTypes()
        {
            return _rules.Select(r => r.Type).ToList();
        }
    }
}
