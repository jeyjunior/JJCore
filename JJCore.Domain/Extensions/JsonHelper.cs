using System.Text.Json;

namespace JJCore.Domain.Extensions
{
    /// <summary>
    /// Provides simple JSON file read and write helpers.
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Reads a JSON file and deserializes it to the requested type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        /// <param name="filePath">The JSON file path.</param>
        /// <returns>The deserialized object, or null when the JSON content is null.</returns>
        public static T? ReadFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file was not found: {filePath}");
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }

        /// <summary>
        /// Serializes an object as indented JSON and saves it to a file.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="instance">The object to serialize.</param>
        /// <param name="filePath">The destination file path.</param>
        public static void SaveFile<T>(T instance, string filePath)
        {
            string json = JsonSerializer.Serialize(instance, WriteOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
