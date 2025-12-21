using System;
using System.Collections.Generic;
using LayoutEditor.Models;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// Main validation entry point - delegates to specialized validators
    /// </summary>
    public static class ValidationHelper
    {
        public static List<ValidationIssue> ValidateLayout(LayoutData layout)
        {
            var issues = new List<ValidationIssue>();

            NodeValidator.Validate(layout, issues);
            PathValidator.Validate(layout, issues);
            ConnectivityValidator.Validate(layout, issues);
            SimulationValidator.Validate(layout, issues);
            OverlapValidator.Validate(layout, issues);
            AGVValidator.Validate(layout, issues);

            return issues;
        }

        public static int ErrorCount(List<ValidationIssue> issues) =>
            issues.FindAll(i => i.Severity == "error").Count;

        public static int WarningCount(List<ValidationIssue> issues) =>
            issues.FindAll(i => i.Severity == "warning").Count;

        public static bool HasErrors(List<ValidationIssue> issues) =>
            ErrorCount(issues) > 0;
    }

    /// <summary>
    /// Validation issue details
    /// </summary>
    public class ValidationIssue
    {
        public string Code { get; set; } = "";
        public string Severity { get; set; } = "warning";
        public string Message { get; set; } = "";
        public string? NodeId { get; set; }
        public string? PathId { get; set; }
    }
}
