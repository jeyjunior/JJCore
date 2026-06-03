using System.Text.Json;

namespace JJCore.Domain.Extensions
{
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

        public static T? ReadFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file was not found: {filePath}");
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }

        public static void SaveFile<T>(T instance, string filePath)
        {
            string json = JsonSerializer.Serialize(instance, WriteOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
