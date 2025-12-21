using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LayoutEditor.Data.Serialization
{
    /// <summary>
    /// Helper class for JSON serialization/deserialization of layout elements
    /// </summary>
    public static class JsonSerializationHelper
    {
        private static readonly JsonSerializerOptions _options;

        static JsonSerializationHelper()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = false,  // Compact JSON for database storage
                PropertyNamingPolicy = null,  // Preserve property names
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter()  // Serialize enums as strings
                }
            };
        }

        /// <summary>
        /// Serializes an object to JSON string
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), DbErrorMessages.SerializationFailed);
            }

            try
            {
                return JsonSerializer.Serialize(obj, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{DbErrorMessages.SerializationFailed}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserializes a JSON string to an object
        /// </summary>
        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{DbErrorMessages.DeserializationFailed}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tries to deserialize a JSON string, returning false if it fails
        /// </summary>
        public static bool TryDeserialize<T>(string json, out T? result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                result = JsonSerializer.Deserialize<T>(json, _options);
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a JSON string is well-formed
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a pretty-printed version of JSON (for debugging/export)
        /// </summary>
        public static string PrettyPrint(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json;  // Return original if parsing fails
            }
        }

        /// <summary>
        /// Clones an object via JSON serialization
        /// </summary>
        public static T? Clone<T>(T obj)
        {
            if (obj == null)
            {
                return default;
            }

            var json = Serialize(obj);
            return Deserialize<T>(json);
        }
    }
}
