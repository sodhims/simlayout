using System.Text.Json;
using System.Text.Json.Serialization;

namespace LayoutEditor.Helpers
{
    /// <summary>
    /// JSON serialization helper
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, WriteOptions);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }

        public static T? Clone<T>(T obj)
        {
            var json = Serialize(obj);
            return Deserialize<T>(json);
        }
    }
}
