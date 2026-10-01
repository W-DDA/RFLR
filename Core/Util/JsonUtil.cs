using System.IO;
using System.Text.Json;

namespace MyLauncher.Core.Util
{
    public static class JsonUtil
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static T Read<T>(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options)!;
        }

        public static T Read<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json, Options)!;
        }

        public static string Write(object obj) => JsonSerializer.Serialize(obj, Options);
    }
}