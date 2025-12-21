using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for managing dynamic property panels
    /// Provides property discovery, validation, and editing
    /// </summary>
    public class PropertyPanelService
    {
        private readonly Dictionary<Type, List<PropertyDescriptor>> _propertyCache = new Dictionary<Type, List<PropertyDescriptor>>();
        private readonly Dictionary<string, IPropertyValidator> _validators = new Dictionary<string, IPropertyValidator>();

        /// <summary>
        /// Get all editable properties for an object
        /// </summary>
        public List<PropertyDescriptor> GetEditableProperties(object target)
        {
            if (target == null)
                return new List<PropertyDescriptor>();

            var type = target.GetType();

            // Check cache first
            if (_propertyCache.TryGetValue(type, out var cached))
                return cached;

            var properties = new List<PropertyDescriptor>();

            // Get all public properties that are readable and writable
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .OrderBy(p => p.Name);

            foreach (var prop in props)
            {
                // Skip complex types that shouldn't be edited directly
                if (IsSimpleType(prop.PropertyType))
                {
                    var descriptor = new PropertyDescriptor
                    {
                        Name = prop.Name,
                        PropertyType = prop.PropertyType,
                        DisplayName = GetDisplayName(prop),
                        Description = GetDescription(prop),
                        Category = GetCategory(prop),
                        IsReadOnly = false,
                        PropertyInfo = prop
                    };

                    properties.Add(descriptor);
                }
            }

            _propertyCache[type] = properties;
            return properties;
        }

        /// <summary>
        /// Get the current value of a property
        /// </summary>
        public object GetPropertyValue(object target, PropertyDescriptor descriptor)
        {
            if (target == null || descriptor == null)
                return null;

            return descriptor.PropertyInfo.GetValue(target);
        }

        /// <summary>
        /// Set a property value with validation
        /// </summary>
        public (bool success, string error) SetPropertyValue(object target, PropertyDescriptor descriptor, object value)
        {
            if (target == null || descriptor == null)
                return (false, "Target or descriptor is null");

            // Validate the value
            var validationResult = ValidatePropertyValue(descriptor, value);
            if (!validationResult.isValid)
                return (false, validationResult.error);

            try
            {
                // Convert value to correct type if needed
                var convertedValue = ConvertValue(value, descriptor.PropertyType);
                descriptor.PropertyInfo.SetValue(target, convertedValue);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to set value: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a validator for a property
        /// </summary>
        public void RegisterValidator(string propertyName, IPropertyValidator validator)
        {
            _validators[propertyName] = validator;
        }

        /// <summary>
        /// Validate a property value
        /// </summary>
        public (bool isValid, string error) ValidatePropertyValue(PropertyDescriptor descriptor, object value)
        {
            if (descriptor == null)
                return (false, "Descriptor is null");

            // Check for registered validator
            if (_validators.TryGetValue(descriptor.Name, out var validator))
            {
                return validator.Validate(value);
            }

            // Default validation - type checking
            if (value != null && !descriptor.PropertyType.IsInstanceOfType(value))
            {
                try
                {
                    ConvertValue(value, descriptor.PropertyType);
                }
                catch
                {
                    return (false, $"Value must be of type {descriptor.PropertyType.Name}");
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Convert a value to the target type
        /// </summary>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsInstanceOfType(value))
                return value;

            // Handle string to number conversions
            if (value is string str)
            {
                if (targetType == typeof(int))
                    return int.Parse(str);
                if (targetType == typeof(double))
                    return double.Parse(str);
                if (targetType == typeof(float))
                    return float.Parse(str);
                if (targetType == typeof(bool))
                    return bool.Parse(str);
            }

            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Check if a type is simple enough to edit in a property panel
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }

        /// <summary>
        /// Get display name from attribute or property name
        /// </summary>
        private string GetDisplayName(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<DisplayNameAttribute>();
            return attr?.DisplayName ?? prop.Name;
        }

        /// <summary>
        /// Get description from attribute
        /// </summary>
        private string GetDescription(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? string.Empty;
        }

        /// <summary>
        /// Get category from attribute
        /// </summary>
        private string GetCategory(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<CategoryAttribute>();
            return attr?.Category ?? "General";
        }
    }

    /// <summary>
    /// Descriptor for a property in the property panel
    /// </summary>
    public class PropertyDescriptor
    {
        public string Name { get; set; }
        public Type PropertyType { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsReadOnly { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
    }

    /// <summary>
    /// Interface for property validators
    /// </summary>
    public interface IPropertyValidator
    {
        (bool isValid, string error) Validate(object value);
    }

    /// <summary>
    /// Range validator for numeric properties
    /// </summary>
    public class RangeValidator : IPropertyValidator
    {
        private readonly double _min;
        private readonly double _max;

        public RangeValidator(double min, double max)
        {
            _min = min;
            _max = max;
        }

        public (bool isValid, string error) Validate(object value)
        {
            if (value == null)
                return (false, "Value cannot be null");

            double numValue;
            if (value is int i)
                numValue = i;
            else if (value is double d)
                numValue = d;
            else if (value is float f)
                numValue = f;
            else
                return (false, "Value must be numeric");

            if (numValue < _min || numValue > _max)
                return (false, $"Value must be between {_min} and {_max}");

            return (true, null);
        }
    }

    /// <summary>
    /// Required validator for non-null values
    /// </summary>
    public class RequiredValidator : IPropertyValidator
    {
        public (bool isValid, string error) Validate(object value)
        {
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                return (false, "Value is required");

            return (true, null);
        }
    }
}
