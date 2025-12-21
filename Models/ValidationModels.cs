using System;
using System.Collections.ObjectModel;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult : NotifyBase
    {
        private DateTime? _lastValidated;

        public DateTime? LastValidated
        {
            get => _lastValidated;
            set => SetProperty(ref _lastValidated, value);
        }

        public ObservableCollection<ValidationIssue> Issues { get; set; } = new();
    }

    /// <summary>
    /// Individual validation issue
    /// </summary>
    public class ValidationIssue : NotifyBase
    {
        private string _code = "";
        private string _severity = "warning";
        private string _message = "";
        private string? _nodeId;
        private string? _pathId;

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public string Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string? NodeId
        {
            get => _nodeId;
            set => SetProperty(ref _nodeId, value);
        }

        public string? PathId
        {
            get => _pathId;
            set => SetProperty(ref _pathId, value);
        }
    }
}
