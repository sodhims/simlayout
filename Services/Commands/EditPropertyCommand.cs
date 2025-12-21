using System;
using System.Reflection;

namespace LayoutEditor.Services.Commands
{
    /// <summary>
    /// Command for editing a property value on any object
    /// Uses reflection to get/set property values
    /// </summary>
    public class EditPropertyCommand : ICommand
    {
        private readonly object _target;
        private readonly string _propertyName;
        private readonly object _oldValue;
        private readonly object _newValue;

        public string Id { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        public EditPropertyCommand(object target, string propertyName, object newValue)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be empty", nameof(propertyName));

            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;

            _target = target;
            _propertyName = propertyName;
            _newValue = newValue;

            // Get current value as old value
            var property = target.GetType().GetProperty(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type {target.GetType().Name}");

            _oldValue = property.GetValue(target);

            Description = $"Edit {propertyName}: {_oldValue} → {_newValue}";
        }

        public void Execute()
        {
            SetPropertyValue(_newValue);
        }

        public void Undo()
        {
            SetPropertyValue(_oldValue);
        }

        private void SetPropertyValue(object value)
        {
            var property = _target.GetType().GetProperty(_propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_target, value);
            }
        }

        public bool CanMergeWith(ICommand other)
        {
            if (other is EditPropertyCommand editCmd)
            {
                // Can merge if same target, same property, and commands are close in time
                return ReferenceEquals(editCmd._target, _target) &&
                       editCmd._propertyName == _propertyName &&
                       (Timestamp - editCmd.Timestamp).TotalSeconds < 1.0;
            }
            return false;
        }

        public void MergeWith(ICommand other)
        {
            // Merging would update _newValue, but since it's readonly, we can't implement this
            // For simplicity, we won't support merging property edits
            throw new NotSupportedException("Property edit commands cannot be merged");
        }
    }
}
